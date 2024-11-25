using FluentValidation;
using Shared.Core.PackFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;

namespace Editors.Audio.BnkCompiler.Validation
{
    public class AudioProjectXmlValidator : AbstractValidator<CompilerData>
    {
        public AudioProjectXmlValidator(IPackFileService pfs, CompilerData projectXml)
        {
            var allItems = GetAllItems(projectXml);

            RuleFor(x => x).NotNull().WithMessage("Project file is missing");

            RuleFor(x => x.ProjectSettings).SetValidator(new SettingsValidator());
            RuleFor(x => x.Sounds).ForEach(x => x.SetValidator(new GameSoundValidator(pfs)));
            RuleFor(x => x.Actions).ForEach(x => x.SetValidator(new ActionValidator(allItems)));
            RuleFor(x => x.Events).ForEach(x => x.SetValidator(new EventValidator(allItems)));

            // Validate that all ids are Unique
            RuleFor(x => x).Custom((projectFile, context) => ValidateUniqeIds(projectXml, context));
        }

        void ValidateUniqeIds(CompilerData projectXml, ValidationContext<CompilerData> context)
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
                context.AddFailure("Duplicate key", $"'{duplicate.Element}' is used {duplicate.Counter} times. Ids must be unique");
        }

        // Check for unreferenced ids

        List<IAudioProjectHircItem> GetAllItems(CompilerData projectXml)
        {
            var output = new List<IAudioProjectHircItem>();
            output.AddRange(projectXml.Actions);
            output.AddRange(projectXml.Events);
            output.AddRange(projectXml.Sounds);
            return output;
        }
    }

    public class EventValidator : AbstractValidator<Event>
    {
        public EventValidator(List<IAudioProjectHircItem> allItems)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");

            RuleFor(x => x.Actions)
                .NotEmpty().WithMessage(x => $"Event '{x.Id}' has no actions")
                .Custom((actionInstanceList, context) =>
                {
                    foreach (var action in actionInstanceList)
                    {
                        if (ValidateActionReference(action, allItems) == false)
                            context.AddFailure($"Event has has an invalid action reference: '{action}'");
                    }
                });
        }

        private bool ValidateActionReference(uint actionId, List<IAudioProjectHircItem> allItems) => allItems.Any(x => x.Id == actionId && x is Action);   // Can only point to actions! 
    }

    public class ActionValidator : AbstractValidator<Action>
    {
        private readonly List<string> ValidActionTypes = new List<string>() { "Play" };

        public ActionValidator(List<IAudioProjectHircItem> allItems)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");
            //RuleFor(x => x.ChildId).Must(x => ValidateChildReference(x, allItems)).WithMessage($"ActionChild has invalid reference");
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("ActionChild has no type")
                .Must(ValidateChildActionType).WithMessage(x => $"ActionChild has invalid type '{x.Type}'. Valid values are {string.Join(", ", ValidActionTypes)}");
        }

        private bool ValidateChildReference(uint id, List<IAudioProjectHircItem> allItems) => allItems.Any(x => x.Id == id);
        private bool ValidateChildActionType(string childType) => ValidActionTypes.Contains(childType, StringComparer.InvariantCultureIgnoreCase);
    }

    public class GameSoundValidator : AbstractValidator<Sound>
    {
        public GameSoundValidator(IPackFileService pfs)
        {
            RuleFor(x => x).NotNull().WithMessage("Item is null");
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x)
                .Custom((x, context) =>
                {
                });
        }
    }

    public class SettingsValidator : AbstractValidator<ProjectSettings>
    {
        public SettingsValidator()
        {
            RuleFor(x => x.Version)
                .Must(x => x == 1).WithMessage("Only version one is allowed");

            RuleFor(x => x.OutputGame)
                .NotEmpty().WithMessage($"Output game not selected. Only '{CompilerConstants.GameWarhammer3}' is supported")
                .Must(x => string.Compare(x, CompilerConstants.GameWarhammer3, StringComparison.InvariantCultureIgnoreCase) == 0).WithMessage("Only warhammer 3 is supported");

            RuleFor(x => x.BnkName)
                 .NotEmpty().WithMessage("Bnk name missing, Example: 'mybank'")
                 .Must(x => Path.GetExtension(x)?.ToLower() == string.Empty).WithMessage("bnkName should not include any extension");
        }
    }
}
