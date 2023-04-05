namespace CSharpCloudJukebox;

public class SongDeleter
{
   private readonly string _songFilePath;
   
   public SongDeleter(string songFilePath)
   {
      _songFilePath = songFilePath;
   }

   public void Run()
   {
      Thread.Sleep(2000);
      if (File.Exists(_songFilePath))
      {
         try
         {
            File.Delete(_songFilePath);
         }
         catch (Exception)
         {
         }
      }
   }
}