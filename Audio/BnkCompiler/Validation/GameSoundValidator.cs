using CommonControls.Services;
using FluentValidation;

namespace Audio.BnkCompiler.Validation
{
    public class GameSoundValidator : AbstractValidator<GameSound>
    {
        public GameSoundValidator(PackFileService pfs) 
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x.Path)
                .NotEmpty().WithMessage(x => $"GameSound '{x.Name}' is missing file path")
                .Must(x=>BeValidPath(x, pfs)).WithMessage(x => $"GameSound '{x.Name}' path does not point to a file: '{x.Path}'");
        }

        private bool BeValidPath(string path, PackFileService pfs)
        {
            if(string.IsNullOrWhiteSpace(path)) 
                return false;
            return pfs.FindFile(path) != null;
        }
    }
}
