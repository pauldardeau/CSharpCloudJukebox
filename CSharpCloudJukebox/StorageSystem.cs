namespace CSharpCloudJukebox;

public abstract class StorageSystem
{
   protected readonly bool DebugMode;
   protected bool Authenticated;
   public bool EncryptFiles;
   public List<string> ListContainers;
   public string ContainerPrefix;
   public string MetadataPrefix;
   private readonly string _storageSystemType;

   public StorageSystem(string storageSystemType, bool debugMode = false)
   {
      DebugMode = debugMode;
      Authenticated = false;
      EncryptFiles = false;
      ListContainers = new List<string>();
      ContainerPrefix = "";
      MetadataPrefix = "";
      _storageSystemType = storageSystemType;
   }

   public string UnPrefixedContainer(string containerName)
   {
      if (ContainerPrefix.Length > 0 && containerName.Length > 0)
      {
         if (containerName.StartsWith(ContainerPrefix))
         {
            return containerName.Substring(ContainerPrefix.Length);
         }
      }
      return containerName;
   }

   public string PrefixedContainer(string containerName)
   {
      return ContainerPrefix + containerName;
   }

   public bool HasContainer(string containerName)
   {
      return ListContainers.Contains(containerName);
   }

   public void AddContainer(string containerName)
   {
      ListContainers.Add(containerName);
   }

   public void RemoveContainer(string containerName)
   {
      if (ListContainers.Contains(containerName))
      {
         ListContainers.Remove(containerName);
      }
   }

   public long RetrieveFile(FileMetadata fm, string localDirectory)
   {
      if (localDirectory.Length > 0)
      {
         string filePath = Path.Join(localDirectory, fm.FileUid);
         if (DebugMode)
         {
            Console.WriteLine("retrieving container={0}", fm.ContainerName);
            Console.WriteLine("retrieving object={0}", fm.ObjectName);
         }
         return GetObject(fm.ContainerName, fm.ObjectName, filePath);
      }
      return 0;
   }

   public bool StoreFile(FileMetadata fm, byte[] fileContents)
   {
      return PutObject(fm.ContainerName,
                       fm.ObjectName,
                       fileContents,
                       fm.ToPropertySet(MetadataPrefix));
   }

   public bool AddFileFromPath(string containerName, string objectName, string filePath)
   {
      byte[] fileContents = File.ReadAllBytes(filePath);
      if (fileContents.Length > 0)
      {
         return PutObject(containerName, objectName, fileContents, null);
      }
      else
      {
         if (DebugMode)
         {
            Console.WriteLine("error: unable to read file {0}", filePath);
         }

         return false;
      }
   }

   public abstract List<string> ListAccountContainers();

   public abstract bool CreateContainer(string containerName);

   public abstract bool DeleteContainer(string containerName);

   public abstract List<string> ListContainerContents(string containerName);

   public abstract PropertySet? GetObjectMetadata(string containerName, string objectName);

   public abstract bool PutObject(string containerName,
                                  string objectName,
                                  byte[] objectBytes,
                                  PropertySet? props);

   public abstract bool DeleteObject(string containerName, string objectName);

   public abstract long GetObject(string containerName, string objectName, string localFilePath);

   public abstract bool Enter();
   public abstract void Exit();

   public string GetStorageSystemType()
   {
      return _storageSystemType;
   }
}