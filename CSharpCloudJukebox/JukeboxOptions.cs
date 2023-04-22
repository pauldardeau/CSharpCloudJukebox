namespace CSharpCloudJukebox;

public class JukeboxOptions
{
   public bool DebugMode;
   public bool CheckDataIntegrity;
   public int FileCacheCount;
   public bool SuppressMetadataDownload;

   public JukeboxOptions()
   {
      DebugMode = false;
      CheckDataIntegrity = false;
      FileCacheCount = 3;
      SuppressMetadataDownload = false;
   }

   public bool ValidateOptions()
   {
      if (FileCacheCount < 0)
      {
         Console.WriteLine("error: file cache count must be non-negative integer value");
         return false;
      }

      return true;
   }
}