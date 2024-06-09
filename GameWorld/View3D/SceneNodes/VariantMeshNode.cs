﻿namespace View3D.SceneNodes
{
    public class VariantMeshNode : GroupNode
    {
        public VariantMeshNode(string name) : base(name) { }

        protected VariantMeshNode() { }

        public override ISceneNode CreateCopyInstance() => new VariantMeshNode();
    }
}
