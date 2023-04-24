namespace CSharpCloudJukebox;

public sealed class FileMetadata
{
   public readonly string FileUid;
   public readonly string ContainerName;
   public readonly string ObjectName;
   public string FileName;
   public int OriginFileSize;
   public int StoredFileSize;
   public int PadCharCount;
   public string FileTime;
   public string Md5Hash;
   public int Compressed;
   public int Encrypted;

   public FileMetadata(string fileUid, string containerName, string objectName)
   {
      FileUid = fileUid;
      FileName = "";
      OriginFileSize = 0;
      StoredFileSize = 0;
      PadCharCount = 0;
      FileTime = "";
      Md5Hash = "";
      Compressed = 0;
      Encrypted = 0;
      ContainerName = containerName;
      ObjectName = objectName;
   }

   public override bool Equals(object? obj)
   {
      if (obj == null)
      {
         return false;
      }
      
      if (obj is FileMetadata other)
      {
         return FileUid == other.FileUid &&
                FileName == other.FileName &&
                OriginFileSize == other.OriginFileSize &&
                StoredFileSize == other.StoredFileSize &&
                PadCharCount == other.PadCharCount &&
                FileTime == other.FileTime &&
                Md5Hash == other.Md5Hash &&
                Compressed == other.Compressed &&
                Encrypted == other.Encrypted &&
                ContainerName == other.ContainerName &&
                ObjectName == other.ObjectName;
      }
      else
      {
         return false;
      }
   }

   public override int GetHashCode()
   {
      int hash = 17;
      hash = hash * 23 + FileUid.GetHashCode();
      hash = hash * 23 + ContainerName.GetHashCode();
      hash = hash * 23 + ObjectName.GetHashCode();
      return hash;
   }

   public static FileMetadata? FromDictionary(Dictionary<string, object> dictionary,
                                              string prefix="")
   {
      string fileUid;
      string containerName;
      string objectName;
      
      if (dictionary.ContainsKey(prefix + "file_uid"))
      {
         fileUid = (string) dictionary[prefix + "file_uid"];
      }
      else
      {
         return null;
      }
      
      if (dictionary.ContainsKey(prefix + "container_name"))
      {
         containerName = (string) dictionary[prefix + "container_name"];
      }
      else
      {
         return null;
      }
      
      if (dictionary.ContainsKey(prefix + "object_name"))
      {
         objectName = (string) dictionary[prefix + "object_name"];
      }
      else
      {
         return null;
      }

      FileMetadata fm = new FileMetadata(fileUid, containerName, objectName);

      if (dictionary.ContainsKey(prefix + "file_name"))
      {
         fm.FileName = (string) dictionary[prefix + "file_name"];
      }
      if (dictionary.ContainsKey(prefix + "origin_file_size"))
      {
         fm.OriginFileSize = (int) dictionary[prefix + "origin_file_size"];
      }
      if (dictionary.ContainsKey(prefix + "stored_file_size"))
      {
         fm.StoredFileSize = (int) dictionary[prefix + "stored_file_size"];
      }
      if (dictionary.ContainsKey(prefix + "pad_char_count"))
      {
         fm.PadCharCount = (int) dictionary[prefix + "pad_char_count"];
      }
      if (dictionary.ContainsKey(prefix + "file_time"))
      {
         fm.FileTime = (string) dictionary[prefix + "file_time"];
      }
      if (dictionary.ContainsKey(prefix + "md5_hash"))
      {
         fm.Md5Hash = (string) dictionary[prefix + "md5_hash"];
      }
      if (dictionary.ContainsKey(prefix + "compressed"))
      {
         fm.Compressed = (int) dictionary[prefix + "compressed"];
      }
      if (dictionary.ContainsKey(prefix + "encrypted"))
      {
         fm.Encrypted = (int) dictionary[prefix + "encrypted"];
      }

      return fm;
   }

   public Dictionary<string, object> ToDictionary(string prefix = "")
   {
      var d = new Dictionary<string, object>()
      {
         {prefix + "file_uid", FileUid},
         {prefix + "file_name", FileName},
         {prefix + "origin_file_size", OriginFileSize},
         {prefix + "stored_file_size", StoredFileSize},
         {prefix + "pad_char_count", PadCharCount},
         {prefix + "file_time", FileTime},
         {prefix + "md5_hash", Md5Hash},
         {prefix + "compressed", Compressed},
         {prefix + "encrypted", Encrypted},
         {prefix + "container_name", ContainerName},
         {prefix + "object_name", ObjectName}
      };
      return d;
   }

   public PropertySet ToPropertySet(string prefix = "")
   {
      PropertySet props = new PropertySet();
      props.Add(prefix + "file_uid", PropertyValue.StringPropertyValue(FileUid));
      props.Add(prefix + "file_name", PropertyValue.StringPropertyValue(FileName));
      props.Add(prefix + "origin_file_size", PropertyValue.LongPropertyValue(OriginFileSize));
      props.Add(prefix + "stored_file_size", PropertyValue.LongPropertyValue(StoredFileSize));
      props.Add(prefix + "pad_char_count", PropertyValue.LongPropertyValue(PadCharCount));
      props.Add(prefix + "file_time", PropertyValue.StringPropertyValue(FileTime));
      props.Add(prefix + "md5_hash", PropertyValue.StringPropertyValue(Md5Hash));
      props.Add(prefix + "compressed", PropertyValue.IntPropertyValue(Compressed));
      props.Add(prefix + "encrypted", PropertyValue.IntPropertyValue(Encrypted));
      props.Add(prefix + "container_name", PropertyValue.StringPropertyValue(ContainerName));
      props.Add(prefix + "object_name", PropertyValue.StringPropertyValue(ObjectName));
      return props;
   }   
}