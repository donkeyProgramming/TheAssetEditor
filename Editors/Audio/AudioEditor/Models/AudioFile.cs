using System;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.Models
{
    public static class AudioFileListExtensions
    {
        public static void TryAdd(this List<AudioFile> audioFiles, AudioFile audioFile)
        {
            ArgumentNullException.ThrowIfNull(audioFiles);
            ArgumentNullException.ThrowIfNull(audioFile);

            if (audioFiles.Any(x => x.Id == audioFile.Id))
                throw new ArgumentException($"Cannot add AudioFile with Id {audioFile.Id} as it already exists.");

            var i = audioFiles.BinarySearch(audioFile, AudioFile.WavPackFileNameComparer);
            if (i < 0) 
                i = ~i;
            audioFiles.Insert(i, audioFile);
        }
    }

    public partial class AudioFile : AudioProjectItem
    {
        public string WavPackFileName { get; set; }
        public string WavPackFilePath { get; set; }
        public string WemPackFileName { get; set; }
        public string WemPackFilePath { get; set; }
        public string WemDiskFilePath { get; set; }
        public List<uint> SoundReferences { get; set; } = [];

        public static AudioFile Create(Guid guid, uint id, string fileName, string filePath)
        {
            return new AudioFile
            {
                Guid = guid,
                Id = id,
                WavPackFileName = fileName,
                WavPackFilePath = filePath
            };
        }

        public static readonly IComparer<AudioFile> WavPackFileNameComparer = new FileNameComparer();

        private sealed class FileNameComparer : IComparer<AudioFile>
        {
            public int Compare(AudioFile left, AudioFile right)
            {
                var leftName = left?.WavPackFileName ?? string.Empty;
                var rightName = right?.WavPackFileName ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }
    }
}
