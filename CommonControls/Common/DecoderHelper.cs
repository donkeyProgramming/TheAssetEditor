using CommonControls.Services;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonControls.Common
{
    public class DecoderHelper
    {
        static public DecoderHelper CreateTestInstance(PackFileService pfs)
        {
            var files = pfs.FindAllFilesInDirectory(@"terrain\tiles\campaign")
                .Where(x=>Path.GetExtension(x.Name) == ".rigid_model_v2")
                .ToList();

            var instance = new DecoderHelper();
            instance.Create(files, LoadKnownModelHeader);
            return instance;
        }

        static DataItem LoadKnownModelHeader(PackFile file)
        {
            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();
                chunk.ReadBytes(252);
                var ShaderFlag = chunk.ReadUShort();
                var RenderFlag = chunk.ReadUShort();
                var MeshSectionSize = chunk.ReadUInt32();
                var VertexOffset = chunk.ReadUInt32();
                var VertexCount = chunk.ReadUInt32();
                var IndexOffset = chunk.ReadUInt32();
                var IndexCount = chunk.ReadUInt32();

                return new DataItem(chunk, file.Name);

            }
            catch
            {
                return new DataItem(null, file.Name);
            }
        }

        List<DataItem> _dataItems;

        public void Create(IEnumerable<PackFile> files, Func<PackFile, DataItem> fileLoader)
        {
            _dataItems = files
                .Select(x=>fileLoader(x))       // Convert to format
                .ToList();

            PeakNextStep();
            CreateSummary();
        }

        void PeakNextStep()
        {
            var parserList = ByteParsers.GetAllParsers().ToList();
            parserList.Add(new FixedStringParser(1));
            parserList.Add(new FixedStringParser(2));
            parserList.Add(new FixedStringParser(10));
            parserList.Add(new FixedStringParser(50));

            foreach (var currentItem in _dataItems)
            {
                if (currentItem.FailedOnLoad)
                    continue;

                currentItem.PossibleNextValues.Clear();
                var currentByteIndex = currentItem.LastKnownOffset;

                foreach (var currentParser in parserList)
                {
                    var result = currentParser.TryDecode(currentItem.Chunk.Buffer, currentByteIndex, out var valueAsString, out var bytesRead, out var errorMsg);
 
                    var possibleValue = new PossibleNextValue()
                    {
                        Value = valueAsString,
                        IsValid = result,
                        Size = bytesRead,
                        ErrorMessage = errorMsg,
                        Type = currentParser.TypeName,
                    };

                    currentItem.PossibleNextValues.Add(possibleValue);
                }
            }
        }

        void CreateSummary()
        {
            var numPossibleValues = _dataItems.First(x=>x.FailedOnLoad==false).PossibleNextValues.Count();
            var validTypes = new List<string>();
            var validValues = new Dictionary<string, List<string>>();

            var invalidTypes = new List<string>();
            var failedValuesAndErrors = new Dictionary<string, List<string>>();

            for (int i = 0; i < numPossibleValues; i++)
            {
                var possibleValues = _dataItems
                    .Where(x=>x.FailedOnLoad == false)
                    .Select(x => x.PossibleNextValues[i]);

                var type = possibleValues.First().Type;
                var allValid = possibleValues.All(x => x.IsValid);

                if (allValid)
                {
                    var allValues = possibleValues
                        .Where(x=>x.IsValid)
                        .Select(x => x.Value)
                        .Distinct()
                        .ToList();

                    var allValuesGrouped = possibleValues
                        .GroupBy(x => x.Value)
                        .Select(x => new { Value = x.Key, Count = possibleValues.Count(y => y.Value == x.Key) })
                        .OrderByDescending(x => x.Count)
                        .Select(x=>$"[{x.Count}] {x.Value}")
                        .ToList();

                    validTypes.Add(type);
                    validValues[type] = allValuesGrouped;
                }
                else
                {
                    var allErrorsGrouped = possibleValues
                        .Where(x => x.IsValid == false)
                        .GroupBy(x=>x.ErrorMessage)
                        .Select(x => new { Value = x.Key, Count = possibleValues.Count(y => y.ErrorMessage == x.Key) })
                        .OrderByDescending(x => x.Count)
                        .Select(x => $"[{x.Count}] {x.Value}")
                        .ToList();

                    /*
                        .Select(x => new { Value = x.First().ErrorMessage, Count = possibleValues.Count(y => y.Value == x.Key) })
                        .OrderByDescending(x => x.Count)
                        .Select(x => $"[{x.Count}]{x.Value}")
                        .ToList();*/


                    //invalidTypes.Add(type);
                    //failedValuesAndErrors[type] = allErrorsGrouped;
                }
            }

            Console.Clear();
            Console.WriteLine("Possible Values");
            foreach (var item in validValues)
            {
                var longestItem = item.Value
                    .OrderByDescending(x=>x.Length)
                    .First().Length;

                Console.WriteLine($"\t{item.Key}:");
                var numItemsPerRow = 5;
                for(int i = 0; i < 10; i++)
                {
                    var currentValueItems = item.Value.Skip(numItemsPerRow * i).Take(numItemsPerRow);
                    var strs = currentValueItems.Select(x => x.PadRight(longestItem));
                    var str = string.Join(",", strs);

                    Console.WriteLine($"\t\t{str}");
                }
                    

                //Console.WriteLine($"\t\t{string.Join(",", item.Value.Take(20))}:");
            }

            //var value = Console.ReadLine();
        }


        public void PickItem(int index)
        { }

        public class DataItem
        { 
            public string DisplayName { get; set; }
            public ByteChunk Chunk { get; set; }
            public int LastKnownOffset { get; set; }

            public List<string> KnownValues { get; set; } = new List<string>();
            public List<string> KnownTypes { get; set; } = new List<string>();
            public List<PossibleNextValue> PossibleNextValues { get; set; } = new List<PossibleNextValue>();
            public bool FailedOnLoad { get; set; }


            public DataItem(ByteChunk chunk, string displayName)
            {
                DisplayName = displayName;
                if (chunk == null)
                {
                    FailedOnLoad = true;
                }
                else
                {
                    FailedOnLoad = false;
                    Chunk = chunk;
                    LastKnownOffset = Chunk.Index;
                }
            }
        }

        [DebuggerDisplay("{Value}, Type = {Type}, IsValid = {IsValid}")]
        public class PossibleNextValue
        { 
            public string Value { get; set; }
            public string Type { get; set; }
            public int Size { get; set; }
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class SummanyItem
        {
            public List<string> Values { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
            public string Type { get; set; }
            public bool HasError { get => Errors.Count != 0; }
        }
    }

    
}
