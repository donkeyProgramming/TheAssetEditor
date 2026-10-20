using System.Globalization;

namespace Shared.GameFormats.Vfx
{
    /// <summary>
    /// A curve-valued vfx property: "channel|channel:mapping|mapping". A channel is "name,colour,flag;key;key",
    /// a key "time,value,inInterp,outInterp,tanInX,tanInY,tanOutX,tanOutY,flag", a mapping
    /// "output,random;minChannel,maxChannel". An output lies between its min and max channels: random 1
    /// draws a point between them, 0 uses the min. Measured over vfx_desc.pack (9.0): 520,858 of 524,102
    /// random-0 mappings have identical channels, and 126,495 of 150,773 random-1 mappings differ.
    /// </summary>
    public class VfxCurve
    {
        public record Key(float Time, float Value, string In, string Out, float TanInX, float TanInY, float TanOutX, float TanOutY);
        public record Output(string Name, bool Random, Key[] Min, Key[] Max);

        public IReadOnlyList<Output> Outputs { get; }

        VfxCurve(IReadOnlyList<Output> outputs) => Outputs = outputs;

        /// <summary>Null when the text does not parse; the caller logs it and uses its default.</summary>
        public static VfxCurve? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var split = text.LastIndexOf(':');
            if (split < 0)
                return null;

            try
            {
                var channels = text[..split].Split('|').Select(ParseChannel).ToArray();
                var outputs = text[(split + 1)..].Split('|').Select(mapping =>
                {
                    var parts = mapping.Split(';');
                    var head = parts[0].Split(',');
                    var range = parts[1].Split(',');
                    var min = channels[int.Parse(range[0], CultureInfo.InvariantCulture)];
                    var max = channels[int.Parse(range[1], CultureInfo.InvariantCulture)];
                    return new Output(head[0].Trim(), head[1].Trim() == "1", min, max);
                }).ToArray();
                return new VfxCurve(outputs);
            }
            catch (Exception e) when (e is FormatException or IndexOutOfRangeException or OverflowException)
            {
                return null;
            }
        }

        public static VfxCurve Constant(params float[] values) => new(values.Select(value =>
        {
            Key[] keys = [new Key(0, value, "linear", "linear", 0, 0, 0, 0)];
            return new Output("x", false, keys, keys);
        }).ToArray());

        // 105k vanilla channels list their keys out of time order.
        static Key[] ParseChannel(string channel) => channel.Split(';').Skip(1).Select(key =>
        {
            var f = key.Split(',');
            if (f.Length != 9)
                throw new FormatException($"a key has {f.Length} fields, not 9");
            return new Key(Number(f[0]), Number(f[1]), f[2], f[3], Number(f[4]), Number(f[5]), Number(f[6]), Number(f[7]));
        }).OrderBy(x => x.Time).ToArray();

        // float.Parse reads "1e39" as infinity and "NaN" as NaN; neither is a value a curve can hold.
        static float Number(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) is var value && float.IsFinite(value)
            ? value
            : throw new FormatException($"{text} is not a finite number");

        public float Evaluate(int output, float t, float random, float fallback = 0)
        {
            if (output >= Outputs.Count)
                return fallback;
            var o = Outputs[output];
            var min = Sample(o.Min, t);
            return o.Random ? min + (Sample(o.Max, t) - min) * random : min;
        }

        public static float Sample(Key[] keys, float t)
        {
            if (keys.Length == 0)
                return 0;
            if (t <= keys[0].Time)
                return keys[0].Value;

            for (var i = 0; i < keys.Length - 1; i++)
            {
                var (a, b) = (keys[i], keys[i + 1]);
                if (t >= b.Time)
                    continue;
                if (a.Out is "bezier_c" or "bezier_q")
                    return Bezier(a, b, t);
                return a.Value + (b.Value - a.Value) * Ease(a.Out, (t - a.Time) / (b.Time - a.Time));
            }
            return keys[^1].Value;
        }

        /// <summary>
        /// A segment eases by its left key's out-interpolation. These shapes are a reading of the names,
        /// not CA's code. An unknown name eases linearly.
        /// </summary>
        public static float Ease(string interpolation, float u) => interpolation switch
        {
            "step" => 0,
            "step_next" => 1,
            "s" => u * u * (3 - 2 * u),
            "s_inv" => 0.5f - MathF.Sin(MathF.Asin(1 - 2 * u) / 3),
            "sine" => (1 - MathF.Cos(MathF.PI * u)) / 2,
            "sine_recip" => MathF.Acos(1 - 2 * u) / MathF.PI,
            "log1" => Log(u, 1),
            "log3" => Log(u, 3),
            "exp1" => Exp(u, 1),
            "exp3" => Exp(u, 3),
            _ => u,
        };

        static float Log(float u, float k) => MathF.Log(1 + (MathF.Exp(k) - 1) * u) / k;
        static float Exp(float u, float k) => (MathF.Exp(k * u) - 1) / (MathF.Exp(k) - 1);

        // A cubic through the two keys whose handles are the tangent offsets (time, value) from each key.
        // bezier_q is drawn the same way.
        static float Bezier(Key a, Key b, float t)
        {
            float lo = 0, hi = 1;
            for (var i = 0; i < 30; i++)
            {
                var mid = (lo + hi) / 2;
                if (Cubic(a.Time, a.Time + a.TanOutX, b.Time + b.TanInX, b.Time, mid) < t)
                    lo = mid;
                else
                    hi = mid;
            }
            return Cubic(a.Value, a.Value + a.TanOutY, b.Value + b.TanInY, b.Value, (lo + hi) / 2);
        }

        static float Cubic(float p0, float p1, float p2, float p3, float s)
        {
            var r = 1 - s;
            return r * r * r * p0 + 3 * r * r * s * p1 + 3 * r * s * s * p2 + s * s * s * p3;
        }
    }
}
