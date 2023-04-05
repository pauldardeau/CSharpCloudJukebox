using System.Text.Json.Serialization;

namespace CSharpCloudJukebox;

public class PlaylistSong
{
   [JsonPropertyName("artist")]
   public string Artist { get; set; }
   [JsonPropertyName("album")]
   public string Album { get; set; }
   [JsonPropertyName("song")]
   public string Song { get; set; }

   public PlaylistSong()
   {
      Artist = "";
      Album = "";
      Song = "";
   }
}

public class Playlist
{
   [JsonPropertyName("name")]
   public string Name { get; set; }
   [JsonPropertyName("tags")]
   public string Tags { get; set; }
   [JsonPropertyName("songs")]
   public List<PlaylistSong> Songs { get; set; }

   public Playlist()
   {
      Name = "";
      Tags = "";
      Songs = new List<PlaylistSong>();
   }
}