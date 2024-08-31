using System.Windows;
using System.Windows.Media;

namespace AssetEditor.Themes.Attached
{
    internal class TabItemContentBackground
    {
        public static readonly DependencyProperty ContentPanelBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "ContentPanelBackground",
                typeof(Brush),
                typeof(TabItemContentBackground),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static void SetHeaderPanelBackground(UIElement element, Brush value)
        {
            element.SetValue(ContentPanelBackgroundProperty, value);
        }

        public static Brush GetHeaderPanelBackground(UIElement element)
        {
            return (Brush)element.GetValue(ContentPanelBackgroundProperty);
        }
    }
}
