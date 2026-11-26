using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.SoundTriggers;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView
{
    public sealed record SoundSwitchOption(uint SwitchId, string Name, bool IsDefault)
    {
        public override string ToString() => IsDefault ? Name + " (default)" : Name;
    }

    public sealed partial class SoundSwitchPicker(string groupName, IReadOnlyList<SoundSwitchOption> options, SoundSwitchOption selected, Action<SoundSwitchOption> picked)
        : ObservableObject
    {
        public string GroupName { get; } = groupName;
        public IReadOnlyList<SoundSwitchOption> Options { get; } = options;
        [ObservableProperty] SoundSwitchOption _selected = selected;

        partial void OnSelectedChanged(SoundSwitchOption value) => picked(value);
    }

    public partial class SuperViewViewModel
    {
        [ObservableProperty] bool _soundOn;

        [ObservableProperty] IReadOnlyList<string> _soundStatus = [];

        public ObservableCollection<SoundSwitchPicker> SoundSwitchPickers { get; } = [];

        public Func<CancellationToken, Task<SoundTriggerResolver>> LoadSounds { get; set; } = null!;

        public Func<uint, bool> PrepareSound { get; set; } = null!;
        public Action<uint> PlaySound { get; set; } = null!;
        public Action StopSounds { get; set; } = null!;

        public Func<uint, string> SoundName { get; set; } = null!;

        // Measured through WASAPI loopback at the mixer's 30 ms latency.
        public const double SoundLeadSeconds = 0.026;

        public Func<SoundTriggerMixer> OpenMixer { get; set; } = () => new SoundTriggerMixer();

        public Task SoundDecoding { get; private set; } = Task.CompletedTask;

        public IReadOnlyList<string> SoundFileProblems
        {
            get
            {
                var selection = _asset.FragAndSlotSelection;
                return [.. Missing("Slot", selection.SoundMetaName), .. Missing("Persistent", selection.SoundMetaPersistName), .. _unreadableSound];

                IEnumerable<string> Missing(string which, string? path) => string.IsNullOrWhiteSpace(path) || _packFileService.FindFile(path) != null
                    ? [] : [$"{which} .snd.meta {path} is in no loaded pack."];
            }
        }

        const bool Campaign = false;

        SoundTriggerMixer? _mixer;
        IAudioRepository _audio = null!;
        readonly List<uint> _toDecode = [];
        SoundTriggerResolver? _resolver;
        SoundTriggerSession? _session;
        SynchronizationContext? _ui;
        volatile int _rebuilds;
        int _applied;
        int _backgroundJobs;
        bool _soundRebuildPosted;
        CancellationTokenSource? _soundLoad;
        string? _soundRefused;
        string? _soundFailed;
        ParsedMetadataFile? _slotSoundMeta;
        ParsedMetadataFile? _persistSoundMeta;
        List<string> _unreadableSound = [];
        IReadOnlyList<SwitchGroupSeen> _shownSwitches = [];
        readonly Stopwatch _sinceAdvance = Stopwatch.StartNew();

        void InitSound(IAudioRepository audio)
        {
            _audio = audio;
            _ui = SynchronizationContext.Current;
            LoadSounds = token => Task.Run(() =>
            {
                audio.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.EnglishUK)]);
                token.ThrowIfCancellationRequested();
                var tags = AudioMetadataTags.Read(_packFileService);
                return new SoundTriggerResolver(tags.Find, id => audio.GetHircs(id).ToList(), id => audio.FindWem(id.ToString()) != null);
            }, token);
            PrepareSound = id =>
            {
                _toDecode.Add(id);
                return _mixer != null;
            };
            PlaySound = id => _mixer?.Play(id);
            StopSounds = () => _mixer?.StopAll();
            SoundName = audio.GetNameFromId;

            _asset.Data.AnimationChanged += OnSoundAnimationChanged;
            _asset.Data.Player.OnFrameChanged += AdvanceSounds;
            Player.Looping += LoopSounds;
            ShowSoundStatus();
        }

        // Before the player puts the new clip on the old frame index, or that frame fires the last clip's triggers.
        void OnSoundAnimationChanged(AnimationClip? clip)
        {
            RebuildSounds();
            StopSounds();
            ShowSwitchPickers([]);
        }

        void LoadSoundMetas(SceneObject owner)
        {
            _unreadableSound = [];
            _slotSoundMeta = Parse("Slot", owner.SoundMetaData);
            _persistSoundMeta = Parse("Persistent", owner.PersistSoundMetaData);
            OnPropertyChanged(nameof(SoundFileProblems));

            ParsedMetadataFile? Parse(string which, PackFile? file)
            {
                if (file == null)
                    return null;
                try
                {
                    return _metaDataFileParser.ParseFile(file.DataSource.ReadData());
                }
                catch (Exception ex)
                {
                    _unreadableSound.Add($"{which} .snd.meta {_packFileService.GetFullPath(file)} could not be read: {ex.Message}");
                    return null;
                }
            }
        }

        partial void OnSoundOnChanged(bool value)
        {
            if (!value)
            {
                _soundLoad?.Cancel();
                _soundLoad = null;
                _rebuilds++;
                StopSounds();
                ShowSwitchPickers([]);
                ShowSoundStatus();
                return;
            }

            _soundRefused = null;
            try
            {
                if (_mixer == null)
                {
                    var mixer = _mixer = OpenMixer();
                    mixer.DeviceStopped += e => OnUi(() => DeviceStopped(mixer, e));
                }
            }
            catch (Exception e)
            {
                Refuse($"No audio device could be opened: {e.Message}");
                return;
            }
            if (_session == null)
                LoadSession();
            RebuildSounds();
        }

        void DeviceStopped(SoundTriggerMixer mixer, Exception e)
        {
            if (mixer != _mixer)
                return;
            _mixer = null;
            mixer.Dispose();
            Refuse($"The sound device stopped: {e.Message}");
        }

        void Refuse(string why)
        {
            _soundRefused = why;
            SoundOn = false;
            ShowSoundStatus();
        }

        async void LoadSession()
        {
            var load = _soundLoad = new CancellationTokenSource();
            SoundTriggerResolver resolver;
            try
            {
                resolver = await LoadSounds(load.Token);
            }
            catch (Exception e)
            {
                if (load == _soundLoad && !load.IsCancellationRequested)
                {
                    _soundLoad = null;
                    Refuse($"The game's sounds could not be loaded: {e.Message}");
                }
                return;
            }
            if (load != _soundLoad || load.IsCancellationRequested)
                return;

            _soundLoad = null;
            _resolver = resolver;
            _session = new SoundTriggerSession(resolver, id => PrepareSound(id), id => PlaySound(id), new Random()) { OffsetSeconds = SoundLeadSeconds };
            RebuildSounds();
        }

        // Only warms the repository's cache off the UI thread; ApplyRebuild resolves the tags again on it.
        void RebuildSounds()
        {
            if (SoundOn && _session != null)
            {
                var (request, tags, choices, resolver) = (++_rebuilds, SoundTriggers(), new Dictionary<uint, uint>(_session.SwitchChoices), _resolver!);
                _soundFailed = null;
                Background(request, () =>
                {
                    foreach (var tag in tags)
                    {
                        if (request != _rebuilds)
                            return;
                        resolver.Resolve(tag.SoundEvent, Campaign, choices);
                    }
                    OnUi(() => ApplyRebuild(request));
                });
            }
            ShowSoundStatus();
        }

        // UpdateView re-adds tags one at a time, each raising a change.
        void RebuildSoundsSoon()
        {
            if (_soundRebuildPosted)
                return;
            _soundRebuildPosted = true;
            OnUi(() =>
            {
                _soundRebuildPosted = false;
                RebuildSounds();
            });
        }

        void ApplyRebuild(int request)
        {
            if (request != _rebuilds || !SoundOn || _session == null)
                return;
            _session.Rebuild(SoundTriggers(), Campaign);
            _applied = request;
            DecodeQueued(request, _session.Leaves.ToHashSet());
            ShowSwitchPickers(_session.Switches);
            ShowSoundStatus();
        }

        void DecodeQueued(int request, HashSet<uint> wanted)
        {
            var (mixer, ids, audio) = (_mixer, _toDecode.ToList(), _audio);
            _toDecode.Clear();
            if (mixer == null)
                return;
            Background(request, () =>
            {
                foreach (var id in ids)
                {
                    if (request != _rebuilds)
                        return;
                    mixer.Prepare(id, () => ReadWem(audio, id));
                }
                mixer.Keep(wanted);
            });
        }

        void Background(int request, Action job)
        {
            _backgroundJobs++;
            SoundDecoding = SoundDecoding.ContinueWith(_ =>
            {
                string? failure = null;
                try
                {
                    job();
                }
                catch (Exception e)
                {
                    failure = e.Message;
                }
                OnUi(() =>
                {
                    _backgroundJobs--;
                    if (failure != null && request == _rebuilds)
                        _soundFailed = $"Sounds could not be prepared: {failure}";
                    ShowSoundStatus();
                });
            }, TaskScheduler.Default);
        }

        void OnUi(Action action)
        {
            if (_ui == null)
                action();
            else
                _ui.Post(_ => action(), null);
        }

        static byte[]? ReadWem(IAudioRepository audio, uint id)
        {
            try
            {
                return audio.FindWem(id.ToString())?.DataSource.ReadData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        List<SoundTriggerTag> SoundTriggers() =>
        [
            .. Triggers(PersistentMetaEditor.ParsedFile, "persistent meta"),
            .. Triggers(MetaEditor.ParsedFile, "clip meta"),
            .. Triggers(_slotSoundMeta, "slot .snd.meta"),
            .. Triggers(_persistSoundMeta, "persistent .snd.meta"),
        ];

        static IEnumerable<SoundTriggerTag> Triggers(ParsedMetadataFile? file, string label) => (file?.Attributes ?? []).Select(x => x switch
        {
            SoundTrigger_v10 tag => new SoundTriggerTag(tag.SoundEvent, tag.StartTime, label),
            SoundTrigger_v4 tag => new SoundTriggerTag(tag.SoundEvent, tag.StartTime, label),
            _ => null,
        }).OfType<SoundTriggerTag>();

        // Until the latest rebuild lands, the session still holds the last clip's triggers.
        bool SoundsLive => SoundOn && _session != null && _applied == _rebuilds;

        void AdvanceSounds(int frame)
        {
            if (!SoundsLive)
                return;
            var player = _asset.Data.Player;
            var elapsed = _sinceAdvance.Elapsed.TotalSeconds;
            _sinceAdvance.Restart();
            _session!.Advance(player.GetTimeUs() / 1_000_000d, player.IsPlaying, elapsed, 1, player.GetAnimationLengthUs() / 1_000_000d,
                Player.LoopAnimation.Value);
        }

        void LoopSounds()
        {
            if (SoundsLive)
                _session!.Loop(1, _asset.Data.Player.GetAnimationLengthUs() / 1_000_000d);
        }

        void ShowSoundStatus()
        {
            IReadOnlyList<string> lines = [.. SoundFileProblems, .. SessionLines()];
            if (!lines.SequenceEqual(SoundStatus))
                SoundStatus = lines;

            IEnumerable<string> SessionLines()
            {
                if (!SoundOn)
                    return _soundRefused == null ? [] : [_soundRefused];
                if (_session == null || _session.StatusLines.Count == 0)
                    return [_soundFailed ?? "Loading sounds..."];
                List<string> lines = _applied == _rebuilds ? [.. _session.StatusLines] : [];
                if (_backgroundJobs != 0)
                    lines.Add("Decoding sounds...");
                if (_soundFailed != null)
                    lines.Add(_soundFailed);
                return lines;
            }
        }

        void ShowSwitchPickers(IReadOnlyList<SwitchGroupSeen> switches)
        {
            switches = [.. switches.Where(x => x.Values.Count != 0)];
            if (switches.Count == _shownSwitches.Count && switches.Zip(_shownSwitches).All(x => x.First.GroupId == x.Second.GroupId && x.First.Values.SequenceEqual(x.Second.Values)))
                return;

            _shownSwitches = switches;
            var session = _session!;
            SoundSwitchPickers.Clear();
            foreach (var group in switches)
            {
                var options = group.Values.Select(x => new SoundSwitchOption(x, SoundName(x), x == group.DefaultSwitch)).ToList();
                var picked = session.SwitchChoices.TryGetValue(group.GroupId, out var choice) ? options.FirstOrDefault(x => x.SwitchId == choice) : null;
                SoundSwitchPickers.Add(new SoundSwitchPicker(SoundName(group.GroupId), options, picked ?? options.FirstOrDefault(x => x.IsDefault) ?? options[0], x =>
                {
                    session.SwitchChoices[group.GroupId] = x.SwitchId;
                    RebuildSounds();
                }));
            }
        }

        void CloseSound()
        {
            _asset.Data.AnimationChanged -= OnSoundAnimationChanged;
            _asset.Data.Player.OnFrameChanged -= AdvanceSounds;
            Player.Looping -= LoopSounds;
            _soundLoad?.Cancel();
            _soundLoad = null;
            _rebuilds++;
            _mixer?.Dispose();
            _mixer = null;
        }
    }
}
