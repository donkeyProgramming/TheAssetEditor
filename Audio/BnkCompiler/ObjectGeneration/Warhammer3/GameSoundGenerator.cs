using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class GameSoundGenerator
    {
        private readonly PackFileService _pfs;

        public GameSoundGenerator(PackFileService pfs)
        {
            _pfs = pfs;
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
