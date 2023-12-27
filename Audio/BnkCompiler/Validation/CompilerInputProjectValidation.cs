using FluentValidation;
using System.IO;


namespace Audio.BnkCompiler.Validation
{
    public class CompilerInputProjectValidation : AbstractValidator<CompilerInputProject>
    {
        public CompilerInputProjectValidation()
        {
            RuleFor(x => x).NotNull().WithMessage("Input project is null");
            RuleFor(x => x.Project).NotNull().WithMessage("No input events found");
            RuleFor(x => x.Settings).NotNull().WithMessage("Input project is missing Settings");

            RuleFor(x => x.Settings.BnkName)
                    .NotEmpty().WithMessage("Bnk name missing, Example: 'mybank'")
                    .Must(x => Path.GetExtension(x)?.ToLower() == string.Empty).WithMessage("bnkName should not include any extension");

            RuleFor(x => x.Settings.Language)
                .Custom((projectFile, context) => { });

            RuleFor(x => x.Project).ForEach(x => x.SetValidator(new InputEventValidation()));
        }
    }

    public class InputEventValidation : AbstractValidator<CompilerInputProject.ProjectContents>
    {
        public InputEventValidation()
        {




        }
    }
}
