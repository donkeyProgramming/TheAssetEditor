using CommonControls.Events.UiCommands;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class GenerateWsModelCommand : IExecutableUiCommand
    {
        private readonly SceneManager _sceneManager;
        private readonly WsModelGeneratorService _wsModelGeneratorService;

        public GenerateWsModelCommand(SceneManager sceneManager, WsModelGeneratorService wsModelGeneratorService)
        {
            _sceneManager = sceneManager;
            _wsModelGeneratorService = wsModelGeneratorService;
        }

        public CommonControls.Services.GameTypeEnum GameFormat { get; set; } = CommonControls.Services.GameTypeEnum.Warhammer3;

        public void Execute()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _wsModelGeneratorService.GenerateWsModel(mainNode, GameFormat);
        }
    }
}
