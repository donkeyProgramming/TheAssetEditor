namespace View3D.Rendering.Geometry
{
    public class FaceIndices
    { 
        public int Index0 { get; set; }
        public int Index1 { get; set; }
        public int Index2 { get; set; }

        public FaceIndices(int index0, int index1, int index2)
        {
            Index0 = index0;
            Index1 = index1;
            Index2 = index2;
        }
    }
}
