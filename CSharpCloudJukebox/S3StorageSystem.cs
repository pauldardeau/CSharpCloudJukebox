namespace CSharpCloudJukebox;

using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

public class S3StorageSystem : StorageSystem
{
   private readonly bool _debugMode;
   private readonly string _awsAccessKey;
   private readonly string _awsSecretKey;
   private AmazonS3Client? _conn;

   public S3StorageSystem(string awsAccessKey,
                          string awsSecretKey,
                          string containerPrefix,
                          bool debugMode=true) :
      base("S3", debugMode)
   {
      _debugMode = debugMode;
      _awsAccessKey = awsAccessKey;
      _awsSecretKey = awsSecretKey;
      _conn = null;
      if (debugMode)
      {
         Console.WriteLine("Using access_key={0}, secret_key={1}", awsAccessKey, awsSecretKey);
      }
      if (containerPrefix.Length > 0)
      {
         if (debugMode)
         {
            Console.WriteLine("using containerPrefix={0}", containerPrefix);
         }
         ContainerPrefix = containerPrefix;
      }
   }

   public override bool Enter()
   {
      if (_debugMode)
      {
         Console.WriteLine("S3StorageSystem.enter");
         Console.WriteLine("attempting to connect to S3");
      }

      AmazonS3Config config = new AmazonS3Config();
      config.ServiceURL = "https://s3.us-central-1.wasabisys.com";

      _conn = new AmazonS3Client(_awsAccessKey,
                                 _awsSecretKey,
                                 config);
      Authenticated = true;
      ListContainers = ListAccountContainers();
      return true;
   }

   public override void Exit()
   {
      if (_debugMode)
      {
         Console.WriteLine("S3StorageSystem.exit");
      }

      if (_conn != null)
      {
         if (_debugMode)
         {
            Console.WriteLine("closing S3 connection object");
         }

         Authenticated = false;
         ListContainers.Clear();
         _conn.Dispose();
         _conn = null;
      }
   }

   private void LogApiCall(HttpStatusCode code, string apiCall)
   {
      if (!_debugMode)
      {
         return;
      }
      
      int statusCode = 0;
      string statusText = "";

      switch (code)
      {
         case HttpStatusCode.Accepted:
            statusCode = 202;
            statusText = "Accepted";
            break;
         case HttpStatusCode.AlreadyReported:
            statusCode = 208;
            statusText = "AlreadyReported";
            break;
         case HttpStatusCode.Ambiguous:
            statusCode = 300;
            statusText = "Ambiguous/MultipleChoices";
            break;
         case HttpStatusCode.BadGateway:
            statusCode = 502;
            statusText = "BadGateway";
            break;
         case HttpStatusCode.BadRequest:
            statusCode = 400;
            statusText = "BadRequest";
            break;
         case HttpStatusCode.Conflict:
            statusCode = 409;
            statusText = "Conflict";
            break;
         case HttpStatusCode.Continue:
            statusCode = 100;
            statusText = "Continue";
            break;
         case HttpStatusCode.Created:
            statusCode = 201;
            statusText = "Created";
            break;
         case HttpStatusCode.EarlyHints:
            statusCode = 103;
            statusText = "EarlyHints";
            break;
         case HttpStatusCode.ExpectationFailed:
            statusCode = 417;
            statusText = "ExpectationFailed";
            break;
         case HttpStatusCode.FailedDependency:
            statusCode = 424;
            statusText = "FailedDependency";
            break;
         case HttpStatusCode.Forbidden:
            statusCode = 403;
            statusText = "Forbidden";
            break;
         case HttpStatusCode.Found:
            statusCode = 302;
            statusText = "Found/Redirect";
            break;
         case HttpStatusCode.GatewayTimeout:
            statusCode = 504;
            statusText = "GatewayTimeout";
            break;
         case HttpStatusCode.Gone:
            statusCode = 410;
            statusText = "Gone";
            break;
         case HttpStatusCode.HttpVersionNotSupported:
            statusCode = 505;
            statusText = "HttpVersionNotSupported";
            break;
         case HttpStatusCode.IMUsed:
            statusCode = 226;
            statusText = "IMUsed";
            break;
         case HttpStatusCode.InsufficientStorage:
            statusCode = 507;
            statusText = "InsufficientStorage";
            break;
         case HttpStatusCode.InternalServerError:
            statusCode = 500;
            statusText = "InternalServerError";
            break;
         case HttpStatusCode.LengthRequired:
            statusCode = 411;
            statusText = "LengthRequired";
            break;
         case HttpStatusCode.Locked:
            statusCode = 423;
            statusText = "Locked";
            break;
         case HttpStatusCode.LoopDetected:
            statusCode = 508;
            statusText = "LoopDetected";
            break;
         case HttpStatusCode.MethodNotAllowed:
            statusCode = 405;
            statusText = "MethodNotAllowed";
            break;
         case HttpStatusCode.MisdirectedRequest:
            statusCode = 421;
            statusText = "MisdirectedRequest";
            break;
         case HttpStatusCode.Moved:
            statusCode = 301;
            statusText = "Moved/MovedPermanently";
            break;
         case HttpStatusCode.MultiStatus:
            statusCode = 207;
            statusText = "MultiStatus";
            break;
         case HttpStatusCode.NetworkAuthenticationRequired:
            statusCode = 511;
            statusText = "NetworkAuthenticationRequired";
            break;
         case HttpStatusCode.NoContent:
            statusCode = 204;
            statusText = "NoContent";
            break;
         case HttpStatusCode.NonAuthoritativeInformation:
            statusCode = 203;
            statusText = "NonAuthoritativeInformation";
            break;
         case HttpStatusCode.NotAcceptable:
            statusCode = 406;
            statusText = "NotAcceptable";
            break;
         case HttpStatusCode.NotExtended:
            statusCode = 510;
            statusText = "NotExtended";
            break;
         case HttpStatusCode.NotFound:
            statusCode = 404;
            statusText = "NotFound";
            break;
         case HttpStatusCode.NotImplemented:
            statusCode = 501;
            statusText = "NotImplemented";
            break;
         case HttpStatusCode.NotModified:
            statusCode = 304;
            statusText = "NotModified";
            break;
         case HttpStatusCode.OK:
            statusCode = 200;
            statusText = "OK";
            break;
         case HttpStatusCode.PartialContent:
            statusCode = 206;
            statusText = "PartialContent";
            break;
         case HttpStatusCode.PaymentRequired:
            statusCode = 402;
            statusText = "PaymentRequired";
            break;
         case HttpStatusCode.PermanentRedirect:
            statusCode = 308;
            statusText = "PermanentRedirect";
            break;
         case HttpStatusCode.PreconditionFailed:
            statusCode = 412;
            statusText = "PreconditionFailed";
            break;
         case HttpStatusCode.PreconditionRequired:
            statusCode = 428;
            statusText = "PreconditionRequired";
            break;
         case HttpStatusCode.Processing:
            statusCode = 102;
            statusText = "Processing";
            break;
         case HttpStatusCode.ProxyAuthenticationRequired:
            statusCode = 407;
            statusText = "ProxyAuthenticationRequired";
            break;
         case HttpStatusCode.RedirectKeepVerb:
            statusCode = 307;
            statusText = "RedirectKeepVerb/TemporaryRedirect";
            break;
         case HttpStatusCode.RedirectMethod:
            statusCode = 303;
            statusText = "RedirectMethod/SeeOther";
            break;
         case HttpStatusCode.RequestedRangeNotSatisfiable:
            statusCode = 416;
            statusText = "RequestedRangeNotSatisfiable";
            break;
         case HttpStatusCode.RequestEntityTooLarge:
            statusCode = 413;
            statusText = "RequestEntityTooLarge";
            break;
         case HttpStatusCode.RequestHeaderFieldsTooLarge:
            statusCode = 431;
            statusText = "RequestHeaderFieldsTooLarge";
            break;
         case HttpStatusCode.RequestTimeout:
            statusCode = 408;
            statusText = "RequestTimeout";
            break;
         case HttpStatusCode.RequestUriTooLong:
            statusCode = 414;
            statusText = "RequestUriTooLong";
            break;
         case HttpStatusCode.ResetContent:
            statusCode = 205;
            statusText = "ResetContent";
            break;
         case HttpStatusCode.ServiceUnavailable:
            statusCode = 503;
            statusText = "ServiceUnavailable";
            break;
         case HttpStatusCode.SwitchingProtocols:
            statusCode = 101;
            statusText = "SwitchingProtocols";
            break;
         case HttpStatusCode.TooManyRequests:
            statusCode = 429;
            statusText = "TooManyRequests";
            break;
         case HttpStatusCode.Unauthorized:
            statusCode = 401;
            statusText = "Unauthorized";
            break;
         case HttpStatusCode.UnavailableForLegalReasons:
            statusCode = 451;
            statusText = "UnavailableForLegalReasons";
            break;
         case HttpStatusCode.UnprocessableEntity:
            statusCode = 422;
            statusText = "UnprocessableEntity";
            break;
         case HttpStatusCode.UnsupportedMediaType:
            statusCode = 415;
            statusText = "UnsupportedMediaType";
            break;
         case HttpStatusCode.Unused:
            statusCode = 306;
            statusText = "Unused";
            break;
         case HttpStatusCode.UpgradeRequired:
            statusCode = 426;
            statusText = "UpgradeRequired";
            break;
         case HttpStatusCode.UseProxy:
            statusCode = 305;
            statusText = "UseProxy";
            break;
         case HttpStatusCode.VariantAlsoNegotiates:
            statusCode = 506;
            statusText = "VariantAlsoNegotiates";
            break;
      }

      if (statusCode > 0 && statusText.Length > 0)
      {
         Console.WriteLine("S3 {0} {1} - {2}", statusCode, statusText, apiCall);
      }
   }

   private static ListBucketsResponse DoListAccountContainers(IAmazonS3 client)
   {
      var task = client.ListBucketsAsync();
      task.Wait();
      return task.Result;
   }

   public override List<string> ListAccountContainers()
   {
      if (_debugMode)
      {
         Console.WriteLine("list_account_containers");
      }

      List<string> listContainerNames = new List<string>();

      if (_conn != null)
      {
         var listResponse = DoListAccountContainers(_conn);
         LogApiCall(listResponse.HttpStatusCode, "ListBuckets");

         if (listResponse.HttpStatusCode == HttpStatusCode.OK)
         {
            foreach (S3Bucket bucket in listResponse.Buckets)
            {
               listContainerNames.Add(UnPrefixedContainer(bucket.BucketName));
            }
         }
      }

      return listContainerNames;
   }

   private static PutBucketResponse DoCreateContainer(IAmazonS3 client,
                                                      PutBucketRequest request)
   {
      var task = client.PutBucketAsync(request);
      task.Wait();
      return task.Result;
   }

   public override bool CreateContainer(string containerName)
   {
      if (_debugMode)
      {
         Console.WriteLine("create_container: {0}", containerName);
      }

      bool containerCreated = false;
      if (_conn != null)
      {
         try
         {
            PutBucketRequest request = new PutBucketRequest();
            request.BucketName = containerName;
            var putBucketResponse = DoCreateContainer(_conn, request);
            LogApiCall(putBucketResponse.HttpStatusCode, "PutBucket: " + containerName);
            if (putBucketResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               AddContainer(containerName);
               containerCreated = true;
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("PutBucket exception: " + e);
         }
      }

      return containerCreated;
   }

   private static DeleteBucketResponse DoDeleteContainer(IAmazonS3 client,
                                                         DeleteBucketRequest request)
   {
      var task = client.DeleteBucketAsync(request);
      task.Wait();
      return task.Result;
   }
 
   public override bool DeleteContainer(string containerName)
   {
      if (_debugMode)
      {
         Console.WriteLine("delete_container: {0}", containerName);
      }

      bool containerDeleted = false;
      if (_conn != null)
      {
         try
         {
            string bucketName = PrefixedContainer(containerName);
            DeleteBucketRequest request = new DeleteBucketRequest();
            request.BucketName = bucketName;
            var deleteResponse = DoDeleteContainer(_conn, request);
            LogApiCall(deleteResponse.HttpStatusCode, "DeleteBucket: " + bucketName);

            if (deleteResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               RemoveContainer(containerName);
               containerDeleted = true;
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("DeleteBucket exception: " + e);
         }
      }

      return containerDeleted;
   }

   private static ListObjectsResponse DoListContainerContents(IAmazonS3 client,
                                                              ListObjectsRequest request)
   {
      var task = client.ListObjectsAsync(request);
      task.Wait();
      return task.Result;
   }

   public override List<string> ListContainerContents(string containerName)
   {
      if (_debugMode)
      {
         Console.WriteLine("list_container_contents: {0}", containerName);
      }

      List<string> listContents = new List<string>();

      if (_conn != null)
      {
         try
         {
            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = containerName;
            var listResponse = DoListContainerContents(_conn, request);
            LogApiCall(listResponse.HttpStatusCode, "ListObjects: " + containerName);
            if (listResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               foreach (S3Object obj in listResponse.S3Objects)
               {
                  listContents.Add(obj.Key);
               }
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("ListObjects exception: {0}", e);
         }
      }

      return listContents;
   }

   private static GetObjectMetadataResponse DoGetObjectMetadata(IAmazonS3 client,
                                                                string containerName,
                                                                string objectName)
   {
      var task = client.GetObjectMetadataAsync(containerName, objectName);
      task.Wait();
      return task.Result;
   }

   public override PropertySet? GetObjectMetadata(string containerName, string objectName)
   {
      if (_debugMode)
      {
         Console.WriteLine("GetObjectMetadata: container={0}, object={1}", containerName, objectName);
      }

      if (_conn != null && containerName.Length > 0 && objectName.Length > 0)
      {
         try
         {
            var getObjectMetadataResponse = DoGetObjectMetadata(_conn, containerName, objectName);
            LogApiCall(getObjectMetadataResponse.HttpStatusCode, "GetObjectMetadata: " + containerName + ":" + objectName);

            if (getObjectMetadataResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               PropertySet props = new PropertySet();
               foreach (var headerKey in getObjectMetadataResponse.Headers.Keys)
               {
                  string headerValue = getObjectMetadataResponse.Headers[headerKey];
                  if (headerValue != null)
                  {
                     props.Add(headerKey, PropertyValue.StringPropertyValue(headerValue));
                  }
               }
               return props;
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("GetObjectMetadata exception: " + e);
         }
      }

      return null;
   }

   private static PutObjectResponse DoPutObject(IAmazonS3 client,
                                                PutObjectRequest request)
   {
      var task = client.PutObjectAsync(request);
      task.Wait();
      return task.Result;
   }

   public override bool PutObject(string containerName,
                                  string objectName,
                                  byte[] fileContents,
                                  PropertySet? props)
   {
      bool objectAdded = false;

      if (_conn != null && containerName.Length > 0 && objectName.Length > 0)
      {
         try
         {
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = containerName;
            request.Key = objectName;
            request.InputStream = new MemoryStream(fileContents);
            //request.Headers = ;  //TODO: (2) add headers (PutObject)

            /*
            if (headers != null)
            {
               if (headers.ContainsKey("ContentType"))
               {
                  string contentType = (string) headers["ContentType"];
                  if (contentType != null && contentType.Length > 0)
                  {
                     request.ContentType = contentType;
                  }
               }
            }
            */
            
            var putObjectResponse = DoPutObject(_conn, request);
            LogApiCall(putObjectResponse.HttpStatusCode, "PutObject: " + containerName + ":" + objectName);
            if (putObjectResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               objectAdded = true;
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("PutObject exception: " + e);
         }
      }

      return objectAdded;
   }

   private static DeleteObjectResponse DoDeleteObject(IAmazonS3 client,
                                                      DeleteObjectRequest request)
   {
      var task = client.DeleteObjectAsync(request);
      task.Wait();
      return task.Result;
   }

   public override bool DeleteObject(string containerName, string objectName)
   {
      if (_debugMode)
      {
         Console.WriteLine("delete_object: container={0}, object={1}", containerName, objectName);
      }

      bool objectDeleted = false;

      if (_conn != null && containerName.Length > 0 && objectName.Length > 0)
      {
         try
         {
            DeleteObjectRequest request = new DeleteObjectRequest();
            request.BucketName = containerName;
            request.Key = objectName;
            var deleteObjectResponse = DoDeleteObject(_conn, request);
            LogApiCall(deleteObjectResponse.HttpStatusCode, "DeleteObject: " + containerName + ":" + objectName);
            objectDeleted = true;
         }
         catch (Exception e)
         {
            Console.WriteLine("DeleteObject exception: " + e);
         }
      }

      return objectDeleted;
   }

   private static GetObjectResponse DoGetObject(IAmazonS3 client,
                                                GetObjectRequest request)
   {
      var task = client.GetObjectAsync(request);
      task.Wait();
      return task.Result;
   }

   public override long GetObject(string containerName, string objectName, string localFilePath)
   {
      if (_debugMode)
      {
         Console.WriteLine("get_object: container={0}, object={1}, local_file_path={2}", containerName,
                                                                                         objectName,
                                                                                         localFilePath);
      }

      long bytesRetrieved = 0;

      if (_conn != null && containerName.Length > 0 && objectName.Length > 0 && localFilePath.Length > 0)
      {
         try
         {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = containerName;
            request.Key = objectName;
            var getObjectResponse = DoGetObject(_conn, request);
            LogApiCall(getObjectResponse.HttpStatusCode, "GetObject: " + containerName + ":" + objectName);
            if (getObjectResponse.HttpStatusCode == HttpStatusCode.OK)
            {
               // AWS SDK BUG: according to Amazon's docs, the following should
               // work, but it's a compilation error.
               //getObjectResponse.WriteResponseStreamToFile(local_file_path);

               // Workaround: we'll do the same thing ourselves
               using(Stream outStream = File.OpenWrite(localFilePath))
               {
                  getObjectResponse.ResponseStream.CopyTo(outStream);
               }

               if (Utils.PathExists(localFilePath))
               {
                  bytesRetrieved = Utils.GetFileSize(localFilePath);
               }
            }
         }
         catch (Exception e)
         {
            Console.WriteLine("GetObject exception: " + e);
         }
      }

      return bytesRetrieved;
   }   
}