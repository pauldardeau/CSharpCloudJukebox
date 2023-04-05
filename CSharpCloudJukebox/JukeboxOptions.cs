namespace CSharpCloudJukebox;

public class JukeboxOptions
{
   public bool DebugMode;
   public bool UseEncryption;
   public bool CheckDataIntegrity;
   public int FileCacheCount;
   public string EncryptionKey;
   public string EncryptionKeyFile;
   public string EncryptionIv;
   public bool SuppressMetadataDownload;

   public JukeboxOptions()
   {
      DebugMode = false;
      UseEncryption = false;
      CheckDataIntegrity = false;
      FileCacheCount = 3;
      EncryptionKey = "";
      EncryptionKeyFile = "";
      EncryptionIv = "";
      SuppressMetadataDownload = false;
   }

   public bool ValidateOptions()
   {
      if (FileCacheCount < 0)
      {
         Console.WriteLine("error: file cache count must be non-negative integer value");
         return false;
      }

      if (EncryptionKeyFile.Length > 0 && ! File.Exists(EncryptionKeyFile))
      {
         Console.WriteLine("error: encryption key file doesn't exist {0}", EncryptionKeyFile);
         return false;
      }

      if (UseEncryption)
      {
         if (EncryptionKey.Length == 0 && EncryptionKeyFile.Length == 0)
         {
            Console.WriteLine("error: encryption key or encryption key file is required for encryption");
            return false;
         }
      }

      return true;
   }
}