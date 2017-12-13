using System.Threading.Tasks;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Kralizek.Extensions.S3.Util;
using System.Linq;
using System;

namespace Amazon.S3
{
    public static class S3ClientExtensions
    {
        public static async Task<SyncFolderResponse> SyncFolderAsync(this IAmazonS3 client, SyncFolderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var localFiles = GetAllLocalItemFiles(request.DirectoryToSync);

            var remoteFiles = await GetAllRemoteItemFiles(request.BucketName, request.KeyPrefix);

            var filesToAdd = localFiles.Except(remoteFiles, ItemFileEqualityComparer.Default);
            var addedFiles = await AddFilesAsync(filesToAdd);

            var filesToRemove = remoteFiles.Except(localFiles, ItemFileEqualityComparer.Default);
            var removedFiles = await RemoveFilesAsync(filesToRemove);

            var filesToCompare = Enumerable.Join(localFiles, remoteFiles, l => l.RelativePath, r => r.RelativePath, (l, r) => new FileToCompare(l,r), StringComparer.Ordinal);
            var updatedFiles = await HandleFilesToCompareAsync(filesToCompare);

            return new SyncFolderResponse
            {
                BucketName = request.BucketName,
                AddedKeys = addedFiles,
                DeletedKeys = removedFiles
            };

            async Task<IReadOnlyList<string>> HandleFilesToCompareAsync(IEnumerable<FileToCompare> files)
            {
                throw new NotImplementedException();
            }

            async Task<IReadOnlyList<string>> AddFilesAsync(IEnumerable<ItemFile> files)
            {
                List<string> keys = new List<string>();

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string key = $"{request.KeyPrefix}/{file.RelativePath}";

                    await client.UploadObjectFromFilePathAsync(request.BucketName, key, file.FullPath, null, cancellationToken);

                    keys.Add(key);
                }

                return keys;
            }

            async Task<IReadOnlyList<string>> RemoveFilesAsync(IEnumerable<ItemFile> files)
            {
                List<string> keys = new List<string>();

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await client.DeleteObjectAsync(request.BucketName, file.FullPath, cancellationToken);

                    keys.Add(file.FullPath);
                }

                return keys;
            }

            string GetFileRelativePath(FileInfo file, DirectoryInfo root)
            {
                return file.FullName.Substring(root.FullName.Length + 1);
            }

            string GetS3ObjectRelativePath(string key, string keyPrefix)
            {
                return key.Substring(keyPrefix.Length + 1);
            }

            ISet<ItemFile> GetAllLocalItemFiles(DirectoryInfo directory)
            {
                var files = from localFile in GetAllLocalFiles(request.DirectoryToSync)
                            let relativePath = GetFileRelativePath(localFile, request.DirectoryToSync)
                            let fixedRelativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/')
                            select new ItemFile
                            {
                                RelativePath = fixedRelativePath,
                                FullPath = localFile.FullName
                            };

                var result = new HashSet<ItemFile>(ItemFileEqualityComparer.Default);

                foreach (var file in files)
                {
                    result.Add(file);
                }

                return result;
            }

            IReadOnlyList<FileInfo> GetAllLocalFiles(DirectoryInfo directory)
            {
                return directory.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
            }

            async Task<ISet<ItemFile>> GetAllRemoteItemFiles(string bucketName, string keyPrefix)
            {
                var files = from rf in await client.ListAllObjectsAsync(bucketName, keyPrefix, cancellationToken)
                            let relativePath = GetS3ObjectRelativePath(rf.Key, keyPrefix)
                            select new ItemFile
                            {
                                FullPath = rf.Key,
                                RelativePath = relativePath,
                                ETag = rf.ETag
                            };

                var result = new HashSet<ItemFile>(ItemFileEqualityComparer.Default);

                foreach (var file in files)
                {
                    result.Add(file);
                }

                return result;
            }
        }

        private struct FileToCompare
        {
            public FileToCompare(ItemFile local, ItemFile remote)
            {
                RelativePath = local.RelativePath;
                LocalFullPath = local.FullPath;
                S3ObjectKey = remote.FullPath;
                S3ETag = remote.ETag;
            }

            public string RelativePath { get; set; }

            public string LocalFullPath { get; set; }

            public string S3ObjectKey { get; set; }

            public string S3ETag { get; set; }
        }

        public static async Task<IReadOnlyList<S3Object>> ListAllObjectsAsync(this IAmazonS3 client, string bucketName, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var results = new List<S3Object>();

            ListObjectsV2Response res;
            string nextToken = null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                res = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = keyPrefix,
                    ContinuationToken = nextToken
                }, cancellationToken);

                results.AddRange(res.S3Objects);

                nextToken = res.NextContinuationToken;

            } while (res.IsTruncated);

            return results;
        }
    }
}