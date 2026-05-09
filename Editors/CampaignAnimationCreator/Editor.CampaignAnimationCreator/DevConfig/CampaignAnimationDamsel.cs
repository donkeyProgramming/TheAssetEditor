using CommunityToolkit.Diagnostics;
using Editor.CampaignAnimationCreator.CampaignAnimationCreator;
using Editors.Shared.Core.Common;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;

namespace Editor.CampaignAnimationCreator.DevConfig
{
    internal class CampaignAnimationDamsel : IDeveloperConfiguration
    {
        private readonly IEditorManager _editorCreator;
        private readonly IPackFileService _packFileService;
        private readonly SceneObjectEditor _sceneObjectEditor;

        public CampaignAnimationDamsel(IEditorManager editorCreator, IPackFileService packFileService, SceneObjectEditor sceneObjectEditor)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
            _sceneObjectEditor = sceneObjectEditor;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.ShowCAWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            // Configure the data we want to load
            var mesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition");
            Guard.IsNotNull(mesh, "Failed to find mesh");
            var fragmentName = @"animations/database/battle/bin/hu1b_bretonnian_fay_enchantress.bin";
            var animationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("Walk_1");

            var editor = _editorCreator.Create(EditorEnums.CampaginAnimation_Editor) as CampaignAnimationCreatorViewModel;
            Guard.IsNotNull(editor, "Failed to cast editor");

            // Load the data
            var sceneObject = editor.SceneObjects.First();
            var sceneObjectData = sceneObject.Data;
            _sceneObjectEditor.SetMesh(sceneObjectData, mesh);

            var frag = sceneObject.FragAndSlotSelection.FragmentList.PossibleValues.FirstOrDefault(x => x.FullPath == fragmentName);
            sceneObject.FragAndSlotSelection.FragmentList.SelectedItem = frag;

            var slot = sceneObject.FragAndSlotSelection.FragmentSlotList.PossibleValues.First(x => x.SlotName == animationSlot.Value);
            sceneObject.FragAndSlotSelection.FragmentSlotList.SelectedItem = slot;
        }
    }
}
