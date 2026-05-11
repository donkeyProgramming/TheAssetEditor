namespace Shared.GameFormats.Wwise.Wem.V132
{
    public static class WemChunks
    {
        // Chunk tags are always exactly 4 bytes so tags shorter than 4 characters are padded with a trailing space.
        // The fmt chunk contains stream format metadata and Wwise Vorbis extension fields.
        public const string Fmt = "fmt "; 
        // The data chunk contains seek / setup data and encoded audio packets.
        public const string Data = "data";
        // The JUNK chunk contains padding bytes used for chunk / data alignment.
        public const string Junk = "JUNK";
        // The akd chunk contains loudness normalisation and optional HDR envelope metadata.
        public const string Akd = "akd ";
        // The cue chunk contains cue point entries for timing markers.
        public const string Cue = "cue ";
    }
}
