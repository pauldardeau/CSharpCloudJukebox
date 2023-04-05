namespace CSharpCloudJukebox;

public class SongDownloader
{
   private readonly Jukebox _jukebox;
   private readonly List<SongMetadata> _listSongs;
   private readonly bool _debugPrint;

   public SongDownloader(Jukebox jb, List<SongMetadata> listSongs, bool debugPrint=false)
   {
      _jukebox = jb;
      _listSongs = listSongs;
      _debugPrint = debugPrint;
   }

   public void Run()
   {
      if (_debugPrint)
      {
         Console.WriteLine("SongDownloader.Run");
      }

      if (_listSongs.Count > 0)
      {
         if (_debugPrint)
         {
            Console.WriteLine("SongDownloader.Run: calling BatchDownloadStart");
         }
         _jukebox.BatchDownloadStart();

         foreach (var song in _listSongs)
         {
            if (_jukebox.ExitRequested)
            {
               if (_debugPrint)
               {
                  Console.WriteLine("SongDownloader.Run: jukebox.ExitRequested is true");
               }
               break;
            }
            else
            {
               if (_debugPrint)
               {
                  Console.WriteLine("downloading song");
               }
               _jukebox.DownloadSong(song);
            }
         }

         if (_debugPrint)
         {
            Console.WriteLine("SongDownloader.Run: call BatchDownloadComplete");
         }
         _jukebox.BatchDownloadComplete();
      }
      else
      {
         Console.WriteLine("SongDownloader.Run: listSongs is empty");
      }
   }  
}