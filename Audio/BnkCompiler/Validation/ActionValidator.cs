using CommonControls.Editors.AudioEditor.BnkCompiler;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;

namespace Audio.BnkCompiler.Validation
{
    public class ActionValidator : AbstractValidator<Action>
    {
        public ActionValidator(List<IHircProjectItem> allItems)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x.ChildList)
                .NotEmpty().WithMessage("No child added.")
                .Must(x=>x.Count == 1).WithMessage("Only one action child is supported at this time.")
                .ForEach(x=>x.SetValidator(new ActionChildValidator(allItems)));
        }
    }

    public class ActionChildValidator : AbstractValidator<ActionChild>
    {
        private readonly List<string> ValidActionTypes = new List<string>() { "Play" };

        public ActionChildValidator(List<IHircProjectItem> allItems)
        {
            RuleFor(x => x.Id).Must(x=>ValidateChildReference(x, allItems)).WithMessage($"ActionChild has invalid reference");
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("ActionChild has no type")
                .Must(ValidateChildActionType).WithMessage(x=>$"ActionChild has invalid type '{x.Type}'. Valid values are {string.Join(", ", ValidActionTypes)}");
        }

        private bool ValidateChildReference(string id, List<IHircProjectItem> allItems) => allItems.Any(x => x.Id == id);
        private bool ValidateChildActionType(string childType) => ValidActionTypes.Contains(childType, StringComparer.InvariantCultureIgnoreCase);
    }
}
