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


        public bool IsRunning { get; set; } = false;
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

        public void AddTexture(string textureGameName, TexureType type)
        {
            var currentFileName = Path.GetFileNameWithoutExtension(textureGameName);
            var newFileName = FilePreFix + currentFileName + ".png";

            var systemFilePath = OutputDirectory + "\\" + newFileName;

            var newTexture = new Texture()
            { 
                GamePath = textureGameName,
                Error = "",
                SystemPath = systemFilePath,
                Type = type,
            };

            _textures.Add(newTexture);
        }



        public List<Texture> GetCurrentTextures() => _textures;

        public void Start()
        {
            if (IsRunning)
                return;

         
            if (Directory.Exists(OutputDirectory))
            {
                DeleteExistingUvMapsInDirectory(OutputDirectory);
                var files = Directory.GetFiles(OutputDirectory);
                if (files.Length != 0)
                {
                    if (MessageBox.Show("The output folder is not empty and some file will be overwritten.\nContinue?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        return;
                }

                try
                {
                    Directory.Delete(OutputDirectory);
                }
                catch
                {
                    MessageBox.Show("Unable to clean up the directory, some files are in use.\nCan not start the process");
                    return;
                }
            }

            DirectoryHelper.EnsureCreated(OutputDirectory);

            // if some overwirte, give warning before next
            foreach (var texture in _textures)
            {
                var packFile = _pfs.FindFile(texture.GamePath);
                TextureConverter.SaveAsPNG(packFile, texture.SystemPath);
            }

            // Delete existing uv maps 
            
            var meshes = _node.GetMeshesInLod(0, false);
            foreach (var mesh in meshes)
                ExportUvMap(OutputDirectory, mesh);

            // Assign

            IsRunning = true;
        }

        private void DeleteExistingUvMapsInDirectory(string directory)
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                if (file.Contains("Uv_map_"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    { }
                }
            }
        }

        public void Refresh()
        {
           // TextureConverter
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            IsRunning = false;

            _textures.Clear();
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

            image.Save(imagePathWithoutExtention);
        }

    }
}
