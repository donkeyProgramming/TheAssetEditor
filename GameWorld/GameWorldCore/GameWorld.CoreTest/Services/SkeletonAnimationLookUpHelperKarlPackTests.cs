using System.Collections.ObjectModel;
using System.Reflection;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Services
{
    internal class SkeletonAnimationLookUpHelperKarlPackTests
    {
        // Set these to concrete values after validating the Karl test pack content.
        // Keep as -1 to only assert "> 0".
        private const int ExpectedKarlSkeletonCount = 2;
        private const int ExpectedKarlAnimationReferenceCount = 3;

        [Test]
        [Timeout(60000)]
        public void LoadAndUnload_KarlPack_UpdatesSkeletonAndAnimationLookup()
        {
            var eventHub = new InMemoryGlobalEventHub();
            var containers = new List<IPackFileContainer>();

            var packFileService = new Mock<IPackFileService>(MockBehavior.Strict);
            packFileService.Setup(x => x.GetAllPackfileContainers())
                .Returns(() => containers.ToList());

            var settingsService = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var loader = new PackFileContainerLoader(settingsService);
            var karlPackPath = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");
            var karlContainer = loader.Load(karlPackPath);

            Assert.That(karlContainer, Is.Not.Null);

            using var helper = new SkeletonAnimationLookUpHelper(packFileService.Object, eventHub);

            containers.Add(karlContainer!);
            eventHub.PublishGlobalEvent(new PackFileContainerAddedEvent(karlContainer!));

            WaitForCondition(() =>
            {
                var (animationCount, skeletonCount) = GetLookupCounts(helper);
                return IsAnimationCountSatisfied(animationCount) && IsSkeletonCountSatisfied(skeletonCount);
            },
            timeoutMs: 45000,
            failMessage: "Karl pack load did not populate skeleton/animation lookup in time.");

            var (loadedAnimationCount, loadedSkeletonCount) = GetLookupCounts(helper);
            AssertLoadedCounts(loadedAnimationCount, loadedSkeletonCount);

            containers.Remove(karlContainer!);
            eventHub.PublishGlobalEvent(new PackFileContainerRemovedEvent(karlContainer!));

            WaitForCondition(() =>
            {
                var (animationCount, skeletonCount) = GetLookupCounts(helper);
                return animationCount == 0 && skeletonCount == 0;
            },
            timeoutMs: 10000,
            failMessage: "Karl pack unload did not clear skeleton/animation lookup in time.");

            var (unloadedAnimationCount, unloadedSkeletonCount) = GetLookupCounts(helper);
            Assert.That(unloadedAnimationCount, Is.EqualTo(0));
            Assert.That(unloadedSkeletonCount, Is.EqualTo(0));
        }

        private static void AssertLoadedCounts(int animationCount, int skeletonCount)
        {
            if (ExpectedKarlAnimationReferenceCount >= 0)
                Assert.That(animationCount, Is.EqualTo(ExpectedKarlAnimationReferenceCount));
            else
                Assert.That(animationCount, Is.GreaterThan(0));

            if (ExpectedKarlSkeletonCount >= 0)
                Assert.That(skeletonCount, Is.EqualTo(ExpectedKarlSkeletonCount));
            else
                Assert.That(skeletonCount, Is.GreaterThan(0));
        }

        private static bool IsAnimationCountSatisfied(int count)
        {
            if (ExpectedKarlAnimationReferenceCount >= 0)
                return count == ExpectedKarlAnimationReferenceCount;
            return count > 0;
        }

        private static bool IsSkeletonCountSatisfied(int count)
        {
            if (ExpectedKarlSkeletonCount >= 0)
                return count == ExpectedKarlSkeletonCount;
            return count > 0;
        }

        private static (int AnimationCount, int SkeletonCount) GetLookupCounts(SkeletonAnimationLookUpHelper helper)
        {
            var animationField = typeof(SkeletonAnimationLookUpHelper)
                .GetField("_skeletonNameToAnimationMap", BindingFlags.NonPublic | BindingFlags.Instance);
            var skeletonField = typeof(SkeletonAnimationLookUpHelper)
                .GetField("_skeletonFileNames", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(animationField, Is.Not.Null);
            Assert.That(skeletonField, Is.Not.Null);

            var animationMap = animationField!.GetValue(helper) as Dictionary<string, ObservableCollection<AnimationReference>>;
            var skeletonNames = skeletonField!.GetValue(helper) as ObservableCollection<string>;

            Assert.That(animationMap, Is.Not.Null);
            Assert.That(skeletonNames, Is.Not.Null);

            var animationCount = animationMap!.Values.Sum(x => x.Count);
            var skeletonCount = skeletonNames!.Count;
            return (animationCount, skeletonCount);
        }

        private static void WaitForCondition(Func<bool> condition, int timeoutMs, string failMessage)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                    return;
                System.Threading.Thread.Sleep(50);
            }
            Assert.That(false, Is.True, failMessage);
        }

        private sealed class InMemoryGlobalEventHub : IGlobalEventHub
        {
            private readonly object _lock = new object();
            private readonly Dictionary<Type, List<(object Owner, Delegate Callback)>> _subscriptions = [];

            public void PublishGlobalEvent<T>(T e)
            {
                List<(object Owner, Delegate Callback)> callbacks;
                lock (_lock)
                {
                    if (_subscriptions.TryGetValue(typeof(T), out var direct) == false)
                        return;

                    callbacks = direct.ToList();
                }

                foreach (var callback in callbacks)
                    ((Action<T>)callback.Callback).Invoke(e);
            }

            public void Register<T>(object owner, Action<T> action)
            {
                lock (_lock)
                {
                    if (_subscriptions.ContainsKey(typeof(T)) == false)
                        _subscriptions[typeof(T)] = [];

                    _subscriptions[typeof(T)].Add((owner, action));
                }
            }

            public void UnRegister(object owner)
            {
                lock (_lock)
                {
                    foreach (var (_, subscribers) in _subscriptions)
                    {
                        subscribers.RemoveAll(x => ReferenceEquals(x.Owner, owner));
                    }
                }
            }
        }
    }
}
