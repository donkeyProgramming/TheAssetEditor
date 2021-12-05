using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.RigidModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Services;
using View3D.Utility;

namespace FileTypesTests.FileTypes.RigidModel
{
    static class Rmv2Validator
    {
        static public void ValidateDefaultDwarfHead(RmvFile file)
        { 
        }

        static public void AssertEqualByteStreams(byte[] expectedBytes, byte[] actualBytes)
        {
            //Assert.AreEqual()
        }
    }

    [TestFixture]
    class Warhammer2_Serialization
    {
        readonly string _dwarfHeadPath = "DwarfHead.rmv2";
        readonly string _swordPath = "DwarfHead.rmv2";

        PackFileService _pfs;
        ModelFactory _modelFactory;
        ResourceLibary _resourceLibary;

         [SetUp]
        public void Init()
        {
            _pfs = new PackFileService(new PackFileDataBase(), null);
            _pfs.Load("TestPackPath");

            _resourceLibary = new ResourceLibary(null, _pfs);

            _modelFactory = ModelFactory.Create();
        }

        [Test]
        public void DwarfHead_LoadAndSave_NoChanges()
        {
            var dwarfHeadBytes = _pfs.FindFile(_dwarfHeadPath).DataSource.ReadData();
            
            var dwarfModel = _modelFactory.Load(dwarfHeadBytes);
            Rmv2Validator.ValidateDefaultDwarfHead(dwarfModel);

            var resavedModelBytes = _modelFactory.Save(dwarfModel);
            Rmv2Validator.AssertEqualByteStreams(dwarfHeadBytes, resavedModelBytes);

            var reloadedDwarfModel = _modelFactory.Load(resavedModelBytes);
            Rmv2Validator.ValidateDefaultDwarfHead(reloadedDwarfModel);
        }

        [Test]
        public void DwarfHead_LoadAndSave_DefaultChanges()
        {
            var dwarfHeadPack = _pfs.FindFile(_dwarfHeadPath);
            var dwarfModel = ModelFactory.Create().Load(dwarfHeadPack.DataSource.ReadData());
            Rmv2Validator.ValidateDefaultDwarfHead(dwarfModel);


        }

        [Test]
        public void Sword_LoadAndSave_NoChanges()
        {
        }


        [Test]
        public void Sword_LoadAndSave_DetaultChanges()
        {
        }






        [Test]
        public void Shit()
        {
            // MeshSaverService

            // SceneLoader

            // ModelFactory


            SceneLoader loader = new SceneLoader(_resourceLibary, _pfs);

            var skeletonName = "";
            var loadedNode = loader.Load(null, null, null, ref skeletonName, null);

            MeshSaverService.Save(false, null, null, RmvVersionEnum.RMV2_V6, false);
        }
    }
}
