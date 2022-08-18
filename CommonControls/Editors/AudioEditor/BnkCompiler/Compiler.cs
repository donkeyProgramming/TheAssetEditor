using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Bkhd;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.FileTypes.Sound.WWise.Hirc.V136;
using CommonControls.Services;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public class Compiler
    {
        private readonly PackFileService _pfs;
        public PackFile OutputBnkFile { get; private set; }
        public PackFile OutputDatFile { get; private set; }
        public AudioProjectXml ProjectFile { get; private set; }

        public Compiler(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public bool Compile(string projectAsString, out ErrorListViewModel.ErrorList errorList)
        {
            errorList = new ErrorListViewModel.ErrorList();
            ProjectFile = LoadFile(projectAsString, ref errorList);
            if (ProjectFile == null)
                return false;

            var validationResult = AudioProjectValidator.Validate(ProjectFile, _pfs, ref errorList);
            if (validationResult == false)
                return false;

            OutputBnkFile = BuildBnk(ProjectFile);
            OutputDatFile = BuildDat(ProjectFile);
            BuildNameLookUpFile(ProjectFile);

            return true;
        }

        public bool CompileAll(out ErrorListViewModel.ErrorList errorList)
        {
            errorList = new ErrorListViewModel.ErrorList();
            errorList.Ok("Compiler", "Finished");
            return true;
        }

        PackFile BuildBnk(AudioProjectXml projectFile)
        {
            var bnkName = Path.GetFileNameWithoutExtension(projectFile.OutputFile);
            

            // Header
            var bankHeader = CompileHeader(projectFile, bnkName);  

            //-------------------
            // Build Hirc list

            var hircList = new List<HircItem>();
            hircList.AddRange(projectFile.GameSounds.Select(x => ConvertToWWiseGameSound(x)));
            hircList.AddRange(projectFile.Actions.Select(x => ConvertToWWiseAction(x, bnkName)));
            hircList.AddRange(projectFile.Events.Select(x => ConvertToWWiseEvent(x)));
            hircList.ForEach(x => x.ComputeSize());

            HircChunk hircChunk = new HircChunk();
            hircChunk.Hircs.AddRange(hircList);
            hircChunk.ChunkHeader.ChunkSize = (uint)(hircChunk.Hircs.Sum(x=>x.Size) + (hircChunk.Hircs.Count() * 5) + 4);
            hircChunk.NumHircItems = (uint)hircChunk.Hircs.Count();
            
            var hircParse = new HircParser();
            var hircBytes = hircParse.GetAsBytes(hircChunk);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(bankHeader);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile("TestFile.bnk", new MemorySource(bytes));
            var result = Bnkparser.Parse(bnkPackFile, "test\\TestFile.bnk"); 

            return bnkPackFile;
        }

        byte[] CompileHeader(AudioProjectXml projectFile, string bnkName)
        {
            var soundBankId = ConvertStringToWWiseId(bnkName);
            var header = new BkhdHeader()
            {
                dwBankGeneratorVersion = 0x80000088,
                dwSoundBankID = soundBankId,
                dwLanguageID = 393239870,
                bFeedbackInBank = 0x10,
                dwProjectID = 2361,
                padding = 0x04,
            };

            return BkhdParser.GetAsByteArray(header);
        }

        private CAkSound_v136 ConvertToWWiseGameSound(GameSound inputSound)
        {
            var filename = $"audio\\wwise\\{inputSound.Text}";
            var file = _pfs.FindFile(filename);
            var soundIdStr = Path.GetFileNameWithoutExtension(inputSound.Text).Trim();
            var soundId = uint.Parse(soundIdStr);

            var wwiseSound = new CAkSound_v136()
            {
                Id = ConvertStringToWWiseId(inputSound.Id),
                Type = HircType.Sound,
                AkBankSourceData = new AkBankSourceData()
                {
                    PluginId = 0x00040001,
                    StreamType = SourceType.Data_BNK,
                    akMediaInformation = new AkMediaInformation()
                    {
                        SourceId = soundId,
                        uInMemoryMediaSize = (uint)file.DataSource.Size,
                        uSourceBits = 0x00,
                    }
                },
                NodeBaseParams = NodeBaseParams.CreateDefault()
            };

            return wwiseSound;
        }

        CAkEvent_v136 ConvertToWWiseEvent(Event inputEvent)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = ConvertStringToWWiseId(inputEvent.Id);
            wwiseEvent.Type = FileTypes.Sound.WWise.HircType.Event;
            wwiseEvent.Actions = new List<CAkEvent_v136.Action>()
            { 
                new CAkEvent_v136.Action(){ ActionId = ConvertStringToWWiseId(inputEvent.Action)}
            };
            return wwiseEvent;
        }

        CAkAction_v136 ConvertToWWiseAction(Action inputAction, string bnkName)
        {
            if (inputAction.ChildList.Count != 1)
                throw new NotImplementedException();

            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = ConvertStringToWWiseId(inputAction.Id);
            wwiseAction.Type = FileTypes.Sound.WWise.HircType.Action;
            wwiseAction.ActionType = FileTypes.Sound.WWise.ActionType.Play;
            wwiseAction.idExt = ConvertStringToWWiseId(inputAction.ChildList.First().Text);

            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = ConvertStringToWWiseId(bnkName);

            return wwiseAction;
        }

        PackFile BuildDat(AudioProjectXml projectFile)
        {
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Id, Value = 0 });

            var bytes = DatParser.GetAsByteArray(datFile);
            var packFile = new PackFile("testfile.data", new MemorySource(bytes));
            var reparsedSanityFile = DatParser.Parse(packFile, false);
            return packFile;
        }

        private void BuildNameLookUpFile(AudioProjectXml projectFile)
        {
            
        }


        uint ConvertStringToWWiseId(string id) => WWiseNameLookUpHelper.ComputeWWiseHash(id);
       

        AudioProjectXml LoadFile(string projectAsString, ref ErrorListViewModel.ErrorList errorList)
        {
            try
            {
                using var stream = GenerateStreamFromString(projectAsString);
                XmlSerializer serializer = new XmlSerializer(typeof(AudioProjectXml));
                var result = serializer.Deserialize(stream);
                return result as AudioProjectXml;
            }
            catch (Exception e)
            {
                errorList.Error("Unable to serialize project file", $"{e.Message} Please validate the XML at https://www.w3schools.com/xml/xml_validator.asp");
                return null;
            }
        }


        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
