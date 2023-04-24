using System.Diagnostics;
using System.Text.Json;

namespace CSharpCloudJukebox;

public class Jukebox
{
   public const string defaultDbFileName = "jukebox_db.sqlite3";

   private static Jukebox? _gJukeboxInstance;


   // containers
   private const string albumContainer = "albums";
	private const string albumArtContainer = "album-art";
	private const string metadataContainer = "music-metadata";
	private const string playlistContainer = "playlists";
	private const string songContainerSuffix = "-artist-songs";

   // directories
	private const string albumArtImportDir = "album-art-import";
	private const string playlistImportDir = "playlist-import";
	private const string songImportDir = "song-import";
	private const string songPlayDir = "song-play";

   // files
   private const string downloadExtension = ".download";
   private const string jukeboxPidFileName = "jukebox.pid";
   private const string JsonFileExt = ".json";
   private const string IniFileName = "audio_player.ini";

   // audio file INI contents
   private const string keyAudioPlayerExeFileName = "audio_player_exe_file_name";
   private const string keyAudioPlayerCommandArgs = "audio_player_command_args";
   private const string keyAudioPlayerResumeArgs = "audio_player_resume_args";

   // placeholders
   private const string phAudioFilePath = "%%AUDIO_FILE_PATH%%";
   private const string phStartSongTimeOffset = "%%START_SONG_TIME_OFFSET%%";


   public volatile bool ExitRequested;

   private readonly JukeboxOptions _jukeboxOptions;
   private readonly StorageSystem _storageSystem;
   private readonly bool _debugPrint;
   private JukeboxDb? _jukeboxDb;
   private readonly string _containerPrefix;
   private readonly string _currentDir;
   private readonly string _songImportDir;
   private readonly string _playlistImportDir;
   private readonly string _songPlayDir;
   private readonly string _albumArtImportDir;
   private readonly string _downloadExtension;
   private readonly string _metadataDbFile;
   private readonly string _metadataContainer;
   private readonly string _playlistContainer;
   private readonly string _albumContainer;
   private readonly string _albumArtContainer;
   private List<SongMetadata> _songList;
   private int _numberSongs;
   private int _songIndex;
   private string _audioPlayerExeFileName;
   private string _audioPlayerCommandArgs;
   private string _audioPlayerResumeArgs;
   private Process? _audioPlayerProcess;
   private readonly int _songPlayLengthSeconds;
   private long _cumulativeDownloadBytes;
   private double _cumulativeDownloadTime;
   private bool _isPaused;
   private double _songStartTime;
   private int _songSecondsOffset;
   private bool _songPlayIsResume;
   private bool _isRepeatMode;
   
   public string? CurrentArtist { get; set; }
   public string? CurrentAlbum { get; set; }
   public string? CurrentSong { get; set; }
   public string? CurrentObject { get; set; }
   public string? ScopeArtist { get; set; }
   public string? ScopeAlbum { get; set; }
   public string? ScopePlaylist { get; set; }
   
   public string? AlbumArtUrl { get; set; }


   private static async void SignalHandler(object? sender,
                                           ConsoleCancelEventArgs args)
   {
      args.Cancel = true;
      Console.WriteLine(" Ctrl-C detected, shutting down");

      if (_gJukeboxInstance != null)
      {
         _gJukeboxInstance.PrepareForTermination();
         
         // wake up the http server thread
         using var client = new HttpClient();
         var result = await client.GetAsync("http://127.0.0.1:5309/api/memoryUsage/");
         Thread.Sleep(2000);
      }
   }

   public static bool InitializeStorageSystem(StorageSystem storageSys,
                                              string containerPrefix)
   {
      // create the containers that will hold songs
      string artistSongChars = "0123456789abcdefghijklmnopqrstuvwxyz";

      foreach (char ch in artistSongChars)
      {
         string containerName =
            containerPrefix + string.Format("{0}{1}", ch, songContainerSuffix);

         if (!storageSys.CreateContainer(containerName))
         {
            Console.WriteLine("error: unable to create container '{0}'",
               containerName);
            return false;
         }
      }

      // create the other (non-song) containers
      string[] containerNames = {metadataContainer,
         albumArtContainer, albumContainer, playlistContainer};

      foreach (string containerName in containerNames)
      {
         string cnrName = containerPrefix + containerName;
         if (!storageSys.CreateContainer(cnrName))
         {
            Console.WriteLine("error: unable to create container '{0}'",
               cnrName);
            return false;
         }
      }

      // delete metadata DB file if present
      if (Utils.FileExists(defaultDbFileName))
      {
         Utils.DeleteFile(defaultDbFileName);
      }

      return true;
   }

   public Jukebox(JukeboxOptions jbOptions,
                  StorageSystem storageSys,
                  string containerPrefix,
                  bool debugPrint = false)
   {
      Jukebox._gJukeboxInstance = this;

      _jukeboxOptions = jbOptions;
      _storageSystem = storageSys;
      _debugPrint = debugPrint;
      _jukeboxDb = null;
      _containerPrefix = containerPrefix;
      _currentDir = Directory.GetCurrentDirectory();
      _songImportDir = Path.Join(_currentDir, songImportDir);
      _playlistImportDir = Path.Join(_currentDir, playlistImportDir);
      _songPlayDir = Path.Join(_currentDir, songPlayDir);
      _albumArtImportDir = Path.Join(_currentDir, albumArtImportDir);
      _downloadExtension = downloadExtension;
      _metadataDbFile = defaultDbFileName;
      _metadataContainer = containerPrefix + metadataContainer;
      _playlistContainer = containerPrefix + playlistContainer;
      _albumContainer = containerPrefix + albumContainer;
      _albumArtContainer = containerPrefix + albumArtContainer;
      _songList = new List<SongMetadata>();
      _numberSongs = 0;
      _songIndex = -1;
      _audioPlayerExeFileName = "";
      _audioPlayerCommandArgs = "";
      _audioPlayerResumeArgs = "";
      _audioPlayerProcess = null;
      _songPlayLengthSeconds = 20;
      _cumulativeDownloadBytes = 0;
      _cumulativeDownloadTime = 0.0;
      ExitRequested = false;
      _isPaused = false;
      _songStartTime = 0.0;
      _songSecondsOffset = 0;
      _songPlayIsResume = false;
      _isRepeatMode = false;

      ScopePlaylist = "";
      AlbumArtUrl = "";

      if (jbOptions.DebugMode)
      {
         debugPrint = true;
      }

      if (debugPrint)
      {
         Console.WriteLine("current dir = {0}", _currentDir);
         Console.WriteLine("song import dir = {0}", _songImportDir);
         Console.WriteLine("song play dir = {0}", _songPlayDir);
      }
   }

   public bool Enter()
   {
      if (_debugPrint)
      {
         Console.WriteLine("Jukebox.enter");
      }

      // look for stored metadata in the storage system
      if (_storageSystem.HasContainer(_metadataContainer) &&
          !_jukeboxOptions.SuppressMetadataDownload)
      {
         // metadata container exists, retrieve container listing
         List<string> containerContents =
            _storageSystem.ListContainerContents(_metadataContainer);

         // does our metadata DB file exist in the metadata container?
         if (containerContents.Contains(_metadataDbFile))
         {
            // download it
            string metadataDbFilePath = GetMetadataDbFilePath();
            string downloadFile = metadataDbFilePath + downloadExtension;

            if (_debugPrint)
            {
               Console.WriteLine("downloading metadata DB to {0}",
                  Path.GetFileName(downloadFile));
            }

            if (_storageSystem.GetObject(_metadataContainer,
                                         _metadataDbFile,
                                         downloadFile) > 0)
            {
               // have an existing metadata DB file?
               if (Utils.PathExists(metadataDbFilePath))
               {
                  if (_debugPrint)
                  {
                     Console.WriteLine("deleting existing metadata DB file");
                  }
                  File.Delete(metadataDbFilePath);
               }
               
               // rename downloaded file
               if (_debugPrint)
               {
                  Console.WriteLine("renaming {0} to {1}",
                     downloadFile, metadataDbFilePath);
               } 
               Utils.RenameFile(downloadFile, metadataDbFilePath);
            }
            else
            {
               if (_debugPrint)
               {
                  Console.WriteLine("error: unable to retrieve metadata DB file");
               }

               return false;
            }
         }
         else
         {
            if (_debugPrint)
            {
               Console.WriteLine("error: no metadata DB file in metadata container");
            }

            return false;
         }
      }
      else
      {
         if (_debugPrint)
         {
            Console.WriteLine("error: no metadata container in storage system");
         }

         return false;
      }

      _jukeboxDb = new JukeboxDb(GetMetadataDbFilePath());
      if (!_jukeboxDb.Open())
      {
         Console.WriteLine("error: unable to connect to database");
         return false;
      }

      return true;
   }

   public void Exit()
   {
      if (_debugPrint)
      {
         Console.WriteLine("Jukebox.exit");
      }

      if (_jukeboxDb != null)
      {
         if (_jukeboxDb.IsOpen())
         {
            _jukeboxDb.Close();
         }
         _jukeboxDb = null;
      }
   }

   protected void KillAudioPlayerProcess()
   {
      if (_audioPlayerProcess != null)
      {
         _audioPlayerProcess.Kill();
         _audioPlayerProcess = null;
      }
   }

   public void TogglePausePlay()
   {
      _isPaused = !_isPaused;
      if (_isPaused)
      {
         Console.WriteLine("paused");
         if (_audioPlayerProcess != null)
         {
            // capture current song position (seconds into song)
            KillAudioPlayerProcess();
         }
      }
      else
      {
         Console.WriteLine("resuming play");
         _songPlayIsResume = true;
      }
   }

   public void AdvanceToNextSong()
   {
      Console.WriteLine("advancing to next song");
      KillAudioPlayerProcess();
      _songPlayIsResume = false;
   }

   public void PrepareForTermination()
   {
      // indicate that it's time to shut down
      ExitRequested = true;

      // terminate audio player if it's running
      KillAudioPlayerProcess();
   }

   private string GetMetadataDbFilePath()
   {
      return Path.Join(_currentDir, _metadataDbFile);
   }
   
   private bool StoreSongMetadata(SongMetadata fsSong)
   {
      if (_jukeboxDb != null)
      {
         SongMetadata? dbSong = _jukeboxDb.RetrieveSong(fsSong.Fm.FileUid);
         if (dbSong != null)
         {
            if (!fsSong.Equals(dbSong))
            {
               return _jukeboxDb.UpdateSong(fsSong);
            }
            else
            {
               return true;  // no insert or update needed (already up-to-date)
            }
         }
         else
         {
            // song is not in the database, insert it
            return _jukeboxDb.InsertSong(fsSong);
         }
      }
      else
      {
         return false;
      }
   }

   private bool StoreSongPlaylist(string fileName, string fileContents)
   {
      bool success = false;

      if (_jukeboxDb != null)
      {
         Playlist? pl = JsonSerializer.Deserialize<Playlist>(fileContents);
         if (pl != null)
         {
            if (pl.Name.Length > 0 && fileName.Length > 0)
            {
               success = _jukeboxDb.InsertPlaylist(fileName, pl.Name);
            }
         }
      }
      return success;
   }

   private string ContainerForSong(string songUid)
   {
      if (songUid.Length == 0)
      {
         return "";
      }

      string artist = JbUtils.ArtistFromFileName(songUid);
      string artistLetter;

      if (artist.StartsWith("A "))
      {
         artistLetter = artist.Substring(2, 1);
      }
      else if (artist.StartsWith("The "))
      {
         artistLetter = artist.Substring(4, 1);
      }
      else
      {
         artistLetter = artist.Substring(0, 1);
      }

      return _containerPrefix + artistLetter.ToLower() + songContainerSuffix;
   }

   public void ImportSongs()
   {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen())
      {
         string[] dirListing = Directory.GetFiles(_songImportDir);
         float numEntries = dirListing.Length;
         double progressbarChars = 0.0;
         int progressbarWidth = 40;
         int progressCharsPerIteration = (int) (progressbarWidth / numEntries);
         char progressbarChar = '#';
         int barChars = 0;

         if (!_debugPrint)
         {
            // setup progressbar
            string bar = new string('*', progressbarWidth);
            string barText = "[" + bar + "]";
            Utils.SysStdoutWrite(barText);
            Utils.SysStdoutFlush();
            bar = new string('\b', progressbarWidth + 1);
            Utils.SysStdoutWrite(bar);  // return to start of line, after '['
         }

         double cumulativeUploadTime = 0.0;
         int cumulativeUploadBytes = 0;
         int fileImportCount = 0;

         foreach (var listingEntry in dirListing)
         {
            string fullPath = Path.Join(_songImportDir, listingEntry);
            // ignore it if it's not a file
            if (Utils.PathIsFile(fullPath))
            {
               string fileName = listingEntry;
               (string, string) pathTuple = Utils.PathSplitExt(fullPath);
               string extension = pathTuple.Item1;

               if (extension.Length > 0)
               {
                  long fileSize = Utils.GetFileSize(fullPath);
                  string artist = JbUtils.ArtistFromFileName(fileName);
                  string album = JbUtils.AlbumFromFileName(fileName);
                  string song = JbUtils.SongFromFileName(fileName);
                  if (fileSize > 0 && artist.Length > 0 && album.Length > 0 &&
                      song.Length > 0)
                  {
                     string objectName = fileName;
                     FileMetadata fm = new FileMetadata(objectName,
                                                        ContainerForSong(fileName),
                                                        objectName);
                     SongMetadata fsSong = new SongMetadata(fm, artist, song);
                     fsSong.AlbumUid = "";
                     fsSong.Fm.OriginFileSize = (int) fileSize;
                     fsSong.Fm.FileTime = Utils.DatetimeFromtimestamp(Utils.PathGetMtime(fullPath));
                     fsSong.Fm.Md5Hash = Utils.Md5ForFile(fullPath);
                     fsSong.Fm.Encrypted = 0;
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
                        Console.WriteLine("error: unable to read file {0}",
                                          fullPath);
                     }

                     if (fileRead && fileContents != null)
                     {
                        fsSong.Fm.StoredFileSize = fileContents.Length;
                        double startUploadTime = Utils.TimeTime();
                        string container =
                           _containerPrefix + fsSong.Fm.ContainerName;

                        // store song file to storage system
                        if (_storageSystem.PutObject(container,
                                                     fsSong.Fm.ObjectName,
                                                     fileContents,
                                                     null))
                        {
                           double endUploadTime = Utils.TimeTime();
                           double uploadElapsedTime =
                              endUploadTime - startUploadTime;
                           cumulativeUploadTime += uploadElapsedTime;
                           cumulativeUploadBytes += fileContents.Length;

                           // store song metadata in local database
                           if (!StoreSongMetadata(fsSong))
                           {
                              // we stored the song to the storage system, but
                              // were unable to store the metadata in the local
                              // database. we need to delete the song from the
                              // storage system since we won't have any way to
                              // access it since we can't store the song
                              // metadata locally.
                              Console.WriteLine("unable to store metadata, deleting obj {0}",
                                 fsSong.Fm.ObjectName);
                                              
                              _storageSystem.DeleteObject(container,
                                                          fsSong.Fm.ObjectName);
                           }
                           else
                           {
                              fileImportCount++;
                           }
                        }
                        else
                        {
                           Console.WriteLine("error: unable to upload {0} to {1}",
                              fsSong.Fm.ObjectName, container);
                        }
                     }
                  }
               }

               if (!_debugPrint)
               {
                  progressbarChars += progressCharsPerIteration;
                  if (progressbarChars > barChars)
                  {
                     int numNewChars = (int) (progressbarChars - barChars);
                     if (numNewChars > 0)
                     {
                        // update progress bar
                        for (int j = 0; j < numNewChars; j++)
                        {
                           Console.Write(progressbarChar);
                        }
                        Utils.SysStdoutFlush();
                        barChars += numNewChars;
                     }
                  }
               }
            }
         }

         if (!_debugPrint)
         {
            // if we haven't filled up the progress bar, fill it now
            if (barChars < progressbarWidth)
            {
               int numNewChars = progressbarWidth - barChars;
               for (int j = 0; j < numNewChars; j++)
               {
                  Console.Write(progressbarChar);
               }
               Utils.SysStdoutFlush();
            }
            Console.WriteLine("");
         }

         if (fileImportCount > 0)
         {
            UploadMetadataDb();
         }

         Console.WriteLine("{0} song files imported", fileImportCount);

         if (cumulativeUploadTime > 0)
         {
            double cumulativeUploadKb = cumulativeUploadBytes / 1000.0;
            int avg = (int) (cumulativeUploadKb / cumulativeUploadTime);
            Console.WriteLine("average upload throughput = {0} KB/sec", avg);
         }
      }
   }

   private string SongPathInPlaylist(SongMetadata song)
   {
      return Path.Join(_songPlayDir, song.Fm.FileUid);
   }

   private bool CheckFileIntegrity(SongMetadata song)
   {
      bool fileIntegrityPassed = true;

      if (_jukeboxOptions.CheckDataIntegrity)
      {
         string filePath = SongPathInPlaylist(song);
         if (File.Exists(filePath))
         {
            if (_debugPrint)
            {
               if (song.Fm.FileUid.Length > 0)
               {
                  Console.WriteLine("checking integrity for {0}",
                     song.Fm.FileUid);
               }
            }

            string playlistMd5 = Utils.Md5ForFile(filePath);
            if (playlistMd5 == song.Fm.Md5Hash)
            {
               if (_debugPrint)
               {
                  Console.WriteLine("integrity check SUCCESS");
               }
               fileIntegrityPassed = true;
            }
            else
            {
               Console.WriteLine("file integrity check failed: {0}",
                  song.Fm.FileUid);
               fileIntegrityPassed = false;
            }
         }
         else
         {
            // file doesn't exist
            Console.WriteLine("file doesn't exist");
            fileIntegrityPassed = false;
         }
      }
      else
      {
         if (_debugPrint)
         {
            Console.WriteLine("file integrity bypassed");
         }
      }

      return fileIntegrityPassed;
   }

   public void BatchDownloadStart()
   {
      _cumulativeDownloadBytes = 0;
      _cumulativeDownloadTime = 0.0;
   }

   public void BatchDownloadComplete()
   {
      if (!ExitRequested)
      {
         if (_cumulativeDownloadTime > 0)
         {
            double cumulativeDownloadKb = _cumulativeDownloadBytes / 1000.0;
            int avg = (int) (cumulativeDownloadKb / _cumulativeDownloadTime);
            Console.WriteLine("average download throughput = {0} KB/sec", avg);
         }
         _cumulativeDownloadBytes = 0;
         _cumulativeDownloadTime = 0.0;
      }
   }

   public long RetrieveFile(FileMetadata fm, string dirPath)
   {
      long bytesRetrieved = 0;
      if (dirPath.Length > 0)
      {
         string localFilePath = Path.Join(dirPath, fm.FileUid);
         string container = _containerPrefix + fm.ContainerName;
         bytesRetrieved = _storageSystem.GetObject(container,
                                                   fm.ObjectName,
                                                   localFilePath);
      }

      return bytesRetrieved;
   }
   
   public bool DownloadSong(SongMetadata song)
   {
      if (ExitRequested)
      {
         return false;
      }

      string filePath = SongPathInPlaylist(song);
      double downloadStartTime = Utils.TimeTime();
      long songBytesRetrieved = RetrieveFile(song.Fm, _songPlayDir);
      if (ExitRequested)
      {
         return false;
      }

      if (_debugPrint)
      {
         Console.WriteLine("bytes retrieved: {0}", songBytesRetrieved);
      }

      if (songBytesRetrieved > 0)
      {
         double downloadEndTime = Utils.TimeTime();
         double downloadElapsedTime = downloadEndTime - downloadStartTime;
         _cumulativeDownloadTime += downloadElapsedTime;
         _cumulativeDownloadBytes += songBytesRetrieved;

         // are we checking data integrity?
         // if so, verify that the storage system retrieved the same length
         // that has been stored
         if (_jukeboxOptions.CheckDataIntegrity)
         {
            if (_debugPrint)
            {
               Console.WriteLine("verifying data integrity");
            }

            if (songBytesRetrieved != song.Fm.StoredFileSize)
            {
               Console.WriteLine("error: data integrity check failed for {0}",
                  filePath);
               return false;
            }
         }

         if (CheckFileIntegrity(song))
         {
            return true;
         }
         else
         {
            // we retrieved the file, but it failed our integrity check
            // if file exists, remove it
            if (File.Exists(filePath))
            {
               File.Delete(filePath);
            }
         }
      }

      return false;
   }

   private void PlaySong(string songFilePath)
   {
      string fileName = Path.GetFileName(songFilePath);
      if (Utils.PathExists(songFilePath))
      {
         if (_audioPlayerExeFileName.Length > 0)
         {
            bool didResume = false;
            string commandArgs = "";
            if (_songPlayIsResume)
            {
               string placeholder = phStartSongTimeOffset;
               int posPlaceholder =
                  _audioPlayerResumeArgs.IndexOf(placeholder);
               if (posPlaceholder > -1)
               {
                  commandArgs = _audioPlayerResumeArgs;
                  string songStartTime;
                  int minutes = _songSecondsOffset / 60;
                  if (minutes > 0) {
                     songStartTime = minutes.ToString();
                     songStartTime += ":";
                     int remainingSeconds = _songSecondsOffset % 60;
                     string secondsText = remainingSeconds.ToString();
                     if (secondsText.Length == 1) {
                        secondsText = "0" + secondsText;
                     }
                     songStartTime += secondsText;
                  } else
                  {
                     songStartTime = _songSecondsOffset.ToString();
                  }
                  //Console.WriteLine("resuming at '{0}'", songStartTime);
                  commandArgs = commandArgs.Replace(
                     phStartSongTimeOffset,
                     songStartTime);
                  commandArgs = commandArgs.Replace(
                     phAudioFilePath,
                     songFilePath);
                  didResume = true;
                  //Console.WriteLine("commandArgs: '{0}'", commandArgs);
               }               
            }
            
            if (!didResume) {
               commandArgs = _audioPlayerCommandArgs;
               commandArgs = commandArgs.Replace(phAudioFilePath, songFilePath);
            }
            
            int exitCode = -1;
            bool startedAudioPlayer = false;
            
            try
            {
               Console.WriteLine("playing {0}", fileName);
               // See ProcessStartInfo
               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = _audioPlayerExeFileName;
               psi.Arguments = commandArgs;
               psi.UseShellExecute = false;
               psi.RedirectStandardError = false;
               psi.RedirectStandardOutput = false;

               _audioPlayerProcess = new Process();
               _audioPlayerProcess.StartInfo = psi;
               _audioPlayerProcess.Start();

               if (_audioPlayerProcess != null)
               {
                  startedAudioPlayer = true;
                  _songStartTime = Utils.TimeTime();
                  _audioPlayerProcess.WaitForExit();
                  if (_audioPlayerProcess.HasExited)
                  {
                     exitCode = _audioPlayerProcess.ExitCode;
                  }
                  _audioPlayerProcess.Dispose();
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
               if (_audioPlayerProcess != null)
               {
                  _audioPlayerProcess.Dispose();
                  _audioPlayerProcess = null;
               }
               exitCode = -1;
            }

            // if the audio player failed or is not present, just sleep
            // for the length of time that audio would be played
            if (!startedAudioPlayer && exitCode != 0)
            {
               Utils.TimeSleep(_songPlayLengthSeconds);
            }
         }
         else
         {
            // we don't know about an audio player, so simulate a
            // song being played by sleeping
            Utils.TimeSleep(_songPlayLengthSeconds);
         }

         if (!_isPaused)
         {
            // delete the song file from the play list directory
            //File.Delete(songFilePath);
            SongDeleter deleter = new SongDeleter(songFilePath);
            Thread deleterThread = new Thread(deleter.Run);
            deleterThread.Start();
         }
      }
      else
      {
         Console.WriteLine("song file doesn't exist: {0}", fileName);
         try
         {
            File.AppendAllText("404.txt", fileName + Environment.NewLine);
         }
         catch (Exception e)
         {
            Console.WriteLine("Unable to write to 404.txt: " + e);
         }
      }
   }

   private void DownloadSongs()
   {
      // scan the play list directory to see if we need to download more songs
      string[] dirListing = Directory.GetFiles(_songPlayDir);
      int songFileCount = 0;
      foreach (var listingEntry in dirListing)
      {
         (string, string) pathTuple = Utils.PathSplitExt(listingEntry);
         string extension = pathTuple.Item1;
         if (extension.Length > 0 && extension != _downloadExtension)
         {
            songFileCount++;
         }
      }

      int fileCacheCount = _jukeboxOptions.FileCacheCount;

      if (songFileCount < fileCacheCount)
      {
         List<SongMetadata> dlSongs = new List<SongMetadata>();
         // start looking at the next song in the list
         int checkIndex = _songIndex + 1;

         try
         {
            for (int j = 0; j < _numberSongs; j++)
            {
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
                     dlSongs.Add(si);
                     if (dlSongs.Count >= fileCacheCount)
                     {
                        break;
                     }
                  }
               }

               checkIndex++;
            }

            if (dlSongs.Count > 0)
            {
               SongDownloader downloader = new SongDownloader(this, dlSongs);
               Thread downloadThread = new Thread(downloader.Run);
               downloadThread.Start();
            }
         }
         catch (Exception e)
         {
            Console.WriteLine(e.ToString());
         }
      }
   }

   private void InstallSignalHandlers()
   {
      Console.CancelKeyPress += new ConsoleCancelEventHandler(SignalHandler);
   }

   public void PlaySongs(bool shuffle = false,
                         string artist = "",
                         string album = "")
   {
      if (_jukeboxDb != null)
      {
         bool haveSongs = false;
         if (artist.Length > 0 && album.Length > 0)
         {
            ScopeArtist = artist;
            ScopeAlbum = album;

            List<SongMetadata> aSongList = new List<SongMetadata>();
            List<string> listTrackObjects = new List<string>();
            if (RetrieveAlbumTrackObjectList(artist, album, listTrackObjects))
            {
               if (listTrackObjects.Count > 0)
               {
                  foreach (var trackObjectName in listTrackObjects)
                  {
                     SongMetadata? song =
                        _jukeboxDb.RetrieveSong(trackObjectName);
                     if (song != null)
                     {
                        aSongList.Add(song);
                     }
                  }

                  if (aSongList.Count == listTrackObjects.Count)
                  {
                     haveSongs = true;
                     _songList = aSongList;
                  }
               }
            }
         }

         if (!haveSongs)
         {
            _songList = _jukeboxDb.RetrieveAlbumSongs(artist, album);
         }

         PlayRetrievedSongs(shuffle);
      }
   }
   
   private void PlayRetrievedSongs(bool shuffle)
   {
      if (_songList.Count > 0)
      {
         // does play list directory exist?
         if (!Directory.Exists(_songPlayDir))
         {
            if (_debugPrint)
            {
               Console.WriteLine("{0} directory does not exist, creating it",
                  songPlayDir);
            }

            Directory.CreateDirectory(_songPlayDir);
         }
         else
         {
            // play list directory exists, delete any files in it
            if (_debugPrint)
            {
               Console.WriteLine("deleting existing files in {0} directory",
                  songPlayDir);
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
         _numberSongs = _songList.Count;
         
         InstallSignalHandlers();
         
         string osIdentifier = Utils.GetPlatformIdentifier();
         if (osIdentifier == "unknown")
         {
            Console.WriteLine("error: no audio-player specific lookup defined for this OS (unknown)");
            return;
         }

         _audioPlayerExeFileName = "";
         _audioPlayerCommandArgs = "";
         _audioPlayerResumeArgs = "";

         IniReader iniReader = new IniReader(IniFileName);
         if (!iniReader.Read())
         {
            Console.WriteLine("error: unable to read ini config file '{0}'",
               IniFileName);
            return;
         }

         KeyValuePairs kvpAudioPlayer = new KeyValuePairs();
         if (!iniReader.ReadSection(osIdentifier, kvpAudioPlayer))
         {
            Console.WriteLine("error: no config section present for '{0}'",
               osIdentifier);
            return;
         }

         string key = keyAudioPlayerExeFileName;
         if (kvpAudioPlayer.HasKey(key))
         {
            _audioPlayerExeFileName = kvpAudioPlayer.GetValue(key);

            if (_audioPlayerExeFileName.StartsWith("\"") &&
                _audioPlayerExeFileName.EndsWith("\""))
            {
               _audioPlayerExeFileName = _audioPlayerExeFileName.Trim('"');
            }

            _audioPlayerExeFileName = _audioPlayerExeFileName.Trim();
            if (_audioPlayerExeFileName.Length == 0) {
               Console.WriteLine("error: no value given for '{0}' within [{1}]",
                  key, osIdentifier);
               return;
            }
         }
         else
         {
            Console.WriteLine("error: {0} missing value for '{1}' within [{2}]",
               IniFileName, key, osIdentifier);
            return;
         }

         key = keyAudioPlayerCommandArgs;
         if (kvpAudioPlayer.HasKey(key))
         {
            _audioPlayerCommandArgs = kvpAudioPlayer.GetValue(key);

            if (_audioPlayerCommandArgs.StartsWith("\"") &&
                _audioPlayerCommandArgs.EndsWith("\""))
            {
               _audioPlayerCommandArgs = _audioPlayerCommandArgs.Trim('"');
            }

            _audioPlayerCommandArgs = _audioPlayerCommandArgs.Trim();
            if (_audioPlayerCommandArgs.Length == 0)
            {
               Console.WriteLine("error: no value given for '{0}' within [{1}]",
                  key, osIdentifier);
               return;
            }

            string placeholder = phAudioFilePath;
            int posPlaceholder = _audioPlayerCommandArgs.IndexOf(placeholder);
            if (posPlaceholder == -1)
            {
               Console.WriteLine("error: {0} value does not contain placeholder '{1}'",
                  key, placeholder);
               return;
            }
         }
         else
         {
            Console.WriteLine("error: {0} missing value for '{1}' within [{2}]",
               IniFileName, key, osIdentifier);
            return;
         }

         key = keyAudioPlayerResumeArgs;
         if (kvpAudioPlayer.HasKey(key))
         {
            _audioPlayerResumeArgs = kvpAudioPlayer.GetValue(key);

            if (_audioPlayerResumeArgs.StartsWith("\"") &&
                _audioPlayerResumeArgs.EndsWith("\""))
            {
               _audioPlayerResumeArgs = _audioPlayerResumeArgs.Trim('"');
            }

            _audioPlayerResumeArgs = _audioPlayerResumeArgs.Trim();
            if (_audioPlayerResumeArgs.Length > 0)
            {
               string placeholder = phStartSongTimeOffset;
               int posPlaceholder = _audioPlayerResumeArgs.IndexOf(placeholder);
               if (posPlaceholder == -1) {
                  Console.WriteLine("error: {0} value does not contain placeholder '{1}'",
                     key, placeholder);
                  Console.WriteLine("ignoring '{0}', using '{1}' for song resume",
                     key, keyAudioPlayerCommandArgs);
                  _audioPlayerResumeArgs = "";
               }
            }
         }

         if (_audioPlayerResumeArgs.Length == 0) {
            _audioPlayerResumeArgs = _audioPlayerCommandArgs;
         }

         Console.WriteLine("downloading first song...");

         if (shuffle)
         {
            // shuffle the song list
            Random rng = new Random();
            int n = _songList.Count;
            while (n > 1)
            {
               n--;
               int k = rng.Next(n + 1);
               var value = _songList[k];
               _songList[k] = _songList[n];
               _songList[n] = value;
            }
         }

         try
         {
            if (DownloadSong(_songList[0]))
            {
               Console.WriteLine("first song downloaded. starting playing now.");

               // write PID to jukebox pid file
               int pidValue = Utils.GetPid();
               File.WriteAllText(jukeboxPidFileName, pidValue.ToString());

               HttpServer httpServer = new HttpServer(this);
               Thread httpServerThread = new Thread(httpServer.Run);
               httpServerThread.Start();

               while (!ExitRequested)
               {
                  if (!_isPaused)
                  {
                     DownloadSongs();
                     SongMetadata sm = _songList[_songIndex];
                     string objectName = sm.Fm.ObjectName;
                     string[] objComponents = objectName.Split("--");
                     if (objComponents.Length == 3)
                     {
                        CurrentAlbum = JbUtils.DecodeValue(objComponents[1]);
                     }
                     else
                     {
                        CurrentAlbum = "";
                     }
                     CurrentArtist = sm.ArtistName;
                     CurrentSong = sm.SongName;
                     CurrentObject = sm.Fm.ObjectName;
                     PlaySong(SongPathInPlaylist(sm));
                  }

                  if (!_isPaused)
                  {
                     _songIndex++;
                     if (_songIndex >= _numberSongs)
                     {
                        if (_isRepeatMode)
                        {
                           _songIndex = 0;
                        }
                        else
                        {
                           ExitRequested = true;
                        }
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
            Console.WriteLine(Environment.NewLine + "exiting jukebox");
            if (File.Exists(jukeboxPidFileName))
            {
               File.Delete(jukeboxPidFileName);
            }
            ExitRequested = true;
         }
      }
   }

   public void ShowListContainers()
   {
      foreach (var containerName in _storageSystem.ListContainers)
      {
         Console.WriteLine(containerName);
      }
   }

   public void ShowListings()
   {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowListings();
      }
   }

   public void ShowArtists()
   {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowArtists();
      }
   }

   public void ShowGenres()
   {
      if (_jukeboxDb != null)
      { 
         _jukeboxDb.ShowGenres();
      }
   }

   public void ShowAlbums()
   {
      if (_jukeboxDb != null)
      {
         _jukeboxDb.ShowAlbums();
      }
   }

   private (bool, byte[]?) ReadFileContents(string filePath)
   {
      bool fileRead = false;
      byte[]? fileContents = null;

      try
      {
          fileContents = File.ReadAllBytes(filePath);
          fileRead = true;
      }
      catch (Exception)
      {
         Console.WriteLine("error: unable to read file {0}", filePath);
      }

      return (fileRead, fileContents);
   }

   public bool UploadMetadataDb()
   {
      bool metadataDbUpload = false;
      bool haveMetadataContainer;

      if (!_storageSystem.HasContainer(_metadataContainer))
      {
         haveMetadataContainer =
            _storageSystem.CreateContainer(_metadataContainer);
      }
      else
      {
         haveMetadataContainer = true;
      }

      if (haveMetadataContainer)
      {
         if (_debugPrint)
         {
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

         if (_debugPrint)
         {
             if (metadataDbUpload)
             {
                Console.WriteLine("metadata db file uploaded");
             }
             else
             {
                Console.WriteLine("unable to upload metadata db file");
             }
         }
      }

      return metadataDbUpload;
   }

   public void ImportPlaylists()
   {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen())
      {
         int fileImportCount = 0;
         string[] dirListing = Directory.GetFiles(_playlistImportDir);
         if (dirListing.Length == 0)
         {
            Console.WriteLine("no playlists found");
            return;
         }

         bool haveContainer;

         if (!_storageSystem.HasContainer(_playlistContainer))
         {
            haveContainer = _storageSystem.CreateContainer(_playlistContainer);
         }
         else
         {
            haveContainer = true;
         }

         if (!haveContainer)
         {
            Console.WriteLine("error: unable to create container for playlists");
            return;
         }

         foreach (var listingEntry in dirListing)
         {
            bool fileRead;
            byte[]? fileContents;
            string objectName = listingEntry;
            
            (fileRead, fileContents) = ReadFileContents(listingEntry);
            
            if (fileRead && fileContents != null)
            {
               if (_storageSystem.PutObject(_playlistContainer,
                                            objectName,
                                            fileContents,
                                            null))
               {
                  Console.WriteLine("put of playlist succeeded");
                  string textFileContents =
                     System.Text.Encoding.UTF8.GetString(fileContents,
                                                         0,
                                                         fileContents.Length);

                  if (!StoreSongPlaylist(objectName, textFileContents))
                  {
                     Console.WriteLine("storing of playlist to db failed");
                     _storageSystem.DeleteObject(_playlistContainer, objectName);
                  }
                  else
                  {
                     Console.WriteLine("storing of playlist succeeded");
                     fileImportCount++;
                  }
               }
            }
         }

         if (fileImportCount > 0)
         {
            Console.WriteLine("{0} playlists imported", fileImportCount);
            // upload metadata DB file
            UploadMetadataDb();
         }
         else
         {
            Console.WriteLine("no files imported");
         }
      }
   }

   public void ShowPlaylists()
   {
      List<string> containerContents =
         _storageSystem.ListContainerContents(_playlistContainer);
      foreach (var playlistName in containerContents)
      {
         Console.WriteLine("{0}", playlistName);
      }
   }

   public void ShowPlaylist(string playlistName)
   {
      List<SongMetadata> listSongs = new List<SongMetadata>();
      if (GetPlaylistSongs(playlistName, listSongs))
      {
         foreach (var song in listSongs)
         {
            Console.WriteLine("{0} : {1}", song.SongName, song.ArtistName);
         }
      }
   }

   public void PlayPlaylist(string playlistName)
   {
      ScopePlaylist = playlistName;
      List<SongMetadata> listSongs = new List<SongMetadata>();
      if (GetPlaylistSongs(playlistName, listSongs))
      {
         _songList = listSongs;
         PlayRetrievedSongs(false);
      }
      else
      {
         Console.WriteLine("error: unable to retrieve playlist songs");
      }
   }

   public bool DeleteSong(string songUid, bool uploadMetadata=true)
   {
      bool isDeleted = false;
      if (songUid.Length > 0 && _jukeboxDb != null)
      {
         bool dbDeleted = _jukeboxDb.DeleteSong(songUid);
         string container = ContainerForSong(songUid);
         bool ssDeleted = false;
         if (container.Length > 0)
         {
            ssDeleted = _storageSystem.DeleteObject(container, songUid);
         }
         
         if (dbDeleted && uploadMetadata)
         {
            UploadMetadataDb();
         }
         
         isDeleted = dbDeleted || ssDeleted;
      }

      return isDeleted;
   }

   public bool DeleteArtist(string artist)
   {
      bool isDeleted = false;
      if (artist.Length > 0 && _jukeboxDb != null)
      {
         List<SongMetadata> songList = _jukeboxDb.SongsForArtist(artist);
         if (songList.Count == 0)
         {
            Console.WriteLine("no songs in jukebox");
            return false;
         }
         else
         {
            foreach (var song in songList)
            {
               if (!DeleteSong(song.Fm.ObjectName, false))
               {
                  Console.WriteLine("error deleting song {0}",
                     song.Fm.ObjectName);
                  return false;
               }
            }
            UploadMetadataDb();
            isDeleted = true;
         }
      }

      return isDeleted;
   }

   public bool DeleteAlbum(string album)
   {
      // ReSharper disable once StringIndexOfIsCultureSpecific.1
      int posDoubleDash = album.IndexOf("--");
      if (posDoubleDash > -1 && _jukeboxDb != null)
      {
         string artist = album.Substring(0, posDoubleDash);
         string albumName = album.Substring(posDoubleDash+2);
         List<SongMetadata> listAlbumSongs =
            _jukeboxDb.RetrieveAlbumSongs(artist, albumName);

         if (listAlbumSongs.Count > 0)
         {
            int numSongsDeleted = 0;
            foreach (var song in listAlbumSongs)
            {
               Console.WriteLine("{0} {1}",
                  song.Fm.ContainerName, song.Fm.ObjectName);

               // delete each song audio file
               string containerName = _containerPrefix + song.Fm.ContainerName;

               if (_storageSystem.DeleteObject(containerName,
                                               song.Fm.ObjectName))
               {
                  numSongsDeleted++;
                  // delete song metadata
                  _jukeboxDb.DeleteSong(song.Fm.ObjectName);
               }
               else
               {
                  Console.WriteLine("error: unable to delete song {0}",
                     song.Fm.ObjectName);
               }
            }
            
            if (numSongsDeleted > 0)
            {
               // upload metadata db
               UploadMetadataDb();
               return true;
            }
         }
         else
         {
            Console.WriteLine("no songs found for artist={0} album name={1}",
               artist, albumName);
         }
      }
      else
      {
         Console.WriteLine("specify album with 'the-artist--the-album-name' format");
      }
      return false;
   }

   public bool DeletePlaylist(string playlistName)
   {
      bool isDeleted = false;
      if (_jukeboxDb != null)
      {
         string objectName = _jukeboxDb.GetPlaylist(playlistName);
         if (objectName.Length > 0)
         {
            bool dbDeleted = _jukeboxDb.DeletePlaylist(playlistName);
            if (dbDeleted)
            {
               Console.WriteLine("container={0}, object={1}",
                  _playlistContainer, objectName);

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

   public void ImportAlbumArt()
   {
      if (_jukeboxDb != null && _jukeboxDb.IsOpen())
      {
         int fileImportCount = 0;
         string[] dirListing = Directory.GetFiles(_albumArtImportDir);
         if (dirListing.Length == 0)
         {
            Console.WriteLine("no album art found");
            return;
         }

         bool haveContainer;

         if (!_storageSystem.HasContainer(_albumArtContainer))
         {
            haveContainer = _storageSystem.CreateContainer(_albumArtContainer);
         }
         else
         {
            haveContainer = true;
         }

         if (!haveContainer)
         {
            Console.WriteLine("error: unable to create container for album art");
            return;
         }

         foreach (var listingEntry in dirListing)
         {
            string objectName = listingEntry;
            bool fileRead;
            byte[]? fileContents;

            (fileRead, fileContents) = ReadFileContents(listingEntry);
            
            if (fileRead && fileContents != null)
            {
               if (_storageSystem.PutObject(_albumArtContainer,
                                            objectName,
                                            fileContents,
                                            null))
               {
                  fileImportCount++;
               }
            }
         }

         if (fileImportCount > 0)
         {
            Console.WriteLine("{0} album art files imported", fileImportCount);
         }
         else
         {
            Console.WriteLine("no files imported");
         }
      }
   }

   public double GetMemoryInUse()
   {
      return Math.Round(Environment.WorkingSet / (double)(1024 * 1024), 2);
   }

   public long GetNumberSongs()
   {
      return _numberSongs;
   }

   public bool IsPaused()
   {
      return _isPaused;
   }

   public int GetSongSecondsOffset()
   {
      return _songSecondsOffset;
   }
   
   public void ShowAlbum(string artist, string albumName)
   {
      string jsonFileName =
         JbUtils.EncodeArtistAlbum(artist, albumName) + JsonFileExt;
      string localJsonFile = Path.Join(_songPlayDir, jsonFileName);

      if (_storageSystem.GetObject(_albumContainer,
                                   jsonFileName,
                                   localJsonFile) > 0)
      {
         string albumJsonContents = File.ReadAllText(localJsonFile);
         if (albumJsonContents.Length > 0)
         {
            Album? album = JsonSerializer.Deserialize<Album>(albumJsonContents);
            if (album != null)
            {
               Console.WriteLine("Album: {0}, Artist: {1}, Tracks:",
                  album, artist);
               int i = 0;
               foreach (var track in album.Tracks)
               {
                  i++;
                  string trackName = track.Title;
                  if (trackName.Length > 0)
                  {
                     Console.WriteLine("{0}  {1}", i, trackName);
                  }
               }
            }
         }
         else
         {
            Console.WriteLine("error: album json file is empty {0}",
               localJsonFile);
         }
      }
      else
      {
         Console.WriteLine("error: unable to retrieve album json ({0}) from storage system ({1})",
            jsonFileName,
            _albumContainer);
      }
   }
   
   public bool GetPlaylistSongs(string playlistName,
                                List<SongMetadata> listSongs)
   {
      if (_jukeboxDb == null)
      {
         return false;
      }
      
      bool success = false;

      string playlistFile = JbUtils.EncodeValue(playlistName);
      if (!playlistFile.EndsWith(JsonFileExt))
      {
         playlistFile += JsonFileExt;
      }

      // retrieve the playlist file from storage
      string localFilePath = Path.Join(Directory.GetCurrentDirectory(),
                                       playlistFile);

      if (_storageSystem.GetObject(_playlistContainer,
                                   playlistFile,
                                   localFilePath) > 0)
      {
         string fileContents = File.ReadAllText(localFilePath);
         if (fileContents.Length > 0)
         {
            Playlist? plJson =
               JsonSerializer.Deserialize<Playlist>(fileContents);

            // print the list of songs
            if (plJson != null)
            {
               int songsAdded = 0;
               List<string> fileExtensions = new List<string>(3);
               fileExtensions.Add(".flac");
               fileExtensions.Add(".m4a");
               fileExtensions.Add(".mp3");

               foreach (var plSong in plJson.Songs)
               {
                  string artist = plSong.Artist;
                  string album = plSong.Album;
                  string songName = plSong.Song;

                  if (artist.Length > 0 &&
                      album.Length > 0 &&
                      songName.Length > 0)
                  {
                     string encodedSong =
                        JbUtils.EncodeArtistAlbumSong(artist, album, songName);
                     bool songFound = false;

                     foreach (var fileExtension in fileExtensions)
                     {
                        string songUid = encodedSong + fileExtension;
                        SongMetadata? song = _jukeboxDb.RetrieveSong(songUid);
                        if (song != null)
                        {
                           listSongs.Add(song);
                           songsAdded++;
                           songFound = true;
                           break;
                        }
                     }

                     if (!songFound)
                     {
                        Console.WriteLine("error: unable to retrieve metadata for '{0}'",
                           encodedSong);
                     }
                  }
               }
               success = (songsAdded > 0);
            }
            else
            {
               Console.WriteLine("error: unable to parse playlist json");
            }
         }
         else
         {
            Console.WriteLine("error: unable to read file '{0}'",
               localFilePath);
         }
      }
      else
      {
         Console.WriteLine("error: playlist not found '{0}'", playlistFile);
      }
      return success;
   }
   
   private bool RetrieveAlbumTrackObjectList(string artist,
                                             string album,
                                             List<string> listTrackObjects)
   {
      bool success = false;
      string jsonFileName = JbUtils.EncodeArtistAlbum(artist, album) +
         JsonFileExt;
      string localJsonFile = Path.Join(_songPlayDir, jsonFileName);
      if (_storageSystem.GetObject(_albumContainer,
                                   jsonFileName,
                                   localJsonFile) > 0)
      {
         string albumJsonContents = File.ReadAllText(localJsonFile);
         if (albumJsonContents.Length > 0)
         {
            Album? albumJson =
               JsonSerializer.Deserialize<Album>(albumJsonContents);
            if (albumJson != null)
            {
               foreach (var track in albumJson.Tracks)
               {
                  listTrackObjects.Add(track.ObjectName);
               }

               if (listTrackObjects.Count > 0)
               {
                  success = true;
               }
            }
         }
         else
         {
            Console.WriteLine("error: unable to read album json file {0}",
               localJsonFile);
         }
      }
      else
      {
         Console.WriteLine("Unable to retrieve '{0}' from '{1}'",
            jsonFileName, _albumContainer);
      }
      return success;
   }
}