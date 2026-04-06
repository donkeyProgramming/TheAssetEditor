using Editors.KitbasherEditor.UiCommands;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.KitbasherEditor.DevConfig
{
    internal class Kitbash_MeshFitter : IDeveloperConfiguration
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IScopeRepository _scopeRepository;

        public Kitbash_MeshFitter(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, IUiCommandFactory uiCommandFactory, IScopeRepository scopeRepository)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _uiCommandFactory = uiCommandFactory;
            _scopeRepository = scopeRepository;
        }

        public void OpenFileOnLoad()
        {
            // Open the kitbashe editor for the given mesh
            var inputFile = _packFileService.FindFile(@"VariantMeshes/wh_variantmodels/br1/ksl/ksl_bear/ksl_bear_light_01.rigid_model_v2");
            var editor = _uiCommandFactory.Create<OpenEditorCommand>().Execute(inputFile, EditorEnums.Kitbash_Editor);
           
            // Import the second mesh so its ready for running the meshFitter tool
            var typedEditor = editor as KitbasherEditor.ViewModels.KitbasherViewModel;
            var editorHandle = _scopeRepository.GetEditorHandles().First();
            var uiCommandFactory = _scopeRepository.GetRequiredService<IUiCommandFactory>(editorHandle);

            uiCommandFactory.Create<ImportReferenceMeshCommand>().Execute(@"variantmeshes\wh_variantmodels\bo1\bst\bst_tuskgor\bst_tuskgor_01.wsmodel");
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\AnimationTransfer_bear.pack";

            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }
    }
}
