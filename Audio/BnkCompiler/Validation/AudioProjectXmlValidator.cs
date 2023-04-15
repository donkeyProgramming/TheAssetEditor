using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler.Validation
{
    public class AudioProjectXmlValidator : AbstractValidator<AudioProjectXml>
    {
        public AudioProjectXmlValidator(PackFileService pfs, AudioProjectXml projectXml)
        {
            var allItems = GetAllItems(projectXml);

            RuleFor(x => x).NotNull().WithMessage("Project file is missing");

            // Validate project settings output file
            RuleFor(x => x.OutputFile)
                .NotEmpty().WithMessage("Output file is missing or invalid. eg MyFileName.bnk")
                .Must(x => Path.GetExtension(x).ToLower() == ".bnk").WithMessage("Output file must be a bnk");

            RuleFor(x => x.OutputGame)
                .NotEmpty().WithMessage("OutputGame must be set")
                .Equal("Warhammer3", StringComparer.InvariantCultureIgnoreCase).WithMessage("Only Warhammer3 supported");

            RuleFor(x => x.Version)
                .NotEmpty().WithMessage("Version must be set")
                .Equal("1", StringComparer.InvariantCultureIgnoreCase).WithMessage("Only 1 supported");

            // Validate all objects
            RuleFor(x => x.GameSounds).ForEach(x => x.SetValidator(new GameSoundValidator(pfs)));
            RuleFor(x => x.Actions).ForEach(x => x.SetValidator(new ActionValidator(allItems)));
            RuleFor(x => x.Events).ForEach(x => x.SetValidator(new EventValidator(allItems)));

            // Validate that all ids are Uniqe
            RuleFor(x => x).Custom((projectFile, context) => ValidateUniqeIds(projectXml, context));
        }

        void ValidateUniqeIds(AudioProjectXml projectXml, ValidationContext<AudioProjectXml> context)
        {
            var allIds = GetAllItems(projectXml)
             .Select(x => x.Id)
             .ToList();

            var query = allIds.GroupBy(x => x)
              .Where(g => g.Count() > 1)
              .Select(y => new { Element = y.Key, Counter = y.Count() })
              .ToList();

            var duplicates = query.Where(x => x.Counter != 1).ToList();
            foreach (var duplicate in duplicates)
                context.AddFailure("Duplicate key", $"{duplicate.Element} is used {duplicate.Counter} times. Ids must be unique");
        }

        List<IHircProjectItem> GetAllItems(AudioProjectXml projectXml)
        {
            var output = new List<IHircProjectItem>();
            output.AddRange(projectXml.Actions);
            output.AddRange(projectXml.Events);
            output.AddRange(projectXml.GameSounds);
            return output;
        }
    }
}
