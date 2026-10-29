using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.VfxEditor.Document;
using Shared.GameFormats.Vfx;

namespace Editors.VfxEditor.ViewModels
{
    /// <summary>A curve's channels, the picked channel's keys, its mapping and a graph of every channel. Every change rebuilds
    /// the curve text and commits it as the property's new text.</summary>
    public sealed partial class CurveEditor : ObservableObject
    {
        readonly Action<string> _commit;
        readonly Action<string> _report;
        CurveText _curve;

        public CurveEditor(CurveText curve, Action<string> commit, Action<string> report)
        {
            _curve = curve;
            _commit = commit;
            _report = report;
            SelectedChannel = curve.Channels.FirstOrDefault();
        }

        public IReadOnlyList<CurveText.Channel> Channels => _curve.Channels;
        /// <summary>A mapping the curve cannot read is reported and the old one kept.</summary>
        public string Mapping
        {
            get => _curve.Mapping;
            set
            {
                if (value == _curve.Mapping)
                    return;
                try
                {
                    _curve.SetMapping(value.Trim());
                }
                catch (ArgumentException e)
                {
                    _report($"{e.Message}, so the curve keeps {_curve.Mapping}");
                    OnPropertyChanged();
                    return;
                }
                _commit(_curve.ToString());
            }
        }

        public const double GraphWidth = 300, GraphHeight = 80;
        const int Samples = 60;
        static readonly string[] s_colours = ["#E8A33D", "#4FA3E0", "#7CC46B", "#D95C5C", "#B07CD9", "#D9D04F"];

        /// <summary>Every channel over 0..1, sampled as the preview samples it, scaled to the curve's own range.</summary>
        public IReadOnlyList<CurveLine> Lines { get; private set; } = [];
        public string Range { get; private set; } = "";

        [RelayCommand]
        void AddChannel()
        {
            _curve.AddChannel(SelectedChannel == null ? 0 : Math.Max(0, _curve.Channels.IndexOf(SelectedChannel)));
            _commit(_curve.ToString());
        }

        [RelayCommand]
        void RemoveChannel()
        {
            if (SelectedChannel == null)
                return;
            try
            {
                _curve.RemoveChannel(_curve.Channels.IndexOf(SelectedChannel));
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                _report(e.Message);
                return;
            }
            _commit(_curve.ToString());
        }

        void Draw()
        {
            var values = _curve.Channels.Select(Sampled).ToList();
            var all = values.SelectMany(v => v).DefaultIfEmpty(0).ToList();
            var (low, high) = (all.Min(), all.Max());
            string Number(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
            string Y(float v) => Number(high > low ? GraphHeight * (1 - (v - low) / (high - low)) : GraphHeight / 2);
            Lines = values.Select((v, c) => new CurveLine(_curve.Channels[c].Name,
                string.Join(' ', v.Select((value, i) => Number(GraphWidth * i / Samples) + "," + Y(value))),
                s_colours[c % s_colours.Length], _curve.Channels[c] == SelectedChannel ? 2 : 1)).ToList();
            Range = $"{low.ToString("0.###", CultureInfo.InvariantCulture)} to {high.ToString("0.###", CultureInfo.InvariantCulture)}";
            OnPropertyChanged(nameof(Lines));
            OnPropertyChanged(nameof(Range));
        }

        static float[] Sampled(CurveText.Channel channel)
        {
            var keys = VfxCurve.Parse(channel + ":x,0;0,0")?.Outputs[0].Min ?? [];
            return Enumerable.Range(0, Samples + 1).Select(i => VfxCurve.Sample(keys, i / (float)Samples)).ToArray();
        }
        public ObservableCollection<CurveKeyRow> Keys { get; } = [];

        [ObservableProperty] CurveText.Channel? _selectedChannel;
        [ObservableProperty] CurveKeyRow? _selectedKey;

        bool _reloading;

        partial void OnSelectedChannelChanged(CurveText.Channel? value)
        {
            if (!_reloading)
                ShowKeys();
        }

        [RelayCommand]
        void AddKey()
        {
            if (SelectedChannel is not { } channel)
                return;
            _curve.InsertKeyAfter(channel, SelectedKey?.Index ?? channel.Keys.Count - 1);
            _commit(_curve.ToString());
        }

        [RelayCommand]
        void DeleteKey()
        {
            if (SelectedChannel is not { Keys.Count: > 1 } channel || SelectedKey == null)
            {
                _report("Pick a key to delete; a channel keeps at least one");
                return;
            }
            channel.Keys.RemoveAt(SelectedKey.Index);
            _commit(_curve.ToString());
        }

        internal void SetField(int key, int field, string value)
        {
            SelectedChannel!.Keys[key][field] = value;
            _commit(_curve.ToString());
        }

        internal void Report(string message) => _report(message);

        /// <summary>The document changed: read the curve again, keeping the picked channel and key.</summary>
        internal void Reload(string text)
        {
            if (CurveText.Parse(text) is not { } curve)
                return;
            var channel = SelectedChannel == null ? 0 : Math.Max(0, _curve.Channels.IndexOf(SelectedChannel));
            var key = SelectedKey?.Index;
            _curve = curve;
            // A bound channel picker pushes null while its list is swapped; the key rows wait for the channel to come back.
            _reloading = true;
            OnPropertyChanged(nameof(Channels));
            OnPropertyChanged(nameof(Mapping));
            SelectedChannel = curve.Channels.ElementAtOrDefault(channel);
            _reloading = false;
            ShowKeys();
            SelectedKey = key is { } k ? Keys.ElementAtOrDefault(k) : null;
        }

        // In place when the count is unchanged, so a box being typed in keeps its focus.
        void ShowKeys()
        {
            Draw();
            var keys = SelectedChannel?.Keys ?? [];
            if (Keys.Count == keys.Count)
            {
                for (var i = 0; i < keys.Count; i++)
                    Keys[i].Show(keys[i]);
                return;
            }
            Keys.Clear();
            for (var i = 0; i < keys.Count; i++)
                Keys.Add(new CurveKeyRow(this, i, keys[i]));
        }
    }

    public sealed record CurveLine(string Name, string Points, string Colour, double Thickness);

    public sealed class CurveKeyRow(CurveEditor owner, int index, string[] fields) : ObservableObject
    {
        string[] _fields = fields;

        public int Index { get; } = index;
        public string Time { get => _fields[0]; set => Edit(0, value); }
        public string Value { get => _fields[1]; set => Edit(1, value); }
        public string In { get => _fields[2]; set => Edit(2, value); }
        public string Out { get => _fields[3]; set => Edit(3, value); }
        public string TanInX { get => _fields[4]; set => Edit(4, value); }
        public string TanInY { get => _fields[5]; set => Edit(5, value); }
        public string TanOutX { get => _fields[6]; set => Edit(6, value); }
        public string TanOutY { get => _fields[7]; set => Edit(7, value); }
        public string Flag { get => _fields[8]; set => Edit(8, value); }

        internal void Show(string[] fields)
        {
            _fields = fields;
            OnPropertyChanged(string.Empty);
        }

        void Edit(int field, string value)
        {
            if (value == _fields[field])
                return;
            if (!CurveText.IsValidField(field, value))
            {
                owner.Report($"{value} is not a valid {CurveText.FieldNames[field]}, so the key keeps {_fields[field]}");
                OnPropertyChanged(string.Empty);
                return;
            }
            owner.SetField(Index, field, value.Trim());
        }
    }
}
