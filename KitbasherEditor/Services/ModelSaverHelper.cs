using Common;
using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using KitbasherEditor.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{


    public static class LinqRecursiveHelper
    {
        /// <summary>
        /// Return item and all children recursively.
        /// </summary>
        /// <typeparam name="T">Type of item.</typeparam>
        /// <param name="item">The item to be traversed.</param>
        /// <param name="childSelector">Child property selector.</param>
        /// <returns></returns>
        public static IEnumerable<T> Traverse<T>(this T item, Func<T, T> childSelector)
        {
            var stack = new Stack<T>(new T[] { item });

            while (stack.Any())
            {
                var next = stack.Pop();
                if (next != null)
                {
                    yield return next;
                    stack.Push(childSelector(next));
                }
            }
        }

        /// <summary>
        /// Return item and all children recursively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="childSelector"></param>
        /// <returns></returns>
        public static IEnumerable<T> Traverse<T>(this T item, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(new T[] { item });

            while (stack.Any())
            {
                var next = stack.Pop();
                //if(next != null)
                //{
                yield return next;
                foreach (var child in childSelector(next))
                {
                    stack.Push(child);
                }
                //}
            }
        }

        /// <summary>
        /// Return item and all children recursively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="childSelector"></param>
        /// <returns></returns>
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items,
          Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }

        public static IEnumerable<T> Flatten<T>(this T source, Func<T, IEnumerable<T>> selector)
        {
            return selector(source).SelectMany(c => Flatten(c, selector))
                                   .Concat(new[] { source });
        }
    }



    public class ModelSaverHelper
    {
        ILogger _logger = Logging.Create<ModelSaverHelper>();

        private readonly  PackFileService _packFileService;
        private readonly  SceneManager _sceneManager;
        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly Rmv2ModelNode _editableMeshNode;

        public ModelSaverHelper(PackFileService packFileService, SceneManager sceneManager, KitbasherViewModel kitbasherViewModel, Rmv2ModelNode editableMeshNode)
        {
           _packFileService = packFileService;
           _sceneManager = sceneManager;
           _kitbasherViewModel = kitbasherViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void Save()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();
                var path = _packFileService.GetFullPath(inputFile);
                SaveHelper.Save(_packFileService, path, inputFile, bytes);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        public void SaveAs()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();

                using (var browser = new SavePackFileWindow(_packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                    if (browser.ShowDialog() == true)
                        SaveHelper.Save(_packFileService, browser.FilePath, inputFile, bytes);
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        private byte[] GetBytesToSave()
        {
            var isAllVisible = true;
            _editableMeshNode.GetLodNodes()[0].ForeachNode((node) =>
            {
                if (!node.IsVisible)
                    isAllVisible = false;
            });

            bool onlySaveVisible = false;
            if (isAllVisible == false)
            {
                if (MessageBox.Show("Only save visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    onlySaveVisible = true;
            }

            var bytes = _editableMeshNode.Save(onlySaveVisible);
            var reloadedModel = new RmvRigidModel(bytes, "reloadedFile");
            return bytes;
        }
    }
}
