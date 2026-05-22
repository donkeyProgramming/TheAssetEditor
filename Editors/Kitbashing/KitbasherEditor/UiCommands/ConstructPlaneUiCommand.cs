using Editors.KitbasherEditor.Commands;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using Shared.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ConstructPlaneUiCommand : ITransientKitbasherUiCommand
    {
        private readonly CommandFactory _commandFactory;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public ConstructPlaneUiCommand(CommandFactory commandFactory, LocalizationManager localizationManager)
        {
            _commandFactory = commandFactory;
            ToolTip = localizationManager.Get("KitbashTool.Debug.Geometry.CreatePlane.Tooltip");
        }

        public void Execute()
        {
            _commandFactory
                .Create<ConstructPrimitiveCommand>()
                .Configure(x => x.Configure(PrimitiveType.Plane))
                .BuildAndExecute();
        }
    }
}
