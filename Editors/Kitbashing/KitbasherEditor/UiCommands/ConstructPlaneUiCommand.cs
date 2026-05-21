using Editors.KitbasherEditor.Commands;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ConstructPlaneUiCommand : ITransientKitbasherUiCommand
    {
        private readonly IUiCommandFactory _commandFactory;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public ConstructPlaneUiCommand(IUiCommandFactory commandFactory, LocalizationManager localizationManager)
        {
            _commandFactory = commandFactory;
            ToolTip = localizationManager.Get("KitbashTool.Debug.Geometry.CreatePlane.Tooltip");
        }

        public void Execute()
        {
            _commandFactory
                .CreateWithBuilder<ConstructPrimitiveCommand>()
                .Configure(x => x.Configure(PrimitiveType.Plane))
                .BuildAndExecute();
        }
    }
}
