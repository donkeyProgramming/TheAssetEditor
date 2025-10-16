using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public abstract class AudioProjectItem
    {
        [JsonPropertyOrder(-4)] public string Name { get; set; }
        [JsonPropertyOrder(-3)]  public Guid Guid { get; set; }
        [JsonPropertyOrder(-2)] public uint Id { get; set; }
        [JsonPropertyOrder(-1)] public AkBkHircType HircType { get; set; }


        public static readonly IComparer<AudioProjectItem> IdComparer = new AudioProjectItemIdComparer();

        private class AudioProjectItemIdComparer : IComparer<AudioProjectItem>
        {
            public int Compare(AudioProjectItem left, AudioProjectItem right)
            {
                if (left == right) 
                    return 0;
                if (left is null) 
                    return -1;
                if (right is null) 
                    return 1;
                return left.Id.CompareTo(right.Id);
            }
        }
    }
}
