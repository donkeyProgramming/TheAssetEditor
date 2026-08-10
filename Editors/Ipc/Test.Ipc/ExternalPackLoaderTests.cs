using Editors.Ipc;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Test.Ipc
{
    public class ExternalPackLoaderTests
    {
        [Test]
        public async Task EnsureLoadedAsync_LoadsAndAddsPackInsideUiDispatcher()
        {
            var packPath = Path.GetFullPath("external.pack");
            var container = new Mock<IPackFileContainer>().Object;
            var packFileService = new Mock<IPackFileService>();
            var containerLoader = new Mock<IPackFileContainerLoader>();
            var dispatcher = new RecordingUiDispatcher();

            packFileService
                .Setup(x => x.IsPackFileLoaded(packPath))
                .Returns(false);
            containerLoader
                .Setup(x => x.CreateFromPackFile(PackFileContainerType.Normal, packPath, true))
                .Returns(() =>
                {
                    Assert.That(dispatcher.IsExecuting, Is.True);
                    return container;
                });
            packFileService
                .Setup(x => x.AddContainer(container, false))
                .Returns(() =>
                {
                    Assert.That(dispatcher.IsExecuting, Is.True);
                    return container;
                });

            var sut = new ExternalPackLoader(packFileService.Object, containerLoader.Object, dispatcher);

            var result = await sut.EnsureLoadedAsync(packPath, CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(dispatcher.InvocationCount, Is.EqualTo(1));
            containerLoader.Verify(
                x => x.CreateFromPackFile(PackFileContainerType.Normal, packPath, true),
                Times.Once);
            packFileService.Verify(x => x.AddContainer(container, false), Times.Once);
        }

        [Test]
        public async Task EnsureLoadedAsync_DoesNotReloadPack_WhenSourcePackIsAlreadyLoaded()
        {
            var packPath = Path.GetFullPath("already-loaded.pack");
            var packFileService = new Mock<IPackFileService>();
            var containerLoader = new Mock<IPackFileContainerLoader>();
            var dispatcher = new RecordingUiDispatcher();

            packFileService
                .Setup(x => x.IsPackFileLoaded(packPath))
                .Returns(true);

            var sut = new ExternalPackLoader(packFileService.Object, containerLoader.Object, dispatcher);

            var result = await sut.EnsureLoadedAsync(packPath, CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(dispatcher.InvocationCount, Is.EqualTo(1));
            containerLoader.Verify(
                x => x.CreateFromPackFile(It.IsAny<PackFileContainerType>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
            packFileService.Verify(
                x => x.AddContainer(It.IsAny<IPackFileContainer>(), It.IsAny<bool>()),
                Times.Never);
        }

        private sealed class RecordingUiDispatcher : IUiDispatcher
        {
            public int InvocationCount { get; private set; }
            public bool IsExecuting { get; private set; }

            public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InvocationCount++;
                IsExecuting = true;
                try
                {
                    return Task.FromResult(action());
                }
                finally
                {
                    IsExecuting = false;
                }
            }
        }
    }
}
