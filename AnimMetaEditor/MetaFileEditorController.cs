using Common;
using AnimMetaEditor.DataType;
using AnimMetaEditor.ViewModels;
using AnimMetaEditor.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTypes.PackFiles.Models;
using CommonControls.Services;
using System.Windows;

namespace AnimMetaEditor
{
    public class MetaFileEditorController
    {
        public static Window MakeWindwo(PackFileService pf)
        {
            Window newWindow = new Window();
            newWindow.Content = CreateDecoder(pf);
            //newWindow.DataContext = viewModel;
            return newWindow;
        }

        public static MetaDataMainView CreateDecoder(PackFileService pf)
        {
            var allMetaFiles = pf.FindAllWithExtention("meta");
            allMetaFiles = allMetaFiles.Where(f => f.Name.Contains("anm.meta")).ToList();
            List<MetaDataFile> allMetaData = new List<MetaDataFile>();


            MetaDataFile master = new MetaDataFile()
            {
                FileName ="Master collection"
            };

            MetaDataFileParser parser = new MetaDataFileParser();
            foreach (var file in allMetaFiles)
            {
                var res = parser.ParseFile(file, pf);
                allMetaData.Add(res);

                foreach (var resultDataItem in res.TagItems)
                {

                    var masterDataItem =  master.TagItems.FirstOrDefault(x => x.Name == resultDataItem.Name && x.Version == resultDataItem.Version);
                    if (masterDataItem == null)
                    {
                        master.TagItems.Add(new MetaDataTagItem() { Name = resultDataItem.Name, Version = resultDataItem.Version});
                        masterDataItem = master.TagItems.Last();
                    }

                    foreach (var tag in resultDataItem.DataItems)
                    {
                        masterDataItem.DataItems.Add(tag);
                    }

                }
            }

            var v = allMetaData.GroupBy(X => X.TagItems.Select(d=>d.Name)).ToList();



            foreach (var item in master.TagItems)
            {
                var versions = item.DataItems.Select(x => x.Version).Distinct().ToList();
                var size = item.DataItems.Select(x => x.Size).Distinct().ToList();
            }

            master.TagItems = master.TagItems.OrderBy(x => x.DisplayName).ToList();



            var view = new MetaDataMainView();
            view.DataContext = new MainViewModel(master, pf, true);
            return view;
        }

        public static void CreateEditor(PackFile file, PackFileService pf)
        {

            MetaDataFileParser parser = new MetaDataFileParser();
            parser.ParseFile(file, pf);
        }
    }
}
