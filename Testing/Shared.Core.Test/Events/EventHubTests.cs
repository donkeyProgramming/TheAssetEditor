using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Test.TestingUtility.TestUtility;

namespace Test.Shared.Core.Events
{
    public class EventHubTests
    {
        SimpleEditor _editorA;
        SimpleEditor _editorB;
        IScopeRepository _scopeRepository;

        [SetUp]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IScopeRepository, ScopeRepository>();
            serviceCollection.AddSingleton<IGlobalEventHub, SingletonScopeEventHub>();
            serviceCollection.AddScoped<IEventHub, LocalScopeEventHub>();
            serviceCollection.AddScoped<ScopedClass>();
            serviceCollection.AddScoped<ScopeToken>();

            // Build first provider
            var initialServiceProvider = serviceCollection.BuildServiceProvider();

            // Add self and build final provider
            serviceCollection.AddSingleton<IServiceProvider>(initialServiceProvider);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Configure the scope repository
            _scopeRepository = serviceProvider.GetRequiredService<IScopeRepository>();

            // Add editors
            _editorA = new SimpleEditor("EditorA");
            _editorB = new SimpleEditor("EditorB");
            _scopeRepository.CreateScope(_editorA);
            _scopeRepository.CreateScope(_editorB);
        }


        [Test]
        public void PublishGlobalEvent_FromGlobalToGlobal()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_global = _scopeRepository.GetRequiredService<IGlobalEventHub>(_editorA);

            var isEventTriggered = false;
            editorA_global.Register<ExampleEvent>(this, x => isEventTriggered = true);

            // Act
            globalHub.PublishGlobalEvent(new ExampleEvent());

            // Assert
            Assert.That(isEventTriggered, Is.True);
        }

        [Test]
        public void PublishGlobalEvent_FromGlobalToLocal()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);

            var isEventTriggered = false;
            editorA_local.Register<ExampleEvent>(this, x => isEventTriggered = true);

            // Act
            globalHub.PublishGlobalEvent(new ExampleEvent());

            // Assert
            Assert.That(isEventTriggered, Is.True);
        }


        [Test]
        public void PublishGlobalEvent_FromLocalToGlobal()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);
            var editorb_local = _scopeRepository.GetRequiredService<IEventHub>(_editorB);

            var isEventA_Triggered = false;
            globalHub.Register<ExampleEvent>(this, x => isEventA_Triggered = true);

            var isEventB_Triggered = false;
            editorA_local.Register<ExampleEvent>(this, x => isEventB_Triggered = true);

            var isEventC_Triggered = false;
            editorb_local.Register<ExampleEvent>(this, x => isEventC_Triggered = true);

            // Act
            editorA_local.PublishGlobalEvent(new ExampleEvent());

            // Assert
            Assert.That(isEventA_Triggered, Is.True);
            Assert.That(isEventB_Triggered, Is.True);
            Assert.That(isEventC_Triggered, Is.True);
        }

        [Test]
        public void Publish_FomLocalToLocal()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);
            var editorb_local = _scopeRepository.GetRequiredService<IEventHub>(_editorB);

            var isEventA_Triggered = false;
            globalHub.Register<ExampleEvent>(this, x => isEventA_Triggered = true);

            var isEventB_Triggered = false;
            editorA_local.Register<ExampleEvent>(this, x => isEventB_Triggered = true);

            var isEventC_Triggered = false;
            editorb_local.Register<ExampleEvent>(this, x => isEventC_Triggered = true);

            // Act
            editorA_local.Publish(new ExampleEvent());

            // Assert
            Assert.That(isEventA_Triggered, Is.False);
            Assert.That(isEventB_Triggered, Is.True);
            Assert.That(isEventC_Triggered, Is.False);
        }

        [Test]
        public void Publish_FomLocalToLocal_SubscribeToBaseType()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);
            var editorb_local = _scopeRepository.GetRequiredService<IEventHub>(_editorB);

            var isEventA_Triggered = false;
            globalHub.Register<BaseEvent>(this, x => isEventA_Triggered = true);

            var isEventB_Triggered = false;
            editorA_local.Register<BaseEvent>(this, x => isEventB_Triggered = true);

            var isEventC_Triggered = false;
            editorb_local.Register<BaseEvent>(this, x => isEventC_Triggered = true);


            BaseEvent t = new ExampleEvent();
            // Act
            editorA_local.Publish(new ExampleEvent());

            // Assert
            Assert.That(isEventA_Triggered, Is.False);
            Assert.That(isEventB_Triggered, Is.True);
            Assert.That(isEventC_Triggered, Is.False);
        }

        [Test]
        public void Publish_FomLocalToLocal_SubscribeToBaseType_DifferentEvent()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);
            var editorb_local = _scopeRepository.GetRequiredService<IEventHub>(_editorB);

            var isEventA_Triggered = false;
            globalHub.Register<BaseEvent>(this, x => isEventA_Triggered = true);

            var isEventB_Triggered = false;
            editorA_local.Register<BaseEvent>(this, x => isEventB_Triggered = true);

            var isEventC_Triggered = false;
            editorb_local.Register<BaseEvent>(this, x => isEventC_Triggered = true);

            // Act
            editorA_local.Publish(new ExampleEventNoBase());

            // Assert
            Assert.That(isEventA_Triggered, Is.False);
            Assert.That(isEventB_Triggered, Is.False);
            Assert.That(isEventC_Triggered, Is.False);
        }

        [Test]
        public void Unsubscribe()
        {
            // Arrange
            var globalHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
            var editorA_local = _scopeRepository.GetRequiredService<IEventHub>(_editorA);
            var editorb_local = _scopeRepository.GetRequiredService<IEventHub>(_editorB);

            var isEventA_Triggered = false;
            globalHub.Register<BaseEvent>(this, x => isEventA_Triggered = true);

            var isEventB_Triggered = false;
            editorA_local.Register<BaseEvent>(this, x => isEventB_Triggered = true);
            editorA_local.UnRegister(this);

            var isEventC_Triggered = false;
            editorb_local.Register<BaseEvent>(this, x => isEventC_Triggered = true);

            // Act
            editorA_local.Publish(new ExampleEventNoBase());

            // Assert
            Assert.That(isEventA_Triggered, Is.False);
            Assert.That(isEventB_Triggered, Is.False);
            Assert.That(isEventC_Triggered, Is.False);
        }
    }
}
