using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace GameWorld.Core.Test.Services
{
    internal class SkeletonAnimationLookUpHelperTests
    {
        [Test]
        public void GetAnimationsForSkeleton_ReturnsImmediately_WhenDataAlreadyExists()
        {
            var releaseInitialLoad = new ManualResetEventSlim(false);
            var packFileService = CreatePackFileServiceThatBlocksInitialLoad(releaseInitialLoad);
            var eventHub = new Mock<IGlobalEventHub>();
            var helper = new SkeletonAnimationLookUpHelper(packFileService.Object, eventHub.Object);

            var container = new Mock<IPackFileContainer>().Object;
            var expected = new ObservableCollection<AnimationReference>
            {
                new AnimationReference("animations\\battle\\unit\\idle.anim", container)
            };

            SetAnimationLookupEntry(helper, "human_skeleton", expected);

            var stopwatch = Stopwatch.StartNew();
            var result = helper.GetAnimationsForSkeleton("human_skeleton");
            stopwatch.Stop();

            Assert.That(result, Is.SameAs(expected));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(200));

            releaseInitialLoad.Set();
            helper.Dispose();
        }

        [Test]
        public async Task GetAnimationsForSkeleton_WaitsForInitialLoad_WhenDataMissing()
        {
            var releaseInitialLoad = new ManualResetEventSlim(false);
            var packFileService = CreatePackFileServiceThatBlocksInitialLoad(releaseInitialLoad);
            var eventHub = new Mock<IGlobalEventHub>();
            var helper = new SkeletonAnimationLookUpHelper(packFileService.Object, eventHub.Object);

            var pendingCall = Task.Run(() => helper.GetAnimationsForSkeleton("missing_skeleton"));
            await Task.Delay(100);

            Assert.That(pendingCall.IsCompleted, Is.False);

            releaseInitialLoad.Set();

            var completed = await Task.WhenAny(pendingCall, Task.Delay(3000));
            Assert.That(completed, Is.SameAs(pendingCall));

            var result = await pendingCall;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));

            helper.Dispose();
        }

        private static Mock<IPackFileService> CreatePackFileServiceThatBlocksInitialLoad(ManualResetEventSlim releaseInitialLoad)
        {
            var packFileService = new Mock<IPackFileService>(MockBehavior.Strict);
            packFileService.Setup(x => x.GetAllPackfileContainers())
                .Returns(() =>
                {
                    releaseInitialLoad.Wait();
                    return [];
                });

            return packFileService;
        }

        private static void SetAnimationLookupEntry(
            SkeletonAnimationLookUpHelper helper,
            string skeletonName,
            ObservableCollection<AnimationReference> entries)
        {
            var field = typeof(SkeletonAnimationLookUpHelper)
                .GetField("_skeletonNameToAnimationMap", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);

            var map = field!.GetValue(helper) as Dictionary<string, ObservableCollection<AnimationReference>>;
            Assert.That(map, Is.Not.Null);

            map![skeletonName] = entries;
        }
    }
}
