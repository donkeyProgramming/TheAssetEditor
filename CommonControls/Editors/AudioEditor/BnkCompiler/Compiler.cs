using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
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
        public class CompileResult
        {
            public PackFile OutputBnkFile { get; set; }
            public PackFile OutputDatFile { get; set; }
            public PackFile NameList { get; set; }
            public List<PackFile> AudioFiles { get; set; } = new List<PackFile>();
        }

        private readonly PackFileService _pfs;

        public AudioProjectXml ProjectFile { get; private set; }

        public Compiler(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public bool CompileAll(out ErrorListViewModel.ErrorList outputList)
        {
            outputList = new ErrorListViewModel.ErrorList();

            if (_pfs.HasEditablePackFile() == false)
                return false;

            var allProjectFiles = _pfs.FindAllWithExtention(".xml").Where(x => x.Name.Contains("bnk.xml"));
            outputList.Ok("Compiler", $"{allProjectFiles.Count()} projects found to compile.");

            foreach (var file in allProjectFiles)
            {
                outputList.Ok("Compiler", $"Starting {_pfs.GetFullPath(file)}");
                var compileResult = CompileProject(file, ref outputList);
                if (compileResult != null)
                {
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputBnkFile, true);
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputDatFile, true);

                    //foreach(var audioFile in compileResult.AudioFiles)
                    //    SaveHelper.SavePackFile(_pfs, "wwise\\audio", audioFile, true);
                }

                outputList.Ok("Compiler", $"Finished {_pfs.GetFullPath(file)}. Overall result:{true}");
            }
            return true;
        }

        public CompileResult CompileProject(PackFile packfile, ref ErrorListViewModel.ErrorList errorList)
        {
            ProjectFile = LoadFile(packfile, ref errorList);
            if (ProjectFile == null)
                return null;

            var validationResult = AudioProjectValidator.Validate(ProjectFile, _pfs, ref errorList);
            if (validationResult == false)
                return null;

            var bnkFile = BuildBnk(ProjectFile);
            var datFile = BuildDat(ProjectFile);
            var files0 = BuildGameAudioFiles(ProjectFile);
            var files1 = BuildFileAudioFiles(ProjectFile);
            var nameList = BuildNameLookUpFile(ProjectFile);

            return new CompileResult()
            {
                OutputBnkFile = bnkFile,
                OutputDatFile = datFile,
                NameList = nameList
            };
        }

        private List<PackFile> BuildGameAudioFiles(AudioProjectXml projectFile)
        {
            foreach (var file in projectFile.GameSounds)
            {
                // If language file, copy
                var dirName = Path.GetDirectoryName(file.Text);
                var fileName = Path.GetFileNameWithoutExtension(file.Text);

                //var dirNamess = Path.
            }

            return new List<PackFile>();
        }

        private List<PackFile> BuildFileAudioFiles(AudioProjectXml projectFile)
        {
            foreach (var file in projectFile.GameSounds)
            {
                // Import
                // Give name
                // Save 

            }
            return new List<PackFile>();
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
            var bnkPackFile = new PackFile(projectFile.OutputFile, new MemorySource(bytes));
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
            var file = _pfs.FindFile(inputSound.Text);
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
            var outputName = "event_data__" + Path.GetFileNameWithoutExtension(projectFile.OutputFile) + ".dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Id, Value = 0 });

            var bytes = DatParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatParser.Parse(packFile, false);
            return packFile;
        }

        private PackFile BuildNameLookUpFile(AudioProjectXml projectFile)
        {
            return null;
        }


        uint ConvertStringToWWiseId(string id) => WWiseNameLookUpHelper.ComputeWWiseHash(id);
       

        AudioProjectXml LoadFile(PackFile packfile, ref ErrorListViewModel.ErrorList errorList)
        {
            try
            {
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);

                using var stream = GenerateStreamFromString(str);
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
