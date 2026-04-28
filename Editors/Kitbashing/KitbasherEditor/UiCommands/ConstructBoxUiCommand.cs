using Editors.KitbasherEditor.Commands;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using Shared.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ConstructBoxUiCommand : ITransientKitbasherUiCommand
    {
        private readonly CommandFactory _commandFactory;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public ConstructBoxUiCommand(CommandFactory commandFactory, LocalizationManager localizationManager)
        {
            _commandFactory = commandFactory;
            ToolTip = localizationManager.Get("KitbashTool.Debug.Geometry.CreateBox.Tooltip");
        }

        public void Execute()
        {
            _commandFactory
                .Create<ConstructPrimitiveCommand>()
                .Configure(x => x.Configure(PrimitiveType.Box))
                .BuildAndExecute();
        }
    }
}