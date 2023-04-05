namespace CSharpCloudJukebox;

public class JbUtils
{
   private const string DoubleDashes = "--";
   
   public static string DecodeValue(string encodedValue)
   {
      return encodedValue.Replace("-", " ");
   }

   public static string EncodeValue(string value)
   {
      return value.Replace(" ", "-");
   }

   public static string EncodeArtistAlbum(string artist, string album) {
      return EncodeValue(artist) + DoubleDashes + EncodeValue(album);
   }

   public static string EncodeArtistAlbumSong(string artist, string album, string song) {
      return EncodeArtistAlbum(artist, album) + DoubleDashes + EncodeValue(song);
   }

   public static string RemovePunctuation(string s)
   {
      if (s.Contains("'"))
      {
         s = s.Replace("'", "");
      }

      if (s.Contains("!"))
      {
         s = s.Replace("!", "");
      }

      if (s.Contains("?"))
      {
         s = s.Replace("?", "");
      }

      return s;
   }
}