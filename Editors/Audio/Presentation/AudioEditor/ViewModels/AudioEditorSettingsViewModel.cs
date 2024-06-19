using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public class AudioEditorSettingsViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor Settings");

        public AudioEditorSettingsViewModel()
        {
        }

        public void Close() { }
        public bool Save() => true;
        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }
}
