using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Gizmo;
using View3D.Rendering;

namespace View3D.Commands
{
    class TransformCommand : CommandBase<TransformCommand>
    {
        class TransformCopy
        {
            public TransformCopy(ITransformable item)
            {
                Position = item.Position;
                Scale = item.Scale;
                Orientation = item.Orientation;
            }

            public Vector3 Position { get; set; }
            public Vector3 Scale { get; set; }
            public Quaternion Orientation { get; set; }
        }

        List<ITransformable> _items;
        Dictionary<ITransformable, TransformCopy> _originalTransforms;

        public TransformCommand(List<ITransformable> items)
        {
            _items = new List<ITransformable>();
            _originalTransforms = new Dictionary<ITransformable, TransformCopy>();

            foreach (var item in items)
            {
                _items.Add(item);
                _originalTransforms[item] = new TransformCopy(item);
            }
        }


        protected override void ExecuteCommand()
        {
            // Not much to do, the transform is alreay applied 
        }

        protected override void UndoCommand()
        {
            foreach (var item in _items)
            {
                item.Position = _originalTransforms[item].Position;
                item.Orientation = _originalTransforms[item].Orientation;
                item.Scale = _originalTransforms[item].Scale;
            }    
        }
    }
}
