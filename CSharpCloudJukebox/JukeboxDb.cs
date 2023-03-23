namespace CSharpCloudJukebox;

using Microsoft.Data.Sqlite;

public class JukeboxDb
{
   private readonly bool _debugPrint;
   private bool _dbIsOpen;
   private SqliteConnection _dbConnection;
   private string _metadataDbFilePath;

   public JukeboxDb(string metadataDbFilePath, bool debugPrint=true) {
      _debugPrint = debugPrint;
      _dbIsOpen = false;
      if (metadataDbFilePath.Length > 0) {
         _metadataDbFilePath = metadataDbFilePath;
      } else {
         _metadataDbFilePath = "jukebox_db.sqlite3";
      }
      _dbConnection = new SqliteConnection("Data Source=" + metadataDbFilePath);
   }

   public bool IsOpen() {
      return _dbIsOpen;
   }

   public bool Open() {
      Close();
      bool openSuccess;
      _dbConnection.Open();
      _dbIsOpen = true;
      if (!HaveTables()) {
         openSuccess = CreateTables();
         if (!openSuccess) {
            Console.WriteLine("error: unable to create all tables");
            _dbConnection.Close();
            _dbIsOpen = false;
         }
      } else {
         openSuccess = true;
      }
      return openSuccess;
   }

   public bool Close() {
      bool didClose = false;
      if (_dbIsOpen) {
         _dbConnection.Close();
         _dbIsOpen = false;
         didClose = true;
      }
      return didClose;
   }

   private bool CreateTable(string sql) {
      SqliteCommand command = _dbConnection.CreateCommand();
      command.CommandText = sql;
      bool tableCreated = false;
      try
      {
         command.ExecuteNonQuery();
         tableCreated = true;
      }
      catch (SqliteException se)
      {
         Console.WriteLine("creation of table failed");
         Console.WriteLine(sql);
         Console.WriteLine(se);
      }
      catch (Exception e)
      {
         Console.WriteLine("creation of table failed");
         Console.WriteLine(sql);
         Console.WriteLine(e);
      }

      command.Dispose();
      return tableCreated;
   }

   private bool CreateTables() {
      if (_dbIsOpen) {
         if (_debugPrint) {
            Console.WriteLine("creating tables");
         }

         string createGenreTable = @"CREATE TABLE genre (
                                          genre_uid TEXT UNIQUE NOT NULL,
                                          genre_name TEXT UNIQUE NOT NULL,
                                          genre_description TEXT)";

         string createArtistTable = @"CREATE TABLE artist (
                                           artist_uid TEXT UNIQUE NOT NULL,
                                           artist_name TEXT UNIQUE NOT NULL,
                                           artist_description TEXT)";

         string createAlbumTable = @"CREATE TABLE album (
                                          album_uid TEXT UNIQUE NOT NULL,
                                          album_name TEXT UNIQUE NOT NULL,
                                          album_description TEXT,
                                          artist_uid TEXT NOT NULL REFERENCES artist(artist_uid),
                                          genre_uid TEXT REFERENCES genre(genre_uid))";

         string createSongTable = @"CREATE TABLE song (
                                         song_uid TEXT UNIQUE NOT NULL,
                                         file_time TEXT,
                                         origin_file_size INTEGER,
                                         stored_file_size INTEGER,
                                         pad_char_count INTEGER,
                                         artist_name TEXT,
                                         artist_uid TEXT REFERENCES artist(artist_uid),
                                         song_name TEXT NOT NULL,
                                         md5_hash TEXT NOT NULL,
                                         compressed INTEGER,
                                         encrypted INTEGER,
                                         container_name TEXT NOT NULL,
                                         object_name TEXT NOT NULL,
                                         album_uid TEXT REFERENCES album(album_uid))";

         string createPlaylistTable = @"CREATE TABLE playlist (
                                             playlist_uid TEXT UNIQUE NOT NULL,
                                             playlist_name TEXT UNIQUE NOT NULL,
                                             playlist_description TEXT)";

         string createPlaylistSongTable = @"CREATE TABLE playlist_song (
                                                  playlist_song_uid TEXT UNIQUE NOT NULL,
                                                  playlist_uid TEXT NOT NULL REFERENCES playlist(playlist_uid),
                                                  song_uid TEXT NOT NULL REFERENCES song(song_uid))";

         return CreateTable(createGenreTable) &&
                CreateTable(createArtistTable) &&
                CreateTable(createAlbumTable) &&
                CreateTable(createSongTable) &&
                CreateTable(createPlaylistTable) &&
                CreateTable(createPlaylistSongTable);
      } else {
         Console.WriteLine("create_tables: db_is_open is false");
         return false;
      }
   }

   private bool HaveTables() {
      bool haveTablesInDb = false;
      if (_dbIsOpen) {
         string sql = @"SELECT name
                        FROM sqlite_master
                        WHERE type='table' AND name='song'";
         try
         {
            using (var command = _dbConnection.CreateCommand())
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  reader.Read();
                  string name = reader.GetString(0);
                  if (name.Length > 0) {
                     haveTablesInDb = true;
                  }
               }
            }
         }
         catch (Exception)
         {
            // ignore
         }
      }

      return haveTablesInDb;
   }

   public void IdForArtist(string artistName) {
   }

/*
    def id_for_album(artist_name: str, album_name: str):
        pass
    def insert_artist(artist_name: str):
        pass
    def insert_album(album_name: str, artist_id: str):
        pass
    def albums_for_artist(artist_id: str):
        pass
    def get_artists():
        pass
    def songs_for_album(album_id: str):
        pass
    def get_playlists():
        pass
    */

   public string GetPlaylist(string playlistName) {
      string plObject = "";
      if (playlistName != null && playlistName.Length > 0) {
         string sql = "SELECT playlist_uid FROM playlist WHERE playlist_name = $playlist_name";
         using (var command = _dbConnection.CreateCommand())
         {
            command.CommandText = sql;
            command.Parameters.AddWithValue("$playlist_name", playlistName);
            using (var reader = command.ExecuteReader())
            {
               while (reader.Read())
               {
                  var playlistUid = reader.GetString(0);
                  plObject = playlistUid;
                  break;
               }
            }
         }
      }
      return plObject;
   }

   private List<SongMetadata> SongsForQuery(SqliteCommand command) {
      List<SongMetadata> resultSongs = new List<SongMetadata>();
      try
      {
         using (var reader = command.ExecuteReader())
         {
            while (reader.Read())
            {
               string fileUid = reader.GetString(0);
               string containerName = reader.GetString(11);
               string objectName = reader.GetString(12);

               string artistName = reader.GetString(5);
               string songName = reader.GetString(7);
               
               FileMetadata fm = new FileMetadata(fileUid, containerName, objectName);
               SongMetadata song = new SongMetadata(fm, artistName, songName);
               song.Fm.FileTime = reader.GetString(1);
               song.Fm.OriginFileSize = reader.GetInt32(2);
               song.Fm.StoredFileSize = reader.GetInt32(3);
               song.Fm.PadCharCount = reader.GetInt32(4);
               song.ArtistUid = reader.GetString(6);
               song.Fm.Md5Hash = reader.GetString(8);
               song.Fm.Compressed = reader.GetInt32(9);
               song.Fm.Encrypted = reader.GetInt32(10);
               if (!reader.IsDBNull(13)) {
                  song.AlbumUid = reader.GetString(13);
               } else {
                  song.AlbumUid = "";
               }
               resultSongs.Add(song);
            }
         }
      }
      catch (SqliteException e)
      {
         Console.WriteLine("error executing query {0}", command.CommandText);
         Console.WriteLine(e);
      }
      return resultSongs;
   }

   public SongMetadata? RetrieveSong(string fileName) {
      if (_dbIsOpen) {
         string sql = @"SELECT song_uid,
                           file_time,
                           origin_file_size,
                           stored_file_size,
                           pad_char_count,
                           artist_name,
                           artist_uid,
                           song_name,
                           md5_hash,
                           compressed,
                           encrypted,
                           container_name,
                           object_name,
                           album_uid
                        FROM song
                        WHERE song_uid = $song_uid";
         using (var command = _dbConnection.CreateCommand())
         {
            command.CommandText = sql;
            command.Parameters.AddWithValue("$song_uid", fileName);
            List<SongMetadata> songResults = SongsForQuery(command);
            if (songResults.Count > 0) {
               return songResults[0];
            }
         }
      }
      return null;
   }

   public bool InsertPlaylist(string plUid, string plName, string plDesc = "") {
      bool insertSuccess = false;

      if (_dbIsOpen && plUid.Length > 0 && plName.Length > 0) {
         string sql = "INSERT INTO playlist VALUES ($pl_uid,$pl_name,$pl_desc)";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               command.Parameters.AddWithValue("$pl_uid", plUid);
               command.Parameters.AddWithValue("$pl_name", plName);
               command.Parameters.AddWithValue("$pl_desc", plDesc);
               command.ExecuteNonQuery();
               insertSuccess = true;
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error inserting playlist: " + e.ToString());
            }
         }
      }

      return insertSuccess;
   }

   public bool DeletePlaylist(string plName) {
      bool deleteSuccess = false;

      if (_dbIsOpen && plName.Length > 0) {
         string sql = @"DELETE
                        FROM playlist
                        WHERE playlist_name = $playlist_name";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               command.Parameters.AddWithValue("$playlist_name", plName);
               command.ExecuteNonQuery();
               deleteSuccess = true;
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error deleting playlist: " + e.ToString());
            }
         }
      }

      return deleteSuccess;
   }

   public bool InsertSong(SongMetadata song) {
      bool insertSuccess = false;

      if (_dbIsOpen) {
         //TODO: (2) fix unidentified column for new song record (insert_song)
         string sql = @"INSERT INTO song
                        VALUES ($file_uid,
                                $file_time,
                                $o_file_size,
                                $s_file_size,
                                $pad_char_count,
                                $artist_name,
                                $xxxx,
                                $song_name,
                                $md5,
                                $compressed,
                                $encrypted,
                                $container_name,
                                $object_name,
                                $album_uid)";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               command.Parameters.AddWithValue("$file_uid", song.Fm.FileUid);
               command.Parameters.AddWithValue("$file_time", song.Fm.FileTime);
               command.Parameters.AddWithValue("$o_file_size", song.Fm.OriginFileSize);
               command.Parameters.AddWithValue("$s_file_size", song.Fm.StoredFileSize);
               command.Parameters.AddWithValue("$pad_char_count", song.Fm.PadCharCount);
               command.Parameters.AddWithValue("$artist_name", song.ArtistName);
               command.Parameters.AddWithValue("", "");
               command.Parameters.AddWithValue("$song_name", song.SongName);
               command.Parameters.AddWithValue("$md5", song.Fm.Md5Hash);
               command.Parameters.AddWithValue("$compressed", song.Fm.Compressed);
               command.Parameters.AddWithValue("$encrypted", song.Fm.Encrypted);
               command.Parameters.AddWithValue("$container_name", song.Fm.ContainerName);
               command.Parameters.AddWithValue("$object_name", song.Fm.ObjectName);
               command.Parameters.AddWithValue("$album_uid", song.AlbumUid);
               command.ExecuteNonQuery();
               insertSuccess = true;
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error inserting song: " + e.ToString());
            }
         }
      }

      return insertSuccess;
   }

   public bool UpdateSong(SongMetadata song) {
      bool updateSuccess = false;

      if (_dbIsOpen && song.Fm != null && song.Fm.FileUid.Length > 0) {
         string sql = @"UPDATE song
                        SET file_time = $file_time,
                            origin_file_size = $o_file_size,
                            stored_file_size = $s_file_size,
                            pad_char_count = $pad_char_count,
                            artist_name = $artist_name,
                            artist_uid = $artist_uid,
                            song_name = $song_name,
                            md5_hash = $md5_hash,
                            compressed = $compressed,
                            encrypted = $encrypted,
                            container_name = $container_name,
                            object_name = $object_name,
                            album_uid = $album_uid 
                        WHERE song_uid = $file_uid";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               command.Parameters.AddWithValue("$file_time", song.Fm.FileTime);
               command.Parameters.AddWithValue("$o_file_size", song.Fm.OriginFileSize);
               command.Parameters.AddWithValue("$s_file_size", song.Fm.StoredFileSize);
               command.Parameters.AddWithValue("$pad_char_count", song.Fm.PadCharCount);
               command.Parameters.AddWithValue("$artist_name", song.ArtistName);
               command.Parameters.AddWithValue("$artist_uid", "");
               command.Parameters.AddWithValue("$song_name", song.SongName);
               command.Parameters.AddWithValue("$md5_hash", song.Fm.Md5Hash);
               command.Parameters.AddWithValue("$compressed", song.Fm.Compressed);
               command.Parameters.AddWithValue("$encrypted", song.Fm.Encrypted);
               command.Parameters.AddWithValue("$container_name", song.Fm.ContainerName);
               command.Parameters.AddWithValue("$object_name", song.Fm.ObjectName);
               command.Parameters.AddWithValue("$album_uid", song.AlbumUid);
               command.Parameters.AddWithValue("$file_uid", song.Fm.FileUid);

               command.ExecuteNonQuery();
               updateSuccess = true;
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error updating song: " + e.ToString());
            }
         }
      }

      return updateSuccess;
   }

   public bool StoreSongMetadata(SongMetadata song) {
      SongMetadata? dbSong = RetrieveSong(song.Fm.FileUid);
      if (dbSong != null) {
         if (!song.Equals(dbSong)) {
            return UpdateSong(song);
         } else {
            return true;  // no insert or update needed (already up-to-date)
         }
      } else {
         // song is not in the database, insert it
         return InsertSong(song);
      }
   }

   private string SqlWhereClause(bool usingEncryption = false,
                                 bool usingCompression = false) {
      string encryption = usingEncryption ? "1" : "0";
      string compression = usingCompression ? "1" : "0";

      string whereClause = "";
      whereClause += " WHERE ";
      whereClause += "encrypted = ";
      whereClause += encryption;
      whereClause += " AND ";
      whereClause += "compressed = ";
      whereClause += compression;
      return whereClause;
   }

   public List<SongMetadata> RetrieveSongs(string artist, string album="") {
      List<SongMetadata> songs = new List<SongMetadata>();
      if (_dbIsOpen) {
         string sql = @"SELECT song_uid,
                           file_time,
                           origin_file_size,
                           stored_file_size,
                           pad_char_count,
                           artist_name,
                           artist_uid,
                           song_name,
                           md5_hash,
                           compressed,
                           encrypted,
                           container_name,
                           object_name,
                           album_uid
                        FROM song";
         sql += SqlWhereClause();
         //if len(artist) > 0:
         //    sql += " AND artist_name='%s'" % artist
         if (album.Length > 0) {
             string encodedArtist = Jukebox.EncodeValue(artist);
             string encodedAlbum = Jukebox.EncodeValue(album);
             sql += " AND object_name LIKE '{encodedArtist}--{encodedAlbum}%'";
         }
         using (var command = _dbConnection.CreateCommand())
         {
            command.CommandText = sql;
            songs = SongsForQuery(command);
         }
      }
      return songs;
   }

   public List<SongMetadata> SongsForArtist(string artistName) {
      List<SongMetadata> songs = new List<SongMetadata>();
      if (_dbIsOpen) {
         string sql = @"SELECT song_uid,
                           file_time,
                           origin_file size,
                           stored_file size,
                           pad_char_count,
                           artist_name,
                           artist_uid,
                           song_name,
                           md5_hash,
                           compressed,
                           encrypted,
                           container_name,
                           object_name,
                           album_uid
                        FROM song";
         sql += SqlWhereClause();
         sql += " AND artist = $artist_name";
         using (var command = _dbConnection.CreateCommand())
         {
            command.CommandText = sql;
            command.Parameters.AddWithValue("$artist_name", artistName);
            songs = SongsForQuery(command);
         }
      }
      return songs;
   }

   public void ShowListings() {
      if (_dbIsOpen) {
         string sql = @"SELECT artist_name, song_name
                        FROM song
                        ORDER BY artist_name, song_name";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  while (reader.Read())
                  {
                     var artistName = reader.GetString(0);
                     var songName = reader.GetString(1);
                     Console.WriteLine("{0}, {1}", artistName, songName);
                  }
               }
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error executing {0}", sql);
               Console.WriteLine(e);
            }
         }
      }
   }

   public void ShowArtists() {
      if (_dbIsOpen) {
         string sql = @"SELECT DISTINCT artist_name
                        FROM song
                        ORDER BY artist_name";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  while (reader.Read())
                  {
                     var artistName = reader.GetString(0);
                     Console.WriteLine("{0}", artistName);
                  }
               }
            }
            catch (SqliteException e)
            {
               Console.WriteLine("error executing {0}", sql);
               Console.WriteLine(e);
            }
         }
      }
   }

   public void ShowGenres() {
      if (_dbIsOpen) {
         string sql = @"SELECT genre_name
                        FROM genre
                        ORDER BY genre_name";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  while (reader.Read())
                  {
                     var genreName = reader.GetString(0);
                     Console.WriteLine("{0}", genreName);
                  }
               }
            }
            catch (SqliteException e)
            {
               Console.WriteLine("Error executing {0}", sql);
               Console.WriteLine(e);
            }
         }
      }
   }

   public void ShowArtistAlbums(string artistName) {
      //TODO: (3) implement (ShowArtistAlbums)
   }

   public void ShowAlbums() {
      if (_dbIsOpen) {
         string sql = @"SELECT album.album_name, artist.artist_name
                        FROM album, artist
                        WHERE album.artist_uid = artist.artist_uid
                        ORDER BY album.album_name";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  while (reader.Read())
                  {
                     var albumName = reader.GetString(0);
                     var artistName = reader.GetString(1);
                     Console.WriteLine("{0} ({1})", albumName, artistName);
                  }
               }
            }
            catch (SqliteException e)
            {
               Console.WriteLine("Error executing {0}", sql);
               Console.WriteLine(e);
            }
         }
      }
   }

   public void ShowPlaylists() {
      if (_dbIsOpen) {
         string sql = @"SELECT playlist_uid, playlist_name
                        FROM playlist
                        ORDER BY playlist_uid";
         using (var command = _dbConnection.CreateCommand())
         {
            try
            {
               command.CommandText = sql;
               using (var reader = command.ExecuteReader())
               {
                  while (reader.Read())
                  {
                     var playlistUid = reader.GetString(0);
                     var playlistName = reader.GetString(1);
                     Console.WriteLine("{0} - {1}", playlistUid, playlistName);
                  }
               }
            }
            catch (SqliteException e)
            {
               Console.WriteLine("Error executing {0}", sql);
               Console.WriteLine(e);
            }
         }
      }
   }

   public bool DeleteSong(string songUid) {
      bool wasDeleted = false;
      if (_dbIsOpen) {
         if (songUid.Length > 0) {
            string sql = "DELETE FROM song WHERE song_uid = $songUid";
            using (var command = _dbConnection.CreateCommand())
            {
               try
               {
                  command.CommandText = sql;
                  command.Parameters.AddWithValue("$song_uid", songUid);
                  command.ExecuteNonQuery();
                  wasDeleted = true;
               }
               catch (SqliteException e)
               {
                  Console.WriteLine("error deleting song {0}", songUid);
                  Console.WriteLine(e);
               }
            }
         }
      } 

      return wasDeleted;
   }   
}