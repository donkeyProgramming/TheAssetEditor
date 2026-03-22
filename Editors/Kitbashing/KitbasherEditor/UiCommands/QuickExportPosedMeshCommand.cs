using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.ImportExport.Exporting;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.MenuSystem;
using Shared.GameFormats.RigidModel;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External; // <- added
using MessageBox = System.Windows.MessageBox;

namespace Editors.KitbasherEditor.UiCommands
{
    public class QuickExportPosedMeshCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Pose selected mesh at current frame and open export dialog";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private readonly GeometrySaveSettings _saveSettings;
        private readonly IExportFileContextMenuHelper _exportFileContextMenuHelper;

        public QuickExportPosedMeshCommand(
            AnimationsContainerComponent animationsContainerComponent,
            SelectionManager selectionManager,
            CommandFactory commandFactory,
            SceneManager sceneManager,
            SaveService saveService,
            GeometrySaveSettings saveSettings,
            IExportFileContextMenuHelper exportFileContextMenuHelper)
        {
            _animationsContainerComponent = animationsContainerComponent;
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
            _sceneManager = sceneManager;
            _saveService = saveService;
            _saveSettings = saveSettings;
            _exportFileContextMenuHelper = exportFileContextMenuHelper;
        }

        public void Execute()
        {
            try
            {
                // Get the current animation frame
                var animationPlayers = _animationsContainerComponent;
                var mainPlayer = animationPlayers.Get("MainPlayer");

                var frame = mainPlayer.GetCurrentAnimationFrame();
                if (frame is null)
                {
                    MessageBox.Show("An animation must be playing for this tool to work");
                    return;
                }

                var state = _selectionManager.GetState<ObjectSelectionState>();
                var selectedObjects = state.SelectedObjects();
                var selectedMeshNodes = selectedObjects.OfType<Rmv2MeshNode>().ToList();

                if (!selectedMeshNodes.Any())
                {
                    MessageBox.Show("No mesh objects selected");
                    return;
                }

                // Step 1: Create a posed static mesh (same as CreateStaticMeshCommand)
                var meshes = new List<Rmv2MeshNode>();
                var groupNodeContainer = new GroupNode("posedMesh_Export");
                var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                var lod0 = root.GetLodNodes()[0];
                lod0.AddObject(groupNodeContainer);

                // Hide originals while we create posed copies so they are not included in the save
                var originalVisibility = new Dictionary<Rmv2MeshNode, bool>();
                foreach (var meshNode in selectedMeshNodes)
                {
                    originalVisibility[meshNode] = meshNode.IsVisible;
                    meshNode.IsVisible = false;

                    var cpy = SceneNodeHelper.CloneNode(meshNode) as Rmv2MeshNode;
                    if (cpy != null)
                    {
                        cpy.IsVisible = true;
                        groupNodeContainer.AddObject(cpy);
                        meshes.Add(cpy);
                    }
                }

                // Step 2: Pose the meshes at the current animation frame
                _commandFactory.Create<CreateAnimatedMeshPoseCommand>()
                    .IsUndoable(false)
                    .Configure(x => x.Configure(meshes, frame, true))
                    .BuildAndExecute();

                // Step 3: Save the posed mesh to a temporary file
                var tempPath = Path.Combine(Path.GetTempPath(), $"posed_{Guid.NewGuid():N}.rigid_model_v2");

                // Update save settings with temp path
                _saveSettings.OutputName = tempPath;

                // Temporarily replace LOD children so only the posed group is saved
                var lodNodes = root.GetLodNodes();
                var originalLodChildren = new Dictionary<Rmv2LodNode, List<ISceneNode>>();
                foreach (var lod in lodNodes)
                {
                    originalLodChildren[lod] = new List<ISceneNode>(lod.Children);
                    lod.Children.Clear();
                }

                // Add posed group to lod0 for saving
                if (lodNodes.Count > 0)
                    lodNodes[0].AddObject(groupNodeContainer);

                var saveResult = _saveService.Save(root, _saveSettings);
                if (saveResult is null || saveResult.Status == false)
                {
                    // Restore LOD children before returning
                    foreach (var kv in originalLodChildren)
                    {
                        var lod = kv.Key;
                        var list = kv.Value;
                        lod.Children.Clear();
                        foreach (var child in list)
                            lod.AddObject(child);
                    }

                    MessageBox.Show("Failed to save posed mesh");
                    return;
                }

                // Step 4: Create a PackFile from the temporary file and export
                try
                {
                    // If the save returned an in-memory RmvFile, serialize that; otherwise read the file bytes
                    byte[] fileBytes;
                    if (saveResult.GeneratedMesh != null)
                    {
                        // Serialize RmvFile to bytes
                        fileBytes = ModelFactory.Create().Save(saveResult.GeneratedMesh);
                    }
                    else if (!string.IsNullOrWhiteSpace(saveResult.GeneratedMeshPath) && File.Exists(saveResult.GeneratedMeshPath))
                    {
                        fileBytes = File.ReadAllBytes(saveResult.GeneratedMeshPath);
                    }
                    else
                    {
                        // Fallback: try reading tempPath if it exists
                        if (File.Exists(tempPath))
                            fileBytes = File.ReadAllBytes(tempPath);
                        else
                        {
                            MessageBox.Show("Unable to obtain generated mesh bytes for export");
                            return;
                        }
                    }

                    var posedPackFile = PackFile.CreateFromBytes(Path.GetFileName(tempPath), fileBytes);

                    _exportFileContextMenuHelper.ShowDialog(posedPackFile);

                    // Clean up temp file after export dialog closes
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                    finally
                    {
                        // Restore original visibility and remove the temporary posed group
                        foreach (var kv in originalVisibility)
                        {
                            kv.Key.IsVisible = kv.Value;
                        }

                        // Remove posed group from scene
                        var parent = groupNodeContainer.Parent;
                        if (parent != null)
                            parent.RemoveObject(groupNodeContainer);
                        // Restore original LOD children
                        foreach (var kv in originalLodChildren)
                        {
                            var lod = kv.Key;
                            var list = kv.Value;
                            lod.Children.Clear();
                            foreach (var child in list)
                                lod.AddObject(child);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting posed mesh: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in quick export: {ex.Message}");
            }
        }
    }
}
