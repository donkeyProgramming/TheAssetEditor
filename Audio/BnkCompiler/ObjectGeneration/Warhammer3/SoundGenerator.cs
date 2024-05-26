using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using Shared.Core.PackFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class SoundGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(GameSound);

        private readonly PackFileService _pfs;

        public SoundGenerator(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as GameSound;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkSound_v136 ConvertToWWise(GameSound inputSound, CompilerData project)
        {
            var file = _pfs.FindFile(inputSound.Path);

            var nodeBaseParams = NodeBaseParams.CreateDefault();
            var wavFile = Path.GetFileName(inputSound.Path);
            var wavFileName = wavFile.Replace(".wem", "");

            var wwiseSound = new CAkSound_v136()
            {
                Id = project.GetHircItemIdFromName(inputSound.Name),
                Type = HircType.Sound,
                AkBankSourceData = new AkBankSourceData()
                {
                    PluginId = 0x00040001,  // [VORBIS]
                    StreamType = SourceType.Streaming,
                    akMediaInformation = new AkMediaInformation()
                    {
                        SourceId = uint.Parse(wavFileName),
                        uInMemoryMediaSize = (uint)file.DataSource.Size,
                        uSourceBits = 0x01,
                    }
                },
                NodeBaseParams = nodeBaseParams
            };

            wwiseSound.NodeBaseParams.DirectParentId = project.GetHircItemIdFromName(inputSound.DirectParentId);

            // Applying Dialogue_Event attenuation directly to sounds as they don't appear to take the vanilla mixer's attenuation
            if (inputSound.IsDialogueEventSound == true)
            {
                var dialogueEventBnk = CompilerConstants.MatchDialogueEventToBnk(inputSound.DialogueEvent);
                var attenuationKey = $"{dialogueEventBnk}_attenuation";

                if (CompilerConstants.VanillaIds.ContainsKey(attenuationKey))
                {
                    var attenuationId = CompilerConstants.VanillaIds[attenuationKey];

                    wwiseSound.NodeBaseParams.NodeInitialParams.AkPropBundle0 = new AkPropBundle()
                    {
                        Values = new List<AkPropBundle.AkPropBundleInstance>()
                        {
                            new(){Type = AkPropBundleType.Attenuation, Value = attenuationId}
                        }
                    };
                }
            }

            // Testing: force apply attenuation ID
            /*
            wwiseSound.NodeBaseParams.NodeInitialParams.AkPropBundle0 = new AkPropBundle()
            {
                Values = new List<AkPropBundle.AkPropBundleInstance>()
                        {
                            new(){Type = AkPropBundleType.Attenuation, Value = 803409642}
                        }
            };
            */

            wwiseSound.UpdateSize();
            return wwiseSound;
        }

        public List<CAkSound_v136> ConvertToWWise(IEnumerable<GameSound> inputSound, CompilerData project)
        {
            return inputSound.Select(x => ConvertToWWise(x, project)).ToList();
        }
    }
}
