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
           
           RuleFor(x => x.AudioBus)
               .NotEmpty().WithMessage(x => $"Event '{x.Id}' did not specify an audio bus")
               .Must(ValidateAudioBus).WithMessage(x => $"Event '{x.Id}' has has an invalid audioBus '{x.AudioBus}'. Valid values are {string.Join(", ", ValidAudioBusses)}");
        }

        private bool ValidateActionReference(string actionId, List<IAudioProjectHircItem> allItems ) => allItems.Any(x => x.Id == actionId && x is Action);   // Can only point to actions! 
        private bool ValidateAudioBus(string childType) => ValidAudioBusses.Contains(childType, StringComparer.InvariantCultureIgnoreCase);
    }


}
