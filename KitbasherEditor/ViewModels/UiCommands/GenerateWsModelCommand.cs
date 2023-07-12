using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public abstract class GenerateWsModelCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly SceneManager _sceneManager;
        private readonly WsModelGeneratorService _wsModelGeneratorService;

        public GenerateWsModelCommand(SceneManager sceneManager, WsModelGeneratorService wsModelGeneratorService)
        {
            _sceneManager = sceneManager;
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        protected CommonControls.Services.GameTypeEnum _gameFormat { get; set; } = CommonControls.Services.GameTypeEnum.Warhammer3;

        public void Execute()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _wsModelGeneratorService.GenerateWsModel(mainNode, _gameFormat);
        }
    }

    public class GenerateWh3WsModelCommand : GenerateWsModelCommand
    {
        public GenerateWh3WsModelCommand(SceneManager sceneManager, WsModelGeneratorService wsModelGeneratorService) : base(sceneManager, wsModelGeneratorService)
        {
            ToolTip = "Generate ws model (wh3)";
            _gameFormat = CommonControls.Services.GameTypeEnum.Warhammer3;
        }
    }

    public class GenerateWh2WsModelCommand : GenerateWsModelCommand
    {
        public GenerateWh2WsModelCommand(SceneManager sceneManager, WsModelGeneratorService wsModelGeneratorService) : base(sceneManager, wsModelGeneratorService)
        {
            ToolTip = "Generate ws model (wh2)";
            _gameFormat = CommonControls.Services.GameTypeEnum.Warhammer2;
        }
    }




}
