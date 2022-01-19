using System.Windows;

namespace CommonControls.Common
{
    // Helper when binding inside ContextMenu
    // https://stackoverflow.com/questions/3583507/wpf-binding-a-contextmenu-to-an-mvvm-command
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get => (object)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}