using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Shared.EmbeddedResources
{
    public static class IconLibrary
    {
        [AllowNull] public static BitmapImage FolderIcon { get; private set; } 
        [AllowNull]public static BitmapImage CollectionIcon { get; private set; }
        [AllowNull]public static BitmapImage FileIcon { get; private set; }
        [AllowNull] public static BitmapImage LockIcon { get; private set; }

        [AllowNull]public static BitmapImage VmdIcon { get; private set; }
        [AllowNull]public static BitmapImage Rmv2ModelIcon { get; private set; }
        [AllowNull]public static BitmapImage MeshIcon { get; private set; } 
        [AllowNull]public static BitmapImage GroupIcon { get; private set; }
        [AllowNull]public static BitmapImage SkeletonIcon { get; private set; } 

        [AllowNull]public static BitmapImage SaveFileIcon { get; private set; } 
        [AllowNull]public static BitmapImage OpenReferenceMeshIcon { get; private set; } 
        [AllowNull]public static BitmapImage UndoIcon { get; private set; } 

        [AllowNull]public static BitmapImage Gizmo_CursorIcon { get; private set; }
        [AllowNull]public static BitmapImage Gizmo_MoveIcon { get; private set; }
        [AllowNull]public static BitmapImage Gizmo_RotateIcon { get; private set; }
        [AllowNull]public static BitmapImage Gizmo_ScaleIcon { get; private set; }

        [AllowNull]public static BitmapImage Selection_Object_Icon { get; private set; } 
        [AllowNull]public static BitmapImage Selection_Face_Icon { get; private set; } 
        [AllowNull]public static BitmapImage Selection_Vertex_Icon { get; private set; }

        [AllowNull]public static BitmapImage ViewSelectedIcon { get; private set; } 

        [AllowNull]public static BitmapImage DivideIntoSubMeshIcon { get; private set; } 
        [AllowNull]public static BitmapImage MergeMeshIcon { get; private set; } 
        [AllowNull]public static BitmapImage DuplicateIcon { get; private set; }
        [AllowNull]public static BitmapImage GroupMeshIcon { get; private set; } 
        [AllowNull]public static BitmapImage DeleteIcon { get; private set; } 
        [AllowNull]public static BitmapImage ReduceMeshIcon { get; private set; } 

        [AllowNull]public static BitmapImage CreateLodIcon { get; private set; } 
        [AllowNull]public static BitmapImage BmiToolIcon { get; private set; } 
        [AllowNull]public static BitmapImage SkeletonReshaperIcon { get; private set; } 
        [AllowNull] public static BitmapImage ReRiggingIcon { get; private set; } 

        [AllowNull]public static BitmapImage FreezeAnimationIcon { get; private set; }
        [AllowNull]public static BitmapImage PinIcon { get; private set; }

        [AllowNull]public static BitmapImage GrowSelectionIcon { get; private set; } 
        [AllowNull]public static BitmapImage FaceToVertexIcon { get; private set; } 
        [AllowNull]public static BitmapImage MergeVertexIcon { get; private set; } 
        [AllowNull]public static BitmapImage VertexDebuggerIcon { get; private set; }

        [AllowNull] public static BitmapSource InformationIcon { get; private set; }

        [AllowNull] public static BitmapImage AudioFileIcon { get; private set; }

        public static void Load()
        {
            FolderIcon = BitmapToImageSource("icons8-folder-48.png");
            CollectionIcon =  BitmapToImageSource("icons8-collectibles-48.png");
            FileIcon = BitmapToImageSource("icons8-file-48.png");
            LockIcon  = BitmapToImageSource("icons8-lock-50.png");

            VmdIcon = BitmapToImageSource("icons8-man-50.png");
            Rmv2ModelIcon = BitmapToImageSource("icons8-orthogonal-view-50.png");
            MeshIcon = BitmapToImageSource("icons8-mesh-50.png");
            GroupIcon = BitmapToImageSource("icons8-folder-50.png");
            SkeletonIcon = BitmapToImageSource("icons8-animated-skeleton-50.png");

            SaveFileIcon = BitmapToImageSource(@"Kitbasher.icons8-save-all-50.png");
            OpenReferenceMeshIcon = BitmapToImageSource(@"Kitbasher.OpenRef.png");
            UndoIcon = BitmapToImageSource(@"Kitbasher.Undo.png");

            Gizmo_CursorIcon = BitmapToImageSource(@"Kitbasher.icons8-cursor-256.png");
            Gizmo_MoveIcon = BitmapToImageSource(@"Kitbasher.Gizmo_move.png");
            Gizmo_RotateIcon = BitmapToImageSource(@"Kitbasher.Gizmo_rotate.png");
            Gizmo_ScaleIcon = BitmapToImageSource(@"Kitbasher.Gizmo_scale.png");

            Selection_Object_Icon = BitmapToImageSource(@"Kitbasher.SelectionMode_object.png");
            Selection_Face_Icon = BitmapToImageSource(@"Kitbasher.SelectionMode_face.png");
            Selection_Vertex_Icon = BitmapToImageSource(@"Kitbasher.SelectionMode_Vertex.png");

            ViewSelectedIcon = BitmapToImageSource(@"Kitbasher.ShowSelection.png");

            DivideIntoSubMeshIcon = BitmapToImageSource(@"Kitbasher.tool_split.png");
            MergeMeshIcon = BitmapToImageSource(@"Kitbasher.tool_combine.png");
            DuplicateIcon = BitmapToImageSource(@"Kitbasher.tool_duplicate.png");
            GroupMeshIcon = BitmapToImageSource(@"Kitbasher.Group.png");
            DeleteIcon = BitmapToImageSource(@"Kitbasher.tool_delete.png");
            ReduceMeshIcon = BitmapToImageSource(@"Kitbasher.icons8-compress-64.png");

            CreateLodIcon = BitmapToImageSource(@"Kitbasher.CreateLod.png");
            BmiToolIcon = BitmapToImageSource(@"Kitbasher.icons8-bmi-48.png");
            SkeletonReshaperIcon = BitmapToImageSource(@"Kitbasher.icons8-body-48.png");
            ReRiggingIcon = BitmapToImageSource(@"icons8-animated-skeleton-50.png");

            FreezeAnimationIcon = BitmapToImageSource(@"Kitbasher.icons8-fruity-ice-pop-64.png");
            PinIcon = BitmapToImageSource(@"Kitbasher.pin.png");

            GrowSelectionIcon = BitmapToImageSource(@"Kitbasher.ExpandSelection.png");
            FaceToVertexIcon = BitmapToImageSource(@"Kitbasher.FaceToVertex.png");
            MergeVertexIcon = BitmapToImageSource(@"Kitbasher.tool_mergeVertex.png");
            VertexDebuggerIcon = BitmapToImageSource(@"icons8-question-mark-48.png");

            InformationIcon = BitmapToImageSource(SystemIcons.Information);

            AudioFileIcon = BitmapToImageSource("audio_file.png");

        }

        static BitmapImage BitmapToImageSource(string path)
        {
            return ResourceLoader.LoadBitmapImage(@"Resources.Icons." + path);
        }

        public static BitmapSource BitmapToImageSource(Icon icon)
        {
            BitmapSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

    }
}
