using AssetEditor.ViewModels.FileTreeView;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Behaviors;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AssetEditor.ViewModels
{
    class MainViewModel
    {
        ILogger _logger = Logging.Create<MainViewModel>();

        public FileTreeViewModel FileTree { get; private set; }
        public MenuBarViewModel MenuBar { get; set; }

        public MainViewModel(IServiceProvider serviceProvider, PackFileService packfileService, ApplicationSettingsService settingsService, GameInformationService gameInformationService)
        {
            MenuBar = (MenuBarViewModel)serviceProvider.GetService(typeof(MenuBarViewModel)); ;

            //var n = typeof(BindableSelectedItemBehavior).Assembly.GetManifestResourceNames();
            // Properties.Resources.
            // var t = new BitmapImage(new Uri("pack://application:,,,/CommonControls;/Resources/Icons/icons8-3d-object-48.png", UriKind.Absolute));


            //C:\Program Files (x86)\Steam\steamapps\common\Total War WARHAMMER II\data\variants_dds2_sc.pack
            Startup();

            //packfileService.Load(@"C:\Program Files (x86)\Steam\steamapps\common\Total War WARHAMMER II\data\warmachines_hb.pack");

            packfileService.LoadAllCaFiles(@"C:\Program Files (x86)\Steam\steamapps\common\Total War WARHAMMER II\data");
            // packfileService.Database.Clear();
            // Create pack file view model
             FileTree = new FileTreeViewModel(packfileService);

            //FileTree.OnFilePreView
            //FileTree.OnFileSelected

        }


        void Startup()
        {
          
        }
    }


    
}
