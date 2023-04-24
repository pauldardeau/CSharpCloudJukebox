namespace CSharpCloudJukebox;

public class JukeboxMain
{
   private StorageSystem? ConnectS3System(Dictionary<string, string> credentials,
      bool inDebugMode,
      bool inUpdateMode)
   {
      string awsAccessKey = "";
      string awsSecretKey = "";
      string updateAwsAccessKey = "";
      string updateAwsSecretKey = "";
      string endpointUrl = "";
      string region = "";

      if (credentials.ContainsKey("aws_access_key"))
      {
         awsAccessKey = credentials["aws_access_key"];
      }

      if (credentials.ContainsKey("aws_secret_key"))
      {
         awsSecretKey = credentials["aws_secret_key"];
      }

      if (credentials.ContainsKey("update_aws_access_key") &&
          credentials.ContainsKey("update_aws_secret_key"))
      {
         updateAwsAccessKey = credentials["update_aws_access_key"];
         updateAwsSecretKey = credentials["update_aws_secret_key"];
      }

      if (credentials.ContainsKey("endpoint_url"))
      {
         endpointUrl = credentials["endpoint_url"];
      }
      else
      {
         Console.WriteLine("error: s3 requires endpoint_url to be configured in creds file");
         return null;
      }

      if (credentials.ContainsKey("region"))
      {
         region = credentials["region"];
      }

      if (inDebugMode)
      {
         Console.WriteLine("aws_access_key={0}", awsAccessKey);
         Console.WriteLine("aws_secret_key={0}", awsSecretKey);
         if (updateAwsAccessKey.Length > 0 && updateAwsSecretKey.Length > 0)
         {
            Console.WriteLine("update_aws_access_key={0}", updateAwsAccessKey);
            Console.WriteLine("update_aws_secret_key={0}", updateAwsSecretKey);
         }
      }

      if (awsAccessKey.Length == 0 || awsSecretKey.Length == 0)
      {
         Console.WriteLine("error: no s3 credentials given. please specify aws_access_key " +
                           "and aws_secret_key in credentials file");
         return null;
      }
      else
      {
         string accessKey;
         string secretKey;

         if (inUpdateMode)
         {
            accessKey = updateAwsAccessKey;
            secretKey = updateAwsSecretKey;
         }
         else
         {
            accessKey = awsAccessKey;
            secretKey = awsSecretKey;
         }

         if (inDebugMode)
         {
            Console.WriteLine("Creating S3StorageSystem");
         }

         return new S3StorageSystem(accessKey,
            secretKey,
            endpointUrl,
            region,
            inDebugMode);
      }
   }

   private StorageSystem? ConnectStorageSystem(string systemName,
      Dictionary<string, string> credentials,
      string containerPrefix,
      bool inDebugMode,
      bool inUpdateMode)
   {
      if (systemName == "s3")
      {
         if (containerPrefix.Length > 0)
         {
            return ConnectS3System(credentials, inDebugMode, inUpdateMode);
         }
         else
         {
            Console.WriteLine("error: a container prefix MUST be specified for s3");
            return null;
         }
      }
      else if (systemName == "fs")
      {
         if (credentials.ContainsKey("root_dir"))
         {
            string rootDir = credentials["root_dir"];
            if (rootDir.Length > 0)
            {
               return new FsStorageSystem(rootDir, inDebugMode);
            }
         }

         Console.WriteLine("error: root_dir must be specified for file-system storage system");
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
      Console.WriteLine("\tdelete-artist      - delete specified artist");
      Console.WriteLine("\tdelete-album       - delete specified album");
      Console.WriteLine("\tdelete-playlist    - delete specified playlist");
      Console.WriteLine("\tdelete-song        - delete specified song");
      Console.WriteLine("\thelp               - show this help message");
      Console.WriteLine("\timport-songs       - import all new songs from song-import subdirectory");
      Console.WriteLine("\timport-playlists   - import all new playlists from playlist-import subdirectory");
      Console.WriteLine("\timport-album-art   - import all album art from album-art-import subdirectory");
      Console.WriteLine("\tlist-songs         - show listing of all available songs");
      Console.WriteLine("\tlist-artists       - show listing of all available artists");
      Console.WriteLine("\tlist-containers    - show listing of all available storage containers");
      Console.WriteLine("\tlist-albums        - show listing of all available albums");
      Console.WriteLine("\tlist-genres        - show listing of all available genres");
      Console.WriteLine("\tlist-playlists     - show listing of all available playlists");
      Console.WriteLine("\tshow-album         - show songs in a specified album");
      Console.WriteLine("\tshow-playlist      - show songs in specified playlist");
      Console.WriteLine("\tplay               - start playing songs");
      Console.WriteLine("\tshuffle-play       - play songs randomly");
      Console.WriteLine("\tplay-playlist      - play specified playlist");
      Console.WriteLine("\tplay-album         - play specified album");
      Console.WriteLine("\tretrieve-catalog   - retrieve copy of music catalog");
      Console.WriteLine("\tupload-metadata-db - upload SQLite metadata");
      Console.WriteLine("\tinit-storage       - initialize storage system");
      Console.WriteLine("\tusage              - show this help message");
      Console.WriteLine("");
   }

   private bool InitStorageSystem(StorageSystem storageSys, string containerPrefix)
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

   public void Main(string[] consoleArgs)
   {
      int exitCode = 0;
      bool debugMode = false;
      string storageType = "swift";
      string artist = "";
      string playlist = "";
      string song = "";
      string album = "";

      ArgumentParser optParser = new ArgumentParser();
      optParser.AddOptionalBoolFlag("--debug", "run in debug mode");
      optParser.AddOptionalIntArgument("--file-cache-count", "number of songs to buffer in cache");
      optParser.AddOptionalBoolFlag("--integrity-checks", "check file integrity after download");
      optParser.AddOptionalBoolFlag("--encrypt", "encrypt file contents");
      optParser.AddOptionalStringArgument("--key", "encryption key");
      optParser.AddOptionalStringArgument("--keyfile", "path to file containing encryption key");
      optParser.AddOptionalStringArgument("--storage", "storage system type (s3, fs)");
      optParser.AddOptionalStringArgument("--artist", "limit operations to specified artist");
      optParser.AddOptionalStringArgument("--playlist", "limit operations to specified playlist");
      optParser.AddOptionalStringArgument("--song", "limit operations to specified song");
      optParser.AddOptionalStringArgument("--album", "limit operations to specified album");
      optParser.AddRequiredArgument("command", "command for jukebox");

      Dictionary<string, object> args = optParser.ParseArgs(consoleArgs);

      JukeboxOptions options = new JukeboxOptions();

      if (args.ContainsKey("debug"))
      {
         debugMode = true;
         options.DebugMode = true;
      }

      if (args.ContainsKey("file_cache_count"))
      {
         int fileCacheCount = (int)args["file_cache_count"];
         if (debugMode)
         {
            Console.WriteLine("setting file cache count={0}", fileCacheCount);
         }

         options.FileCacheCount = fileCacheCount;
      }

      if (args.ContainsKey("integrity_checks"))
      {
         if (debugMode)
         {
            Console.WriteLine("setting integrity checks on");
         }

         options.CheckDataIntegrity = true;
      }

      if (args.ContainsKey("storage"))
      {
         string storage = (string)args["storage"];
         List<string> supportedSystems = new List<string> { "s3", "fs" };
         if (!supportedSystems.Contains(storage))
         {
            Console.WriteLine("error: invalid storage type {0}", storage);
            //Console.WriteLine("supported systems are: %s" % str(supported_systems));
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

      if (args.ContainsKey("artist"))
      {
         artist = (string)args["artist"];
      }

      if (args.ContainsKey("playlist"))
      {
         playlist = (string)args["playlist"];
      }

      if (args.ContainsKey("song"))
      {
         song = (string)args["song"];
      }

      if (args.ContainsKey("album"))
      {
         album = (string)args["album"];
      }

      if (args.ContainsKey("command"))
      {
         if (debugMode)
         {
            Console.WriteLine("using storage system type {0}", storageType);
         }

         string containerPrefix = "";
         string credsFile = storageType + "_creds.txt";
         Dictionary<string, string> creds = new Dictionary<string, string>();
         string credsFilePath = Path.Join(Directory.GetCurrentDirectory(), credsFile);

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
                           if (key == "container_prefix")
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
                  Console.WriteLine("error: unable to read file {0}", credsFilePath);
               }
            }
         }
         else
         {
            Console.WriteLine("no creds file ({0})", credsFilePath);
         }

         string command = (string)args["command"];

         List<string> helpCmds = new List<string> { "help", "usage" };
         List<string> nonHelpCmds = new List<string>
         {
            "import-songs", "play", "shuffle-play", "list-songs",
            "list-artists", "list-containers", "list-genres",
            "list-albums", "retrieve-catalog", "import-playlists",
            "list-playlists", "show-playlist", "play-playlist",
            "delete-song", "delete-album", "delete-playlist",
            "delete-artist", "upload-metadata-db",
            "import-album-art", "play-album", "show-album"
         };
         List<string> updateCmds = new List<string>
         {
            "import-songs", "import-playlists", "delete-song",
            "delete-album", "delete-playlist", "delete-artist",
            "upload-metadata-db", "import-album-art", "init-storage"
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
                  options.SuppressMetadataDownload = (command == "upload-metadata-db");

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
                           if (command == "init-storage")
                           {
                              if (InitStorageSystem(storageSystem, containerPrefix))
                              {
                                 Environment.Exit(0);
                              }
                              else
                              {
                                 Environment.Exit(1);
                              }
                           }

                           jukebox = new Jukebox(options, storageSystem, containerPrefix);
                           if (jukebox.Enter())
                           {
                              try
                              {
                                 if (command == "import-songs")
                                 {
                                    jukebox.ImportSongs();
                                 }
                                 else if (command == "import-playlists")
                                 {
                                    jukebox.ImportPlaylists();
                                 }
                                 else if (command == "play")
                                 {
                                    bool shuffle = false;
                                    jukebox.PlaySongs(shuffle, artist, album);
                                 }
                                 else if (command == "shuffle-play")
                                 {
                                    bool shuffle = true;
                                    jukebox.PlaySongs(shuffle, artist, album);
                                 }
                                 else if (command == "list-songs")
                                 {
                                    jukebox.ShowListings();
                                 }
                                 else if (command == "list-artists")
                                 {
                                    jukebox.ShowArtists();
                                 }
                                 else if (command == "list-containers")
                                 {
                                    jukebox.ShowListContainers();
                                 }
                                 else if (command == "list-genres")
                                 {
                                    jukebox.ShowGenres();
                                 }
                                 else if (command == "list-albums")
                                 {
                                    jukebox.ShowAlbums();
                                 }
                                 else if (command == "list-playlists")
                                 {
                                    jukebox.ShowPlaylists();
                                 }
                                 else if (command == "show-playlist")
                                 {
                                    if (playlist.Length > 0)
                                    {
                                       jukebox.ShowPlaylist(playlist);
                                    }
                                    else
                                    {
                                       Console.WriteLine("error: playlist must be specified using --playlist option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "play-playlist")
                                 {
                                    if (playlist.Length > 0)
                                    {
                                       jukebox.PlayPlaylist(playlist);
                                    }
                                    else
                                    {
                                       Console.WriteLine("error: playlist must be specified using --playlist option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "retrieve-catalog")
                                 {
                                    Console.WriteLine("retrieve-catalog not yet implemented");
                                 }
                                 else if (command == "delete-song")
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
                                       Console.WriteLine("error: song must be specified using --song option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "delete-artist")
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
                                       Console.WriteLine("error: artist must be specified using --artist option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "delete-album")
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
                                       Console.WriteLine("error: album must be specified using --album option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "delete-playlist")
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
                                       Console.WriteLine("error: playlist must be specified using --playlist option");
                                       exitCode = 1;
                                    }
                                 }
                                 else if (command == "upload-metadata-db")
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
                                 else if (command == "import-album-art")
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