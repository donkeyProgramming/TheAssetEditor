using Common;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.RigidModel;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using View3D.Animation;
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

        public void Load(string path, ISceneNode parent, AnimationPlayer player, ref string skeletonName)
        {
            var file = _packFileService.FindFile(path);
            if (file == null)
            {
                _logger.Here().Error($"File {path} not found");
                return;
            }

            Load(file, parent, player, ref skeletonName);
        }

        public ISceneNode Load(PackFile file, ISceneNode parent, AnimationPlayer player, ref string skeletonName)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.Name}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent, player, ref skeletonName);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent, player, ref skeletonName);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent, player, ref skeletonName);
                    break;
                default:
                    throw new Exception("Unknown mesh extention");
            }

            return parent;
        }


        void LoadVariantMesh(PackFile file, ref ISceneNode parent, AnimationPlayer player, ref string skeletonName)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            try
            {
                var vmdContent = Encoding.UTF8.GetString(file.DataSource.ReadData());
                var meshFile = VariantMeshToXmlConverter.LoadFromString(vmdContent);
                LoadVariantMesh(meshFile, variantMeshElement, player, ref skeletonName);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to load file : " + file.Name);
                _logger.Here().Error("Error : " + e.ToString());
                throw e;
            }
        }

        void LoadVariantMesh(VariantMesh mesh, ISceneNode root, AnimationPlayer player, ref string skeletonName)
        {
            if (mesh.ChildSlots.Count != 0)
                root = root.AddObject(new SlotsNode("Slots"));

            // Load model
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) != true)
                 Load(mesh.ModelReference.ToLower(), root, player, ref skeletonName); 

            foreach (var slot in mesh.ChildSlots)
            {
                var slotNode = root.AddObject(new SlotNode(slot.Name + " " + slot.AttachmentPoint, slot.AttachmentPoint));

                foreach (var childMesh in slot.ChildMeshes)
                    LoadVariantMesh(childMesh, slotNode, player, ref skeletonName);

                foreach (var meshReference in slot.ChildReferences)
                    Load(meshReference.Reference.ToLower(), slotNode, player, ref skeletonName);

                for (int i = 0; i < slotNode.Children.Count(); i++)
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

        Rmv2ModelNode LoadRigidMesh(PackFile file, ref ISceneNode parent, AnimationPlayer player, ref string skeletonName)
        {
            var rmvModel = new RmvRigidModel(file.DataSource.ReadData(), file.Name);
            var model = new Rmv2ModelNode(rmvModel, _resourceLibary, Path.GetFileName( rmvModel.FileName), player, GeometryGraphicsContextFactory.CreateInstance(_device));

            if (parent == null)
                parent = model;
            else
                parent.AddObject(model);
            if(!string.IsNullOrWhiteSpace(rmvModel.Header.SkeletonName))
                skeletonName = rmvModel.Header.SkeletonName;
            return model;
        }

        void LoadWsModel(PackFile file, ref ISceneNode parent, AnimationPlayer player, ref string skeletonName)
        {
            var wsModelNode = new WsModelGroup("WsModel - " + file.Name);
            if (parent == null)
                parent = wsModelNode;
            else
                parent.AddObject(wsModelNode);

            var buffer = file.DataSource.ReadData();
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            
            var geometryNodes = doc.SelectNodes(@"/model/geometry");
            if (geometryNodes.Count != 0)
            {
                var geometryNode = geometryNodes.Item(0);
                var modelFile = _packFileService.FindFile(geometryNode.InnerText) as PackFile;
                var modelAsBase = wsModelNode as ISceneNode;
                var loadedModelNode = LoadRigidMesh(modelFile, ref modelAsBase, player, ref skeletonName);

                // Materials
                var materialNodes = doc.SelectNodes(@"/model/materials/material");
                foreach (XmlNode materialNode in materialNodes)
                {
                    var materialFilePath = materialNode.InnerText;
                    var partIndex = materialNode.Attributes.GetNamedItem("part_index").InnerText;
                    var lodIndex = materialNode.Attributes.GetNamedItem("lod_index").InnerText;

                    var materialFile = _packFileService.FindFile(materialFilePath);
                    var materialConfig = new WsModelMaterial(materialFile as PackFile, "");

                    var mesh = loadedModelNode.GetMeshNode(int.Parse(lodIndex), int.Parse(partIndex));
                    if (mesh == null)
                    {
                        _logger.Here().Error($"Trying to access mesh at index {partIndex} at lod {lodIndex}, which is not found ");
                    }
                    else
                    {
                        bool useAlpha = materialConfig.Alpha;
                        var alphaSettings = mesh.MeshModel.AlphaSettings;
                        if (useAlpha)
                            alphaSettings.Mode = AlphaMode.Alpha_Test;
                        else
                            alphaSettings.Mode = AlphaMode.Opaque;
                        mesh.MeshModel.AlphaSettings = alphaSettings;

                        foreach (var newTexture in materialConfig.Textures)
                            mesh.UpdateTexture(newTexture.Value, newTexture.Key);
                    }
                }
            }
        }

        /*Dictionary<TexureType, string> ParseMaterialFile(string path)
        {
            var output = new Dictionary<TexureType, string>();

            
            var buffer = (materialFile as PackFile).DataSource.ReadData();
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            var textureNodes = doc.SelectNodes(@"/material/textures/texture");

            foreach (XmlNode node in textureNodes)
            {
                var slotNode = node.SelectNodes("slot");
                var pathNode = node.SelectNodes("source");

                var textureSlotName = slotNode[0].InnerText;
                var texturePath = pathNode[0].InnerText;

                if (textureSlotName == "s_diffuse")
                    output[TexureType.Diffuse] = texturePath;

                if (textureSlotName == "s_gloss")
                    output[TexureType.Gloss] = texturePath;

                if (textureSlotName == "s_mask")
                    output[TexureType.Mask] = texturePath;

                if (textureSlotName == "s_normal")
                    output[TexureType.Normal] = texturePath;

                if (textureSlotName == "s_specular")
                    output[TexureType.Specular] = texturePath;
            }

            return output;
        }*/
    }
}
