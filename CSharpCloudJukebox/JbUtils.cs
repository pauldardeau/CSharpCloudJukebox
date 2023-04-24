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
      var cleanedValue = RemovePunctuation(value);
      return cleanedValue.Replace(" ", "-");
   }

   public static string EncodeArtistAlbum(string artist, string album) {
      return EncodeValue(artist) + DoubleDashes + EncodeValue(album);
   }

   public static string EncodeArtistAlbumSong(string artist,
                                              string album,
                                              string song) {
      return EncodeArtistAlbum(artist, album) +
             DoubleDashes +
             EncodeValue(song);
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

      if (s.Contains("&"))
      {
         s = s.Replace("&", "");
      }

      return s;
   }

   public static string[] ComponentsFromFileName(string fileName)
   {
      if (fileName.Length == 0)
      {
         return new string[] { };
      }
      int posExtension = fileName.IndexOf('.');
      string baseFileName;
      if (posExtension > -1)
      {
         baseFileName = fileName.Substring(0, posExtension);
      }
      else
      {
         baseFileName = fileName;
      }
      string[] components = baseFileName.Split("--");
      if (components.Length == 3)
      {
         return new[] {JbUtils.DecodeValue(components[0]),
                       JbUtils.DecodeValue(components[1]),
                       JbUtils.DecodeValue(components[2])};
      }
      else
      {
         return new[] { "", "", "" };
      }
   }

   public static string ArtistFromFileName(string fileName)
   {
      if (fileName.Length > 0)
      {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3)
         {
            return components[0];
         }
      }
      return "";
   }

   public static string AlbumFromFileName(string fileName)
   {
      if (fileName.Length > 0)
      {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3)
         {
            return components[1];
         }
      }
      return "";
   }

   public static string SongFromFileName(string fileName)
   {
      if (fileName.Length > 0)
      {
         string[] components = ComponentsFromFileName(fileName);
         if (components.Length == 3)
         {
            return components[2];
         }
      }
      return "";
   }

}