using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using System;

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

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, AudioInputProject project, HircProjectItemRepository repository)
        {
            var typedProjectItem = projectItem as GameSound;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, repository);
        }

        public CAkSound_v136 ConvertToWWise(GameSound inputSound, HircProjectItemRepository repository)
        {
            var file = _pfs.FindFile(inputSound.Path);
            var soundIdStr = Path.GetFileNameWithoutExtension(inputSound.Path).Trim();
            var soundId = uint.Parse(soundIdStr);

            var wwiseSound = new CAkSound_v136()
            {
                Id = repository.GetHircItemId(inputSound.Id),
                Type = HircType.Sound,
                AkBankSourceData = new AkBankSourceData()
                {
                    PluginId = 0x00010001,  // [PCM]
                    StreamType = SourceType.Data_BNK,
                    akMediaInformation = new AkMediaInformation()
                    {
                        SourceId = soundId,
                        uInMemoryMediaSize = (uint)file.DataSource.Size,
                        uSourceBits = 0x01,
                    }
                },
                NodeBaseParams = NodeBaseParams.CreateDefault()
            };

            wwiseSound.UpdateSize();
            return wwiseSound;
        }

        public List<CAkSound_v136> ConvertToWWise(IEnumerable<GameSound> inputSound, HircProjectItemRepository repository)
        {
            return inputSound.Select(x => ConvertToWWise(x, repository)).ToList();
        }
    }
}
