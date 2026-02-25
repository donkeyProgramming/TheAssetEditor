using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Windows;
using Editors.AnimationMeta.Presentation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description => "Copy object for MetaDataTag";
        public List<ParsedMetadataAttribute> Items { get; set; } = [];
    }

    class CopyPastCommand : IUiCommand
    {
        private readonly CopyPasteManager _copyPasteManager;
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;

        public CopyPastCommand(CopyPasteManager copyPasteManager, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _copyPasteManager = copyPasteManager;
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
        }

        public void ExecuteCopy(MetaDataEditorViewModel controller)
        {
            var selectedTags = controller.Tags
                .Where(x => x.IsSelected)
                .ToList();

            // Check for errors

            foreach (var tag in selectedTags)
            {
                if (string.IsNullOrWhiteSpace(tag.HasError()) == false || tag._input == null)
                {
                    MessageBox.Show($"Can not copy object due to: {tag.HasError()}");
                    return;
                }
            }
            var copyPastItem = new MetaDataTagCopyItem();
            foreach (var item in selectedTags)
            {
                var copy = CreateShallowCopy(item._input);
                copyPastItem.Items.Add(copy);

            }
            _copyPasteManager.SetCopyItem(copyPastItem);
        }
      

           /* var metaDataTagDeSerializer = new MetaDataTagDeSerializer();

            var data = new List<byte>();

            foreach (var item in selectedTags)
            {
                var bytes = metaDataTagDeSerializer.Serialize(item., out var errorStr);



                var copyPastItem = new MetaDataTagCopyItem();
            foreach (var tag in selectedTags)
            {
                var fileFormatData = tag.GetAsFileFormatData();
                var entry = new ParsedUnknownMetadataAttribute()
                {
                    Name = fileFormatData.Name,
                    Data = fileFormatData.DataItem.Bytes,
                    Version = tag.Version,
                };
                copyPastItem.Items.Add(entry);
            }
            _copyPasteManager.SetCopyItem(copyPastItem);*/
        



     


        public void ExecutePaste(MetaDataEditorViewModel controller)
        {
            var pastObject = _copyPasteManager.GetPasteObject<MetaDataTagCopyItem>();
            if (pastObject != null)
            {
                controller._metaDataFile.Attributes.AddRange(pastObject.Items);
            }

            controller.UpdateView();
        }

        public static T CreateShallowCopy<T>(T original) where T : class
        {
            if (original == null)
            {
                return null;
            }

            // Create a new instance of the same type as the original object.
            // Activator.CreateInstance() uses reflection to instantiate the type dynamically.
            T copy = (T)Activator.CreateInstance(original.GetType());

            // Get all public, instance properties of the class.
            PropertyInfo[] properties = original.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                // Check if the property can be read and written to.
                if (property.CanRead && property.CanWrite)
                {
                    // Get the value from the original object and set it on the new copy.
                    object value = property.GetValue(original);
                    property.SetValue(copy, value);
                }
            }

            return copy;
        }




    }
}

