using System;
using System.Collections.Generic;
using System.Text;

namespace Kralizek.Extensions.S3.Util
{
    public class ItemFile
    {
        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        public string ETag { get; set; }
    }

    public class ItemFileEqualityComparer : IEqualityComparer<ItemFile>
    {
        public static readonly IEqualityComparer<ItemFile> Default = new ItemFileEqualityComparer();

        private ItemFileEqualityComparer() { }

        public bool Equals(ItemFile x, ItemFile y)
        {
            if (x == null && y == null) return true;

            if (x == null ^ y == null) return false;

            return string.Equals(x.RelativePath, y.RelativePath);
        }

        public int GetHashCode(ItemFile obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return typeof(ItemFile).FullName.GetHashCode() ^ obj.RelativePath.GetHashCode();
        }
    }
}
