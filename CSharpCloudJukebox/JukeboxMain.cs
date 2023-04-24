namespace CSharpCloudJukebox;

public class JukeboxMain
{
   private const string argPrefix = "--";
   private const string argDebug = "debug";
   private const string argFileCacheCount = "file-cache-count";
   private const string argIntegrityChecks = "integrity-checks";
   private const string argStorage = "storage";
   private const string argArtist = "artist";
   private const string argPlaylist = "playlist";
   private const string argSong = "song";
   private const string argAlbum = "album";
   private const string argCommand = "command";
   private const string argFormat = "format";

   private const string cmdDeleteAlbum = "delete-album";
   private const string cmdDeleteArtist = "delete-artist";
   private const string cmdDeletePlaylist = "delete-playlist";
   private const string cmdDeleteSong = "delete-song";
   private const string cmdExportAlbum = "export-album";
   private const string cmdExportPlaylist = "export-playlist";
   private const string cmdHelp = "help";
   private const string cmdImportAlbum = "import-album";
   private const string cmdImportAlbumArt = "import-album-art";
   private const string cmdImportPlaylists = "import-playlists";
   private const string cmdImportSongs = "import-songs";
   private const string cmdInitStorage = "init-storage";
   private const string cmdListAlbums = "list-albums";
   private const string cmdListArtists = "list-artists";
   private const string cmdListContainers = "list-containers";
   private const string cmdListGenres = "list-genres";
   private const string cmdListPlaylists = "list-playlists";
   private const string cmdListSongs = "list-songs";
   private const string cmdPlay = "play";
   private const string cmdPlayAlbum = "play-album";
   private const string cmdPlayPlaylist = "play-playlist";
   private const string cmdRetrieveCatalog = "retrieve-catalog";
   private const string cmdShowAlbum = "show-album";
   private const string cmdShowPlaylist = "show-playlist";
   private const string cmdShufflePlay = "shuffle-play";
   private const string cmdUploadMetadataDb = "upload-metadata-db";
   private const string cmdUsage = "usage";

   private const string ssFs = "fs";
   private const string ssS3 = "s3";

   private const string credsFileSuffix = "_creds.txt";
   private const string credsContainerPrefix = "container_prefix";

   private const string awsAccessKey = "aws_access_key";
   private const string awsSecretKey = "aws_secret_key";
   private const string updateAwsAccessKey = "update_aws_access_key";
   private const string updateAwsSecretKey = "update_aws_secret_key";
   private const string endpointUrl = "endpoint_url";
   private const string region = "region";

   private const string fsRootDir = "root_dir";

   private const string audioFileTypeMp3 = "mp3";
   private const string audioFileTypeM4a = "m4a";
   private const string audioFileTypeFlac = "flac";

   private string artist;
   private string album;
   private string song;
   private string playlist;


   public JukeboxMain()
   {
      artist = "";
      album = "";
      song = "";
      playlist = "";
   }

   private StorageSystem? ConnectS3System(Dictionary<string, string> credentials,
                                          bool inDebugMode,
                                          bool inUpdateMode)
   {
      string theAwsAccessKey = "";
      string theAwsSecretKey = "";
      string theUpdateAwsAccessKey = "";
      string theUpdateAwsSecretKey = "";
      string theEndpointUrl = "";
      string theRegion = "";

      if (credentials.ContainsKey(awsAccessKey))
      {
         theAwsAccessKey = credentials[awsAccessKey];
      }

      if (credentials.ContainsKey(awsSecretKey))
      {
         theAwsSecretKey = credentials[awsSecretKey];
      }

      if (credentials.ContainsKey(updateAwsAccessKey) &&
          credentials.ContainsKey(updateAwsSecretKey))
      {
         theUpdateAwsAccessKey = credentials[updateAwsAccessKey];
         theUpdateAwsSecretKey = credentials[updateAwsSecretKey];
      }

      if (credentials.ContainsKey(endpointUrl))
      {
         theEndpointUrl = credentials[endpointUrl];
      }
      else
      {
         Console.WriteLine("error: s3 requires {0} to be configured in creds file",
                           endpointUrl);
         return null;
      }

      if (credentials.ContainsKey(region))
      {
         theRegion = credentials[region];
      }

      if (inDebugMode)
      {
         Console.WriteLine("{0}={1}", awsAccessKey, theAwsAccessKey);
         Console.WriteLine("{0}={1}", awsSecretKey, theAwsSecretKey);
         if (theUpdateAwsAccessKey.Length > 0 && theUpdateAwsSecretKey.Length > 0)
         {
            Console.WriteLine("{0}={1}", updateAwsAccessKey, theUpdateAwsAccessKey);
            Console.WriteLine("{0}={1}", updateAwsSecretKey, theUpdateAwsSecretKey);
         }
      }

      if (theAwsAccessKey.Length == 0 || theAwsSecretKey.Length == 0)
      {
         Console.WriteLine("error: no s3 credentials given. please specify {0} and {1} in credentials file",
                           awsAccessKey, awsSecretKey);
         return null;
      }
      else
      {
         string accessKey;
         string secretKey;

         if (inUpdateMode)
         {
            accessKey = theUpdateAwsAccessKey;
            secretKey = theUpdateAwsSecretKey;
         }
         else
         {
            accessKey = theAwsAccessKey;
            secretKey = theAwsSecretKey;
         }

         if (inDebugMode)
         {
            Console.WriteLine("Creating S3StorageSystem");
         }

         return new S3StorageSystem(accessKey,
                                    secretKey,
                                    theEndpointUrl,
                                    theRegion,
                                    inDebugMode);
      }
   }

   private StorageSystem? ConnectStorageSystem(string systemName,
                                               Dictionary<string, string> credentials,
                                               string containerPrefix,
                                               bool inDebugMode,
                                               bool inUpdateMode)
   {
      if (systemName == ssS3)
      {
         if (containerPrefix.Length > 0)
         {
            return ConnectS3System(credentials, inDebugMode, inUpdateMode);
         }
         else
         {
            Console.WriteLine("error: a container prefix must be specified for s3");
            return null;
         }
      }
      else if (systemName == ssFs)
      {
         if (credentials.ContainsKey(fsRootDir))
         {
            string rootDir = credentials[fsRootDir];
            if (rootDir.Length > 0)
            {
               return new FsStorageSystem(rootDir, inDebugMode);
            }
         }

         Console.WriteLine("error: {0} must be specified for file-system storage system", fsRootDir);
         return null;
      }
      else
      {
         Console.WriteLine("error: unrecognized storage system {0}", systemName);
         return null;
      }
   }

   private void ShowUsage()
   {
      Console.WriteLine("Supported Commands:");
      Console.WriteLine("\t{0}      - delete specified artist", cmdDeleteArtist);
      Console.WriteLine("\t{0}       - delete specified album", cmdDeleteAlbum);
      Console.WriteLine("\t{0}    - delete specified playlist", cmdDeletePlaylist);
      Console.WriteLine("\t{0}        - delete specified song", cmdDeleteSong);
      Console.WriteLine("\t{0}               - show this help message", cmdHelp);
      Console.WriteLine("\t{0}       - import all new songs from song-import subdirectory", cmdImportSongs);
      Console.WriteLine("\t{0}   - import all new playlists from playlist-import subdirectory", cmdImportPlaylists);
      Console.WriteLine("\t{0}   - import all album art from album-art-import subdirectory", cmdImportAlbumArt);
      Console.WriteLine("\t{0}         - show listing of all available songs", cmdListSongs);
      Console.WriteLine("\t{0}       - show listing of all available artists", cmdListArtists);
      Console.WriteLine("\t{0}    - show listing of all available storage containers", cmdListContainers);
      Console.WriteLine("\t{0}        - show listing of all available albums", cmdListAlbums);
      Console.WriteLine("\t{0}        - show listing of all available genres", cmdListGenres);
      Console.WriteLine("\t{0}     - show listing of all available playlists", cmdListPlaylists);
      Console.WriteLine("\t{0}         - show songs in a specified album", cmdShowAlbum);
      Console.WriteLine("\t{0}      - show songs in specified playlist", cmdShowPlaylist);
      Console.WriteLine("\t{0}               - start playing songs", cmdPlay);
      Console.WriteLine("\t{0}       - play songs randomly", cmdShufflePlay);
      Console.WriteLine("\t{0}      - play specified playlist", cmdPlayPlaylist);
      Console.WriteLine("\t{0}         - play specified album", cmdPlayAlbum);
      Console.WriteLine("\t{0}   - retrieve copy of music catalog", cmdRetrieveCatalog);
      Console.WriteLine("\t{0} - upload SQLite metadata", cmdUploadMetadataDb);
      Console.WriteLine("\t{0}       - initialize storage system", cmdInitStorage);
      Console.WriteLine("\t{0}              - show this help message", cmdUsage);
      Console.WriteLine("");
   }

   private bool InitStorageSystem(StorageSystem storageSys,
                                  string containerPrefix)
   {
      bool success = false;
      Console.WriteLine("starting storage system initialization...");
      if (Jukebox.InitializeStorageSystem(storageSys, containerPrefix))
      {
         Console.WriteLine("storage system successfully initialized");
         success = true;
      }
      else
      {
         Console.WriteLine("error: unable to initialize storage system");
         success = false;
      }
      return success;
   }

   private int RunJukeboxCommand(Jukebox jukebox, String command)
   {
      var exitCode = 0;

      try
      {
         if (command == cmdImportSongs)
         {
            jukebox.ImportSongs();
         }
         else if (command == cmdImportPlaylists)
         {
            jukebox.ImportPlaylists();
         }
         else if (command == cmdPlay)
         {
            bool shuffle = false;
            jukebox.PlaySongs(shuffle, artist, album);
         }
         else if (command == cmdShufflePlay)
         {
            bool shuffle = true;
            jukebox.PlaySongs(shuffle, artist, album);
         }
         else if (command == cmdListSongs)
         {
            jukebox.ShowListings();
         }
         else if (command == cmdListArtists)
         {
            jukebox.ShowArtists();
         }
         else if (command == cmdListContainers)
         {
            jukebox.ShowListContainers();
         }
         else if (command == cmdListGenres)
         {
            jukebox.ShowGenres();
         }
         else if (command == cmdListAlbums)
         {
            jukebox.ShowAlbums();
         }
         else if (command == cmdListPlaylists)
         {
            jukebox.ShowPlaylists();
         }
         else if (command == cmdShowPlaylist)
         {
            if (playlist.Length > 0)
            {
               jukebox.ShowPlaylist(playlist);
            }
            else
            {
               Console.WriteLine("error: playlist must be specified using {0}{1} option", argPrefix, argPlaylist);
               exitCode = 1;
            }
         }
         else if (command == cmdPlayPlaylist)
         {
            if (playlist.Length > 0)
            {
               jukebox.PlayPlaylist(playlist);
            }
            else
            {
               Console.WriteLine("error: playlist must be specified using {0}{1} option", argPrefix, argPlaylist);
               exitCode = 1;
            }
         }
         else if (command == cmdRetrieveCatalog)
         {
            Console.WriteLine("retrieve-catalog not yet implemented");
         }
         else if (command == cmdDeleteSong)
         {
            if (song.Length > 0)
            {
               if (jukebox.DeleteSong(song))
               {
                  Console.WriteLine("song deleted");
               }
               else
               {
                  Console.WriteLine("error: unable to delete song");
                  exitCode = 1;
               }
            }
            else
            {
               Console.WriteLine("error: song must be specified using {0}{1} option", argPrefix, argSong);
               exitCode = 1;
            }
         }
         else if (command == cmdDeleteArtist)
         {
            if (artist.Length > 0)
            {
               if (jukebox.DeleteArtist(artist))
               {
                  Console.WriteLine("artist deleted");
               }
               else
               {
                  Console.WriteLine("error: unable to delete artist");
                  exitCode = 1;
               }
            }
            else
            {
               Console.WriteLine("error: artist must be specified using {0}{1} option", argPrefix, argArtist);
               exitCode = 1;
            }
         }
         else if (command == cmdDeleteAlbum)
         {
            if (album.Length > 0)
            {
               if (jukebox.DeleteAlbum(album))
               {
                  Console.WriteLine("album deleted");
               }
               else
               {
                  Console.WriteLine("error: unable to delete album");
                  exitCode = 1;
               }
            }
            else
            {
               Console.WriteLine("error: album must be specified using {0}{1} option", argPrefix, argAlbum);
               exitCode = 1;
            }
         }
         else if (command == cmdDeletePlaylist)
         {
            if (playlist.Length > 0)
            {
               if (jukebox.DeletePlaylist(playlist))
               {
                  Console.WriteLine("playlist deleted");
               }
               else
               {
                  Console.WriteLine("error: unable to delete playlist");
                  exitCode = 1;
               }
            }
            else
            {
               Console.WriteLine("error: playlist must be specified using {0}{1} option", argPrefix, argPlaylist);
               exitCode = 1;
            }
         }
         else if (command == cmdUploadMetadataDb)
         {
            if (jukebox.UploadMetadataDb())
            {
               Console.WriteLine("metadata db uploaded");
            }
            else
            {
               Console.WriteLine("error: unable to upload metadata db");
               exitCode = 1;
            }
         }
         else if (command == cmdImportAlbumArt)
         {
            jukebox.ImportAlbumArt();
         }
      }
      catch (Exception e)
      {
         Console.WriteLine("exception caught: " + e);
      }
      finally
      {
         jukebox.Exit();
      }

      return exitCode;
   }

   public void Main(string[] consoleArgs)
   {
      int exitCode = 0;
      bool debugMode = false;
      string storageType = ssS3;

      ArgumentParser optParser = new ArgumentParser();
      optParser.AddOptionalBoolFlag(argPrefix+argDebug, "run in debug mode");
      optParser.AddOptionalIntArgument(argPrefix+argFileCacheCount, "number of songs to buffer in cache");
      optParser.AddOptionalBoolFlag(argPrefix+argIntegrityChecks, "check file integrity after download");
      optParser.AddOptionalStringArgument(argPrefix+argStorage, "storage system type (s3, fs)");
      optParser.AddOptionalStringArgument(argPrefix+argArtist, "limit operations to specified artist");
      optParser.AddOptionalStringArgument(argPrefix+argPlaylist, "limit operations to specified playlist");
      optParser.AddOptionalStringArgument(argPrefix+argSong, "limit operations to specified song");
      optParser.AddOptionalStringArgument(argPrefix+argAlbum, "limit operations to specified album");
      optParser.AddRequiredArgument(argCommand, "command for jukebox");

      Dictionary<string, object> args = optParser.ParseArgs(consoleArgs);

      JukeboxOptions options = new JukeboxOptions();

      if (args.ContainsKey(argDebug))
      {
         debugMode = true;
         options.DebugMode = true;
      }

      if (args.ContainsKey(argFileCacheCount))
      {
         int fileCacheCount = (int)args[argFileCacheCount];
         if (debugMode)
         {
            Console.WriteLine("setting file cache count={0}", fileCacheCount);
         }

         options.FileCacheCount = fileCacheCount;
      }

      if (args.ContainsKey(argIntegrityChecks))
      {
         if (debugMode)
         {
            Console.WriteLine("setting integrity checks on");
         }

         options.CheckDataIntegrity = true;
      }

      if (args.ContainsKey(argStorage))
      {
         string storage = (string)args[argStorage];
         List<string> supportedSystems = new List<string> { ssS3, ssFs };
         if (!supportedSystems.Contains(storage))
         {
            Console.WriteLine("error: invalid storage type {0}", storage);
            //Console.WriteLine("supported systems are: %s" % str(supportedSystems));
            Utils.SysExit(1);
         }
         else
         {
            if (debugMode)
            {
               Console.WriteLine("setting storage system to {0}", storage);
            }

            storageType = storage;
         }
      }

      if (args.ContainsKey(argArtist))
      {
         artist = (string)args[argArtist];
      }

      if (args.ContainsKey(argPlaylist))
      {
         playlist = (string)args[argPlaylist];
      }

      if (args.ContainsKey(argSong))
      {
         song = (string)args[argSong];
      }

      if (args.ContainsKey(argAlbum))
      {
         album = (string)args[argAlbum];
      }

      if (args.ContainsKey(argCommand))
      {
         if (debugMode)
         {
            Console.WriteLine("using storage system type {0}", storageType);
         }

         string containerPrefix = "";
         string credsFile = storageType + credsFileSuffix;
         Dictionary<string, string> creds = new Dictionary<string, string>();
         string credsFilePath = Path.Join(Directory.GetCurrentDirectory(),
                                          credsFile);

         if (Utils.PathExists(credsFilePath))
         {
            if (debugMode)
            {
               Console.WriteLine("reading creds file {0}", credsFilePath);
            }

            try
            {
               foreach (var fileLine in File.ReadLines(credsFilePath))
               {
                  string trimmedFileLine = fileLine.Trim();
                  if (trimmedFileLine.Length > 0)
                  {
                     string[] tokens = trimmedFileLine.Split("=");
                     if (tokens.Length == 2)
                     {
                        string key = tokens[0].Trim();
                        string value = tokens[1].Trim();
                        if (key.Length > 0 && value.Length > 0)
                        {
                           creds[key] = value;
                           if (key == credsContainerPrefix)
                           {
                              containerPrefix = value;
                           }
                        }
                     }
                  }
               }
            }
            catch (Exception)
            {
               if (debugMode)
               {
                  Console.WriteLine("error: unable to read file {0}",
                                    credsFilePath);
               }
            }
         }
         else
         {
            Console.WriteLine("no creds file ({0})", credsFilePath);
         }

         string command = (string)args[argCommand];

         List<string> helpCmds = new List<string> { cmdHelp, cmdUsage };
         List<string> nonHelpCmds = new List<string>
         {
            cmdImportSongs, cmdPlay, cmdShufflePlay, cmdListSongs,
            cmdListArtists, cmdListContainers, cmdListGenres,
            cmdListAlbums, cmdRetrieveCatalog, cmdImportPlaylists,
            cmdListPlaylists, cmdShowPlaylist, cmdPlayPlaylist,
            cmdDeleteSong, cmdDeleteAlbum, cmdDeletePlaylist,
            cmdDeleteArtist, cmdUploadMetadataDb,
            cmdImportAlbumArt, cmdPlayAlbum, cmdShowAlbum
         };
         List<string> updateCmds = new List<string>
         {
            cmdImportSongs, cmdImportPlaylists, cmdDeleteSong,
            cmdDeleteAlbum, cmdDeletePlaylist, cmdDeleteArtist,
            cmdUploadMetadataDb, cmdImportAlbumArt, cmdInitStorage
         };
         List<string> allCmds = new List<string>();
         allCmds.AddRange(helpCmds);
         allCmds.AddRange(nonHelpCmds);

         if (!allCmds.Contains(command))
         {
            Console.WriteLine("Unrecognized command {0}", command);
            Console.WriteLine("");
            ShowUsage();
         }
         else
         {
            if (helpCmds.Contains(command))
            {
               ShowUsage();
            }
            else
            {
               if (!options.ValidateOptions())
               {
                  Utils.SysExit(1);
               }

               StorageSystem? storageSystem;
               Jukebox? jukebox;

               try
               {
                  options.SuppressMetadataDownload = (command == cmdUploadMetadataDb);

                  bool inUpdateMode = updateCmds.Contains(command);

                  storageSystem = ConnectStorageSystem(storageType,
                     creds,
                     containerPrefix,
                     debugMode,
                     inUpdateMode);
                  if (storageSystem == null)
                  {
                     Console.WriteLine("error: unable to connect to storage system");
                     Utils.SysExit(1);
                  }
                  else
                  {
                     if (storageSystem.Enter())
                     {
                        Console.WriteLine("storage system entered");

                        try
                        {
                           if (command == cmdInitStorage)
                           {
                              if (InitStorageSystem(storageSystem,
                                                    containerPrefix))
                              {
                                 Environment.Exit(0);
                              }
                              else
                              {
                                 Environment.Exit(1);
                              }
                           }

                           jukebox = new Jukebox(options,
                                                 storageSystem,
                                                 containerPrefix);
                           if (jukebox.Enter())
                           {
                              exitCode = RunJukeboxCommand(jukebox, command);
                           }
                        }
                        finally
                        {
                           storageSystem.Exit();
                        }
                     }
                  }
               }
               catch (Exception e)
               {
                  Console.WriteLine("Exception caught: " + e);
                  exitCode = 1;
               }
            }
         }
      }
      else
      {
         Console.WriteLine("Error: no command given");
         ShowUsage();
      }

      Utils.SysExit(exitCode);
   }
}