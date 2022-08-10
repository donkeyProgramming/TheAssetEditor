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

        public Compiler(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public bool Compile(string projectAsString, out ErrorListViewModel.ErrorList errorList)
        {
            errorList = new ErrorListViewModel.ErrorList();
            var projectFile = LoadFile(projectAsString, ref errorList);
            if (projectFile == null)
                return false;

            var validationResult = AudioProjectValidator.Validate(projectFile, _pfs, ref errorList);
            if (validationResult == false)
                return false;

            OutputBnkFile = BuildBnk(projectFile);
            OutputDatFile = BuildDat(projectFile);
            BuildNameLookUpFile(projectFile);

            return true;
        }




        PackFile BuildBnk(AudioProjectXml projectFile)
        {
            var bnkName = Path.GetFileNameWithoutExtension(projectFile.OutputFile);
            using var memStream = new MemoryStream();

            projectFile.GameSounds.Clear();
            projectFile.Actions.Clear();
            //projectFile.Events.Clear();

            // Header
            var bankHeader = CompileHeader(projectFile, bnkName);  
            memStream.Write(bankHeader);

            var sounds = projectFile.GameSounds.Select(x => CompileGameSound(x)).ToList();
            var actions = projectFile.Actions.Select(x => CompileAction(x, bnkName)).ToList();


            //-------------------
            // Build Hirc list

            var hircList = new List<HircItem>();
            var hircEvents = projectFile.Events.Select(x => ConvertToWWiseEvent(x));
            hircList.AddRange(hircEvents);


            hircList.ForEach(x => x.ComputeSize());

            HircChunk hircChunk = new HircChunk();
            hircChunk.Hircs.AddRange(hircEvents);
            hircChunk.ChunkHeader.ChunkSize = (uint)(hircEvents.Sum(x=>x.Size) + (hircChunk.Hircs.Count() * 5) + 4);
            hircChunk.NumHircItems = (uint)hircChunk.Hircs.Count();
            
            var hircParse = new HircParser();
            var hircBytes = hircParse.GetAsBytes(hircChunk);
            memStream.Write(hircBytes);

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile("TestFile.bnk", new MemorySource(memStream.ToArray()));
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

        private byte[] CompileGameSound(GameSound inputSound)
        {
            return new byte[] { };
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
            wwiseEvent.ComputeSize();
            return wwiseEvent;
        }



        byte[] CompileAction(Action inputAction, string bnkName)
        {
            var wwiseAction = new CAkAction_v136();
            wwiseAction.Id = ConvertStringToWWiseId(inputAction.Id);
            wwiseAction.Type = FileTypes.Sound.WWise.HircType.Action;
            wwiseAction.ActionType = FileTypes.Sound.WWise.ActionType.Play;
            wwiseAction.idExt = ConvertStringToWWiseId(inputAction.Child);

            wwiseAction.AkPlayActionParams.byBitVector = 0x04;
            wwiseAction.AkPlayActionParams.bankId = ConvertStringToWWiseId(bnkName);

            return wwiseAction.GetAsByteArray();
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
