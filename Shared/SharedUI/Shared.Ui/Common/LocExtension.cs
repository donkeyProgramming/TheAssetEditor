using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Services;

namespace Shared.Ui.Common
{
    public class LocExtension : MarkupExtension
    {
        private static readonly IReadOnlyDictionary<string, string> s_emptyDesignerStrings =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        private static readonly Lazy<IReadOnlyDictionary<string, string>> s_designerStrings = new(LoadDesignerStrings);

        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Application.Current is IAssetEditorMain appMain)
            {
                var applicationServiceProivder = appMain.ServiceProvider;
                var service = applicationServiceProivder.GetRequiredService<LocalizationManager>();
                return service.Get(Key);
            }

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()) &&
                s_designerStrings.Value.TryGetValue(Key, out var value))
                return value;

            return Key;
        }

        private static IReadOnlyDictionary<string, string> LoadDesignerStrings()
        {
            try
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var directPath = Path.Combine(currentDirectory, "Language_En.json");
                var assetEditorPath = Path.Combine(currentDirectory, "AssetEditor", "Language_En.json");
                var filePath = File.Exists(directPath) ? directPath : assetEditorPath;
                if (!File.Exists(filePath))
                    return s_emptyDesignerStrings;

                var json = File.ReadAllText(filePath);
                var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (strings != null)
                    return new ReadOnlyDictionary<string, string>(strings);
                else
                    return s_emptyDesignerStrings;
            }
            catch
            {
                return s_emptyDesignerStrings;
            }
        }
    }
}
