using System;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Services;

namespace Shared.Ui.Common
{
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var applicationServiceProivder = ((IAssetEditorMain)Application.Current).ServiceProvider;
            var service = applicationServiceProivder.GetRequiredService<LocalizationManager>();
            return service.Get(Key);
        }
    }
}
