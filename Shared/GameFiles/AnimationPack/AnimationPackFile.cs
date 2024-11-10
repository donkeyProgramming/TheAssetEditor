using Shared.GameFormats.AnimationPack.AnimPackFileTypes;

namespace Shared.GameFormats.AnimationPack
{
    public class AnimationPackFile
    {
        public string FileName { get; private set; }
        private readonly List<IAnimationPackFile> _files = new();

        public IEnumerable<IAnimationPackFile> Files { get => _files; }

        public AnimationPackFile(string fileName)
        {
            FileName = fileName;
        }

        public void AddFile(IAnimationPackFile file)
        {
            file.Parent = this;
            _files.Add(file);
        }

        public List<IAnimationBinGenericFormat> GetGenericAnimationSets(string? skeletonName = null)
        {
            var sets = _files.Where(x => x is IAnimationBinGenericFormat).Cast<IAnimationBinGenericFormat>();
            if (skeletonName != null)
                sets = sets.Where(x => x.SkeletonName == skeletonName);
            return sets.ToList();
        }
    }
}
