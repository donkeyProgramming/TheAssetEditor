using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace AssetEditor.Views
{
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class CachedTabControl : TabControl
    {
        private Panel _itemsHolderPanel = null;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Retrieve the container defined in XAML
            _itemsHolderPanel = GetTemplateChild("PART_ItemsHolder") as Panel;
            UpdateSelectedItem();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (_itemsHolderPanel == null) return;

            object selectedItem = this.SelectedItem;
            if (selectedItem == null) return;

            ContentPresenter cp = FindChildContentPresenter(selectedItem);
            if (cp == null)
            {
                cp = new ContentPresenter
                {
                    Content = selectedItem,
                    ContentTemplate = this.SelectedContentTemplate,
                    ContentTemplateSelector = this.ContentTemplateSelector,
                    ContentStringFormat = this.SelectedContentStringFormat,
                    // Force stretch to fill the entire container to prevent layout collapsing
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                _itemsHolderPanel.Children.Add(cp);
            }

            // Core caching logic: Show the selected item and hide the unselected ones
            foreach (ContentPresenter child in _itemsHolderPanel.Children)
            {
                child.Visibility = (child.Content == selectedItem) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (_itemsHolderPanel == null) return;

            // Handle tab closure to prevent memory leaks
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var cp = FindChildContentPresenter(item);
                        if (cp != null) _itemsHolderPanel.Children.Remove(cp);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemsHolderPanel.Children.Clear();
            }
        }

        private ContentPresenter FindChildContentPresenter(object data)
        {
            if (_itemsHolderPanel == null) return null;
            foreach (ContentPresenter cp in _itemsHolderPanel.Children)
            {
                if (cp.Content == data) return cp;
            }
            return null;
        }
    }
}
