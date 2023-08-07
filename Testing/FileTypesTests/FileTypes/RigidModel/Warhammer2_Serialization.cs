/*using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using NUnit.Framework;
using View3D.Utility;

namespace FileTypesTests.FileTypes.RigidModel
{
    static class Rmv2Validator
    {
        static public void ValidateDefaultDwarfHead(RmvFile file, int expectedBoneNames, bool validateOffsets = true)
        {
            // BuilderPattern
        }

        static public void AssertEqualByteStreams(byte[] expectedBytes, byte[] actualBytes)
        {
            Assert.AreEqual(expectedBytes.Length, actualBytes.Length);
            for (int i = 0; i < expectedBytes.Length; i++)
                Assert.AreEqual(expectedBytes[i], actualBytes[i]);
        }
    }

    [TestFixture]
    class Warhammer2_Serialization
    {
        //readonly string _dwarfHeadPath = "DwarfHead.rmv2";
        //readonly string _swordPath = "DwarfHead.rmv2";
        //
        //PackFileService _pfs;
        //ModelFactory _modelFactory;
        //ResourceLibary _resourceLibary;

        [SetUp]
        public void Init()
        {
            //_pfs = new PackFileService(new PackFileDataBase(), null);
            //_pfs.Load("TestPackPath");
            //
            //_resourceLibary = new ResourceLibary(null, _pfs);
            //
            //_modelFactory = ModelFactory.Create();
        }

        [Test]
        public void DwarfHead_LoadAndSave_NoChanges()
        {
            //var dwarfHeadBytes = _pfs.FindFile(_dwarfHeadPath).DataSource.ReadData();
            //
            //var dwarfModel = _modelFactory.Load(dwarfHeadBytes);
            //Rmv2Validator.ValidateDefaultDwarfHead(dwarfModel, 12);
            //
            //var resavedModelBytes = _modelFactory.Save(dwarfModel);
            //Rmv2Validator.AssertEqualByteStreams(dwarfHeadBytes, resavedModelBytes);
            //
            //var reloadedDwarfModel = _modelFactory.Load(resavedModelBytes);
            //Rmv2Validator.ValidateDefaultDwarfHead(reloadedDwarfModel, 12);
        }

        [Test]
        public void DwarfHead_LoadAndSave_DefaultChanges()
        {
            //var dwarfHeadPack = _pfs.FindFile(_dwarfHeadPath);
            //var dwarfModel = ModelFactory.Create().Load(dwarfHeadPack.DataSource.ReadData());
            //Rmv2Validator.ValidateDefaultDwarfHead(dwarfModel, 12);
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

            //
            //SceneLoader loader = new SceneLoader(_resourceLibary, _pfs, GeometryGraphicsContextFactory.CreateInstance(_resourceLibary.GraphicsDevice));
            //var loadedNode = loader.Load(_pfs.FindFile(_dwarfHeadPath), null, null);
            //
            //var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(loadedNode);
            //

            //MeshSaverService.Save(true, modelNodes, null, RmvVersionEnum.RMV2_V7, true);
        }
    }
}
*/
