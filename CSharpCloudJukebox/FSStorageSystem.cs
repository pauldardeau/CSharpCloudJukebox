namespace CSharpCloudJukebox;

public class FsStorageSystem : StorageSystem
{
   private readonly string _rootDir;
   private List<string> _listContainerNames;


   public FsStorageSystem(string theRootDir, bool debugMode = false) : 
      base("FS", debugMode)
   {
      _rootDir = theRootDir;
      _listContainerNames = new List<string>();
   }

   public override bool Enter()
   {
      if (!Directory.Exists(_rootDir))
      {
         Directory.CreateDirectory(_rootDir);
      }
      return Directory.Exists(_rootDir);
   }

   public override void Exit()
   {
   }

   public override List<string> ListAccountContainers()
   {
      List<string> listSubdirs = new List<string>();
      string[] subdirs = Directory.GetDirectories(_rootDir);
      foreach (var subdir in subdirs)
      {
         listSubdirs.Add(subdir);
      }
      return listSubdirs;
   }

   public override bool CreateContainer(string containerName)
   {
      bool containerCreated;
      if (!HasContainer(containerName))
      {
         string containerDir = Path.Join(_rootDir, containerName);
         try
         {
            Directory.CreateDirectory(containerDir);
            containerCreated = true;
         }
         catch (Exception)
         {
            containerCreated = false;
         }
         if (containerCreated)
         {
            if (DebugMode)
            {
               Console.WriteLine("container created: '{0}'", containerName);
            }
            _listContainerNames.Add(containerName);
         }
      }
      else
      {
         containerCreated = false;
      }
      return containerCreated;
   }

   public override bool DeleteContainer(string containerName)
   {
      bool containerDeleted;
      string containerDir = Path.Join(_rootDir, containerName);
      try
      {
         Directory.Delete(containerDir);
         containerDeleted = true;
      }
      catch (Exception)
      {
         containerDeleted = false;
      }

      if (containerDeleted)
      {
         if (DebugMode)
         {
            Console.WriteLine("container deleted: '{0}'", containerName);
         }

         _listContainerNames.Remove(containerName);
      }
      return containerDeleted;
   }

   public override List<string> ListContainerContents(string containerName)
   {
      List<string> listContents = new List<string>();
      string containerDir = Path.Join(_rootDir, containerName);
      if (Directory.Exists(containerDir))
      {
         string[] dirFiles = Directory.GetFiles(containerDir);
         foreach (var dirFile in dirFiles)
         {
            listContents.Add(dirFile);
         }
      }
      return listContents;
   }

   public override PropertySet? GetObjectMetadata(string containerName,
                                                  string objectName)
   {
      if (containerName.Length > 0 && objectName.Length > 0)
      {
         string containerDir = Path.Join(_rootDir, containerName);
         if (Directory.Exists(containerDir))
         {
            string objectPath = Path.Join(containerDir, objectName);
            string metaPath = objectPath + ".meta";
            if (File.Exists(metaPath))
            {
               PropertySet ps = new PropertySet();
               ps.ReadFromFile(metaPath);
               return ps;
            }
         }
      }
      return null;
   }

   public override bool PutObject(string containerName,
                                  string objectName,
                                  byte[] fileContents,
                                  PropertySet? props)
   {
      bool objectAdded = false;

      if (containerName.Length > 0 &&
          objectName.Length > 0 &&
          fileContents.Length > 0)
      {
         string containerDir = Path.Join(_rootDir, containerName);
         if (Directory.Exists(containerDir))
         {
            string objectPath = Path.Join(containerDir, objectName);
            File.WriteAllBytes(objectPath, fileContents);
            objectAdded = true;
            if (objectAdded)
            {
               if (DebugMode)
               {
                  Console.WriteLine("object added: {0}/{1}",
                                    containerName,
                                    objectName);
               }
               if (props != null && props.Count() > 0)
               {
                  string metaPath = objectPath + ".meta";
                  props.WriteToFile(metaPath);
               }
            }
            else
            {
               Console.WriteLine("File.WriteAllBytes failed to write object contents, put failed");
            }
         }
         else
         {
            if (DebugMode)
            {
               Console.WriteLine("container doesn't exist, can't put object");
            }
         }
      }
      else
      {
         if (DebugMode)
         {
            if (containerName.Length == 0)
            {
               Console.WriteLine("container name is missing, can't put object");
            }
            else
            {
               if (objectName.Length == 0)
               {
                  Console.WriteLine("object name is missing, can't put object");
               }
               else
               {
                  if (fileContents.Length == 0)
                  {
                     Console.WriteLine("object content is empty, can't put object");
                  }
               }
            }
         }
      }
      return objectAdded;
   }

   public override bool DeleteObject(string containerName,
                                     string objectName)
   {
      bool objectDeleted = false;
      if (containerName.Length > 0 && objectName.Length > 0)
      {
         string containerDir = Path.Join(_rootDir, containerName);
         string objectPath = Path.Join(containerDir, objectName);
         if (File.Exists(objectPath))
         {
            File.Delete(objectPath);
            objectDeleted = true;
            if (objectDeleted)
            {
               if (DebugMode)
               {
                  Console.WriteLine("object deleted: {0}/{1}",
                                    containerName,
                                    objectName);
               }
               string metaPath = objectPath + ".meta";
               if (File.Exists(metaPath))
               {
                  File.Delete(metaPath);
               }
            }
            else
            {
               if (DebugMode)
               {
                  Console.WriteLine("delete of object file failed");
               }
            }
         }
         else
         {
            if (DebugMode)
            {
               Console.WriteLine("cannot delete object, path doesn't exist");
            }
         }
      }
      else
      {
         if (DebugMode)
         {
            Console.WriteLine("cannot delete object, container name or object name is missing");
         }
      }
      return objectDeleted;
   }

   public override long GetObject(string containerName,
                                  string objectName,
                                  string localFilePath)
   {
      long bytesRetrieved = 0;
      if (containerName.Length > 0 &&
          objectName.Length > 0 &&
          localFilePath.Length > 0)
      {
         string containerDir = Path.Join(_rootDir, containerName);
         string objectPath = Path.Join(containerDir, objectName);
         if (File.Exists(objectPath))
         {
            byte[] objFileContents = File.ReadAllBytes(objectPath);
            if (objFileContents.Length > 0)
            {
               File.WriteAllBytes(localFilePath, objFileContents);
               bytesRetrieved = objFileContents.Length;
            }
         }
      }
      return bytesRetrieved;
   }   
}