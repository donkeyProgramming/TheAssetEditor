﻿using Audio.Presentation.AudioExplorer;
using Editors.Shared.DevConfig.Base;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.DevConfig.Configs
{
    internal class Audio_Wh3 : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public Audio_Wh3(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = false;
        }

        public void OpenFileOnLoad()
        {
            _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioEditorViewModel>();
        }
    }
}
