using System.Collections.ObjectModel;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.Core.SceneExplorer;
using Editors.KitbasherEditor.Events;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;

namespace KitbasherEditor.Views
{
    public partial class SceneExplorerNode : ObservableObject
    {
        public ObservableCollection<SceneExplorerNode> Children { get; set; } = [];

        public ISceneNode Content { get; set; }
        [ObservableProperty] bool _isSelected;
        [ObservableProperty] bool _isReference;

        public SceneExplorerNode(ISceneNode content, bool isReference)
        {
            Content = content;
            _isReference = isReference;
        }
    }

    public class MultiSelectTreeView : TreeView, IDisposable
    {
        private readonly ILogger _logger = Logging.Create<MultiSelectTreeView>();

        TreeViewItem? _lastItemSelected;
        bool _ignoreSelectionEvents = false;
        bool _setInitialNodeExpandInfo = true;

        private readonly ObservableCollection<SceneExplorerNode> _nodes = [];

        public IEventHub EventHub
        {
            get { return (IEventHub)GetValue(EventHubProperty); }
            set { SetValue(EventHubProperty, value); }
        }

        public SceneManager SceneManager
        {
            get { return (SceneManager)GetValue(SceneManagerProperty); }
            set { SetValue(SceneManagerProperty, value); }
        }

        public SelectionManager SelectionManager
        {
            get { return (SelectionManager)GetValue(SelectionManagerProperty); }
            set { SetValue(SelectionManagerProperty, value); }
        }

        public static readonly DependencyProperty EventHubProperty = DependencyProperty.Register(nameof(EventHub), typeof(IEventHub), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(AttemptRegistrationEventHub));
        public static readonly DependencyProperty SceneManagerProperty = DependencyProperty.Register(nameof(SceneManager), typeof(SceneManager), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(AttemptRegistrationSceneManager));
        public static readonly DependencyProperty SelectionManagerProperty = DependencyProperty.Register(nameof(SelectionManager), typeof(SelectionManager), typeof(MultiSelectTreeView));

        private static void AttemptRegistrationSceneManager(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MultiSelectTreeView)?.Register();
        }

        private static void AttemptRegistrationEventHub(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeView = d as MultiSelectTreeView;
            if (treeView == null)
                return;

            treeView.OnRegisterEventHub((IEventHub)e.OldValue, (IEventHub)e.NewValue);
        }

        public MultiSelectTreeView()
        {
            SelectedItemChanged += MyTreeView_SelectedItemChanged;
            Focusable = true;
            PreviewMouseDoubleClick += MultiSelectTreeView_PreviewMouseDoubleClick;
            PreviewMouseRightButtonDown += MultiSelectTreeView_PreviewMouseRightButtonDown;
            ItemsSource = _nodes;
            Unloaded += MultiSelectTreeView_Unloaded;
        }

        private void MultiSelectTreeView_Unloaded(object sender, RoutedEventArgs e) => Dispose();

        private void MultiSelectTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == null)
            {
                _logger.Here().Error("MultiSelectTreeView_PreviewMouseRightButtonDown: e.OriginalSource is null");
                return;
            }

            var dp = e.OriginalSource as DependencyObject;
            if (dp == null)
            {
                _logger.Here().Error($"MultiSelectTreeView_PreviewMouseRightButtonDown: e.OriginalSource is not a DependencyObject, its: {dp.GetType()}");
                return;
            }

            var treeViewItem = ItemContainerGeneratorHelper.VisualUpwardSearch(dp);
            if (treeViewItem == null)
            {
                _logger.Here().Error($"MultiSelectTreeView_PreviewMouseRightButtonDown: treeViewItem is null.");
                return;
            }

            var sceneNode = treeViewItem.DataContext as SceneExplorerNode;
            if (sceneNode == null)
            {
                _logger.Here().Error($"MultiSelectTreeView_PreviewMouseRightButtonDown: sceneNode is null.");
                return;
            }

            if (sceneNode != null)
            {
                if (sceneNode.IsSelected == false)
                    treeViewItem.Focus();
                e.Handled = true;
            }
        }

        public void OnRegisterEventHub(IEventHub oldValue, IEventHub newValue)
        {
            if (newValue == null && oldValue != null)
                oldValue.UnRegister(this);

            if (newValue != null)
            {
                EventHub.Register<SelectionChangedEvent>(this, SelectionEventHandler);
                EventHub.Register<SceneObjectAddedEvent>(this, x => RebuildTree());
                EventHub.Register<SceneObjectRemovedEvent>(this, x => RebuildTree());
            }

            Register();
        }


        public void Register()
        {
            if (SceneManager == null || EventHub == null)
                return;

            RebuildTree();
        }

        private void RebuildTree()
        {
            SceneExplorerNodeBuilder.Update(_nodes, SceneManager.RootNode, _setInitialNodeExpandInfo);
            _setInitialNodeExpandInfo = false;
        }

        private void SelectionEventHandler(SelectionChangedEvent notification)
        {
            if (_ignoreSelectionEvents)
                return;


            if (notification.NewState is not ObjectSelectionState objectSelection)
                return;

            ForEachNode(_nodes, objectSelection.CurrentSelection());

            SendNodeSelectionEvent();
        }

        void ForEachNode(IEnumerable<SceneExplorerNode> nodes, List<ISelectable> selectedWorldNodes)
        {
            _lastItemSelected = null;
            foreach (var node in nodes)
            {
                var selectedNode = selectedWorldNodes.FirstOrDefault(x => x == node.Content);
                if (selectedNode != null)
                    node.IsSelected = true;
                else
                    node.IsSelected = false;

                ForEachNode(node.Children, selectedWorldNodes);
            }
        }

        void SendNodeSelectionEvent()
        {
            if (_nodes == null)
                _logger.Here().Error($"{nameof(_nodes)} is null");

            if (EventHub == null)
                _logger.Here().Error($"{nameof(EventHub)} is null");

            if (_nodes == null)
                return;

            var selectedNodes = GetSelectedNodes(_nodes);
            EventHub?.Publish(new SceneNodeSelectedEvent(selectedNodes));
        }

        private void MultiSelectTreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = ItemContainerGeneratorHelper.VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                var node = treeViewItem.DataContext as SceneExplorerNode;
                if (node != null)
                    node.Content.IsExpanded = !node.Content.IsExpanded;
            }
            
            e.Handled = true;
        }

        void MyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var node = (e.NewValue as SceneExplorerNode);
            if (node == null)
                return;

            // ------------
            var treeViewItem = ItemContainerGenerator.ContainerFromItemRecursive(node);
            if (treeViewItem == null)
                return;

            treeViewItem.IsSelected = false;
            treeViewItem.Focus();

            if (!IsCtrlPressed)
            {
                var items = GetTreeViewItems(this, true);
                foreach (var i in items)
                    (i.DataContext as SceneExplorerNode).IsSelected = false;
            }

            if (IsShiftPressed && _lastItemSelected != null)
            {
                var items = GetTreeViewItemRange(_lastItemSelected, treeViewItem);
                foreach (var i in items)
                    (i.DataContext as SceneExplorerNode).IsSelected = true;

            }
            else
            {
                _lastItemSelected = treeViewItem;
                (treeViewItem.DataContext as SceneExplorerNode).IsSelected = true;
            }

            try
            {
                // Create the selection state
                var newSelection = new ObjectSelectionState();
                UpdateSelectionStateFromNode(_nodes, newSelection);

                // Check if the selection is same as before
                var currentSelection = SelectionManager.GetState() as ObjectSelectionState;
                var selectionEqual = false;
                if (currentSelection != null)
                    selectionEqual = currentSelection.IsSelectionEqual(newSelection);

                // Send the new selection state
                if (selectionEqual == false)
                {
                    _ignoreSelectionEvents = true;
                    SelectionManager.SetState(newSelection);
                }
            }
            finally 
            {
                _ignoreSelectionEvents = false;
            }

            SendNodeSelectionEvent();
        }


        void UpdateSelectionStateFromNode(IEnumerable<SceneExplorerNode> nodes, ObjectSelectionState selectionState)
        {
            foreach (var node in nodes)
            {
                if (node.IsSelected == true)
                {
                    var content = node.Content as ISelectable;
                    if (content != null && content.IsSelectable)
                        selectionState.ModifySelectionSingleObject(content, false);
                }

                UpdateSelectionStateFromNode(node.Children, selectionState);
            }
        }


        List<ISceneNode> GetSelectedNodes(IEnumerable<SceneExplorerNode> nodes)
        {
            var output = new List<ISceneNode>();
            foreach (var node in nodes)
            {
                if (node.IsSelected)
                    output.Add(node.Content);

                var result = GetSelectedNodes(node.Children);
                output.AddRange(result);
            }

            return output;
        }




        private static List<TreeViewItem> GetTreeViewItems(ItemsControl parentItem, bool includeCollapsedItems, List<TreeViewItem> itemList = null)
        {
            if (itemList == null)
                itemList = new List<TreeViewItem>();

            for (var index = 0; index < parentItem.Items.Count; index++)
            {
                var tvItem = parentItem.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
                if (tvItem == null) continue;

                itemList.Add(tvItem);
                if (includeCollapsedItems || tvItem.IsExpanded)
                    GetTreeViewItems(tvItem, includeCollapsedItems, itemList);
            }
            return itemList;
        }
        private List<TreeViewItem> GetTreeViewItemRange(TreeViewItem start, TreeViewItem end)
        {
            var items = GetTreeViewItems(this, false);

            var startIndex = items.IndexOf(start);
            var endIndex = items.IndexOf(end);
            var rangeStart = startIndex > endIndex || startIndex == -1 ? endIndex : startIndex;
            var rangeCount = startIndex > endIndex ? startIndex - endIndex + 1 : endIndex - startIndex + 1;

            if (startIndex == -1 && endIndex == -1)
                rangeCount = 0;

            else if (startIndex == -1 || endIndex == -1)
                rangeCount = 1;

            return rangeCount > 0 ? items.GetRange(rangeStart, rangeCount) : new List<TreeViewItem>();
        }

        public void Dispose()
        {
            EventHub?.UnRegister(this);

            SelectedItemChanged -= MyTreeView_SelectedItemChanged;
            PreviewMouseDoubleClick -= MultiSelectTreeView_PreviewMouseDoubleClick;
            PreviewMouseRightButtonDown -= MultiSelectTreeView_PreviewMouseRightButtonDown;
            Unloaded -= MultiSelectTreeView_Unloaded;
        }

        private static bool IsCtrlPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl); }
        }
        private static bool IsShiftPressed
        {
            get { return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift); }
        }

    }

    public static class ItemContainerGeneratorHelper
    {
        public static TreeViewItem ContainerFromItemRecursive(this ItemContainerGenerator root, object item)
        {
            var treeViewItem = root.ContainerFromItem(item) as TreeViewItem;
            if (treeViewItem != null)
                return treeViewItem;
            foreach (var subItem in root.Items)
            {
                treeViewItem = root.ContainerFromItem(subItem) as TreeViewItem;
                var search = treeViewItem?.ItemContainerGenerator.ContainerFromItemRecursive(item);
                if (search != null)
                    return search;
            }
            return null;
        }


        public static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
    }
}
