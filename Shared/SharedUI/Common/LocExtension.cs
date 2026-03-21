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

        // 1. 必须添加这个默认无参构造函数！否则 XAML 设计器会报错
        public LocExtension()
        {
        }

        // 2. 带参数的构造函数，用于处理 {loc:Loc KeyName} 语法
        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key)) return string.Empty;

            if (Application.Current is IAssetEditorMain mainApp)
            {
                var applicationServiceProvider = mainApp.ServiceProvider;
                var service = applicationServiceProvider.GetRequiredService<LocalizationManager>();

                var translatedText = service.Get(Key);

                // 如果翻译结果是空的，或者和 Key 一样，说明没找到！
                if (string.IsNullOrEmpty(translatedText) || translatedText == Key)
                {
                    // 你可以暂时放开下面这行代码来排查是不是服务根本没读到词典：
                    // MessageBox.Show($"警告：找不到 Key 为 [{Key}] 的翻译，请检查 JSON 是否加载成功！");
                    return Key;
                }

                return translatedText;
            }

            return $"[{Key}]";
        }
    }
}
