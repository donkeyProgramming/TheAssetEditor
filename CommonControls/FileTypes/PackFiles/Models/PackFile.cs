// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommonControls.Common;

namespace CommonControls.FileTypes.PackFiles.Models
{
    public class PackFile : NotifyPropertyChangedImpl
    {
        public IDataSource DataSource { get; set; }

        public PackFile(string name, IDataSource dataSource)
        {
            Name = name;
            DataSource = dataSource;
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        public override string ToString() { return Name; }

        public string Extention { get => Path.GetExtension(Name); }


        public static PackFile CreateFromBytes(string fileName, byte[] bytes) => new PackFile(fileName, new MemorySource(bytes));
        public static PackFile CreateFromASCII(string fileName, string str) => new PackFile(fileName, new MemorySource(System.Text.Encoding.ASCII.GetBytes(str)));
    }



}
