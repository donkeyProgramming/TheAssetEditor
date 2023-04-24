using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.Validation
{
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

        private bool ValidateActionReference(string actionId, List<IAudioProjectHircItem> allItems ) => allItems.Any(x => x.Name == actionId && x is Action);   // Can only point to actions! 
    }


}
