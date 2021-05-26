using Common;
using CommonControls.PackFileBrowser;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.SceneNodes;
using static View3D.Commands.Object.GroupObjectsCommand;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class SceneExplorerContextMenuHandler : NotifyPropertyChangedImpl
    {
        ObservableCollection<ContextMenuItem> _contextMenu;
        public ObservableCollection<ContextMenuItem> Items { get => _contextMenu; set => SetAndNotify(ref _contextMenu, value); }
        CommandExecutor CommandExecutor { get; }
        public Rmv2ModelNode EditableMeshNode { get; internal set; }

        ISceneNode _activeNode;

        public SceneExplorerContextMenuHandler(CommandExecutor commandExecutor)
        {
            CommandExecutor = commandExecutor;
        }

        public void Create(ISceneNode activeNode)
        {
            _activeNode = activeNode;

            Items = new ObservableCollection<ContextMenuItem>();
            if (activeNode == null)
                return;

            if (CanMakeEditable(_activeNode))
            {
                _contextMenu.Add(new ContextMenuItem("Make Editable", new RelayCommand(MakeEditable)));
            }

            if (IsUngroupable(_activeNode))
                _contextMenu.Add(new ContextMenuItem("Ungroup", new RelayCommand(Ungroup)));

            if (IsLockable(_activeNode))
                _contextMenu.Add(new ContextMenuItem("Lock", new RelayCommand(ToggleLock)));
            else if (IsUnlockable(_activeNode))
                _contextMenu.Add(new ContextMenuItem("Unlock", new RelayCommand(ToggleLock)));

            if (IsRemovable(_activeNode))
            {
                if (_contextMenu.Count != 0)
                    _contextMenu.Add(null);
                _contextMenu.Add(new ContextMenuItem("Remove", new RelayCommand(RemoveNode)));
            }
        }



        bool CanMakeEditable(ISceneNode node)
        {
            if (node.IsEditable == false)
            {
                if (node is Rmv2ModelNode)
                    return true;
                if (node is Rmv2MeshNode)
                    return true;
                if (node is WsModelGroup)
                    return true;
            }
            return false;
        }

        bool IsUngroupable(ISceneNode node)
        {
            if (node is GroupNode gn && gn.IsUngroupable)
                return true;
            else if (node.Parent is GroupNode g && g.IsUngroupable)
                return true;

            return false;
        }

        bool IsRemovable(ISceneNode node)
        {
            if (node.Name == "Root")
                return false;
            if (node.Name == "Editable Model")
                return false;
            if (node.Name == "Reference meshs")
                return false;

            if (node is Rmv2LodNode)
            {
                if (node.Parent.Name == "Editable Model")
                    return false;
            }

            if (node is SlotNode)
                return true;

            if (node is SlotsNode)
                return true;

            if (node is SkeletonNode)
                return false;

            return true;
        }

        bool IsLockable(ISceneNode node)
        {
            if (node.IsEditable == true)
            {
                if (node is ISelectable selectable)
                {
                    if (selectable.IsSelectable == true)
                    {
                        if (node is Rmv2ModelNode)
                            return true;
                        if (node is Rmv2MeshNode)
                            return true;
                    }
                }
                else if (node is GroupNode groupNode && groupNode.IsLockable)
                {
                    if (groupNode.IsSelectable == true)
                        return true;
                }
            }

            return false;
        }

        bool IsUnlockable(ISceneNode node)
        {
            if (node.IsEditable == true)
            {
                if (node is ISelectable selectable)
                {
                    if (selectable.IsSelectable == false)
                    {
                        if (node is Rmv2ModelNode)
                            return true;
                        if (node is Rmv2MeshNode)
                            return true;
                    }
                }
                else if (node is GroupNode groupNode)
                {
                    if (groupNode.IsSelectable == false && groupNode.IsLockable)
                        return true;
                }
            }

            return false;
        }

        void RemoveNode()
        {
            var deleteCommand = new DeleteObjectsCommand(_activeNode as SceneNode);
            CommandExecutor.ExecuteCommand(deleteCommand);
        }

        void MakeEditable()
        {
            var node = _activeNode;
            if (node is Rmv2MeshNode meshNode)
            {
                node.Parent.RemoveObject(node);
                EditableMeshNode.GetLodNodes()[0].AddObject(node);
                meshNode.IsSelectable = true;
                node.IsEditable = true;
                return;
            }

            if (node is Rmv2LodNode lodNode)
            {
                var index = lodNode.LodValue;
                foreach (var lodModel in lodNode.Children)
                {
                    (lodModel as Rmv2MeshNode).IsSelectable = true;
                    EditableMeshNode.GetLodNodes()[0].AddObject(lodModel);
                }
            }

            if (node is Rmv2ModelNode modelNode)
            {
                MakeModelNodeEditable(modelNode);
            }

            if (node is WsModelGroup)
            {
                var child = node.Children.First();
                MakeModelNodeEditable(child as Rmv2ModelNode);
            }

            node.Parent.RemoveObject(node);
            node.ForeachNode(x =>
            {
                x.IsEditable = true;
                if (x is Rmv2MeshNode mesh)
                    mesh.IsSelectable = true;
            });
        }

        void MakeModelNodeEditable(Rmv2ModelNode modelNode)
        {
            foreach (var lodChild in modelNode.Children)
            {
                if (lodChild is Rmv2LodNode lodNode0)
                {
                    var index = lodNode0.LodValue;
                    foreach (var lodModel in lodNode0.Children)
                    {
                        if (index > 3)
                            continue;
                        (lodModel as Rmv2MeshNode).IsSelectable = true;
                        EditableMeshNode.GetLodNodes()[0].AddObject(lodModel);
                    }
                    break;
                }
            }
       
        }

        void MakeEditableAsGroup()
        {

        }

        void Ungroup()
        {
            if (_activeNode is GroupNode gn && gn.IsUngroupable)
                CommandExecutor.ExecuteCommand(new UnGroupObjectsCommand(_activeNode.Parent, _activeNode.Children.Select(x => x as ISelectable).ToList(), _activeNode));
            else if (_activeNode.Parent is GroupNode g && g.IsUngroupable)
                CommandExecutor.ExecuteCommand(new UnGroupObjectsCommand(_activeNode.Parent.Parent, new List<ISelectable>() { _activeNode as ISelectable }, _activeNode.Parent));
        }

        void ToggleLock()
        {
            if (_activeNode.IsEditable == true && _activeNode is ISelectable selectable)
            {
                selectable.IsSelectable = !selectable.IsSelectable;
            }
            if (_activeNode.IsEditable == true && _activeNode is GroupNode groupNode && groupNode.IsLockable)
            {
                groupNode.IsSelectable = !groupNode.IsSelectable;
            }
        }
    }
}
