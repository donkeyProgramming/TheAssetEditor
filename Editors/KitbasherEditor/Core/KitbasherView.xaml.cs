using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace KitbasherEditor.Views
{
    /// <summary>
    /// Interaction logic for KitbasherView.xaml
    /// </summary>
    public partial class KitbasherView : UserControl
    {
        public KitbasherView()
        {
            InitializeComponent();
        }

        private void treeView_Drop(object sender, DragEventArgs e)
        {
            var dropTarget = DataContext as IDropTarget<TreeNode>;
            if (dropTarget != null)
            {
                var formats = e.Data.GetFormats();
                object droppedObject = e.Data.GetData(formats[0]);
                var node = droppedObject as TreeNode;

                if (dropTarget.AllowDrop(node))
                {
                    dropTarget.Drop(node);
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }
    }
}
