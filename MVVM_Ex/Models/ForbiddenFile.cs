using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Ex.Models
{
    internal class ForbiddenFile
    {
        public string PathToFile { get; set; }
        public List<string> ForbiddenWords { get; set; }
        public long SizeInBytes { get; set; }
        public ForbiddenFile(string path)
        {
            PathToFile = path;
            ForbiddenWords = new List<string>();
            SizeInBytes = new FileInfo(path).Length;
        }

        public override string ToString()
        {
            return $"Path: \"{Path.GetFileName(PathToFile)}\", Size: {SizeInBytes/1048576} MB, Replaces count: {ForbiddenWords.Count}";
        }
    }
}
