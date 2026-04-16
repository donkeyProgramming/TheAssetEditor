using System;
using System.IO;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.Vmd;
using Shared.GameFormats.WsModel;
using static Shared.GameFormats.Vmd.VariantMeshDefinition;

namespace GameWorld.Core.Services
{
    public class ComplexMeshLoader
    {
        private readonly ILogger _logger = Logging.Create<ComplexMeshLoader>();
        private readonly IPackFileService _packFileService;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;

        public ComplexMeshLoader(Rmv2ModelNodeLoader rmv2ModelNodeLoader, IPackFileService packFileService)
        {
            _packFileService = packFileService;
            _rmv2ModelNodeLoader = rmv2ModelNodeLoader;
        }

        public SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            return Load(file, parent, player, null, onlyLoadRootNode, onlyLoadFirstMesh);
        }

        public SceneNode Load(PackFile file, AnimationPlayer player, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            return Load(file, null, player, null, onlyLoadRootNode, onlyLoadFirstMesh);
        }

        SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            try
            {
                if (file == null)
                    throw new Exception("File is null in SceneLoader::Load");

                _logger.Here().Information($"Attempting to load file {file.Name}");

                switch (file.Extension)
                {
                    case ".variantmeshdefinition":
                        LoadVariantMesh(file, ref parent, player, attachmentPointName, onlyLoadRootNode, onlyLoadFirstMesh);
                        break;

                    case ".rigid_model_v2":
                        LoadRigidMesh(file, ref parent, player, attachmentPointName, onlyLoadRootNode);
                        break;

                    case ".wsmodel":
                        LoadWsModel(file, ref parent, player, attachmentPointName, onlyLoadRootNode);
                        break;
                    default:
                        throw new Exception("Unknown mesh extention");
                }

                return parent;
            }
            catch (Exception e)
            {
                var packFileOwner = _packFileService.GetPackFileContainer(file);
                var errorMessage = $"Failed to load file : '{file.Name}' from '{packFileOwner?.Name}' - IsCa:{packFileOwner?.IsCaPackFile}";
                _logger.Here().Error(errorMessage);
                _logger.Here().Error("Error : " + e.ToString());

                throw new Exception(errorMessage, e);
            }
        }

        void Load(string path, SceneNode parent, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            var file = _packFileService.FindFile(path);
            if (file == null)
            {
                _logger.Here().Error($"File {path} not found");
                return;
            }

            Load(file, parent, player, attachmentPointName, onlyLoadRootNode, onlyLoadFirstMesh);
        }


        void LoadVariantMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            
            var meshFile = VariantMeshDefinitionLoader.Load(file);
            LoadVariantMesh(meshFile, variantMeshElement, player, attachmentPointName, onlyLoadRootNode, onlyLoadFirstMesh);
        }

        void LoadVariantMesh(VariantMesh mesh, SceneNode root, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode, bool onlyLoadFirstMesh)
        {
            if (mesh.ChildSlots.Count != 0)
                root = root.AddObject(new SlotsNode("Slots"));

            // Load model
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) != true)
                Load(mesh.ModelReference.ToLower(), root, player, attachmentPointName, onlyLoadRootNode, onlyLoadFirstMesh);

            foreach (var slot in mesh.ChildSlots)
            {
                var slotNode = root.AddObject(new SlotNode(slot.Name + " " + slot.AttachmentPoint, slot.AttachmentPoint));

               
                foreach (var childMesh in slot.ChildMeshes)
                {
                    if (onlyLoadFirstMesh)
                    {
                        var meshNodes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(slotNode);
                        if (meshNodes.Any())
                            break;
                    }

                    LoadVariantMesh(childMesh, slotNode, player, attachmentPointName, onlyLoadRootNode, onlyLoadFirstMesh);
                }

                foreach (var meshReference in slot.ChildReferences)
                {
                    if (onlyLoadFirstMesh)
                    {
                        var meshNodes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(slotNode);
                        if (meshNodes.Any())
                            break;
                    }

                    Load(meshReference.Reference.ToLower(), slotNode, player, slot.AttachmentPoint, onlyLoadRootNode, onlyLoadFirstMesh);
                }

                for (var i = 0; i < slotNode.Children.Count(); i++)
                {
                    slotNode.Children[i].IsVisible = i == 0;
                    slotNode.Children[i].IsExpanded = false;

                    if (slotNode.Name.Contains("stump_"))
                    {
                        slotNode.IsVisible = false;
                        slotNode.IsExpanded = false;
                    }
                }
            }
        }

        Rmv2ModelNode LoadRigidMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode, WsModelFile? wsModel = null)
        {
            var rmvModel = ModelFactory.Create().Load(file.DataSource.ReadData());

            var modelFullPath = _packFileService.GetFullPath(file);
            var modelNode = new Rmv2ModelNode(Path.GetFileName(file.Name));
            var lodNodes = _rmv2ModelNodeLoader.CreateModelNodesFromFile(rmvModel, modelFullPath, onlyLoadRootNode, wsModel);
            foreach (var lodNode in lodNodes)
            {
                SceneNodeHelper
                    .GetChildrenOfType<Rmv2MeshNode>(lodNode)
                    .ForEach(x => x.AnimationPlayer = player);

                modelNode.AddObject(lodNode);
            }


            foreach (var mesh in modelNode.GetMeshNodes(0))
                mesh.AttachmentPointName = attachmentPointName;

            if (parent == null)
                parent = modelNode;
            else
                parent.AddObject(modelNode);

            return modelNode;
        }

        void LoadWsModel(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName, bool onlyLoadRootNode)
        {
            var wsModelNode = new WsModelGroup("WsModel - " + file.Name);
            if (parent == null)
                parent = wsModelNode;
            else
                parent.AddObject(wsModelNode);

            var wsMaterial = new WsModelFile(file);
            if (string.IsNullOrWhiteSpace(wsMaterial.GeometryPath) == false)
            {
                var modelFile = _packFileService.FindFile(wsMaterial.GeometryPath);
                var modelAsBase = wsModelNode as SceneNode;
                var loadedModelNode = LoadRigidMesh(modelFile, ref modelAsBase, player, attachmentPointName, onlyLoadRootNode, wsMaterial);
            }
        }
    }
}
