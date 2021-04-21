using CommonControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimMetaEditor.DataType
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public string FileName { get; set; }
        public List<MetaDataTagItem> TagItems { get; set; } = new List<MetaDataTagItem>();

        public override string ToString()
        {
            return $"{FileName} - {TagItems.Count}";
        }


        public void Validate(SchemaManager schemaManager )
        {
            foreach (var metaItem in TagItems)
            {
                var schema = schemaManager.GetMetaDataDefinition(metaItem.Name, metaItem.Version);
                if (schema == null)
                {
                    metaItem.IsDecodedCorrectly = false;
                    continue;
                }

                var fields = schema.ColumnDefinitions;
                var totalBytesRead = 0;
                var expectedBytes = metaItem.DataItems[0].Size;
                var parsingFailed = false;
                for (int i = 0; i < fields.Count; i++)
                {
                    var parser = Filetypes.ByteParsing.ByteParserFactory.Create(fields[i].Type);
                    var result = parser.TryDecode(metaItem.DataItems[0].Bytes, metaItem.DataItems[0].Start + totalBytesRead, out string value, out var bytesRead, out var error);
                    totalBytesRead += bytesRead;

                    if (result == false)
                    {
                        parsingFailed = true;
                        break;
                    }
                }

                if (parsingFailed)
                {
                    metaItem.IsDecodedCorrectly = false;
                    continue;
                }

                if (totalBytesRead != expectedBytes)
                {
                    metaItem.IsDecodedCorrectly = false;
                    continue;
                }

                metaItem.IsDecodedCorrectly = true;
            }
        }
    }
}
