using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;
using static Filetypes.RigidModel.VariantMeshDefinition;

namespace View3D.Services
{
    public class SceneLoader
    {
        ILogger _logger = Logging.Create<SceneLoader>();
        PackFileService _packFileService;
        GraphicsDevice _device;
        ResourceLibary _resourceLibary;

        public SceneLoader(PackFileService packFileService, ResourceLibary resourceLibary)
        {
            _packFileService = packFileService;
            _device = resourceLibary.GraphicsDevice;
            _resourceLibary = resourceLibary;
        }

        public void Load(string path, ISceneNode parent, AnimationPlayer player)
        {
            var file = _packFileService.FindFile(path);
            Load(file as PackFile, parent, player);
        }

        public ISceneNode Load(PackFile file, ISceneNode parent, AnimationPlayer player)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.Name}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent, player);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent, player);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent, player);
                    break;
            }

            return parent;
        }

        void LoadVariantMesh(PackFile file, ref ISceneNode parent, AnimationPlayer player)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            var slotsElement = variantMeshElement.AddObject( new SlotsNode("Slots"));

            var vmdContent = Encoding.UTF8.GetString(file.DataSource.ReadData());

            VariantMeshFile meshFile = null;
            try
            {
                meshFile = VariantMeshDefinition.Create(vmdContent);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to load file : " + file.Name);
                _logger.Here().Error("File content : " + vmdContent);
                _logger.Here().Error("Error : " + e.ToString());
                throw e;
            }
           

            foreach (var slot in meshFile.VARIANT_MESH.SLOT)
            {
                var slotElement = slotsElement.AddObject(new SlotNode(slot.Name));

                foreach (var mesh in slot.VariantMeshes)
                {
                    if (mesh.Name != null)
                        Load(mesh.Name.ToLower(), slotElement, player);
                }

                foreach (var meshReference in slot.VariantMeshReferences)
                    Load(meshReference.definition.ToLower(), slotElement, player);

                for (int i = 0; i < slotElement.Children.Count(); i++)
                {
                    slotElement.Children[i].IsVisible = i == 0;
                    slotElement.Children[i].IsExpanded = false;

                    if (slotElement.Name.Contains("stump_"))
                    {
                        slotElement.IsVisible = false;
                        slotElement.IsExpanded = false;
                    }
                }
            }
        }

        void LoadRigidMesh(PackFile file, ref ISceneNode parent, AnimationPlayer player)
        {
            var rmvModel = new RmvRigidModel(file.DataSource.ReadData(), file.Name);
            var model = new Rmv2ModelNode(rmvModel, _device, _resourceLibary, Path.GetFileName( rmvModel.FileName), player);

            if (parent == null)
                parent = model;
            else
                parent.AddObject(model);
        }

        void LoadWsModel(PackFile file, ref ISceneNode parent, AnimationPlayer player)
        {
            var wsModelNode = new GroupNode("WsModel - " + file.Name);
            if (parent == null)
                parent = wsModelNode;
            else
                parent.AddObject(wsModelNode);

            var buffer = file.DataSource.ReadData();
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            var nodes = doc.SelectNodes(@"/model/geometry");
            foreach (XmlNode node in nodes)
            {
                var modelFile = _packFileService.FindFile( node.InnerText) as PackFile;
                var modelAsBase = wsModelNode as ISceneNode;
                LoadRigidMesh(modelFile, ref modelAsBase, player);
            }
        }
    }
}
