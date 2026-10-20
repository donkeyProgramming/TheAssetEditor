using System.Globalization;

namespace Editors.VfxEditor.Document
{
    /// <summary>A curve property's text, split so keys can be changed, added or removed and the rest rebuilt as written:
    /// "channel|channel:mapping|mapping", a channel "name,colour,flag;key;key", a key nine fields.</summary>
    public sealed class CurveText
    {
        public const int KeyFields = 9;
        public static readonly string[] Interpolations = ["linear", "bezier_c", "log3", "sine", "exp3", "sine_recip", "log1", "exp1", "s", "step", "bezier_q", "s_inv", "step_next"];
        public static readonly string[] FieldNames = ["time", "value", "in interpolation", "out interpolation", "in tangent x", "in tangent y", "out tangent x", "out tangent y", "flag"];

        public sealed class Channel(string header)
        {
            public string Header { get; } = header;
            public string Name => Header.Split(',')[0];
            public List<string[]> Keys { get; } = [];

            public override string ToString() => string.Join(';', Keys.Select(k => string.Join(',', k)).Prepend(Header));
        }

        string _leading = "", _trailing = "", _mapping = "";

        public List<Channel> Channels { get; } = [];
        public string Mapping => _mapping;

        /// <summary>Null for text that is not a curve: no mapping, or a key without nine fields. A channel may have no keys.</summary>
        public static CurveText? Parse(string text)
        {
            var body = text.Trim();
            var split = body.LastIndexOf(':');
            if (split < 0)
                return null;
            var curve = new CurveText
            {
                _leading = text[..(text.Length - text.TrimStart().Length)],
                _trailing = text[text.TrimEnd().Length..],
                _mapping = body[(split + 1)..],
            };
            foreach (var part in body[..split].Split('|'))
            {
                var pieces = part.Split(';');
                var channel = new Channel(pieces[0]);
                foreach (var key in pieces.Skip(1))
                {
                    var fields = key.Split(',');
                    if (fields.Length != KeyFields)
                        return null;
                    channel.Keys.Add(fields);
                }
                curve.Channels.Add(channel);
            }
            return curve;
        }

        public override string ToString() =>
            _leading + string.Join('|', Channels) + ':' + _mapping + _trailing;

        /// <summary>A copy of key `index` at a time halfway to the next later key, or to 1 when there is none. A channel with no
        /// keys (vanilla writes them) takes a first key at 0 with the tangents vanilla uses.</summary>
        public void InsertKeyAfter(Channel channel, int index)
        {
            if (channel.Keys.Count == 0)
            {
                channel.Keys.Add(["0.0", "0.0", "linear", "linear", "-0.05", "0.0", "0.05", "0.0", "0"]);
                return;
            }
            var key = (string[])channel.Keys[index].Clone();
            var time = Number(key[0]);
            var later = channel.Keys.Select(k => Number(k[0])).Where(t => t > time).ToList();
            var next = later.Count > 0 ? later.Min() : 1f;
            key[0] = ((time + next) / 2).ToString("0.0###", CultureInfo.InvariantCulture);
            channel.Keys.Insert(index + 1, key);
        }

        /// <summary>A copy of channel `index`, named with "_copy", at the end, so no output's channel index moves.</summary>
        public void AddChannel(int index)
        {
            var source = Channels[index];
            var header = source.Header.Split(',');
            header[0] += "_copy";
            var copy = new Channel(string.Join(',', header));
            copy.Keys.AddRange(source.Keys.Select(k => (string[])k.Clone()));
            Channels.Add(copy);
        }

        /// <summary>Refused while an output reads the channel; the indices past it move down.</summary>
        public void RemoveChannel(int index)
        {
            if (Channels.Count == 1)
                throw new InvalidOperationException("a curve keeps at least one channel");
            var outputs = Outputs(_mapping, Channels.Count);
            if (outputs.FirstOrDefault(o => o.Min == index || o.Max == index) is { Output: { } reader })
                throw new InvalidOperationException($"{reader} reads {Channels[index].Name}, so it stays; point the mapping at another channel first");
            Channels.RemoveAt(index);
            int Moved(int i) => i > index ? i - 1 : i;
            _mapping = string.Join('|', outputs.Select(o => $"{o.Output},{o.Random};{Moved(o.Min)},{Moved(o.Max)}"));
        }

        public void SetMapping(string mapping)
        {
            Outputs(mapping, Channels.Count);
            _mapping = mapping;
        }

        // "output,random;minChannel,maxChannel" joined by '|', each channel one the curve has.
        static List<(string Output, string Random, int Min, int Max)> Outputs(string mapping, int channels)
        {
            var outputs = new List<(string, string, int, int)>();
            foreach (var part in mapping.Split('|'))
            {
                var pieces = part.Split(';');
                var head = pieces[0].Split(',');
                var range = pieces.Length == 2 ? pieces[1].Split(',') : [];
                if (head.Length != 2 || head[0].Trim().Length == 0 || head[1] is not ("0" or "1") || range.Length != 2
                    || !int.TryParse(range[0], NumberStyles.None, CultureInfo.InvariantCulture, out var min) || min >= channels
                    || !int.TryParse(range[1], NumberStyles.None, CultureInfo.InvariantCulture, out var max) || max >= channels)
                    throw new ArgumentException($"\"{part}\" is not output,random;min,max with random 0 or 1 and channels 0 to {channels - 1}");
                outputs.Add((head[0], head[1], min, max));
            }
            return outputs;
        }

        /// <summary>Interpolations must be one of the 13 vanilla uses; every other field a finite number.</summary>
        public static bool IsValidField(int field, string value) => field is 2 or 3
            ? Interpolations.Contains(value)
            : float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && float.IsFinite(number);

        static float Number(string text) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
