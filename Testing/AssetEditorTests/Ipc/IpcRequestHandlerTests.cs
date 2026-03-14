using AssetEditor.Services.Ipc;
using Shared.Core.PackFiles.Models;

namespace AssetEditorTests.Ipc
{
    [TestClass]
    public class IpcRequestHandlerTests
    {
        [TestMethod]
        public async Task HandleAsync_ReturnsUnsupportedAction_ForUnknownAction()
        {
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup();
            var opener = new FakeOpenExecutor();
            var notifier = new FakeNotifier();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, notifier);

            var result = await sut.HandleAsync(new IpcRequest { Action = "ping", Path = "x" }, CancellationToken.None);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual("Unsupported action", result.Error);
            Assert.AreEqual(0, opener.OpenCallCount);
            Assert.AreEqual(0, notifier.CallCount);
            Assert.AreEqual(0, packLoader.CallCount);
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsError_ForEmptyPath()
        {
            var sut = new IpcRequestHandler(new FakePackLoader(), new FakeLookup(), new FakeOpenExecutor(), new FakeNotifier());

            var result = await sut.HandleAsync(new IpcRequest { Action = "open", Path = "   " }, CancellationToken.None);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual("Path is empty", result.Error);
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsNotFound_AndShowsDialog_WhenLookupFails()
        {
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup();
            var opener = new FakeOpenExecutor();
            var notifier = new FakeNotifier();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, notifier);

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = @"C:\tmp\variantmeshes\foo\bar.rigid_model_v2"
            }, CancellationToken.None);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual("File not found", result.Error);
            Assert.AreEqual(@"variantmeshes\foo\bar.rigid_model_v2", result.NormalizedPath);
            Assert.AreEqual(@"variantmeshes\foo\bar.rigid_model_v2", lookup.LastRequestedPath);
            Assert.AreEqual(1, notifier.CallCount);
            Assert.AreEqual(@"variantmeshes\foo\bar.rigid_model_v2", notifier.LastPath);
            Assert.AreEqual(0, opener.OpenCallCount);
            Assert.AreEqual(0, packLoader.CallCount);
        }

        [TestMethod]
        public async Task HandleAsync_OpensFile_AndReturnsOk_WhenLookupSucceeds()
        {
            var packFile = PackFile.CreateFromBytes("bird.rigid_model_v2", []);
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup { Result = packFile };
            var opener = new FakeOpenExecutor();
            var notifier = new FakeNotifier();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, notifier);

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = @"variantmeshes\foo\bird.rigid_model_v2"
            }, CancellationToken.None);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, opener.OpenCallCount);
            Assert.AreSame(packFile, opener.LastFile);
            Assert.IsTrue(opener.LastBringToFront);
            Assert.IsFalse(opener.LastOpenInExistingKitbashTab);
            Assert.AreEqual(0, notifier.CallCount);
            Assert.AreEqual(0, packLoader.CallCount);
        }

        [TestMethod]
        public async Task HandleAsync_DoesNotBringToFront_WhenBringToFrontFalse()
        {
            var packFile = PackFile.CreateFromBytes("bird.rigid_model_v2", []);
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup { Result = packFile };
            var opener = new FakeOpenExecutor();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, new FakeNotifier());

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = @"variantmeshes\foo\bird.rigid_model_v2",
                BringToFront = false
            }, CancellationToken.None);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, opener.OpenCallCount);
            Assert.IsFalse(opener.LastBringToFront);
            Assert.IsFalse(opener.LastOpenInExistingKitbashTab);
            Assert.AreEqual(0, packLoader.CallCount);
        }

        [TestMethod]
        public async Task HandleAsync_LoadsPackFromDisk_WhenPackPathProvided()
        {
            var packFile = PackFile.CreateFromBytes("arb_base_elephant.rigid_model_v2", []);
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup { Result = packFile };
            var opener = new FakeOpenExecutor();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, new FakeNotifier());

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = "variantmeshes/wh_variantmodels/el1/arb/arb_new_elephants/arb_base_elephant/arb_base_elephant.rigid_model_v2",
                PackPathOnDisk = "k:/SteamLibrary/steamapps/common/Total War WARHAMMER III/data/ovn_araby.pack"
            }, CancellationToken.None);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, packLoader.CallCount);
            Assert.AreEqual("k:/SteamLibrary/steamapps/common/Total War WARHAMMER III/data/ovn_araby.pack", packLoader.LastPackPath);
            Assert.AreEqual(1, opener.OpenCallCount);
            Assert.IsFalse(opener.LastOpenInExistingKitbashTab);
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsFailure_WhenPackLoadFails()
        {
            var packLoader = new FakePackLoader
            {
                Result = PackLoadResult.Fail("Pack file load failed")
            };
            var lookup = new FakeLookup();
            var opener = new FakeOpenExecutor();
            var notifier = new FakeNotifier();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, notifier);

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = @"variantmeshes\foo\bird.rigid_model_v2",
                PackPathOnDisk = @"k:\mods\ovn_araby.pack"
            }, CancellationToken.None);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual("Pack file load failed", result.Error);
            Assert.AreEqual(1, packLoader.CallCount);
            Assert.AreEqual(0, opener.OpenCallCount);
            Assert.AreEqual(0, notifier.CallCount);
        }

        [TestMethod]
        public async Task HandleAsync_PassesOpenInExistingKitbashTabFlag_ToExecutor()
        {
            var packFile = PackFile.CreateFromBytes("arb_base_elephant_1.wsmodel", []);
            var packLoader = new FakePackLoader();
            var lookup = new FakeLookup { Result = packFile };
            var opener = new FakeOpenExecutor();
            var sut = new IpcRequestHandler(packLoader, lookup, opener, new FakeNotifier());

            var result = await sut.HandleAsync(new IpcRequest
            {
                Action = "open",
                Path = @"variantmeshes\foo\arb_base_elephant_1.wsmodel",
                OpenInExistingKitbashTab = true
            }, CancellationToken.None);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, opener.OpenCallCount);
            Assert.IsTrue(opener.LastOpenInExistingKitbashTab);
        }

        private class FakeLookup : IExternalPackFileLookup
        {
            public PackFile? Result { get; set; }
            public string? LastRequestedPath { get; private set; }

            public PackFile? FindByPath(string path)
            {
                LastRequestedPath = path;
                return Result;
            }
        }

        private class FakePackLoader : IExternalPackLoader
        {
            public int CallCount { get; private set; }
            public string? LastPackPath { get; private set; }
            public PackLoadResult Result { get; set; } = PackLoadResult.Ok();

            public Task<PackLoadResult> EnsureLoadedAsync(string packPathOnDisk, CancellationToken cancellationToken)
            {
                CallCount++;
                LastPackPath = packPathOnDisk;
                return Task.FromResult(Result);
            }
        }

        private class FakeOpenExecutor : IExternalFileOpenExecutor
        {
            public int OpenCallCount { get; private set; }
            public PackFile? LastFile { get; private set; }
            public bool LastBringToFront { get; private set; }
            public bool LastOpenInExistingKitbashTab { get; private set; }

            public Task OpenAsync(PackFile file, bool bringToFront, bool openInExistingKitbashTab, CancellationToken cancellationToken)
            {
                OpenCallCount++;
                LastFile = file;
                LastBringToFront = bringToFront;
                LastOpenInExistingKitbashTab = openInExistingKitbashTab;
                return Task.CompletedTask;
            }
        }

        private class FakeNotifier : IIpcUserNotifier
        {
            public int CallCount { get; private set; }
            public string? LastPath { get; private set; }

            public Task ShowExternalOpenFailedAsync(string normalizedPath, CancellationToken cancellationToken)
            {
                CallCount++;
                LastPath = normalizedPath;
                return Task.CompletedTask;
            }
        }
    }
}
