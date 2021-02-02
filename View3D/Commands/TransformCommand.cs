using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;
using View3D.Scene.Gizmo;

namespace View3D.Commands
{
    class TransformCommand : ICommand
    {
        ILogger _logger = Logging.Create<TransformCommand>();

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


        List<RenderItem> _items;
        Dictionary<RenderItem, TransformCopy> _originalTransforms;
        public TransformCommand(List<RenderItem> items)
        {
            _items = new List<RenderItem>();
            _originalTransforms = new Dictionary<RenderItem, TransformCopy>();

            foreach (var item in items)
            {
                _items.Add(item);
                _originalTransforms[item] = new TransformCopy(item);
            }
        }

        public void Cancel()
        {
            Undo();
        }

        public void Execute()
        {
            // Not much to do, the transform is alreay applied 
            _logger.Here().Information($"Executing TransformCommand");
        }

        public void Undo()
        {
            _logger.Here().Information($"Undoing TransformCommand");

            foreach (var item in _items)
            {
                item.Position = _originalTransforms[item].Position;
                item.Orientation = _originalTransforms[item].Orientation;
                item.Scale = _originalTransforms[item].Scale;
            }    
        }
    }
}
