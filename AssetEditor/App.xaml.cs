using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AssetEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        DependencyInjectionConfig _config;

        protected override void OnStartup(StartupEventArgs e)
        {
            _config = new DependencyInjectionConfig();
            _config.ShowMainWindow();
        }
    }
}
