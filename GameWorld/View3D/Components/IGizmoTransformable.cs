using System;
using GameWorld.Core.Commands;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Gizmo
{
    public interface IGizmoTransformable
    {
        Matrix WorldMatrix { get; set; }
        Vector3 Pivot { get; }
        Quaternion ParentWorldRotation { get; }

        // [NEW] 独立的旋转方向限定符！完美适配各类模型的刁钻坐标系！
        Vector2 RotationMultiplier { get; }

        void OnGizmoDragStart();
        void OnGizmoDragEnd(CommandExecutor commandManager);
    }

    public class GlobalGizmoTransformCommand : ICommand
    {
        public string HintText => "Gizmo Transform";
        public bool IsMutation => true;

        private readonly IGizmoTransformable _target;
        private readonly Matrix _initialMatrix;
        private readonly Matrix _finalMatrix;
        private readonly Action _onCommit;

        public GlobalGizmoTransformCommand(IGizmoTransformable target, Matrix initialMatrix, Matrix finalMatrix, Action onCommit)
        {
            _target = target;
            _initialMatrix = initialMatrix;
            _finalMatrix = finalMatrix;
            _onCommit = onCommit;
        }

        public void Execute()
        {
            _target.WorldMatrix = _finalMatrix;
            _onCommit?.Invoke();
        }

        public void Undo()
        {
            _target.WorldMatrix = _initialMatrix;
            _onCommit?.Invoke();
        }
    }
}
