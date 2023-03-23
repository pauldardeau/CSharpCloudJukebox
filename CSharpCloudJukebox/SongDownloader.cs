namespace CSharpCloudJukebox;

public class SongDownloader
{
   private readonly Jukebox _jukebox;
   private readonly List<SongMetadata> _listSongs;

   public SongDownloader(Jukebox jb, List<SongMetadata> listSongs) {
      _jukebox = jb;
      _listSongs = listSongs;
   }

   public void Run() {
      //Console.WriteLine("SongDownloader.Run");
      if (_listSongs.Count > 0) {
         //Console.WriteLine("SongDownloader.Run: call BatchDownloadStart");
         _jukebox.BatchDownloadStart();

         foreach (var song in _listSongs) {
            if (_jukebox.ExitRequested) {
               Console.WriteLine("SongDownloader.Run: jukebox.ExitRequested is true");
               break;
            } else {
               //Console.WriteLine("downloading song");
               _jukebox.DownloadSong(song);
            }
         }
         //Console.WriteLine("SongDownloader.Run: call BatchDownloadComplete");
         _jukebox.BatchDownloadComplete();
      } else {
         Console.WriteLine("SongDownloader.Run: listSongs is empty");
      }
   }  
}