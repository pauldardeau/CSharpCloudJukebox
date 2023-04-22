// ReSharper disable StringLastIndexOfIsCultureSpecific.1

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CSharpCloudJukebox;

public class Utils
{
   public static string DatetimeDatetimeFromtimestamp(double ts)
   {
      // python datetime.datetime.fromtimestamp

      //TODO: (1) implement (DatetimeDatetimeFromtimestamp)
      return "";
   }

   public static void TimeSleep(int seconds)
   {
      // python time.sleep

      // Time in hours, minutes, seconds
      TimeSpan ts = new TimeSpan(0, 0, seconds);
      Thread.Sleep(ts);
   }

   public static double TimeTime()
   {
      // python time.time

      return DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
   }

   public static void SysStdoutWrite(string s)
   {
      // python sys.stdout.write

      //TODO: (2) implement (SysStdoutWrite)
   }
 
   public static void SysStdoutFlush()
   {
      // python: sys.stdout.flush

      //TODO: (2) implement (SysStdoutFlush)
   }

   public static void SysExit(int exitCode)
   {
      // python: sys.exit
      Environment.Exit(exitCode);
   }

   public static bool PathExists(string path)
   {
      // python: os.path.exists
      return File.Exists(path) || Directory.Exists(path);
   }

   public static bool PathIsFile(string path)
   {
      // python: os.path.isfile
      return File.Exists(path);
   }

   public static bool FileExists(string path)
   {
      return File.Exists(path);
   }

   public static bool DeleteFile(string path)
   {
      bool deleted = false;
      if (FileExists(path))
      {
         try
         {
            File.Delete(path);
            deleted = true;
         }
         catch (Exception)
         {
         }
      }

      return deleted;
   }

   public static (string root, string ext) PathSplitExt(string path)
   {
      // python: os.path.splitext

      // splitext("bar") -> ("bar", "")
      // splitext("foo.bar.exe") -> ("foo.bar", ".exe")
      // splitext("/foo/bar.exe") -> ("/foo/bar", ".exe")
      // splitext(".cshrc") -> (".cshrc", "")
      // splitext("/foo/....jpg") -> ("/foo/....jpg", "")

      string root = "";
      string ext = "";

      if (path.Length > 0)
      {
         int posLastDot = path.LastIndexOf(".");
         if (posLastDot == -1)
         {
            // no '.' exists in path (i.e., "bar")
            root = path;
         }
         else
         {
            // is the last '.' the first character? (i.e., ".cshrc")
            if (posLastDot == 0)
            {
               root = path;
            }
            else
            {
               char preceding = path[posLastDot-1];
               // is the preceding char also a '.'? (i.e., "/foo/....jpg")
               if (preceding == '.')
               {
                  root = path;
               }
               else
               {
                  // splitext("foo.bar.exe") -> ("foo.bar", ".exe") 
                  // splitext("/foo/bar.exe") -> ("/foo/bar", ".exe")
                  root = path.Substring(0, posLastDot);
                  ext = path.Substring(posLastDot);
               }
            }
         }
      }

      return (root, ext);
   }

   public static double PathGetMtime(string path)
   {
      // python: os.path.getmtime

      //SEE: DateTimeOffset.ToUnixTimeSeconds

      DateTime dtModify = File.GetLastWriteTime(path);
      TimeSpan timeSpan = dtModify - new DateTime(1970, 1, 1, 0, 0, 0);
      return timeSpan.TotalSeconds;
   }

   public static int GetPid()
   {
      // python: os.getpid
      return Environment.ProcessId;
   }

   public static long GetFileSize(string pathToFile)
   {
      // python: os.path.getsize
      FileInfo fi = new FileInfo(pathToFile);
      return fi.Length;
   }

   public static bool OsRename(string existingFile, string newFile)
   {
      // python: os.rename
      return RenameFile(existingFile, newFile);
   }

   public static bool RenameFile(string existingFile, string newFile)
   {
      // python: os.rename

      try
      {
         File.Move(existingFile, newFile);
         return true;
      }
      catch (IOException)
      {
         return false;
      }
   }

   public static string Md5ForFile(string pathToFile)
   {
      byte[] fileBytes = File.ReadAllBytes(pathToFile);
      if (fileBytes.Length > 0)
      {
         // Use input string to calculate MD5 hash
         using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
         {
            byte[] hashBytes = md5.ComputeHash(fileBytes);
            return Convert.ToHexString(hashBytes); // .NET 5 +
         }
      }

      return "";
   }
   
   public static string GetPlatformIdentifier()
   {
      string osIdentifier;

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
         osIdentifier = "linux";
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
         osIdentifier = "mac";
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
      {
         osIdentifier = "freebsd";
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
         osIdentifier = "windows";
      }
      else
      {
         osIdentifier = "unknown";
      }

      return osIdentifier;
   }
   
   public static string GetLocalIpAddress()
   {
      var host = Dns.GetHostEntry(Dns.GetHostName());
      foreach (var ip in host.AddressList)
      {
         if (ip.AddressFamily == AddressFamily.InterNetwork)
         {
            return ip.ToString();
         }
      }
      throw new Exception("No network adapters with an IPv4 address in the system!");
   }
}
