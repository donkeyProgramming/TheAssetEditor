using System.Security.Cryptography;
using System.Text;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage.CacheDatabase;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, List<HircItem>> HircsById { get; }
        Dictionary<uint, List<DidxAudio>> DidxAudioListById { get; }
        Dictionary<string, PackFile> PackFileByBnkName { get; }
        Dictionary<uint, string> NameById { get; }
        Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; }
        Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; }
        Dictionary<string, List<string>> StatesByStateGroup { get; }

        void EnsureGameFilesCache();
        void Load(List<string> languages);
        void Clear();
        List<HircItem> GetHircs(AkBkHircType type);
        List<HircItem> GetHircs(uint id);
        List<HircItem> GetHircs(uint id, string owningFileName);
        Dictionary<uint, List<HircItem>> GetHircs(IReadOnlyCollection<uint> ids);
        string GetNameFromId(uint value);
        string GetNameFromId(uint value, out bool found);
        string GetNameFromId(uint? key);
        HashSet<uint> GetUsedVanillaHircIdsByLanguageId(uint languageId);
        HashSet<uint> GetUsedVanillaSourceIdsByLanguageId(uint languageId);
        Dictionary<string, Dictionary<string, List<HircItem>>> GetVanillaDialogueEventsByBnkByLanguage();
        Dictionary<string, Dictionary<string, List<HircItem>>> GetModdedHircsByBnkByLanguage();
        Dictionary<string, List<HircItem>> GetModdedDialogueEventsByLanguage(List<string> moddedSoundBanks);
        List<string> GetModdedSoundBankFilePaths(string bnkNameSubstring);
        PackFile FindWem(string wemId);
        byte[] FindDataWem(uint dataSoundbankId, int fileOffset, int byteCount);
    }

    internal class AudioRepository : IAudioRepository, IDisposable
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IPackFileService _packFileService;
        private readonly IAudioCacheHelper _cacheHelper;
        private readonly BnkLoader _bnkLoader;
        private readonly IEventHub _eventHub;

        private readonly List<string> _loadedBnkDataLanguages = [];
        private readonly List<LoadedLayer> _loadedLayers = [];
        private string _loadedFingerprint = "";
        private bool _allCachedHircsLoaded;
        private bool _allCachedDidxLoaded;
        private Dictionary<uint, List<HircItem>> _hircsById = [];
        private Dictionary<AkBkHircType, List<HircItem>> _hircsByType = [];
        private Dictionary<uint, List<DidxAudio>> _didxAudioListById = [];

        public Dictionary<uint, List<HircItem>> HircsById => GetAllCachedHircs();
        public Dictionary<uint, List<DidxAudio>> DidxAudioListById => GetAllCachedDidx();
        public Dictionary<string, PackFile> PackFileByBnkName { get; private set; } = [];
        public Dictionary<uint, string> NameById { get; private set; } = [];
        public Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; private set; } = [];
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; private set; } = [];
        public Dictionary<string, List<string>> StatesByStateGroup { get; private set; } = [];

        public AudioRepository(
            ApplicationSettingsService applicationSettingsService,
            IPackFileService packFileService,
            IAudioCacheHelper cacheHelper,
            BnkLoader bnkLoader,
            IEventHub eventHub)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
            _cacheHelper = cacheHelper;
            _bnkLoader = bnkLoader;
            _eventHub = eventHub;
            _eventHub.Register<PackFileContainerSetAsMainEditableEvent>(this, OnPackFileContainerSetAsMainEditable);
        }

        private void OnPackFileContainerSetAsMainEditable(PackFileContainerSetAsMainEditableEvent e)
        {
            if (e.Container == null || e.Container.IsCaPackFile || _loadedLayers.Count == 0)
                return;

            Load([]);
        }

        public void EnsureGameFilesCache()
        {
            var source = CreateGameFilesCacheSource();
            if (source == null)
                return;

            using var repository = LoadCachedRepository(source);
        }

        public void Load(List<string> languages)
        {
            var requestedLanguages = _loadedBnkDataLanguages
                .Union(languages, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var gameInformation = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame);
            if (gameInformation.BankGeneratorVersion == GameBnkVersion.Unsupported)
                return;

            var cacheSources = CreateCacheSources();
            if (cacheSources.Count == 0)
                return;

            LoadCacheSources(cacheSources, requestedLanguages);
        }

        internal void LoadCacheSources(List<AudioCacheSource> cacheSources, List<string> requestedLanguages)
        {
            var fingerprint = CreateCombinedFingerprint(cacheSources);
            if (_loadedLayers.Count != 0
                && fingerprint == _loadedFingerprint
                && requestedLanguages.All(
                    language => _loadedBnkDataLanguages.Contains(
                        language,
                        StringComparer.OrdinalIgnoreCase)))
            {
                return;
            }

            var layers = LoadLayers(cacheSources);
            try
            {
                var resolvedBnks = CreateEffectiveBnks(layers, requestedLanguages);
                foreach (var bnk in resolvedBnks.Values)
                    bnk.Layer.ResolvedBnkPaths.Add(bnk.Bnk.Path);

                var datData = MergeDatData(layers.Select(layer => layer.AudioCache.LoadDatData()));
                ApplyLoadedLayers(
                    layers,
                    resolvedBnks.Keys.ToList(),
                    datData,
                    requestedLanguages,
                    fingerprint);
            }
            catch
            {
                DisposeLayers(layers);
                throw;
            }

        }

        internal List<AudioCacheSource> CreateCacheSources()
        {
            var allContainers = _packFileService.GetAllPackfileContainers();
            var gameFileContainers = allContainers.Where(container => container.IsCaPackFile).ToList();
            if (gameFileContainers.Count == 0)
                return [];

            var sources = new List<AudioCacheSource> { CreateGameFilesCacheSource(gameFileContainers) };

            var projectFileContainers = allContainers.Where(container => !container.IsCaPackFile).ToList();
            if (!HasRelevantAudioFiles(projectFileContainers))
                return sources;

            var projectFilesFingerprint = _cacheHelper.ComputeFingerprint(projectFileContainers, "project files");
            var editableContainer = _packFileService.GetEditablePack();
            var cacheOwner = editableContainer ?? projectFileContainers[^1];
            var projectFilesLabel = cacheOwner.Name;

            sources.Add(new AudioCacheSource(
                _cacheHelper.GetCacheFilePath(projectFilesLabel, projectFilesFingerprint),
                projectFilesFingerprint,
                false,
                projectFileContainers,
                projectFileContainers));
            return sources;
        }

        public void Clear()
        {
            DisposeLayers(_loadedLayers);
            _loadedLayers.Clear();
            _loadedBnkDataLanguages.Clear();
            _loadedFingerprint = "";
            _allCachedHircsLoaded = false;
            _allCachedDidxLoaded = false;
            _hircsById = [];
            _hircsByType = [];
            _didxAudioListById = [];
            PackFileByBnkName = [];
            NameById = [];
            StateGroupsByDialogueEvent = [];
            QualifiedStateGroupByStateGroupByDialogueEvent = [];
            StatesByStateGroup = [];

        }

        public List<HircItem> GetHircs(AkBkHircType hircType)
        {
            if (_loadedLayers.Count != 0 && !_allCachedHircsLoaded)
            {
                if (_hircsByType.TryGetValue(hircType, out var cachedHircs))
                    return cachedHircs;

                var references = new List<BnkHircReference>();
                foreach (var layer in _loadedLayers)
                {
                    references.AddRange(
                        layer.AudioCache.FindHircs(
                            hircType,
                            layer.ResolvedBnkPaths));
                }

                var hircs = _bnkLoader.LoadHircs(references);
                _hircsByType[hircType] = hircs;
                return hircs;
            }

            return _hircsById
                .SelectMany(entry => entry.Value)
                .Where(hirc => hirc.HircType == hircType)
                .ToList();
        }

        public List<HircItem> GetHircs(uint id)
        {
            if (_hircsById.TryGetValue(id, out var hircs))
                return hircs;

            if (_loadedLayers.Count != 0 && !_allCachedHircsLoaded)
            {
                var references = new List<BnkHircReference>();
                foreach (var layer in _loadedLayers)
                {
                    references.AddRange(
                        layer.AudioCache.FindHircs(
                            id,
                            layer.ResolvedBnkPaths));
                }

                hircs = _bnkLoader.LoadHircs(references);
                _hircsById[id] = hircs;
                return hircs;
            }

            return [];
        }

        public List<HircItem> GetHircs(uint id, string owningFileName) => GetHircs(id).Where(x => x.BnkFilePath == owningFileName).ToList();

        public Dictionary<uint, List<HircItem>> GetHircs(IReadOnlyCollection<uint> ids)
        {
            var resolvedHircsById = new Dictionary<uint, List<HircItem>>();
            var uncachedIds = new HashSet<uint>();

            foreach (var id in ids)
            {
                if (_hircsById.TryGetValue(id, out var cachedHircs))
                    resolvedHircsById[id] = cachedHircs;
                else
                    uncachedIds.Add(id);
            }

            if (uncachedIds.Count != 0 && _loadedLayers.Count != 0 && !_allCachedHircsLoaded)
            {
                var references = new List<BnkHircReference>();
                foreach (var layer in _loadedLayers)
                    references.AddRange(layer.AudioCache.FindHircs(uncachedIds, layer.ResolvedBnkPaths));

                var loadedHircs = _bnkLoader.LoadHircs(references);
                foreach (var hircsForId in loadedHircs.GroupBy(hirc => hirc.Id))
                {
                    var groupedHircs = hircsForId.ToList();
                    _hircsById[hircsForId.Key] = groupedHircs;
                    resolvedHircsById[hircsForId.Key] = groupedHircs;
                }
            }

            return resolvedHircsById;
        }

        public string GetNameFromId(uint value) => GetNameFromId(value, out var _);

        public string GetNameFromId(uint value, out bool found)
        {
            found = NameById.ContainsKey(value);
            if (found)
                return NameById[value];
            return value.ToString();
        }

        public string GetNameFromId(uint? key)
        {
            if (key.HasValue)
                return GetNameFromId(key.Value);
            else
                throw new Exception("Cannot get name from ID");
        }

        public HashSet<uint> GetUsedVanillaHircIdsByLanguageId(uint languageId)
        {
            if (_loadedLayers.Count != 0 && !_allCachedHircsLoaded)
            {
                var result = new HashSet<uint>();
                foreach (var layer in _loadedLayers)
                {
                    result.UnionWith(
                        layer.AudioCache.FindHircIds(
                            languageId,
                            true,
                            layer.ResolvedBnkPaths));
                }
                return result;
            }

            return HircsById
                .SelectMany(
                    entry => entry.Value
                        .Where(hirc => hirc.LanguageId == languageId && hirc.IsCA == true)
                        .Select(_ => entry.Key))
                .ToHashSet();
        }

        public HashSet<uint> GetUsedVanillaSourceIdsByLanguageId(uint languageId)
        {
            return GetHircs(AkBkHircType.Sound)
                .Where(hirc => hirc.LanguageId == languageId && hirc is ICAkSound && hirc.IsCA == true)
                .Select(hirc => ((ICAkSound)hirc).GetSourceId())
                .ToHashSet();
        }

        public Dictionary<string, Dictionary<string, List<HircItem>>> GetVanillaDialogueEventsByBnkByLanguage()
        {
            return GetHircs(AkBkHircType.Dialogue_Event)
                .Where(hirc => hirc.IsCA)
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(
                    languageGroup => languageGroup.Key,
                    languageGroup => languageGroup
                        .GroupBy(hirc => hirc.BnkFilePath)
                        .ToDictionary(bnkGroup => bnkGroup.Key, bnkGroup => bnkGroup.ToList())
                );
        }

        public Dictionary<string, Dictionary<string, List<HircItem>>> GetModdedHircsByBnkByLanguage()
        {
            return HircsById
                .SelectMany(hirc => hirc.Value)
                .Where(hirc => hirc.IsCA == false)
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(
                    languageGroup => languageGroup.Key,
                    languageGroup => languageGroup
                        .GroupBy(hircItem => hircItem.BnkFilePath)
                        .ToDictionary(bnkGroup => bnkGroup.Key, bnkGroup => bnkGroup.ToList())
                );
        }

        public Dictionary<string, List<HircItem>> GetModdedDialogueEventsByLanguage(List<string> moddedSoundBanks)
        {
            return GetHircs(AkBkHircType.Dialogue_Event)
                .Where(hirc => hirc.IsCA == false && moddedSoundBanks.Contains(hirc.BnkFilePath))
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        public List<string> GetModdedSoundBankFilePaths(string bnkNameSubstring)
        {
            return HircsById
                .SelectMany(hircDictionaryEntry => hircDictionaryEntry.Value)
                .Where(hirc => hirc.IsCA == false && hirc.BnkFilePath.Contains(bnkNameSubstring))
                .Select(hirc => hirc.BnkFilePath )
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(bnkFilePath => bnkFilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public PackFile FindWem(string wemId)
        {
            var wemFile = _packFileService.FindFile($"audio\\wwise\\{wemId}.wem");
            if (wemFile != null)
                return wemFile;

            foreach (var language in Enum.GetValues<Wh3Language>())
            {
                wemFile = _packFileService.FindFile($"audio\\wwise\\{Wh3LanguageInformation.GetLanguageAsString(language)}\\{wemId}.wem");
                if (wemFile != null)
                    return wemFile;
            }

            return null;
        }

        public byte[] FindDataWem(uint dataSoundbankId, int fileOffset, int byteCount)
        {
            var dataSoundbankName = GetNameFromId(dataSoundbankId, out var found);
            if (!found)
                return null;

            var packFile = PackFileByBnkName[$"{dataSoundbankName}.bnk"];
            if (packFile == null)
                return null;

            var byteChunk = packFile.DataSource.ReadDataAsChunk();
            byteChunk.Advance(fileOffset);
            return byteChunk.ReadBytes(byteCount);
        }

        private AudioCacheSource CreateGameFilesCacheSource()
        {
            var gameFileContainers = _packFileService
                .GetAllPackfileContainers()
                .Where(container => container.IsCaPackFile)
                .ToList();
            return gameFileContainers.Count == 0 ? null : CreateGameFilesCacheSource(gameFileContainers);
        }

        private AudioCacheSource CreateGameFilesCacheSource(List<IPackFileContainer> gameFileContainers)
        {
            var fingerprint = _cacheHelper.ComputeFingerprint(gameFileContainers, "game files");
            var label = gameFileContainers[0].Name;
            return new AudioCacheSource(_cacheHelper.GetCacheFilePath(label, fingerprint), fingerprint, true, gameFileContainers, gameFileContainers);
        }

        private AudioCache LoadCachedRepository(AudioCacheSource source)
        {
            return _cacheHelper.TryLoadFromCache(source.CacheFilePath, source.Fingerprint) ?? _cacheHelper.SaveAndLoadCache(source);
        }

        private List<LoadedLayer> LoadLayers(List<AudioCacheSource> sources)
        {
            var layers = new List<LoadedLayer>();
            try
            {
                foreach (var source in sources)
                    layers.Add(new LoadedLayer(LoadCachedRepository(source)));
                return layers;
            }
            catch
            {
                DisposeLayers(layers);
                throw;
            }
        }

        private void ApplyLoadedLayers(List<LoadedLayer> layers, List<string> bnkPaths, CachedAudioDatData datData, List<string> languages, string fingerprint)
        {
            DisposeLayers(_loadedLayers);
            _loadedLayers.Clear();
            _loadedLayers.AddRange(layers);
            _hircsById = [];
            _hircsByType = [];
            _didxAudioListById = [];
            _allCachedHircsLoaded = false;
            _allCachedDidxLoaded = false;
            NameById = datData.NameById;
            StateGroupsByDialogueEvent = datData.StateGroupsByDialogueEvent;

            // Add qualifiers to State Groups as some events have the same State Group twice e.g. VO_Actor.
            QualifiedStateGroupByStateGroupByDialogueEvent = DatLoader.BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroups(StateGroupsByDialogueEvent);
            StatesByStateGroup = datData.StatesByStateGroup;
            _loadedBnkDataLanguages.Clear();
            _loadedBnkDataLanguages.AddRange(languages);
            _loadedFingerprint = fingerprint;
            SetCurrentBnkFiles(bnkPaths);
        }

        internal static CachedAudioDatData MergeDatData(IEnumerable<CachedAudioDatData> layers)
        {
            var result = new CachedAudioDatData();
            foreach (var layer in layers)
            {
                foreach (var (id, name) in layer.NameById)
                    result.NameById[id] = name;

                AppendLists(result.StateGroupsByDialogueEvent, layer.StateGroupsByDialogueEvent);
                AppendLists(result.StatesByStateGroup, layer.StatesByStateGroup);
            }

            return result;
        }

        private static void AppendLists(Dictionary<string, List<string>> target, Dictionary<string, List<string>> source)
        {
            foreach (var (key, values) in source)
            {
                if (!target.TryGetValue(key, out var targetValues))
                {
                    targetValues = [];
                    target[key] = targetValues;
                }

                targetValues.AddRange(values);
            }
        }

        private void SetCurrentBnkFiles(List<string> bnkPaths)
        {
            PackFileByBnkName = [];
            foreach (var bnkPath in bnkPaths)
            {
                var bnk = _packFileService.FindFile(bnkPath);
                if (bnk != null)
                    PackFileByBnkName.TryAdd(bnk.Name, bnk);
            }
        }

        private static Dictionary<string, ResolvedBnk> CreateEffectiveBnks(List<LoadedLayer> layers, List<string> languages)
        {
            var result = new Dictionary<string, ResolvedBnk>(StringComparer.OrdinalIgnoreCase);
            foreach (var layer in layers)
            {
                foreach (var bnk in layer.AudioCache.GetBnks())
                {
                    if (IsLanguageIncluded(bnk.Path, languages))
                        result[bnk.Path] = new ResolvedBnk(layer, bnk);
                }
            }
            return result;
        }

        private void EnsureAllCachedHircsLoaded()
        {
            if (_loadedLayers.Count == 0 || _allCachedHircsLoaded)
                return;

            var references = new List<BnkHircReference>();
            foreach (var layer in _loadedLayers)
                references.AddRange(layer.AudioCache.FindAllHircs(layer.ResolvedBnkPaths));

            _hircsById = _bnkLoader
                .LoadHircs(references)
                .GroupBy(hirc => hirc.Id)
                .ToDictionary(group => group.Key, group => group.ToList());
            _hircsByType.Clear();
            _allCachedHircsLoaded = true;
        }

        private Dictionary<uint, List<HircItem>> GetAllCachedHircs()
        {
            EnsureAllCachedHircsLoaded();
            return _hircsById;
        }

        private void EnsureAllCachedDidxLoaded()
        {
            if (_loadedLayers.Count == 0 || _allCachedDidxLoaded)
                return;

            var didxById = new Dictionary<uint, List<DidxAudio>>();
            foreach (var layer in _loadedLayers)
            {
                var references = layer.AudioCache.FindDidx(
                    layer.ResolvedBnkPaths);
                foreach (var reference in references)
                {
                    var didx = _bnkLoader.LoadDidx(reference);
                    if (didx == null)
                        continue;

                    if (!didxById.TryGetValue(didx.Id, out var entries))
                    {
                        entries = [];
                        didxById[didx.Id] = entries;
                    }
                    entries.Add(didx);
                }
            }

            _didxAudioListById = didxById;
            _allCachedDidxLoaded = true;
        }

        private Dictionary<uint, List<DidxAudio>> GetAllCachedDidx()
        {
            EnsureAllCachedDidxLoaded();
            return _didxAudioListById;
        }

        private static string CreateCombinedFingerprint(List<AudioCacheSource> sources)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", sources.Select(source => source.Fingerprint)))));
        }

        private static bool HasRelevantAudioFiles(List<IPackFileContainer> containers)
        {
            return containers.Any(container => container.SearchFiles(null, [".bnk", ".dat", ".wwiseids"]).Count != 0);
        }

        private static bool IsLanguageIncluded(string bnkPath, List<string> languages)
        {
            if (languages.Count == 0)
                return true;

            var normalisedPath = bnkPath.Replace('/', '\\');
            var localisedLanguages = Wh3LanguageInformation.GetAllLanguages().Where(language => !language.Equals("sfx", StringComparison.OrdinalIgnoreCase));
            foreach (var language in localisedLanguages)
            {
                if (!normalisedPath.Contains($"\\{language}\\", StringComparison.OrdinalIgnoreCase))
                    continue;

                return languages.Contains(language, StringComparer.OrdinalIgnoreCase);
            }

            return true;
        }

        private static void DisposeLayers(List<LoadedLayer> layers)
        {
            foreach (var layer in layers)
                layer.AudioCache.Dispose();
        }

        public void Dispose()
        {
            _eventHub.UnRegister(this);
            Clear();
        }

        private sealed class LoadedLayer(AudioCache audioCache)
        {
            public AudioCache AudioCache { get; } = audioCache;
            public HashSet<string> ResolvedBnkPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed record ResolvedBnk(LoadedLayer Layer, AudioCache.CachedAudioBnk Bnk);
    }
}
