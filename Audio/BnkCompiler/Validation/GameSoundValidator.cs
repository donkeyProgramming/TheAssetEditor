using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using FluentValidation;

namespace Audio.BnkCompiler.Validation
{
    public class GameSoundValidator : AbstractValidator<GameSound>
    {
        public GameSoundValidator(PackFileService pfs) 
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x.Path)
                .NotEmpty().WithMessage(x => $"GameSound '{x.Id}' is missing file path")
                .Must(x=>BeValidPath(x, pfs)).WithMessage(x => $"GameSound '{x.Id}' path does not point to anything: {x.Path}");
        }

        private bool BeValidPath(string path, PackFileService pfs) => pfs.FindFile(path) != null;
    }
}
