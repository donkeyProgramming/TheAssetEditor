using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharedCore.ByteParsing;
using SharedCore.PackFiles.Models;

namespace CommonControls.FormatResearch
{
    public class DecoderHelper
    {
        List<DataItem> _dataItems;
        List<IByteParser> _parserList;
        List<IByteParser> _addedFields = new List<IByteParser>();

        public void Create(IEnumerable<PackFile> files, List<IByteParser> parserList, Func<PackFile, DataItem> fileLoader, Action<List<DataItem>> peakComplateDataSet = null)
        {
            _dataItems = files
                .Select(x => fileLoader(x))         // Convert to format
                .Where(x => x != null)                // Skip things we dont want to care about
                .ToList();

            if (peakComplateDataSet != null)
                peakComplateDataSet(_dataItems);

            _parserList = parserList;

            while (true)
            {
                PeakNextStep();

                Console.Clear();
                CreateSummary();
                DisplayMenu();
            }
        }

        public void DisplayMenu()
        {
            while (true)
            {
                Console.WriteLine("\tMenu:");
                int buttonCounter = 1;
                foreach (var possibleType in _parserList)
                    Console.WriteLine($"\t\t{buttonCounter++}: Add as {possibleType.TypeName}");

                Console.WriteLine();

                Console.WriteLine($"\t\tP: Peak at next bytes for all (as chars)");
                Console.WriteLine($"\t\tpT: Peak types");
                Console.WriteLine($"\t\tPB: Peak at next bytes for all (as bytes)");

                Console.WriteLine($"\t\tC: Print current bytes read");
                Console.WriteLine($"\t\tS: Show scema");
                Console.WriteLine($"\t\tD: Trigger Debug");

                Console.SetWindowPosition(0, 0);
                var keyStr = Console.ReadLine().ToLower();
                Console.Clear();

                if (int.TryParse(keyStr, out var number))
                {
                    try
                    {
                        var parser = _parserList[number - 1];
                        PickItem(number - 1);
                    }
                    catch
                    {
                        Console.WriteLine($"\t\tInvalid input");
                    }
                }
                else if (keyStr == "d")
                {
                    Debugger.Break();
                }
                else if (keyStr == "p")
                {
                    PeakNextBytes(400, true);
                }
                else if (keyStr == "pt")
                {
                    PeakNextStep();
                    CreateSummary();
                }
                else if (keyStr == "pb")
                {
                    PeakNextBytes(64, false);
                }
                else if (keyStr == "c")
                {
                    PrintBytesRead();
                }
                else if (keyStr == "s")
                {
                    foreach (var item in _addedFields)
                        Console.WriteLine($"\t\t{item.TypeName}");
                }
                else
                {
                    Console.WriteLine($"\t\tInvalid input");
                }
            }
        }

        void PeakNextStep()
        {
            foreach (var currentItem in _dataItems)
            {
                if (currentItem.FailedOnLoad)
                    continue;

                currentItem.PossibleNextValues.Clear();
                var currentByteIndex = currentItem.LastKnownOffset;

                foreach (var decoder in _addedFields)
                {
                    decoder.TryDecode(currentItem.Chunk.Buffer, currentByteIndex, out var value, out var bytesRead, out var error);
                    currentByteIndex += bytesRead;
                }

                foreach (var currentParser in _parserList)
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


        public void PickItem(int index)
        {
            _addedFields.Add(_parserList[index]);
        }

        List<SummaryItem> CreateSummaryList()
        {
            var summaryList = new List<SummaryItem>();
            var numPossibleValues = _dataItems.First(x => x.FailedOnLoad == false).PossibleNextValues.Count();
            for (int i = 0; i < numPossibleValues; i++)
            {
                var allItemsOfType = _dataItems
                    .Where(x => x.FailedOnLoad == false)
                    .Select(x => x.PossibleNextValues[i]);

                var allValues = allItemsOfType
                    .Select(x => x.Value)
                    .Distinct()
                    .ToList();

                decimal? range = null;
                if (double.TryParse(allValues.First(), out _))
                {
                    var decimals = allValues
                        .Select(x =>
                        {
                            double? output = null;
                            if (double.TryParse(x, out var result))
                                return result;
                            return output;
                        })
                        .Where(x => x != null);
                    var min = decimals.Min();
                    var max = decimals.Max();
                    if (double.IsNaN(min.Value) || double.IsNaN(max.Value))
                    {
                        range = decimal.MaxValue;
                    }
                    else
                    {
                        range = (decimal)(max - min);
                    }


                }

                var allValuesGrouped = allItemsOfType
                    .GroupBy(x => x.Value)
                    .Select(x => new { Value = x.Key, Count = allItemsOfType.Count(y => y.Value == x.Key) })
                    .OrderByDescending(x => x.Count)
                    .Select(x => $"[{x.Count}] {x.Value}")
                    .ToList();

                var allErrorsGrouped = allItemsOfType
                    .Where(x => x.IsValid == false)
                    .GroupBy(x => x.ErrorMessage)
                    .Select(x => new { Value = x.Key, Count = allItemsOfType.Count(y => y.ErrorMessage == x.Key) })
                    .OrderByDescending(x => x.Count)
                    .Select(x => $"[{x.Count}] {x.Value}")
                    .ToList();

                var failCount = allItemsOfType.Count(x => x.IsValid == false);
                var summaryItem = new SummaryItem()
                {
                    Type = allItemsOfType.First().Type,
                    TotalItems = allItemsOfType.Count(),
                    FailedItems = failCount,
                    FailPercentage = (float)failCount / allItemsOfType.Count(),

                    ErrorMessages = allErrorsGrouped,
                    PossibleValues = allValuesGrouped,
                    Range = range
                };
                summaryList.Add(summaryItem);
            }
            return summaryList;
        }

        void PeakNextBytes(int numBytes, bool asStr)
        {
            Console.WriteLine($"\t--------------------------------------------------------------");

            int counter = 0;
            foreach (var item in _dataItems)
            {
                if (item.FailedOnLoad)
                    continue;

                if (counter == 37)
                { }

                var bytesLeft = item.Chunk.Buffer.Length - item.LastKnownOffset;

                var bufferSize = Math.Min(numBytes, bytesLeft);
                var endOfDataStr = "";
                if (item.DataMaxSize.HasValue)
                {
                    var dataLeftInData = (int)item.DataMaxSize.Value - (int)item.DataRead;
                    if (dataLeftInData < bufferSize)
                    {
                        bufferSize = (int)item.DataMaxSize.Value - (int)item.DataRead;
                        endOfDataStr = "[EOD]";
                    }
                }

                item.Chunk.Index = item.LastKnownOffset;
                var data = item.Chunk.ReadBytes(bufferSize);

                if (asStr)
                {
                    var str = Encoding.ASCII.GetString(data);
                    str = SanitizeString(str);
                    Console.WriteLine($"\t\t[{counter++.ToString().PadLeft(5, ' ')}]:{str}{endOfDataStr}");
                }
                else
                {
                    var str = string.Join(" ", data.Select(x => x.ToString().PadLeft(3, ' ')));
                    Console.WriteLine($"\t\t[{counter++.ToString().PadLeft(5, ' ')}]:{str}{endOfDataStr}");
                }
            }

            Console.WriteLine($"\t--------------------------------------------------------------");
        }

        void PrintBytesRead()
        {
            Console.WriteLine($"\t--------------------------------------------------------------");

            int counter = 0;
            foreach (var item in _dataItems)
            {
                if (item.FailedOnLoad)
                    continue;

                var str = item.DataRead.ToString().PadLeft(5, ' ');
                var sizeStr = "";
                if (item.DataMaxSize.HasValue)
                    sizeStr = $" MaxSize = {item.DataMaxSize.Value} Left = {item.DataMaxSize.Value - item.DataRead} ";

                Console.WriteLine($"\t\t[{counter++.ToString().PadLeft(5, ' ')}]:Read:{str} {sizeStr}- {item.DisplayName}");
            }

            Console.WriteLine($"\t--------------------------------------------------------------");
        }

        string SanitizeString(string input)
        {
            StringBuilder literal = new StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\0': literal.Append(@" "); break;
                    case '\"': literal.Append(@"|"); break;
                    case '\\': literal.Append(@"|"); break;
                    case '\a': literal.Append(@"|"); break;
                    case '\b': literal.Append(@"|"); break;
                    case '\f': literal.Append(@"|"); break;
                    case '\n': literal.Append(@"|"); break;
                    case '\r': literal.Append(@"|"); break;
                    case '\t': literal.Append(@"|"); break;
                    case '\v': literal.Append(@"|"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append("?");
                        }
                        break;
                }
            }

            var str = SanitizeStringBasic(literal.ToString(), true);

            return str;
        }

        public string SanitizeStringBasic(string input, bool nullToSpace)
        {
            var cpy = new string(input);
            cpy = cpy.Replace('\n', '|');
            if (nullToSpace)
                cpy = cpy.Replace('\0', ' ');
            cpy = cpy.Replace('\x0A', '|');
            return cpy;
        }

        void CreateSummary()
        {
            var summaryList = CreateSummaryList();

            Console.WriteLine("Possible Values:");
            foreach (var item in summaryList)
            {
                var rangeStr = item.Range == null ? "" : $"|  Range = {item.Range}";
                Console.WriteLine($"\t{item.Type}: {item.FailedItems}[Failed] / {item.TotalItems}[Total] = {(1.0f - item.FailPercentage) * 100}[%OK] {rangeStr}");
                Console.WriteLine($"\t--------------------------------------------------------------");

                // if (item.FailPercentage < 0.10f)
                {
                    var longestItem = item.PossibleValues
                          .OrderByDescending(x => x.Length)
                          .First().Length;

                    if (item.FailPercentage != 0)
                        longestItem = Math.Min(longestItem, 30);

                    longestItem = Math.Min(longestItem, 30);

                    var numItemsPerRow = 8;
                    var numRows = 20;
                    for (int i = 0; i < numRows; i++)
                    {
                        var currentValueItems = item.PossibleValues.Skip(numItemsPerRow * i).Take(numItemsPerRow);
                        if (currentValueItems.Count() != 0)
                        {
                            var strs = currentValueItems.Select(x => x.PadRight(longestItem).Substring(0, longestItem));
                            strs = strs.Select(x => x.Replace('\n', '|'));
                            var str = string.Join(",", strs);
                            var safeStr = str;// SanitizeStringBasic(str, false);

                            Console.WriteLine($"\t\t{safeStr}");
                        }
                    }
                }

                if (item.FailedItems != 0)
                {
                    var errorsToPrint = item.ErrorMessages.Take(3);

                    foreach (var error in errorsToPrint)
                        Console.WriteLine($"\t\t{error}");
                }

                Console.WriteLine();
            }
        }





        public class DataItem
        {
            public string DisplayName { get; set; }
            public ByteChunk Chunk { get; set; }
            public int LastKnownOffset { get; set; }

            public List<string> KnownValues { get; set; } = new List<string>();
            public List<string> KnownTypes { get; set; } = new List<string>();
            public List<PossibleNextValue> PossibleNextValues { get; set; } = new List<PossibleNextValue>();
            public bool FailedOnLoad { get; set; }
            public object AdditionalData { get; set; }
            public uint? DataMaxSize { get; set; }
            public uint DataRead { get; set; }
            public DataItem(ByteChunk chunk, string displayName, object additionalData = null)
            {
                AdditionalData = additionalData;
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

        class SummaryItem
        {
            public string Type { get; set; }
            public List<string> PossibleValues { get; set; }
            public int TotalItems { get; set; }
            public int FailedItems { get; set; }
            public float FailPercentage { get; set; }
            public decimal? Range { get; set; }
            public List<string> ErrorMessages { get; set; }
        }
    }
}
