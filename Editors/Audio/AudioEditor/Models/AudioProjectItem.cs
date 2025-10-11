using System;
using System.Text.Json.Serialization;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public abstract class AudioProjectItem
    {
        [JsonPropertyOrder(-4)] public string Name { get; set; }
        [JsonPropertyOrder(-3)]  public Guid Guid { get; set; }
        [JsonPropertyOrder(-2)] public uint Id { get; set; }
        [JsonPropertyOrder(-1)] public AkBkHircType HircType { get; set; }
    }
}
