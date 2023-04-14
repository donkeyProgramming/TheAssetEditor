using CommonControls.Editors.AudioEditor.BnkCompiler;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;

namespace Audio.BnkCompiler.Validation
{
    public class EventValidator : AbstractValidator<Event>
    {
        readonly List<string> ValidAudioBusses = new List<string>() { "battle", "master" };

        public EventValidator(List<IHircProjectItem> allItems)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage(x => $"Event '{x.Id}' has an empty Action")
                .Must(actionId=>ValidateActionReference(actionId, allItems)).WithMessage(x => $"Event '{x.Id}' has has an invalid action reference '{x.Action}'");

            RuleFor(x => x.AudioBus)
                .NotEmpty().WithMessage(x => $"Event '{x.Id}' did not specify an audio bus")
                .Must(ValidateAudioBus).WithMessage(x => $"Event '{x.Id}' has has an invalid audioB bus '{x.AudioBus}'. Valid values are {string.Join(", ", ValidAudioBusses)}");
        }

        private bool ValidateActionReference(string actionId, List<IHircProjectItem> allItems ) => allItems.Any(x => x.Id == actionId && x is Action);   // Can only point to actions! 
        private bool ValidateAudioBus(string childType) => ValidAudioBusses.Contains(childType, StringComparer.InvariantCultureIgnoreCase);
    }
}
