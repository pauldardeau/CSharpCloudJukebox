namespace CSharpCloudJukebox;

public class Jukebox
{
   private static Jukebox? _gJukeboxInstance;

   public bool ExitRequested;

   private readonly JukeboxOptions _jukeboxOptions;
   private readonly StorageSystem _storageSystem;
   private readonly bool _debugPrint;
   private JukeboxDb? _jukeboxDb;
   private readonly string _currentDir;
   private readonly string _songImportDir;
   private readonly string _playlistImportDir;
   private readonly string _songPlayDir;
   private readonly string _albumArtImportDir;
   private readonly string _downloadExtension;
   private readonly string _metadataDbFile;
   private readonly string _metadataContainer;
   private readonly string _playlistContainer;
   private readonly string _albumArtContainer;
   private List<SongMetadata> _songList;
   private int _numberSongs;
   private int _songIndex;
   private string _audioPlayerExeFileName;
   private string _audioPlayerCommandArgs;
   private System.Diagnostics.Process? _audioPlayerProcess;
   private readonly int _songPlayLengthSeconds;
   private long _cumulativeDownloadBytes;
   private double _cumulativeDownloadTime;
   private bool _isPaused;
   private double _songStartTime;
   private int _songSecondsOffset;

   public Jukebox(JukeboxOptions jbOptions,
                  StorageSystem storageSys,
                  bool debugPrint = false) {

      Jukebox._gJukeboxInstance = this;

      _jukeboxOptions = jbOptions;
      _storageSystem = storageSys;
      _debugPrint = debugPrint;
      _jukeboxDb = null;
      _currentDir = Directory.GetCurrentDirectory();
      _songImportDir = Path.Join(_currentDir, "song-import");
      _playlistImportDir = Path.Join(_currentDir, "playlist-import");
      _songPlayDir = Path.Join(_currentDir, "song-play");
      _albumArtImportDir = Path.Join(_currentDir, "album-art-import");
      _downloadExtension = ".download";
      _metadataDbFile = "jukebox_db.sqlite3";
      _metadataContainer = "music-metadata";
      _playlistContainer = "playlists";
      _albumArtContainer = "album-art";
      _songList = new List<SongMetadata>();
      _numberSongs = 0;
      _songIndex = -1;
      _audioPlayerExeFileName = "";
      _audioPlayerCommandArgs = "";
      _audioPlayerProcess = null;
      _songPlayLengthSeconds = 20;
      _cumulativeDownloadBytes = 0;
      _cumulativeDownloadTime = 0.0;
      ExitRequested = false;
      _isPaused = false;
      _songStartTime = 0.0;
      _songSecondsOffset = 0;

      if (jbOptions.DebugMode) {
         debugPrint = true;
      }

      if (debugPrint) {
         Console.WriteLine("current_dir = {0}", _currentDir);
         Console.WriteLine("song_import_dir = {0}", _songImportDir);
         Console.WriteLine("song_play_dir = {0}", _songPlayDir);
      }
   }

   public void Enter() {
      if (_debugPrint) {
         Console.WriteLine("Jukebox.enter");
      }

      // look for stored metadata in the storage system
      if (_storageSystem.HasContainer(_metadataContainer) &&
          !_jukeboxOptions.SuppressMetadataDownload) {

         // metadata container exists, retrieve container listing
         List<string> containerContents = _storageSystem.ListContainerContents(_metadataContainer);

         // does our metadata DB file exist in the metadata container?
         if (containerContents.Contains(_metadataDbFile)) {
            //Console.WriteLine("metadata DB file exists in container, attempting to download");

            // download it
            string metadataDbFilePath = GetMetadataDbFilePath();
            string downloadFile = metadataDbFilePath + ".download";

            Console.WriteLine("downloading metadata DB to {0}", downloadFile);
            if (_storageSystem.GetObject(_metadataContainer, _metadataDbFile, downloadFile) > 0) {
               // have an existing metadata DB file?
               if (Utils.PathExists(metadataDbFilePath)) {
                  if (_debugPrint) {
                     Console.WriteLine("deleting existing metadata DB file");
                  }
                  File.Delete(metadataDbFilePath);
               }
               // rename downloaded file
               if (_debugPrint) {
                  Console.WriteLine("renaming {0} to {1}", downloadFile, metadataDbFilePath);
               } 
               Utils.RenameFile(downloadFile, metadataDbFilePath);
            } else {
               if (_debugPrint) {
                  Console.WriteLine("error: unable to retrieve metadata DB file");
               }
            }
         } else {
            if (_debugPrint) {
               Console.WriteLine("no metadata DB file in metadata container");
            }
         }
      } else {
         if (_debugPrint) {
            Console.WriteLine("no metadata container in storage system");
         }
      }

      _jukeboxDb = new JukeboxDb(GetMetadataDbFilePath());
      if (!_jukeboxDb.Open()) {
         Console.WriteLine("unable to connect to database");
      }
   }

   public void Exit() {
      if (_debugPrint) {
         Console.WriteLine("Jukebox.exit");
      }

      if (_jukeboxDb != null) {
         if (_jukeboxDb.IsOpen()) {
            _jukeboxDb.Close();
         }
         _jukeboxDb = null;
      }
   }

   public void TogglePausePlay() {
      _isPaused = !_isPaused;
      if (_isPaused) {
         Console.WriteLine("paused");
         if (_audioPlayerProcess != null) {
            // capture current song position (seconds into song)
            _audioPlayerProcess.Kill();
         }
      } else {
         Console.WriteLine("resuming play");
      }
   }

   public void AdvanceToNextSong() {
      Console.WriteLine("advancing to next song");
      if (_audioPlayerProcess != null) {
         _audioPlayerProcess.Kill();
      }
   }

   private string GetMetadataDbFilePath() {
      return Path.Join(_currentDir, _metadataDbFile);
   }

   private static string UnencodeValue(string encodedValue) {
      return encodedValue.Replace('-', ' ');
   }

   public static string EncodeValue(string value) {
      return value.Replace(' ', '-');
   }

   private string[] ComponentsFromFileName(string fileName) {
      if (fileName.Length == 0) {
         return new string[] {}; 
      }
      int posExtension = fileName.IndexOf('.');
      string baseFileName;
      if (posExtension > -1) {
         baseFileName = fileName.Substring(0, posExtension);
      } else {
         baseFileName = fileName;
      }
      string[] components = baseFileName.Split("--");
      if (components.Length == 3) {
         return new[] {UnencodeValue(components[0]),
                       UnencodeValue(components[1]),
                       UnencodeValue(components[2])};
      } else {
         return new[] {"", "", ""};
      }
   }

   private string ArtistFromFileName(string fileName) {
      if (fileName.Length > 0) {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3) {
            return components[0];
         }
      }
      return "";
   }

   private string AlbumFromFileName(string fileName) {
      if (fileName.Length > 0) {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3) {
            return components[1];
         }
      }
      return "";
   }

   private string SongFromFileName(string fileName) {
      if (fileName.Length > 0) {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3) {
            return components[2];
         }
      }
      return "";
   }

   private bool StoreSongMetadata(SongMetadata fsSong) {
      if (_jukeboxDb != null)
      {
         SongMetadata? dbSong = _jukeboxDb.RetrieveSong(fsSong.Fm.FileUid);
         if (dbSong != null) {
            if (!fsSong.Equals(dbSong)) {
               return _jukeboxDb.UpdateSong(fsSong);
            } else {
               return true;  // no insert or update needed (already up-to-date)
            }
         } else {
            // song is not in the database, insert it
            return _jukeboxDb.InsertSong(fsSong);
         }
      }
      else
      {
         return false;
      }
   }

   public bool StoreSongPlaylist(string fileName, string fileContents) {
      //TODO: (2) json deserialization (store_song_playlist)
      //pl = json.loads(fileContents);
      //if ("name" in pl.keys()) {
      //    string pl_name = pl["name"];
      //    string pl_uid = file_name;
      //    return jukebox_db.insert_playlist(pl_uid, pl_name);
      //} else {
      //   return false;
      //}
      return false;
   }

   public void GetEncryptor() {
      //TODO: (3) encryption (get_encryptor)
      // key_block_size = 16  // AES-128
      // key_block_size = 24  // AES-192
      //int key_block_size = 32;  // AES-256
      //return AESBlockEncryption(key_block_size,
      //                          jukebox_options.encryption_key,
      //                          jukebox_options.encryption_iv);
   }

   private string GetContainerSuffix() {
      string suffix = "";
      if (_jukeboxOptions.UseEncryption && _jukeboxOptions.UseCompression) {
         suffix += "-ez";
      } else if (_jukeboxOptions.UseEncryption) {
         suffix += "-e";
      } else if (_jukeboxOptions.UseCompression) {
         suffix += "-z";
      }
      return suffix;
   }

   private string ObjectFileSuffix() {
      string suffix = "";
      if (_jukeboxOptions.UseEncryption && _jukeboxOptions.UseCompression) {
         suffix = ".egz";
      } else if (_jukeboxOptions.UseEncryption) {
         suffix = ".e";
      } else if (_jukeboxOptions.UseCompression) {
         suffix = ".gz";
      }
      return suffix;
   }

   private string ContainerForSong(string songUid) {
      if (songUid.Length == 0) {
         return "";
      }
      string containerSuffix = "-artist-songs" + GetContainerSuffix();

      string artist = ArtistFromFileName(songUid);
      string artistLetter;
      if (artist.StartsWith("A ")) {
         artistLetter = artist.Substring(2, 1);
      } else if (artist.StartsWith("The ")) {
         artistLetter = artist.Substring(4, 1);
      } else {
         artistLetter = artist.Substring(0, 1);
      }

      return artistLetter.ToLower() + containerSuffix;
   }

   public void ImportSongs() {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen()) {
         string[] dirListing = Directory.GetFiles(_songImportDir);
         float numEntries = dirListing.Length;
         double progressbarChars = 0.0;
         int progressbarWidth = 40;
         int progressCharsPerIteration = (int) (progressbarWidth / numEntries);
         char progressbarChar = '#';
         int barChars = 0;

         if (!_debugPrint) {
            // setup progressbar
            string bar = new string('*', progressbarWidth);
            string barText = "[" + bar + "]";
            Utils.SysStdoutWrite(barText);
            Utils.SysStdoutFlush();
            bar = new string('\b', progressbarWidth + 1);
            Utils.SysStdoutWrite(bar);  // return to start of line, after '['
         }

         //TODO: (3) encryption support (import_songs)
         //if (jukebox_options != null && jukebox_options.use_encryption) {
         //   encryption = get_encryptor();
         //} else {
         //   encryption = null;
         //}

         double cumulativeUploadTime = 0.0;
         int cumulativeUploadBytes = 0;
         int fileImportCount = 0;

         foreach (var listingEntry in dirListing) {
            string fullPath = Path.Join(_songImportDir, listingEntry);
            // ignore it if it's not a file
            if (Utils.PathIsFile(fullPath)) {
               string fileName = listingEntry;
               (string, string) pathTuple = Utils.PathSplitExt(fullPath);
               string extension = pathTuple.Item1;
               if (extension.Length > 0) {
                  long fileSize = Utils.GetFileSize(fullPath);
                  string artist = ArtistFromFileName(fileName);
                  string album = AlbumFromFileName(fileName);
                  string song = SongFromFileName(fileName);
                  if (fileSize > 0 && artist.Length > 0 && album.Length > 0 && song.Length > 0) {
                     string objectName = fileName + ObjectFileSuffix();
                     FileMetadata fm = new FileMetadata(objectName, ContainerForSong(fileName), objectName);
                     SongMetadata fsSong = new SongMetadata(fm, artist, song);
                     fsSong.AlbumUid = "";
                     fsSong.Fm.OriginFileSize = (int) fileSize;
                     fsSong.Fm.FileTime = Utils.DatetimeDatetimeFromtimestamp(Utils.PathGetMtime(fullPath));
                     fsSong.Fm.Md5Hash = Utils.Md5ForFile(fullPath);
                     fsSong.Fm.Compressed = _jukeboxOptions.UseCompression ? 1 : 0;
                     fsSong.Fm.Encrypted = _jukeboxOptions.UseEncryption ? 1 : 0;
                     fsSong.Fm.PadCharCount = 0;

                     // read file contents
                     bool fileRead = false;
                     byte[]? fileContents = null;

                     try
                     {
                        fileContents = File.ReadAllBytes(fullPath);
                        fileRead = true;
                     }
                     catch (Exception)
                     {
                        Console.WriteLine("error: unable to read file {0}", fullPath);
                     }

                     if (fileRead && fileContents != null) {
                        // for general purposes, it might be useful or helpful to have
                        // a minimum size for compressing
                        if (_jukeboxOptions.UseCompression) {
                           if (_debugPrint) {
                              Console.WriteLine("compressing file");
                           }

                           //TODO: (3) compression (import_songs)
                           //file_bytes = bytes(file_contents, 'utf-8');
                           //file_contents = zlib.compress(file_bytes, 9);
                        }

                        if (_jukeboxOptions.UseEncryption) {
                           if (_debugPrint) {
                              Console.WriteLine("encrypting file");
                           }

                           //TODO: (3) encryption (import_songs)

                           // the length of the data to encrypt must be a multiple of 16
                           //num_extra_chars = file_contents.Length % 16;
                           //if (num_extra_chars > 0) {
                           //   if (debug_print) {
                           //      Console.WriteLine("padding file for encryption");
                           //   }
                           //   num_pad_chars = 16 - num_extra_chars;
                           //   file_contents += "".ljust(num_pad_chars, ' ');
                           //   fs_song.fm.pad_char_count = num_pad_chars;
                           //}

                           //file_contents = encryption.encrypt(file_contents);
                        }

                        // now that we have the data that will be stored, set the file size for
                        // what's being stored
                        fsSong.Fm.StoredFileSize = fileContents.Length;
                        double startUploadTime = Utils.TimeTime();

                        // store song file to storage system
                        if (_storageSystem.PutObject(fsSong.Fm.ContainerName,
                                                    fsSong.Fm.ObjectName,
                                                    fileContents,
                                                    null)) {
                           double endUploadTime = Utils.TimeTime();
                           double uploadElapsedTime = endUploadTime - startUploadTime;
                           cumulativeUploadTime += uploadElapsedTime;
                           cumulativeUploadBytes += fileContents.Length;

                           // store song metadata in local database
                           if (!StoreSongMetadata(fsSong)) {
                              // we stored the song to the storage system, but were unable to store
                              // the metadata in the local database. we need to delete the song
                              // from the storage system since we won't have any way to access it
                              // since we can't store the song metadata locally.
                              Console.WriteLine("unable to store metadata, deleting obj {0}", fsSong.Fm.ObjectName);
                                              
                              _storageSystem.DeleteObject(fsSong.Fm.ContainerName,
                                                         fsSong.Fm.ObjectName);
                           } else {
                              fileImportCount += 1;
                           }
                        } else {
                           Console.WriteLine("error: unable to upload {0} to {1}", fsSong.Fm.ObjectName, fsSong.Fm.ContainerName);
                        }
                     }
                  }
               }

               if (!_debugPrint) {
                  progressbarChars += progressCharsPerIteration;
                  if (progressbarChars > barChars) {
                     int numNewChars = (int) (progressbarChars - barChars);
                     if (numNewChars > 0) {
                        // update progress bar
                        for (int j = 0; j < numNewChars; j++) {
                           Console.Write(progressbarChar);
                        }
                        Utils.SysStdoutFlush();
                        barChars += numNewChars;
                     }
                  }
               }
            }
         }

         if (!_debugPrint) {
            // if we haven't filled up the progress bar, fill it now
            if (barChars < progressbarWidth) {
               int numNewChars = progressbarWidth - barChars;
               for (int j = 0; j < numNewChars; j++) {
                  Console.Write(progressbarChar);
               }
               Utils.SysStdoutFlush();
            }
            Console.WriteLine("");
         }

         if (fileImportCount > 0) {
            UploadMetadataDb();
         }

         Console.WriteLine("{0} song files imported", fileImportCount);

         if (cumulativeUploadTime > 0) {
            double cumulativeUploadKb = cumulativeUploadBytes / 1000.0;
            int avg = (int) (cumulativeUploadKb / cumulativeUploadTime);
            Console.WriteLine("average upload throughput = {0} KB/sec", avg);
         }
      }
   }

   private string SongPathInPlaylist(SongMetadata song) {
      return Path.Join(_songPlayDir, song.Fm.FileUid);
   }

   private bool CheckFileIntegrity(SongMetadata song) {
      bool fileIntegrityPassed = true;

      if (_jukeboxOptions.CheckDataIntegrity) {
         string filePath = SongPathInPlaylist(song);
         if (File.Exists(filePath)) {
            if (_debugPrint) {
               if (song.Fm.FileUid.Length > 0) {
                  Console.WriteLine("checking integrity for {0}", song.Fm.FileUid);
               }
            }

            string playlistMd5 = Utils.Md5ForFile(filePath);
            if (playlistMd5 == song.Fm.Md5Hash) {
               if (_debugPrint) {
                  Console.WriteLine("integrity check SUCCESS");
               }
               fileIntegrityPassed = true;
            } else {
               Console.WriteLine("file integrity check failed: {0}", song.Fm.FileUid);
               fileIntegrityPassed = false;
            }
         } else {
            // file doesn't exist
            Console.WriteLine("file doesn't exist");
            fileIntegrityPassed = false;
         }
      } else {
         if (_debugPrint) {
            Console.WriteLine("file integrity bypassed, no jukebox options or check integrity not turned on");
         }
      }

      return fileIntegrityPassed;
   }

   public void BatchDownloadStart() {
      _cumulativeDownloadBytes = 0;
      _cumulativeDownloadTime = 0.0;
   }

   public void BatchDownloadComplete() {
      if (!ExitRequested) {
         if (_cumulativeDownloadTime > 0) {
            double cumulativeDownloadKb = _cumulativeDownloadBytes / 1000.0;
            int avg = (int) (cumulativeDownloadKb / _cumulativeDownloadTime);
            Console.WriteLine("average download throughput = {0} KB/sec", avg);
         }
         _cumulativeDownloadBytes = 0;
         _cumulativeDownloadTime = 0.0;
      }
   }

   public bool DownloadSong(SongMetadata song) {
      if (ExitRequested) {
         return false;
      }

      string filePath = SongPathInPlaylist(song);
      double downloadStartTime = Utils.TimeTime();
      long songBytesRetrieved = _storageSystem.RetrieveFile(song.Fm, _songPlayDir);
      if (ExitRequested) {
         return false;
      }

      if (_debugPrint) {
         Console.WriteLine("bytes retrieved: {0}", songBytesRetrieved);
      }

      if (songBytesRetrieved > 0) {
         double downloadEndTime = Utils.TimeTime();
         double downloadElapsedTime = downloadEndTime - downloadStartTime;
         _cumulativeDownloadTime += downloadElapsedTime;
         _cumulativeDownloadBytes += songBytesRetrieved;

         // are we checking data integrity?
         // if so, verify that the storage system retrieved the same length that has been stored
         if (_jukeboxOptions.CheckDataIntegrity) {
            if (_debugPrint) {
               Console.WriteLine("verifying data integrity");
            }

            if (songBytesRetrieved != song.Fm.StoredFileSize) {
               Console.WriteLine("error: data integrity check failed for {0}", filePath);
               return false;
            }
         }

         // is it encrypted? if so, unencrypt it
         //int encrypted = song.Fm.Encrypted;
         //int compressed = song.Fm.Compressed;

         //TODO: (3) encryption and compression (download_song)
         //if (encrypted == 1 || compressed == 1) {
         //     try:
         //         with open(file_path, 'rb') as content_file:
         //             file_contents = content_file.read()
         //     except IOError:
         //         print("error: unable to read file %s" % file_path)
         //         return false

         //     if (encrypted) {
         //        encryption = get_encryptor()
         //        file_contents = encryption.decrypt(file_contents)
         //     }
         //     if (compressed) {
         //        file_contents = zlib.decompress(file_contents)
         //     }

         // re-write out the uncompressed, unencrypted file contents
         //     try:
         //         with open(file_path, 'wb') as content_file:
         //             content_file.write(file_contents)
         //     except IOError:
         //         print("error: unable to write unencrypted/uncompressed file '%s'" % file_path)
         //         return false
         //}

         if (CheckFileIntegrity(song)) {
            return true;
         } else {
            // we retrieved the file, but it failed our integrity check
            // if file exists, remove it
            if (File.Exists(filePath)) {
               File.Delete(filePath);
            }
         }
      }

      return false;
   }

   private void PlaySong(string songFilePath) {
      if (Utils.PathExists(songFilePath)) {
         if (_audioPlayerExeFileName.Length > 0) {
            string cmdArgs = _audioPlayerCommandArgs + songFilePath;
            int exitCode = -1;
            bool startedAudioPlayer = false;
            try
            {
               Console.WriteLine("playing {0}", songFilePath);
               // See ProcessStartInfo
               System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
               psi.FileName = _audioPlayerExeFileName;
               psi.Arguments = cmdArgs;
               psi.UseShellExecute = false;
               psi.RedirectStandardError = false;
               psi.RedirectStandardOutput = false;

               _audioPlayerProcess = new System.Diagnostics.Process();
               _audioPlayerProcess.StartInfo = psi;
               _audioPlayerProcess.Start();

               if (_audioPlayerProcess != null) {
                  startedAudioPlayer = true;
                  _songStartTime = Utils.TimeTime();
                  _audioPlayerProcess.WaitForExit();
                  if (_audioPlayerProcess.HasExited) {
                     exitCode = _audioPlayerProcess.ExitCode;
                  }
                  _audioPlayerProcess = null;
               }
            }
            catch (Exception e)
            {
               Console.WriteLine("Exception caught:");
               Console.WriteLine(e.ToString());
               // audio player not available
               _audioPlayerExeFileName = "";
               _audioPlayerCommandArgs = "";
               _audioPlayerProcess = null;
               exitCode = -1;
            }

            // if the audio player failed or is not present, just sleep
            // for the length of time that audio would be played
            if (!startedAudioPlayer && exitCode != 0) {
               Utils.TimeSleep(_songPlayLengthSeconds);
            }
         } else {
            // we don't know about an audio player, so simulate a
            // song being played by sleeping
            Utils.TimeSleep(_songPlayLengthSeconds);
         }

         if (!_isPaused) {
            // delete the song file from the play list directory
            File.Delete(songFilePath);
         }
      } else {
         Console.WriteLine("song file doesn't exist: {0}", songFilePath);
         try
         {
            File.AppendAllText("404.txt", songFilePath);
         }
         catch (Exception e)
         {
            Console.WriteLine("Unable to write to 404.txt: " + e);
         }
      }
   }

   private void DownloadSongs() {
      // scan the play list directory to see if we need to download more songs
      string[] dirListing = Directory.GetFiles(_songPlayDir);
      int songFileCount = 0;
      foreach (var listingEntry in dirListing) {
         string fullPath = Path.Join(_songPlayDir, listingEntry);
         if (Utils.PathIsFile(fullPath)) {
            (string, string) pathTuple = Utils.PathSplitExt(fullPath);
            string extension = pathTuple.Item1;
            if (extension.Length > 0 && extension != _downloadExtension) {
               songFileCount += 1;
            }
         }
      }

      int fileCacheCount = _jukeboxOptions.FileCacheCount;

      if (songFileCount < fileCacheCount) {
         List<SongMetadata> dlSongs = new List<SongMetadata>();
         // start looking at the next song in the list
         int checkIndex = _songIndex + 1;

         //Console.WriteLine("DEBUG: number_songs = {0}", NumberSongs);

         try
         {
            for (int j = 0; j < _numberSongs; j++)
            {
               //Console.WriteLine("DEBUG: j = {0}", j);
               //Console.WriteLine("DEBUG: j = {0}", j);

               if (checkIndex >= _numberSongs)
               {
                  checkIndex = 0;
               }

               if (checkIndex != _songIndex)
               {
                  SongMetadata si = _songList[checkIndex];
                  string filePath = SongPathInPlaylist(si);
                  if (!File.Exists(filePath))
                  {
                     //Console.WriteLine("adding song to dlSongs");
                     dlSongs.Add(si);
                     if (dlSongs.Count >= fileCacheCount)
                     {
                        //Console.WriteLine("DEBUG: dlSongs.Count >= fileCacheCount, breaking");
                        break;
                     }
                  }
               }

               checkIndex++;
            }

            if (dlSongs.Count > 0)
            {
               //Console.WriteLine("creating SongDownloader");
               SongDownloader downloader = new SongDownloader(this, dlSongs);
               Thread downloadThread = new Thread(downloader.Run);
               //Console.WriteLine("starting thread to download songs");
               downloadThread.Start();
            }
         }
         catch (Exception e)
         {
            Console.WriteLine(e.ToString());
         }
      }
   }

   public void PlaySongs(bool shuffle=false, string artist="", string album="") {
      if (_jukeboxDb != null)
      {
         _songList = _jukeboxDb.RetrieveSongs(artist, album);
         _numberSongs = _songList.Count;

         if (_numberSongs == 0)
         {
            Console.WriteLine("no songs in jukebox");
            return;
         }

         // does play list directory exist?
         if (!Directory.Exists(_songPlayDir))
         {
            if (_debugPrint)
            {
               Console.WriteLine("song-play directory does not exist, creating it");
            }

            Directory.CreateDirectory(_songPlayDir);
         }
         else
         {
            // play list directory exists, delete any files in it
            if (_debugPrint)
            {
               Console.WriteLine("deleting existing files in song-play directory");
            }

            foreach (var theFile in Directory.GetFiles(_songPlayDir))
            {
               try
               {
                  File.Delete(theFile);
               }
               catch (Exception)
               {
               }
            }
         }

         _songIndex = 0;
         //TODO: (2) set up signal handlers (play_songs)
         //install_signal_handlers();

         OperatingSystem os = Environment.OSVersion;
         PlatformID pid = os.Platform;

         if (pid == PlatformID.Unix)
         {
            if (File.Exists("/usr/bin/afplay"))    // macOS?
            {
               _audioPlayerExeFileName = "/usr/bin/afplay";
               _audioPlayerCommandArgs = "";
            }
            else
            {
               _audioPlayerExeFileName = "mplayer";
               _audioPlayerCommandArgs = "-novideo -nolirc -really-quiet ";
            }
         }
         else if (pid == PlatformID.Win32NT)
         {
            // we really need command-line support for /play and /close arguments. unfortunately,
            // this support used to be available in the built-in Windows Media Player, but is
            // no longer present.
            // audio_player_command_args = "C:\Program Files\Windows Media Player\wmplayer.exe ";
            _audioPlayerExeFileName = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
            _audioPlayerCommandArgs = "/play /close /minimized ";
         }
         else
         {
            _audioPlayerExeFileName = "";
            _audioPlayerCommandArgs = "";
         }

         Console.WriteLine("downloading first song...");

         if (shuffle)
         {
            //TODO: (1) implement shuffling list (play_songs)
            //song_list = random.sample(song_list, song_list.Count);
         }

         try
         {
            if (DownloadSong(_songList[0]))
            {
               Console.WriteLine("first song downloaded. starting playing now.");

               // write PID to "jukebox.pid"
               int pidValue = Utils.GetPid();
               File.WriteAllText("jukebox.pid", pidValue.ToString());

               while (!ExitRequested)
               {
                  if (!_isPaused)
                  {
                     DownloadSongs();
                     PlaySong(SongPathInPlaylist(_songList[_songIndex]));
                  }

                  if (!_isPaused)
                  {
                     _songIndex++;
                     if (_songIndex >= _numberSongs)
                     {
                        _songIndex = 0;
                     }
                  }
                  else
                  {
                     Utils.TimeSleep(1);
                  }
               }
            }
            else
            {
               Console.WriteLine("error: unable to download songs");
               Utils.SysExit(1);
            }
         }
         finally
         {
            Console.WriteLine("\nexiting jukebox");
            if (File.Exists("jukebox.pid"))
            {
               File.Delete("jukebox.pid");
            }
            ExitRequested = true;
         }
      }
   }

   public void ShowListContainers() {
      foreach (var containerName in _storageSystem.ListContainers) {
         Console.WriteLine(containerName);
      }
   }

   public void ShowListings() {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowListings();
      }
   }

   public void ShowArtists() {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowArtists();
      }
   }

   public void ShowGenres() {
      if (_jukeboxDb != null)
      { 
         _jukeboxDb.ShowGenres();
      }
   }

   public void ShowAlbums() {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowAlbums();
      }
   }

   public (bool, byte[]?, int) ReadFileContents(string filePath,
                                                bool allowEncryption = true) {
      bool fileRead = false;
      byte[]? fileContents = null;
      int padChars = 0;

      try
      {
          fileContents = File.ReadAllBytes(filePath);
          fileRead = true;
      }
      catch (Exception)
      {
         Console.WriteLine("error: unable to read file {0}", filePath);
      }

      if (fileRead && fileContents != null) {
         if (fileContents.Length > 0) {
            // for general purposes, it might be useful or helpful to have
            // a minimum size for compressing
            if (_jukeboxOptions.UseCompression) {
               //TODO: (3) compression (read_file_contents)
               /*
               if (debug_print) {
                  Console.WriteLine("compressing file");
               }
               file_bytes = bytes(file_contents, 'utf-8');
               file_contents = zlib.compress(file_bytes, 9);
               */
            }

            if (allowEncryption && _jukeboxOptions.UseEncryption) {
               //TODO: (3) encryption (read_file_contents)
               /*
               if (debug_print) {
                  Console.WriteLine("encrypting file");
               }
               // the length of the data to encrypt must be a multiple of 16
               int num_extra_chars = file_contents.Length % 16;
               if (num_extra_chars > 0) {
                   if (debug_print) {
                      Console.WriteLine("padding file for encryption");
                   }
                   pad_chars = 16 - num_extra_chars;
                   file_contents += "".ljust(pad_chars, ' ');
               }
               file_contents = encryption.encrypt(file_contents);
               */
            }
         }
      }

      return (fileRead, fileContents, padChars);
   }

   public bool UploadMetadataDb() {
      bool metadataDbUpload = false;
      bool haveMetadataContainer;

      if (!_storageSystem.HasContainer(_metadataContainer)) {
         haveMetadataContainer = _storageSystem.CreateContainer(_metadataContainer);
      } else {
         haveMetadataContainer = true;
      }

      if (haveMetadataContainer) {
         if (_debugPrint) {
            Console.WriteLine("uploading metadata db file to storage system");
         }

         if (_jukeboxDb != null)
         {
            _jukeboxDb.Close();
            _jukeboxDb = null;
         }

         // upload metadata DB file
         byte[] dbFileContents = File.ReadAllBytes(GetMetadataDbFilePath());

         metadataDbUpload = _storageSystem.PutObject(_metadataContainer,
                                                    _metadataDbFile,
                                                    dbFileContents,
                                                    null);

         if (_debugPrint) {
             if (metadataDbUpload) {
                Console.WriteLine("metadata db file uploaded");
             } else {
                Console.WriteLine("unable to upload metadata db file");
             }
         }
      }

      return metadataDbUpload;
   }

   public void ImportPlaylists() {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen()) {
         int fileImportCount = 0;
         string[] dirListing = Directory.GetFiles(_playlistImportDir);
         if (dirListing.Length == 0) {
            Console.WriteLine("no playlists found");
            return;
         }

         bool haveContainer;

         if (!_storageSystem.HasContainer(_playlistContainer)) {
            haveContainer = _storageSystem.CreateContainer(_playlistContainer);
         } else {
            haveContainer = true;
         }

         if (!haveContainer) {
            Console.WriteLine("error: unable to create container for playlists. unable to import");
            return;
         }

         foreach (var listingEntry in dirListing) {
            string fullPath = Path.Join(_playlistImportDir, listingEntry);
            // ignore it if it's not a file
            if (File.Exists(fullPath)) {
               //string objectName = listingEntry;
               //TODO: (2) read playlist (import_playlists)
               /*
               file_read, file_contents, _ = read_file_contents(full_path)
               if (file_read && file_contents != null) {
                  if (storage_system.put_object(playlist_container,
                                                object_name,
                                                file_contents,
                                                null)) {
                     Console.WriteLine("put of playlist succeeded");
                     if (!store_song_playlist(object_name, file_contents)) {
                        Console.WriteLine("storing of playlist to db failed");
                        storage_system.delete_object(playlist_container, object_name);
                     } else {
                        Console.WriteLine("storing of playlist succeeded");
                        file_import_count += 1;
                     }
                  }
               }
               */
            }
         }

         if (fileImportCount > 0) {
            Console.WriteLine("{0} playlists imported", fileImportCount);
            // upload metadata DB file
            UploadMetadataDb();
         } else {
            Console.WriteLine("no files imported");
         }
      }
   }

   public void ShowPlaylists() {
      if (_jukeboxDb != null) {
         _jukeboxDb.ShowPlaylists();
      }
   }

   public void ShowPlaylist(string playlist) {
      Console.WriteLine("TODO: (2) implement (show_playlist)");
   }

   public void PlayPlaylist(string playlist) {
      Console.WriteLine("TODO: (2) implement (play_playlist)");
   }

   public bool DeleteSong(string songUid, bool uploadMetadata=true) {
      bool isDeleted = false;
      if (songUid.Length > 0 && _jukeboxDb != null) {
         bool dbDeleted = _jukeboxDb.DeleteSong(songUid);
         string container = ContainerForSong(songUid);
         bool ssDeleted = false;
         if (container.Length > 0) {
            ssDeleted = _storageSystem.DeleteObject(container, songUid);
         }
         if (dbDeleted && uploadMetadata) {
            UploadMetadataDb();
         }
         isDeleted = dbDeleted || ssDeleted;
      }

      return isDeleted;
   }

   public bool DeleteArtist(string artist) {
      bool isDeleted = false;
      if (artist.Length > 0 && _jukeboxDb != null) {
         List<SongMetadata> songList = _jukeboxDb.RetrieveSongs(artist);
         if (songList.Count == 0) {
            Console.WriteLine("no songs in jukebox");
            return false;
         } else {
            foreach (var song in songList) {
               if (!DeleteSong(song.Fm.ObjectName, false)) {
                  Console.WriteLine("error deleting song {0}", song.Fm.ObjectName);
                  return false;
               }
            }
            UploadMetadataDb();
            isDeleted = true;
         }
      }

      return isDeleted;
   }

   public bool DeleteAlbum(string album) {
      // ReSharper disable once StringIndexOfIsCultureSpecific.1
      int posDoubleDash = album.IndexOf("--");
      if (posDoubleDash > -1 && _jukeboxDb != null) {
         string artist = album.Substring(0, posDoubleDash);
         string albumName = album.Substring(posDoubleDash+2);
         List<SongMetadata> listAlbumSongs = _jukeboxDb.RetrieveSongs(artist, albumName);
         if (listAlbumSongs.Count > 0) {
            int numSongsDeleted = 0;
            foreach (var song in listAlbumSongs) {
               Console.WriteLine("{0} {1}", song.Fm.ContainerName, song.Fm.ObjectName);
               // delete each song audio file
               if (_storageSystem.DeleteObject(song.Fm.ContainerName, song.Fm.ObjectName)) {
                  numSongsDeleted += 1;
                  // delete song metadata
                  _jukeboxDb.DeleteSong(song.Fm.ObjectName);
               } else {
                  Console.WriteLine("error: unable to delete song {0}", song.Fm.ObjectName);
                  //TODO: (3) delete song metadata if we got 404 (delete_album)
               }
            }
            if (numSongsDeleted > 0) {
               // upload metadata db
               UploadMetadataDb();
               return true;
            }
         } else {
            Console.WriteLine("no songs found for artist={0} album name={1}", artist, albumName);
         }
      } else {
         Console.WriteLine("specify album with 'the-artist--the-song-name' format");
      }
      return false;
   }

   public bool DeletePlaylist(string playlistName) {
      bool isDeleted = false;
      if (_jukeboxDb != null)
      {
         string objectName = _jukeboxDb.GetPlaylist(playlistName);
         if (objectName.Length > 0)
         {
            bool dbDeleted = _jukeboxDb.DeletePlaylist(playlistName);
            if (dbDeleted)
            {
               Console.WriteLine("container={0}, object={1}", _playlistContainer, objectName);
               if (_storageSystem.DeleteObject(_playlistContainer, objectName))
               {
                  isDeleted = true;
               }
               else
               {
                  Console.WriteLine("error: object delete failed");
               }
            }
            else
            {
               Console.WriteLine("error: database delete failed");
            }

            if (isDeleted)
            {
               UploadMetadataDb();
            }
            else
            {
               Console.WriteLine("delete of playlist failed");
            }
         }
         else
         {
            Console.WriteLine("invalid playlist name");
         }
      }

      return isDeleted;
   }

   public void ImportAlbumArt() {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen()) {
         int fileImportCount = 0;
         string[] dirListing = Directory.GetFiles(_albumArtImportDir);
         if (dirListing.Length == 0) {
            Console.WriteLine("no album art found");
            return;
         }

         bool haveContainer;

         if (!_storageSystem.HasContainer(_albumArtContainer)) {
            haveContainer = _storageSystem.CreateContainer(_albumArtContainer);
         } else {
            haveContainer = true;
         }

         if (!haveContainer) {
            Console.WriteLine("error: unable to create container for album art. unable to import");
            return;
         }

         foreach (var listingEntry in dirListing) {
            string fullPath = Path.Join(_albumArtImportDir, listingEntry);
            // ignore it if it's not a file
            if (Utils.PathIsFile(fullPath)) {
               //string objectName = listingEntry;
               //TODO: (3) album art import (import_album_art)
               /*
               file_read, file_contents, _ = read_file_contents(full_path);
               if (file_read && file_contents != null) {
                  if (storage_system.put_object(album_art_container,
                                                object_name,
                                                file_contents)) {
                     file_import_count += 1;
                  }
               }
               */
            }
         }

         if (fileImportCount > 0) {
            Console.WriteLine("{0} album art files imported", fileImportCount);
         } else {
            Console.WriteLine("no files imported");
         }
      }
   }
}