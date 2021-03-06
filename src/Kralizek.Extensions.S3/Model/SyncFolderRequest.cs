using System.IO;

namespace Amazon.S3.Model
{
    public class SyncFolderRequest
    {
        public DirectoryInfo DirectoryToSync { get; set; }

        public string BucketName { get; set; }

        public string KeyPrefix { get; set; }

        public bool AllowDeletes { get; set; } = true;
    }
}