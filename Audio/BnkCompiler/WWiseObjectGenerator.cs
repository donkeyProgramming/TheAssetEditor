using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Utility;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public class WWiseObjectGenerator
    {
        public class WWiseProject
        {
            public BkhdHeader Header { get; set; }
            public HircChunk HircItems { get; set; }
        }


        private readonly PackFileService _pfs;
        private readonly AudioProjectXml _projectFile;
        private List<IHircProjectItem> _allProjectItems = new List<IHircProjectItem>();

        public WWiseObjectGenerator(PackFileService pfs, AudioProjectXml projectFile)
        {
            _pfs = pfs;
            _projectFile = projectFile;

            _allProjectItems.AddRange(_projectFile.Events);
            _allProjectItems.AddRange(_projectFile.Actions);
            _allProjectItems.AddRange(_projectFile.GameSounds);
        }

        uint ConvertStringToWWiseId(string id) => WWiseHash.Compute(id);
       
        uint GetHircItemId(string reference)
        {
            var item = _allProjectItems.First(x => x.Id == reference);
            if (item.ForceId.HasValue == true)
                return item.ForceId.Value;
            return ConvertStringToWWiseId(item.Id);
        }

        public WWiseProject Generate()
        {
            WWiseProject output = new WWiseProject();
            var bnkName = Path.GetFileNameWithoutExtension(_projectFile.OutputFile);

            // Header
            output.Header = ConstructHeader(bnkName);

            // Build Hirc list
            var hircList = new List<HircItem>();
            hircList.AddRange(_projectFile.GameSounds.Select(x => ConvertToWWiseGameSound(x)));
            hircList.AddRange(_projectFile.Actions.Select(x => ConvertToWWiseAction(x, bnkName)));
            hircList.AddRange(_projectFile.Events.Select(x => ConvertToWWiseEvent(x)));
            hircList.ForEach(x => x.UpdateSize());

            output.HircItems = new HircChunk();
            output.HircItems.SetFromHircList(hircList);

            // Validate this is same as before
            output.HircItems.ChunkHeader.ChunkSize = (uint)(output.HircItems.Hircs.Sum(x => x.Size) + (output.HircItems.Hircs.Count() * 5) + 4);
            output.HircItems.NumHircItems = (uint)output.HircItems.Hircs.Count();

            return output;
        }

        CAkEvent_v136 ConvertToWWiseEvent(Event inputEvent)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = GetHircItemId(inputEvent.Id);
            wwiseEvent.Type = HircType.Event;
            wwiseEvent.Actions = new List<CAkEvent_v136.Action>()
            {
                new CAkEvent_v136.Action(){ ActionId = GetHircItemId(inputEvent.Action)}
            };
            return wwiseEvent;
        }

        CAkAction_v136 ConvertToWWiseAction(Action inputAction, string bnkName)
        {
            if (inputAction.ChildList.Count != 1)
                throw new NotImplementedException();

            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = GetHircItemId(inputAction.Id);
            wwiseAction.Type = HircType.Action;
            wwiseAction.ActionType = ActionType.Play;
            wwiseAction.idExt = GetHircItemId(inputAction.ChildList.First().Id);

            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = ConvertStringToWWiseId(bnkName);

            return wwiseAction;
        }

        BkhdHeader ConstructHeader(string bnkName)
        {
            var soundBankId = ConvertStringToWWiseId(bnkName);
            var header = new BkhdHeader()
            {
                dwBankGeneratorVersion = 0x80000088,
                dwSoundBankID = soundBankId,
                dwLanguageID = 550298558, // English(UK)
                bFeedbackInBank = 0x10,
                dwProjectID = 2361,
                padding = 0x04,
            };
        
            return header;
        }

        private CAkSound_v136 ConvertToWWiseGameSound(GameSound inputSound)
        {
            var file = _pfs.FindFile(inputSound.Path);
            var soundIdStr = Path.GetFileNameWithoutExtension(inputSound.Path).Trim();
            var soundId = uint.Parse(soundIdStr);

            var wwiseSound = new CAkSound_v136()
            {
                Id = GetHircItemId(inputSound.Id),
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

            return wwiseSound;
        }





    }
}
