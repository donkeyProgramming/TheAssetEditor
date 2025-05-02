using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Bkhd;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioProjectCompiler
{
    public class SoundBankGenerator
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IFileSaveService _fileSaveService;

        public SoundBankGenerator(
            ApplicationSettingsService applicationSettingsService,
            IFileSaveService fileSaveService)
        {
            _applicationSettingsService = applicationSettingsService;
            _fileSaveService = fileSaveService;
        }

        public void GenerateSoundBanks(AudioProject audioProject)
        {
            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            var wwiseHircGeneratorServiceFactory = WwiseHircGeneratorServiceFactory.CreateFactory(bankGeneratorVersion);

            foreach (var soundBank in audioProject.SoundBanks)
            {
                var bkhdChunk = BkhdChunkGenerator.GenerateBkhdChunk(audioProject, bankGeneratorVersion, soundBank);

                var hircItems = new List<HircItem>();

                if (soundBank.ActionEvents != null)
                    GenerateActionEventSoundBanks(wwiseHircGeneratorServiceFactory, soundBank, hircItems);

                if (soundBank.DialogueEvents != null)
                    GenerateDialogueEventSoundBanks(wwiseHircGeneratorServiceFactory, soundBank, hircItems);

                var hircChunk = new HircChunk();
                hircChunk.WriteData(hircItems);

                var headerBytes = BkhdParser.WriteData(bkhdChunk);
                var hircBytes = new HircParser().WriteData(hircChunk, bankGeneratorVersion);

                // Write
                using var memStream = new MemoryStream();
                memStream.Write(headerBytes);
                memStream.Write(hircBytes);
                var bytes = memStream.ToArray();

                // Convert to output and parse for sanity
                var bnkPackFile = new PackFile(soundBank.SoundBankFileName, new MemorySource(bytes));
                var parser = new BnkParser();
                var reparsedSanityFile = parser.Parse(bnkPackFile, "test\\fakefilename.bnk", true);

                _fileSaveService.Save(soundBank.SoundBankFilePath, bnkPackFile.DataSource.ReadData(), true);
            }
        }

        private static void GenerateActionEventSoundBanks(WwiseHircGeneratorServiceFactory wwiseHircGeneratorServiceFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            var sourceHircs = new Dictionary<HircItem, List<HircItem>>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (actionEvent.Sound != null)
                {
                    var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.Sound, soundBank);
                    if (!sourceHircs.Keys.Any(sourceHirc => sourceHirc.Id == soundHirc.Id))
                        sourceHircs.Add(soundHirc, []);
                }
                else
                {
                    var soundHircs = actionEvent.RandomSequenceContainer.Sounds
                        .Select(sound => wwiseHircGeneratorServiceFactory.GenerateHirc(sound, soundBank))
                        .ToList();

                    var randomSequenceContainerHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.RandomSequenceContainer, soundBank);
                    if (!sourceHircs.Keys.Any(sourceHirc => sourceHirc.Id == randomSequenceContainerHirc.Id))
                        sourceHircs.Add(randomSequenceContainerHirc, soundHircs);
                }
            }

            foreach (var sourceHirc in sourceHircs.OrderBy(sourceHirc => sourceHirc.Key.Id))
            {
                foreach (var soundHirc in sourceHirc.Value)
                    hircItems.Add(soundHirc);

                hircItems.Add(sourceHirc.Key);
            }

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                foreach (var action in actionEvent.Actions)
                {
                    var actionHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(action, soundBank);
                    hircItems.Add(actionHirc);
                }

                var actionEventHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent, soundBank);
                hircItems.Add(actionEventHirc);
            }
        }

        private static void GenerateDialogueEventSoundBanks(WwiseHircGeneratorServiceFactory wwiseHircGeneratorServiceFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            var sourceHircs = new Dictionary<HircItem, List<HircItem>>();

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                foreach (var statePath in dialogueEvent.StatePaths)
                {
                    if (statePath.Sound != null)
                    {
                        var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(statePath.Sound, soundBank);
                        sourceHircs.Add(soundHirc, []);
                    }
                    else
                    {
                        var soundHircs = statePath.RandomSequenceContainer.Sounds
                            .Select(sound => wwiseHircGeneratorServiceFactory.GenerateHirc(sound, soundBank))
                            .ToList();

                        var randomSequenceContainerHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(statePath.RandomSequenceContainer, soundBank);
                        sourceHircs.Add(randomSequenceContainerHirc, soundHircs);
                    }
                }
            }

            foreach (var sourceHirc in sourceHircs.OrderBy(sourceHirc => sourceHirc.Key.Id))
            {
                foreach (var soundHirc in sourceHirc.Value)
                    hircItems.Add(soundHirc);

                hircItems.Add(sourceHirc.Key);
            }

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                var actionHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(dialogueEvent, soundBank);
                hircItems.Add(actionHirc);
            }
        }
    }
}
