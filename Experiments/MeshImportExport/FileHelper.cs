using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace MeshImportExport
{
    internal class FileHelper
    {
        private readonly PackFileContainer _container;

        public FileHelper(string packFilePath)
        {

            using var fileStream = File.OpenRead(packFilePath);
            using var reader = new BinaryReader(fileStream, Encoding.ASCII);
            _container = PackFileSerializer.Load(packFilePath, reader, null, false, new CaPackDuplicatePackFileResolver());
        }

        public PackFile FindFile(string path)
        {
            var p = path.Replace("/", "\\");

            return _container.FileList
             .Where(x => x.Key.ToLower().Contains(p))
             .Select(x => x.Value)
             .First();
        }

    }
}
