namespace CSharpCloudJukebox;

using System.Net;
using System.Text.Json;

public class JukeboxInfo
{
   public long NumberSongs { get; set; }
   public string? CurrentArtist { get; set; }
   public string? CurrentAlbum { get; set; }
   public string? CurrentSong { get; set; }
   public string? CurrentObject { get; set; }
   public string? ScopeArtist { get; set; }
   public string? ScopeAlbum { get; set; }
   public string? ScopePlaylist { get; set; }
   public bool IsPaused { get; set; }
   public int SongSecondsOffset { get; set; }
   public string? AlbumArtUrl { get; set; }
}
public class MemoryUsage
{
   public double MemoryUseMegabytes { get; set; }
}

public class HttpServer
{
   private const string EndPointSongAdvance = "/songAdvance/";
   private const string EndPointTogglePausePlay = "/togglePausePlay/";
   private const string EndPointApiInfo = "/api/info/";
   private const string EndPointApiMemoryUsage = "/api/memoryUsage/";
   
   private readonly Jukebox _jukebox;
   
   public HttpServer(Jukebox jukebox)
   {
      _jukebox = jukebox;
   }

   public void Run()
   {
      if (!HttpListener.IsSupported)
      {
         Console.WriteLine("HttpListener is not supported on this platform.");
         return;
      }

      int portNumber = 5309;

      string localHostPrefix = string.Format("http://127.0.0.1:{0}", portNumber);
      string ipHostPrefix = string.Format("http://{0}:{1}", Utils.GetLocalIpAddress(), portNumber);

      string[] prefixes = { localHostPrefix, ipHostPrefix };
      string[] endpoints = { EndPointSongAdvance, EndPointTogglePausePlay, EndPointApiInfo, EndPointApiMemoryUsage };
      
      // URI prefixes are required,
      // for example "http://contoso.com:8080/index/".
      if (prefixes == null || prefixes.Length == 0)
      {
         throw new ArgumentException("prefixes");
      }

      using (var listener = new HttpListener())
      {
         try
         {
            foreach (string s in prefixes)
            {
               foreach (string e in endpoints)
               {
                  string prefix = s + e;
                  listener.Prefixes.Add(prefix);
               }
            }
      
            listener.Start();
            Console.WriteLine("Http server listening...");

            while (!_jukebox.ExitRequested)
            {
               // GetContext blocks while waiting for a request
               HttpListenerContext context = listener.GetContext();
            
               if (_jukebox.ExitRequested)
               {
                  continue;
               }
            
               HttpListenerRequest request = context.Request;
               HttpListenerResponse response = context.Response;

               string responseBody = "Server error";

               string? endpoint = request.RawUrl;
               if (endpoint != null)
               {
                  if (endpoint == EndPointSongAdvance)
                  {
                     _jukebox.AdvanceToNextSong();
                     responseBody = "advanced to next song";
                  }
                  else if (endpoint == EndPointTogglePausePlay)
                  {
                     _jukebox.TogglePausePlay();
                     responseBody = "toggled pause/play";
                  }
                  else if (endpoint == EndPointApiInfo)
                  {
                     JukeboxInfo jukeInfo = new JukeboxInfo();
                     jukeInfo.NumberSongs = _jukebox.GetNumberSongs();
                     jukeInfo.CurrentArtist = _jukebox.CurrentArtist;
                     jukeInfo.CurrentAlbum = _jukebox.CurrentAlbum;
                     jukeInfo.CurrentSong = _jukebox.CurrentSong;
                     jukeInfo.CurrentObject = _jukebox.CurrentObject;
                     jukeInfo.ScopeArtist = _jukebox.ScopeArtist;
                     jukeInfo.ScopeAlbum = _jukebox.ScopeAlbum;
                     jukeInfo.ScopePlaylist = _jukebox.ScopePlaylist;
                     jukeInfo.IsPaused = _jukebox.IsPaused();
                     jukeInfo.SongSecondsOffset = _jukebox.GetSongSecondsOffset();
                     jukeInfo.AlbumArtUrl = _jukebox.AlbumArtUrl;
                     responseBody = JsonSerializer.Serialize(jukeInfo);
                  }
                  else if (endpoint == EndPointApiMemoryUsage)
                  {
                     MemoryUsage memUsage = new MemoryUsage();
                     memUsage.MemoryUseMegabytes = _jukebox.GetMemoryInUse();
                     responseBody = JsonSerializer.Serialize(memUsage);
                  }
                  else
                  {
                     responseBody = "Hello world!";
                  }
               }

               string responseString = string.Format("<HTML><BODY>{0}</BODY></HTML>", responseBody);
               byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
               response.ContentLength64 = buffer.Length;
               Stream output = response.OutputStream;
               try
               {
                  output.Write(buffer,0,buffer.Length);
               }
               finally
               {
                  output.Close();
               }
            }
         }
         finally
         {
            listener.Stop();
            Console.WriteLine("http server ended");
         }
      }
   }
}