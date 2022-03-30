using CommonControls.Common;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Services
{
    public class TextureFileEditorService
    {
        public string FilePreFix { get; set; } = "cust_";
        public string ProjectPath { get; set; } = "";

        public class Texture
        {
            public TexureType Type { get; set; }
            public string GamePath { get; set; }
            public string SystemPath { get; set; }
            public string Error { get; set; }
        }

        public string OutputDirectory{ get; set; }
       
        public List<TexureType> ValidTextureTypes { get => new List<TexureType>() { TexureType.BaseColour, TexureType.Normal, TexureType.Mask, TexureType.MaterialMap }; }

        List<Texture> _textures { get; set; } = new List<Texture>();
        PackFileService _pfs;
        MainEditableNode _node;

        public TextureFileEditorService(MainEditableNode node, PackFileService pfs)
        {
            _pfs = pfs;
            _node = node;
        }


        public void CreateProject(string systemPath, string prefix, List<Texture> textures)
        { 
        
        }

        public List<Texture> GetCurrentTextures() => _textures;

        public void SaveUvMaps(string projectPath)
        {
            var meshes = _node.GetMeshesInLod(0, false);
            foreach (var mesh in meshes)
                ExportUvMap(projectPath+ "\\UvMaps\\", mesh);
        }

        private void ExportUvMap(string outputDirectory, Rmv2MeshNode mesh)
        {
            Pen blackPen = new Pen(Color.Red, 1);
            using Bitmap image = new Bitmap(1024, 1024);
            using var graphics = Graphics.FromImage(image);

            for (int i = 0; i < mesh.Geometry.GetIndexCount(); i += 3)
            {
                var idx0 = mesh.Geometry.IndexArray[i + 0];
                var idx1 = mesh.Geometry.IndexArray[i + 1];
                var idx2 = mesh.Geometry.IndexArray[i + 2];

                var uv0 = mesh.Geometry.VertexArray[idx0].TextureCoordinate * 1024;
                var uv1 = mesh.Geometry.VertexArray[idx1].TextureCoordinate * 1024;
                var uv2 = mesh.Geometry.VertexArray[idx2].TextureCoordinate * 1024;

                graphics.DrawLine(blackPen, uv0.X, uv0.Y, uv1.X, uv1.Y);
                graphics.DrawLine(blackPen, uv1.X, uv1.Y, uv2.X, uv2.Y);
                graphics.DrawLine(blackPen, uv2.X, uv2.Y, uv0.X, uv0.Y);
            }

            var imagePathWithoutExtention = outputDirectory + "\\Uv_map_" + mesh.Name;
            for (var index = 0; index < 1024; index++)
            {
                var name = (index == 0) ? imagePathWithoutExtention : string.Format("{0} _{1}", imagePathWithoutExtention, index);
                name += ".png";
                if (File.Exists(name))
                    continue;

                imagePathWithoutExtention = name;
                break;
            }

            DirectoryHelper.EnsureCreated(outputDirectory);
            image.Save(imagePathWithoutExtention);
        }
    }
}
