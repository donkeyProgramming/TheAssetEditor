﻿using CommonControls.Services;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler.Validation
{
    public class AudioProjectXmlValidator : AbstractValidator<CompilerData>
    {
        public AudioProjectXmlValidator(PackFileService pfs, CompilerData projectXml)
        {
            var allItems = GetAllItems(projectXml);

            RuleFor(x => x).NotNull().WithMessage("Project file is missing");

            RuleFor(x => x.ProjectSettings).SetValidator(new SettingsValidator());
            RuleFor(x => x.GameSounds).ForEach(x => x.SetValidator(new GameSoundValidator(pfs)));
            RuleFor(x => x.Actions).ForEach(x => x.SetValidator(new ActionValidator(allItems)));
            RuleFor(x => x.Events).ForEach(x => x.SetValidator(new EventValidator(allItems)));

            // Validate that all ids are Uniqe
            RuleFor(x => x).Custom((projectFile, context) => ValidateUniqeIds(projectXml, context));
        }

        void ValidateUniqeIds(CompilerData projectXml, ValidationContext<CompilerData> context)
        {
            var allIds = GetAllItems(projectXml)
             .Select(x => x.Name)
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
            output.AddRange(projectXml.GameSounds);
            return output;
        }
    }

    public class EventValidator : AbstractValidator<Event>
    {
        public EventValidator(List<IAudioProjectHircItem> allItems)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Item is missing ID");

            RuleFor(x => x.Actions)
                .NotEmpty().WithMessage(x => $"Event '{x.Name}' has no actions")
                .Custom((actionInstanceList, context) =>
                {
                    foreach (var action in actionInstanceList)
                    {
                        if (ValidateActionReference(action, allItems) == false)
                            context.AddFailure($"Event has has an invalid action reference: '{action}'");
                    }
                });
        }

        private bool ValidateActionReference(string actionId, List<IAudioProjectHircItem> allItems) => allItems.Any(x => x.Name == actionId && x is Action);   // Can only point to actions! 
    }

    public class ActionValidator : AbstractValidator<Action>
    {
        private readonly List<string> ValidActionTypes = new List<string>() { "Play" };

        public ActionValidator(List<IAudioProjectHircItem> allItems)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Item is missing ID");
            //6RuleFor(x => x.ChildId).Must(x => ValidateChildReference(x, allItems)).WithMessage($"ActionChild has invalid reference");
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("ActionChild has no type")
                .Must(ValidateChildActionType).WithMessage(x => $"ActionChild has invalid type '{x.Type}'. Valid values are {string.Join(", ", ValidActionTypes)}");
        }

        private bool ValidateChildReference(string id, List<IAudioProjectHircItem> allItems) => allItems.Any(x => x.Name == id);
        private bool ValidateChildActionType(string childType) => ValidActionTypes.Contains(childType, StringComparer.InvariantCultureIgnoreCase);
    }

    public class GameSoundValidator : AbstractValidator<GameSound>
    {
        public GameSoundValidator(PackFileService pfs)
        {
            RuleFor(x => x).NotNull().WithMessage("Item is null");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x)
                .Custom((x, context) =>
                {
                    //bool hasPackFileRef = string.IsNullOrWhiteSpace(x.Path) == false;
                    //var refCount = Convert.ToInt32(hasPackFileRef) + Convert.ToInt32(hasSystemFileRef);
                    //if (refCount != 1)
                    //{
                    //    context.AddFailure("Item has both file and pack file reference");
                    //    return;
                    //}
                    //if (hasPackFileRef)
                    //    EnsureIsValidPackFileRef(x, pfs, context);
                    //
                    //if (hasSystemFileRef)
                    //    EnsureValidFileSystemRef(x, context);
                });
        }

        // private void EnsureIsValidPackFileRef(GameSound gamesound, PackFileService pfs, ValidationContext<GameSound> context)
        // {
        //     if (pfs.FindFile(gamesound.Path) == null)
        //         context.AddFailure($"{gamesound.Name} - Path does not poing to a valid packfile '{gamesound.Path}'");
        // }
        //
        // private void EnsureValidFileSystemRef(GameSound gamesound, ValidationContext<GameSound> context)
        // {
        //     if (File.Exists(gamesound.SystemFilePath) == false)
        //         context.AddFailure($"{gamesound.Name} - SystemFilePath does not poing to a valid file on disk '{gamesound.SystemFilePath}'");
        // }
    }

    public class SettingsValidator : AbstractValidator<ProjectSettings>
    {
        public SettingsValidator()
        {
            RuleFor(x => x.Version)
                .Must(x => x == 1).WithMessage("Only version one is allowed");

            RuleFor(x => x.OutputGame)
                .NotEmpty().WithMessage($"Output game not selected. Only '{CompilerConstants.Game_Warhammer3}' is supporeted")
                .Must(x => string.Compare(x, CompilerConstants.Game_Warhammer3, StringComparison.InvariantCultureIgnoreCase) == 0).WithMessage("Only warhammer 3 is supported");

            RuleFor(x => x.BnkName)
                 .NotEmpty().WithMessage("Bnk name missing, Example: 'mybank'")
                 .Must(x => Path.GetExtension(x)?.ToLower() == string.Empty).WithMessage("bnkName should not include any extension");
        }
    }
}
