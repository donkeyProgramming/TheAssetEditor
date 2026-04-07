using System;
using System.Linq;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.Ui.Common.MenuSystem;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    public class OpenMeshFitterToolCommand : IScopedKitbasherUiCommand, IDisposable
    {
        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly IStandardDialogs _standardDialogs;
        private readonly SelectionManager _selectionManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonHelper;
        private readonly IAbstractFormFactory<MeshFitterWindow> _formFactory;
        private readonly SceneManager _sceneManager;
        private readonly LocalizationManager _localizationManager;

        MeshFitterWindow? _windowHandle;

        public OpenMeshFitterToolCommand(IStandardDialogs standardDialogs, SelectionManager selectionManager, ISkeletonAnimationLookUpHelper skeletonHelper, IAbstractFormFactory<MeshFitterWindow> formFactory, SceneManager sceneManager, LocalizationManager localizationManager)
        {
            _standardDialogs = standardDialogs;
            _selectionManager = selectionManager;
            _skeletonHelper = skeletonHelper;
            _formFactory = formFactory;
            _sceneManager = sceneManager;
            _localizationManager = localizationManager;
            ToolTip = _localizationManager.Get("KitbashTool.MeshFitterTool.ToolTip");
        }

        public void Execute()
        {
            if (_windowHandle != null)
            {
                _windowHandle.BringIntoView();
                return;
            }

            var meshesToFit = _selectionManager
                .GetState<ObjectSelectionState>()
                .CurrentSelection();

            var meshNodes = meshesToFit
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .Cast<Rmv2MeshNode>()
                .ToList();

            if (meshNodes.Count == 0)
            {
                ShowError("KitbashTool.MeshFitterTool.NoMeshSelected");
                return;
            }

            if (meshNodes.Any(x => string.IsNullOrWhiteSpace(x.Geometry.SkeletonName)))
            {
                ShowError("KitbashTool.MeshFitterTool.SelectedMeshMissingSkeleton");
                return;
            }

            var allSkeltonNames = meshNodes
                .Select(x => x.Geometry.SkeletonName)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            if (allSkeltonNames.Count != 1)
            {
                var commaList = string.Join(",", allSkeltonNames);
                ShowError("KitbashTool.MeshFitterTool.InvalidSkeletonSelection", commaList);
                return;
            }

            var currentSkeletonName = allSkeltonNames.First();
            var currentSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(currentSkeletonName);
            if (currentSkeletonFile == null)
            {
                ShowError("KitbashTool.MeshFitterTool.MeshSkeletonNotFound", currentSkeletonName);
                return;
            }

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x => (int)x)
                .ToList();

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            if (rootNode?.SkeletonNode?.Skeleton == null)
            {
                ShowError("KitbashTool.MeshFitterTool.TargetSkeletonMissing");
                return;
            }

            var targetSkeletonNode = rootNode.SkeletonNode;
            var targetSkeletonName = targetSkeletonNode.Skeleton.SkeletonName;
            if (string.IsNullOrWhiteSpace(targetSkeletonName))
            {
                ShowError("KitbashTool.MeshFitterTool.TargetSkeletonMissing");
                return;
            }

            var targetSkeletonFile = _skeletonHelper.GetSkeletonFileFromName(targetSkeletonName);
            if (targetSkeletonFile == null)
            {
                ShowError("KitbashTool.MeshFitterTool.TargetSkeletonNotFound", targetSkeletonName);
                return;
            }

            var config = new RemappedAnimatedBoneConfiguration();
            config.ParnetModelSkeletonName = targetSkeletonName;
            config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeletonFile);

            config.MeshSkeletonName = currentSkeletonName;
            config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(currentSkeletonFile, usedBoneIndexes);

            _windowHandle = _formFactory.Create();
            _windowHandle.ViewModel.Initialize(config, meshNodes, targetSkeletonNode.Skeleton, currentSkeletonFile);
            _windowHandle.Show();

            _windowHandle.Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_windowHandle != null)
            {
                _windowHandle.Closed -= OnWindowClosed;
                _windowHandle.Dispose();
                _windowHandle = null;
            }
        }

        public void Dispose()
        {
            _windowHandle?.Close();
            _windowHandle = null;
        }

        private void ShowError(string localizationKey, params object[] args)
        {
            var message = _localizationManager.Get(localizationKey);
            if (args.Length > 0)
                message = string.Format(message, args);

            var title = _localizationManager.Get("KitbasherTool.BoneMapping.ErrorTitle");
            _standardDialogs.ShowDialogBox(message, title);
        }
    }
}
