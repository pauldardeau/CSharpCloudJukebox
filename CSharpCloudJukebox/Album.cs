using System.Text.Json.Serialization;

namespace CSharpCloudJukebox;

public class AlbumTrack
{
   [JsonPropertyName("number")]
   public int Number { get; set; }
   [JsonPropertyName("title")]
   public string Title  { get; set; }
   [JsonPropertyName("object")]
   public string ObjectName  { get; set; }
   [JsonPropertyName("length")]
   public string TrackLength  { get; set; }

   public AlbumTrack()
   {
      Number = 0;
      Title = "";
      ObjectName = "";
      TrackLength = "";
   }
}

public class Album
{
   [JsonPropertyName("artist")]
   public string Artist  { get; set; }
   [JsonPropertyName("album")]
   public string AlbumName  { get; set; }
   [JsonPropertyName("album-art")]
   public string AlbumArt  { get; set; }
   [JsonPropertyName("year")]
   public string Year  { get; set; }
   [JsonPropertyName("genre")]
   public List<string> Genres  { get; set; }
   [JsonPropertyName("type")]
   public string AlbumType  { get; set; }
   [JsonPropertyName("wiki")]
   public string Wiki  { get; set; }
   [JsonPropertyName("tracks")]
   public List<AlbumTrack> Tracks { get; set; }

   public Album()
   {
      Artist = "";
      AlbumName = "";
      AlbumArt = "";
      Year = "";
      Genres = new List<string>();
      AlbumType = "";
      Wiki = "";
      Tracks = new List<AlbumTrack>();
   }
}