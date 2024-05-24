using Shared.GameFormats.AnimationPack.AnimPackFileTypes;

namespace Shared.GameFormats.AnimationPack
{

    public interface IAnimationBinGenericFormat
    {
        public AnimationPackFile PackFileReference { get; }
        public string Name { get; }
        public string FullPath { get; }
        public string SkeletonName { get; }
        public List<AnimationBinEntryGenericFormat> Entries { get; }
    }

    public class AnimationBinEntryGenericFormat
    {
        public int Index { get; set; } = -1;
        public int SlotIndex { get; set; }
        public string SlotName { get; set; }
        public string DisplayName { get => GetDisplayName(); }
        public string AnimationFile { get; set; }
        public string MetaFile { get; set; }
        public string SoundFile { get; set; }
        public float BlendInTime { get; set; }
        public float SelectionWeight { get; set; }
        public int WeaponBools { get; set; }

        string GetDisplayName()
        {
            if (Index == -1)
                return SlotName;
            return $"[{Index}] {SlotName}";
        }
    }


    public class AnimationPackFile
    {
        public string FileName { get; set; }
        List<IAnimationPackFile> _files { get; set; } = new List<IAnimationPackFile>();

        public IEnumerable<IAnimationPackFile> Files { get => _files; }

        public void AddFile(IAnimationPackFile file)
        {
            file.Parent = this;
            _files.Add(file);
        }

        /*public List<AnimationFragmentFile> GetAnimationSets(string skeletonName = null)
        {
            var sets = _files.Where(x => x is AnimationFragmentFile).Cast<AnimationFragmentFile>();
            if(skeletonName != null)
                sets = sets.Where(x => x.Skeletons.Values.Contains(skeletonName));

            return sets.ToList();
        }*/

        public List<IAnimationBinGenericFormat> GetGenericAnimationSets(string skeletonName = null)
        {
            var sets = _files.Where(x => x is IAnimationBinGenericFormat).Cast<IAnimationBinGenericFormat>();
            if (skeletonName != null)
                sets = sets.Where(x => x.SkeletonName == skeletonName);
            return sets.ToList();
        }
    }
}
