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

            // Export to file 
            RuleFor(x => x)
                .Custom((settings, context) =>
                {
                    if (settings.ExportResultToFile == false)
                        return;

                    // OutputFilePath
                    if (string.IsNullOrEmpty(settings.OutputFilePath))
                        context.AddFailure("OutputFilePath is not specified");
                    if (Directory.Exists(settings.OutputFilePath) == false)
                        context.AddFailure("The output directory is not found or is invalid");

                });

            // Export to xml, requires export to file to be enabled 
            RuleFor(x => x)
                .Custom((settings, context) =>
                {
                    if (settings.ConvertResultToXml == false)
                        return;

                    if (settings.ExportResultToFile == false)
                        context.AddFailure("If ConvertResultToXml is set, ExportResultToFile must be true as well.");

                    // WWiserPath
                    if (string.IsNullOrEmpty(settings.WWiserPath))
                        context.AddFailure("WWiser path is null or empty");
                    if (File.Exists(settings.WWiserPath) == false)
                        context.AddFailure("Path to wwiser is not valid");
                });

        }
    }
}
