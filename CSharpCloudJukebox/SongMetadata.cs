namespace CSharpCloudJukebox;

public sealed class SongMetadata
{
   public readonly FileMetadata Fm;
   public readonly string ArtistName;
   public readonly string SongName;
   public string ArtistUid;
   public string AlbumUid;

   public SongMetadata(FileMetadata fm, string artistName, string songName)
   {
      Fm = fm;
      ArtistUid = "";
      ArtistName = artistName;
      AlbumUid = "";
      SongName = songName;
   }

   public override bool Equals(object? o)
   {
      if (o == null)
      {
         return false;
      }
      if (o is SongMetadata other)
      {
         return Fm.Equals(other.Fm) &&
                ArtistUid == other.ArtistUid &&
                ArtistName == other.ArtistName &&
                AlbumUid == other.AlbumUid &&
                SongName == other.SongName;
      }
      else
      {
         return false;
      }
   }

   public override int GetHashCode()
   {
      int hash = 17;
      hash = hash * 23 + Fm.GetHashCode();
      hash = hash * 23 + ArtistName.GetHashCode();
      hash = hash * 23 + SongName.GetHashCode();
      return hash;
   }

   public static SongMetadata? FromDictionary(Dictionary<string, object> dictionary, string prefix = "")
   {
      FileMetadata? fm = FileMetadata.FromDictionary(dictionary, prefix);
      if (fm != null)
      {
         string artistName;
         string songName;
         
         if (dictionary.ContainsKey(prefix + "artist_name"))
         {
            artistName = (string) dictionary[prefix + "artist_name"];
         }
         else
         {
            return null;
         }
         
         if (dictionary.ContainsKey(prefix + "song_name"))
         {
            songName = (string) dictionary[prefix + "song_name"];
         }
         else
         {
            return null;
         }
         
         SongMetadata sm = new SongMetadata(fm, artistName, songName);
         if (dictionary.ContainsKey(prefix + "artist_uid"))
         {
            sm.ArtistUid = (string) dictionary[prefix + "artist_uid"];
         }
         if (dictionary.ContainsKey(prefix + "album_uid"))
         {
            sm.AlbumUid = (string) dictionary[prefix + "album_uid"];
         }

         return sm;
      }
      else
      {
         return null;
      }
   }

   public Dictionary<string, object> ToDictionary(string prefix="")
   {
      var d = new Dictionary<string, object>()
      {
         {prefix + "fm", Fm.ToDictionary(prefix)},
         {prefix + "artist_uid", ArtistUid},
         {prefix + "artist_name", ArtistName},
         {prefix + "album_uid", AlbumUid},
         {prefix + "song_name", SongName}
      };
      return d;
   }   
}