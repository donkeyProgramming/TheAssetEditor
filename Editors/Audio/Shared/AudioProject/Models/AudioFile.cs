using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class AudioFile : AudioProjectItem
    {
        public string WavPackFileName { get; set; }
        public string WavPackFilePath { get; set; }
        [JsonIgnore] public string WemPackFileName { get; set; }
        [JsonIgnore] public string WemPackFilePath { get; set; }
        [JsonIgnore] public string WemDiskFilePath { get; set; }
        public List<uint> Sounds { get; set; } = [];

        public AudioFile(Guid guid, uint id, string wavPackFileName, string wavPackFilePath)
        {
            Guid = guid;
            Id = id;
            WavPackFileName = wavPackFileName;
            WavPackFilePath = wavPackFilePath;
        }
    }

    public static class AudioFileListExtensions
    {
        private static readonly IComparer<AudioFile> s_wavPackFileNameComparer = new FileNameComparer();

        private sealed class FileNameComparer : IComparer<AudioFile>
        {
            public int Compare(AudioFile left, AudioFile right)
            {
                var leftName = left?.WavPackFileName ?? string.Empty;
                var rightName = right?.WavPackFileName ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }

        public static void TryAdd(this List<AudioFile> existingAudioFiles, AudioFile audioFile)
        {
            ArgumentNullException.ThrowIfNull(existingAudioFiles);
            ArgumentNullException.ThrowIfNull(audioFile);

            if (existingAudioFiles.Any(existingAudioFile => existingAudioFile.Id == audioFile.Id))
                throw new ArgumentException($"Cannot add AudioFile with Id {audioFile.Id} as it already exists.");

            var index = existingAudioFiles.BinarySearch(audioFile, s_wavPackFileNameComparer);
            if (index < 0)
                index = ~index;

            existingAudioFiles.Insert(index, audioFile);
        }
    }
}
