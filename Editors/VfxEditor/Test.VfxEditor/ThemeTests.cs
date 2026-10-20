using System.Windows;
using System.Windows.Controls;

namespace Test.VfxEditor
{
    class ThemeTests
    {
        // A ToolBar styles its children by its own keys, not by the app's implicit styles, so a key the theme leaves out
        // draws in WPF's light toolbar look on the dark theme (the VFX editor's quality picker did).
        [Test, Apartment(ApartmentState.STA)]
        public void Controls_in_a_toolbar_take_the_apps_own_style()
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            var theme = (ResourceDictionary)Application.LoadComponent(new Uri("/AssetEditor;component/Themes/Controls.xaml", UriKind.Relative));
            Assert.That(((Style)theme[ToolBar.ComboBoxStyleKey]).BasedOn, Is.SameAs(theme[typeof(ComboBox)]));
            Assert.That(((Style)theme[ToolBar.TextBoxStyleKey]).BasedOn, Is.SameAs(theme[typeof(TextBox)]));
        }
    }
}
