using BuilderAPIData;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.IO.Hashing;

namespace BuilderAPI
{
    internal class File
    {
        public FileRecord FileInfo { get; set; }

        public ulong Hash { get; set; }

        public DateTime LastUpdate { get; set; }

        public File(FileRecord file)
        {
            FileInfo = file;
            LastUpdate = file.LastUpdate;
            Hash = 0;
        }

        public string GetFullPath()
        {
            return Path.Combine([FileInfo.Path, $"{FileInfo.Name}.{FileInfo.Extention}"]);
        }

        public ulong GenerateHash()
        {
            var buffer = System.IO.File.ReadAllBytes(GetFullPath());
            return XxHash3.HashToUInt64(buffer);
        }
    }
}
