using CommonControls.Services;
using FluentValidation;
using System;
using System.IO;

namespace Audio.BnkCompiler.Validation
{
    public class GameSoundValidator : AbstractValidator<GameSound>
    {
        public GameSoundValidator(PackFileService pfs) 
        {
            RuleFor(x => x).NotNull().WithMessage("Item is null");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Item is missing ID");
            RuleFor(x => x)
                .Custom((x, context) => 
                {
                    bool hasPackFileRef = string.IsNullOrWhiteSpace(x.Path) == false;
                    bool hasSystemFileRef = string.IsNullOrWhiteSpace(x.SystemFilePath) == false;
                    var refCount = Convert.ToInt32(hasPackFileRef) + Convert.ToInt32(hasSystemFileRef);
                    if (refCount != 1)
                    {
                        context.AddFailure("Boop boop");
                        return;
                    }
                    if (hasPackFileRef)
                        EnsureIsValidPackFileRef(x, pfs, context);

                    if (hasSystemFileRef)
                        EnsureValidFileSystemRef(x, context);
                });
        }

        private void EnsureIsValidPackFileRef(GameSound gamesound, PackFileService pfs, ValidationContext<GameSound> context)
        {
            if(pfs.FindFile(gamesound.Path) == null)
                context.AddFailure($"{gamesound.Name} - Path does not poing to a valid packfile '{gamesound.Path}'");
        }

        private void EnsureValidFileSystemRef(GameSound gamesound, ValidationContext<GameSound> context)
        {
            if (File.Exists(gamesound.SystemFilePath) == false)
                context.AddFailure($"{gamesound.Name} - SystemFilePath does not poing to a valid file on disk '{gamesound.SystemFilePath}'");
        }
    }
}
