using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public abstract class AudioProjectItem : IComparable, IComparable<AudioProjectItem>, IEquatable<AudioProjectItem>
    {
        [JsonPropertyOrder(-4)] public string Name { get; set; }
        [JsonPropertyOrder(-3)]  public Guid Guid { get; set; }
        [JsonPropertyOrder(-2)] public uint Id { get; set; }
        [JsonPropertyOrder(-1)] public AkBkHircType HircType { get; set; }

        public int CompareTo(object obj) => CompareTo(obj as AudioProjectItem);

        public int CompareTo(AudioProjectItem other) => StringComparer.OrdinalIgnoreCase.Compare(Name, other?.Name);

        public bool Equals(AudioProjectItem other) => StringComparer.OrdinalIgnoreCase.Equals(Name, other?.Name);

        public override bool Equals(object obj) => Equals(obj as AudioProjectItem);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        public static bool InsertAlphabeticallyUnique<T>(List<T> list, T item)  where T : IComparable<T>
        {
            var index = list.BinarySearch(item);

            // Prevents duplicates being added
            if (index >= 0)
                return false;

            list.Insert(~index, item);
            return true;
        }
    }
}
