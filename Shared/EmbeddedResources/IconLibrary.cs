using System.Windows.Media.Imaging;

namespace Shared.EmbeddedResources
{
    public static class IconLibrary
    {
        public static BitmapImage FolderIcon { get; private set; }
        public static BitmapImage CollectionIcon { get; private set; }
        public static BitmapImage FileIcon { get; private set; }
        public static BitmapImage LockIcon { get; private set; }

        public static BitmapImage VmdIcon { get; private set; }
        public static BitmapImage Rmv2ModelIcon { get; private set; }
        public static BitmapImage MeshIcon { get; private set; }
        public static BitmapImage GroupIcon { get; private set; }
        public static BitmapImage SkeletonIcon { get; private set; }


        public static BitmapImage SaveFileIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-save-all-50.png");
        public static BitmapImage OpenReferenceMeshIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.OpenRef.png");
        public static BitmapImage UndoIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.Undo.png");

        public static BitmapImage Gizmo_CursorIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-cursor-256.png");
        public static BitmapImage Gizmo_MoveIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.Gizmo_move.png");
        public static BitmapImage Gizmo_RotateIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.Gizmo_rotate.png");
        public static BitmapImage Gizmo_ScaleIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.Gizmo_scale.png");

        public static BitmapImage Selection_Object_Icon { get; private set; } = BitmapToImageSource(@"Kitbasher.SelectionMode_object.png");
        public static BitmapImage Selection_Face_Icon { get; private set; } = BitmapToImageSource(@"Kitbasher.SelectionMode_face.png");
        public static BitmapImage Selection_Vertex_Icon { get; private set; } = BitmapToImageSource(@"Kitbasher.SelectionMode_Vertex.png");

        public static BitmapImage ViewSelectedIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.ShowSelection.png");

        public static BitmapImage DivideIntoSubMeshIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.tool_split.png");
        public static BitmapImage MergeMeshIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.tool_combine.png");
        public static BitmapImage DuplicateIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.tool_duplicate.png");
        public static BitmapImage GroupMeshIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.Group.png");
        public static BitmapImage DeleteIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.tool_delete.png");
        public static BitmapImage ReduceMeshIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-compress-64.png");

        public static BitmapImage CreateLodIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.CreateLod.png");
        public static BitmapImage BmiToolIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-bmi-48.png");
        public static BitmapImage SkeletonReshaperIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-body-48.png");
        public static BitmapImage ReRiggingIcon { get; private set; } = BitmapToImageSource(@"icons8-animated-skeleton-50.png");

        public static BitmapImage FreezeAnimationIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.icons8-fruity-ice-pop-64.png");
        public static BitmapImage PinIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.pin.png");

        public static BitmapImage GrowSelectionIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.ExpandSelection.png");
        public static BitmapImage FaceToVertexIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.FaceToVertex.png");
        public static BitmapImage MergeVertexIcon { get; private set; } = BitmapToImageSource(@"Kitbasher.tool_mergeVertex.png");
        public static BitmapImage VertexDebuggerIcon { get; private set; } = BitmapToImageSource(@"icons8-question-mark-48.png");

        public static void Load()
        {
            FolderIcon = BitmapToImageSource("icons8-folder-48.png");
            FileIcon = BitmapToImageSource("icons8-file-48.png");
            CollectionIcon = BitmapToImageSource("icons8-collectibles-48.png");
            LockIcon = BitmapToImageSource("icons8-lock-50.png");

            VmdIcon = BitmapToImageSource("icons8-man-50.png");
            Rmv2ModelIcon = BitmapToImageSource("icons8-orthogonal-view-50.png");
            MeshIcon = BitmapToImageSource("icons8-mesh-50.png");
            GroupIcon = BitmapToImageSource("icons8-folder-50.png");
            SkeletonIcon = BitmapToImageSource("icons8-animated-skeleton-50.png");
        }

        static BitmapImage BitmapToImageSource(string path)
        {
            return ResourceLoader.LoadBitmapImage(@"Resources.Icons." + path);
        }

    }
}
