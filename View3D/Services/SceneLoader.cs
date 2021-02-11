using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using static Filetypes.RigidModel.VariantMeshDefinition;

namespace View3D.Services
{
    public class SceneLoader
    {
        ILogger _logger = Logging.Create<SceneLoader>();
        PackFileService _packFileService;
        GraphicsDevice _device;

        public SceneLoader(PackFileService packFileService, GraphicsDevice device)
        {
            _packFileService = packFileService;
            _device = device;
        }

        public void Load(string path, SceneNode parent)
        {
            var file = _packFileService.FindFile(path);
            Load(file as PackFile, parent);
        }

        public SceneNode Load(PackFile file, SceneNode parent)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.FullPath}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent);
                    break;
            }

            return parent;
        }

        void LoadVariantMesh(PackFile file, ref SceneNode parent)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            var slotsElement = variantMeshElement.AddObject( new SlotsNode("Slots"));

            var vmdContent = Encoding.Default.GetString(file.DataSource.ReadData());
            VariantMeshFile meshFile = VariantMeshDefinition.Create(vmdContent);

            foreach (var slot in meshFile.VARIANT_MESH.SLOT)
            {
                var slotElement = slotsElement.AddObject(new SlotNode(slot.Name));

                foreach (var mesh in slot.VariantMeshes)
                {
                    if (mesh.Name != null)
                        Load(mesh.Name.ToLower(), slotElement);
                }

                foreach (var meshReference in slot.VariantMeshReferences)
                    Load(meshReference.definition.ToLower(), slotElement);

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

        void LoadRigidMesh(PackFile file, ref SceneNode parent)
        {
            var rmvModel = new RmvRigidModel(file.DataSource.ReadData(), file.FullPath);
            var model = new Rmv2ModelNode(rmvModel, _device,  Path.GetFileName( rmvModel.FileName));

            if (parent == null)
                parent = model;
            else
                parent.AddObject(model);
        }

        void LoadWsModel(PackFile file, ref SceneNode parent)
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
                var modelAsBase = wsModelNode as SceneNode;
                LoadRigidMesh(modelFile, ref modelAsBase);
            }
        }

        public void Simplify(SceneNode node)
        {
            //if (node is SlotNode slotNode)
            //{
            //    if (slotNode.Name.Contains("stump_"))
            //        slotNode.Parent.RemoveObject(slotNode);
            //    else if(slotNode.Children.Count == 0)
            //        slotNode.Parent.RemoveObject(slotNode);
            //}
            //
            //foreach (var child in node.Children)
            //    Simplify(child);
        }
    }
}
