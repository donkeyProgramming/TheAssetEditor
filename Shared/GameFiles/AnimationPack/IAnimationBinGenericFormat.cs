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
}
