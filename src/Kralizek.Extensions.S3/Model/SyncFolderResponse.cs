
using System;
using System.Collections.Generic;

namespace Amazon.S3.Model
{
    public class SyncFolderResponse
    {
        public string BucketName { get; set; }

        public IReadOnlyList<string> AddedKeys { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ModifiedKeys { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> DeletedKeys { get; set; } = Array.Empty<string>();
    }
}