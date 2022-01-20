using CommonControls.Common;
using CommonControls.PackFileBrowser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            try
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
            catch (Exception)
            {
            }
        }
    }
}
