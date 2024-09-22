﻿using System;
using System.IO;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;
using Shared.Ui.Editors.VariantMeshDefinition;
using static Shared.GameFormats.Vmd.VariantMeshDefinition;

namespace GameWorld.Core.Services
{
    public class ComplexMeshLoader
    {
        private readonly ILogger _logger = Logging.Create<ComplexMeshLoader>();
        private readonly PackFileService _packFileService;
        private readonly Rmv2ModelNodeLoader _rmv2ModelNodeLoader;

        public ComplexMeshLoader(Rmv2ModelNodeLoader rmv2ModelNodeLoader, PackFileService packFileService)
        {
            _packFileService = packFileService;
            _rmv2ModelNodeLoader = rmv2ModelNodeLoader;
        }

        public SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player)
        {
            return Load(file, parent, player, null);
        }

        public SceneNode Load(PackFile file, AnimationPlayer player)
        {
            return Load(file, null, player, null);
        }

        SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.Name}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent, player, attachmentPointName);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent, player, attachmentPointName);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent, player, attachmentPointName);
                    break;
                default:
                    throw new Exception("Unknown mesh extention");
            }

            return parent;
        }

        void Load(string path, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var file = _packFileService.FindFile(path);
            if (file == null)
            {
                _logger.Here().Error($"File {path} not found");
                return;
            }

            Load(file, parent, player, attachmentPointName);
        }


        void LoadVariantMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            try
            {
                var meshFile = VariantMeshToXmlConverter.Load(file);
                LoadVariantMesh(meshFile, variantMeshElement, player, attachmentPointName);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to load file : " + file.Name);
                _logger.Here().Error("Error : " + e.ToString());
                throw;
            }
        }

        void LoadVariantMesh(VariantMesh mesh, SceneNode root, AnimationPlayer player, string attachmentPointName)
        {
            if (mesh.ChildSlots.Count != 0)
                root = root.AddObject(new SlotsNode("Slots"));

            // Load model
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) != true)
                Load(mesh.ModelReference.ToLower(), root, player, attachmentPointName);

            foreach (var slot in mesh.ChildSlots)
            {
                var slotNode = root.AddObject(new SlotNode(slot.Name + " " + slot.AttachmentPoint, slot.AttachmentPoint));

                foreach (var childMesh in slot.ChildMeshes)
                    LoadVariantMesh(childMesh, slotNode, player, attachmentPointName);

                foreach (var meshReference in slot.ChildReferences)
                    Load(meshReference.Reference.ToLower(), slotNode, player, slot.AttachmentPoint);

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

        Rmv2ModelNode LoadRigidMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName, WsModelFile? wsModel = null)
        {
            var rmvModel = ModelFactory.Create().Load(file.DataSource.ReadData());

            var modelFullPath = _packFileService.GetFullPath(file);
            var modelNode = new Rmv2ModelNode(Path.GetFileName(file.Name));
            var lodNodes = _rmv2ModelNodeLoader.CreateModelNodesFromFile(rmvModel, modelFullPath, player, wsModel);
            foreach (var lodNode in lodNodes)
                modelNode.AddObject(lodNode);

            foreach (var mesh in modelNode.GetMeshNodes(0))
                mesh.AttachmentPointName = attachmentPointName;

            if (parent == null)
                parent = modelNode;
            else
                parent.AddObject(modelNode);


            return modelNode;
        }

        void LoadWsModel(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName)
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
                var loadedModelNode = LoadRigidMesh(modelFile, ref modelAsBase, player, attachmentPointName, wsMaterial);
            }
        }
    }
}
