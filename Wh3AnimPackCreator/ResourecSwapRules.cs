using CommonControls.Editors.AnimMeta;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.MetaData.Definitions;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;

namespace Wh3AnimPackCreator
{
    public class ResourecSwapRules
    {
        protected BaseAnimationSlotHelper _targetGame;
        List<string> _supportedMetaTags;
        LogService _logService;

        protected Dictionary<string, string> _animationSlots = new Dictionary<string, string>();
        protected Dictionary<string, string> _vfx = new Dictionary<string, string>();

        public ResourecSwapRules(List<string> supportedMetaTags, LogService logService, BaseAnimationSlotHelper slotHelper)
        {
            _logService = logService;
            _targetGame = slotHelper;
            _supportedMetaTags = supportedMetaTags;
        }

        public string GetMatchingAnimationSlotName(string binName, string inputSlot)
        {
            _logService.StatsDb.ProcessedSlots.Add(inputSlot);

            if (_targetGame.GetfromValue(inputSlot) != null)
                return inputSlot;

            if (_animationSlots.ContainsKey(inputSlot))
            {
                _logService.AddLogItem(LogService.LogType.Info, binName, $"Converting AnimationSlot {inputSlot} => {_animationSlots[inputSlot]}", "SLOT_CONVERTION");
                return _animationSlots[inputSlot];
            }

            _logService.AddLogItem(LogService.LogType.Error, binName, $"Skipping AnimationSlot {inputSlot}, no mapping found", "SLOT_CONVERTION");
            _logService.StatsDb.MissingSlots.Add(inputSlot);

            return null;
        }

        public string GetmatchingVfx(string inputVfx)
        {
            if (_vfx.ContainsKey(inputVfx))
            {
                Console.WriteLine($"\t\t\t Converting VFX {inputVfx} => {_vfx[inputVfx]}");
                return _vfx[inputVfx];
            }

            Console.WriteLine($"\t\t\t Skipping VFX {inputVfx}");
            return null;
        }

        internal byte[] ConvertMetaFile(string binName, PackFile metaFile)
        {
            var bytes = metaFile.DataSource.ReadData();

            var metaParser = new MetaDataFileParser();
            var parsedMetaFile = metaParser.ParseFile(bytes);
            var metaDataList = new List<MetaDataTagItem>();

            foreach (var item in parsedMetaFile.Items)
            {
                bool keepMetaTag = false;
                //if (item is UnknownMetaEntry)
                //{
                //    _logService.AddLogItem(LogService.LogType.Error, binName, $"Unkown meta tag {item.Name}", "ANIMATION_META_UNKOWN");
                //    _logService.ErrorDatabase.UnsupportedMetadataTag.Add(item.Name);
                //}

                _logService.StatsDb.ProcessedMetadataTag.Add(item.DisplayName);

                var currentTagName = item.Name + "_V" + item.Version;
                if (_supportedMetaTags.Contains(currentTagName) == false || item.Description == "_TroyOnly")
                {
                    _logService.AddLogItem(LogService.LogType.Error, binName, $"Meta tag not supported {item.Description}", "ANIMATION_META_NOT_SUPPORTED");
                    _logService.StatsDb.UnsupportedMetadataTag.Add(item.DisplayName);
                    keepMetaTag = false;
                }

                if (item is IEffectMeta effectMeta)
                {
                    _logService.StatsDb.ProcessedEffects.Add(effectMeta.VfxName);
                    _logService.StatsDb.MissingEffects.Add(effectMeta.VfxName);
                }

                if (item is SoundTrigger_v10 soundMeta)
                {
                    _logService.StatsDb.ProcessedSounds.Add(soundMeta.SoundEvent);
                    _logService.StatsDb.MissingSounds.Add(soundMeta.SoundEvent);
                }

                if (keepMetaTag)
                {
                    var t = new MetaDataTagItemViewModel(item);
                    var tagData = t.GetAsData();
                    metaDataList.Add(tagData);
                }
            }

            var outputBytes = metaParser.GenerateBytes(parsedMetaFile.Version, metaDataList);
            return outputBytes;
        }
    }

    public class TroyResourceSwapRules : ResourecSwapRules
    {
        public TroyResourceSwapRules(List<string> supportedMetaTags, LogService logService)
            : base(supportedMetaTags, logService, new BaseAnimationSlotHelper(GameTypeEnum.Warhammer3))
        {
            _animationSlots["SPECIAL_ABILITY_SAVAGE_ROAR"] = "CAST_SPELL_FORWARD_MEDIUM";
        }

    }
}
