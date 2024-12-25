using Shared.Ui.Common.MenuSystem;
using System.Windows;
using System.Windows.Controls;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ButtonDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultButtonTemplate { get; set; }
        public DataTemplate RadioButtonTemplate { get; set; }
        public DataTemplate SeperatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var button = (MenuBarButton)item;
            if (button.IsSeperator)
                return SeperatorTemplate;
            switch (button)
            {
                case MenuBarGroupButton _:
                    return RadioButtonTemplate;
                default:
                    return DefaultButtonTemplate;
            }
        }
    }
}
