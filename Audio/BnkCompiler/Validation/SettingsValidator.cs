using CommonControls.Editors.AudioEditor.BnkCompiler;
using FluentValidation;
using System;
using System.IO;

namespace Audio.BnkCompiler.Validation
{
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
                 .NotEmpty().WithMessage("Bnk name missing, mest end with .bnk. Example: 'mybank'")
                 .Must(x => Path.GetExtension(x)?.ToLower() == string.Empty).WithMessage("bnkName should not include any exptentions");
        }
    }
}
