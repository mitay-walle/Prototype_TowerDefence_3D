BInject is a lightweight Dependency Injection framework for Unity.

Core concepts:
- Context represents dependency scope and hierarchy.
- Context.Create("name") creates a named global context.
- Only one context per name can exist.
- Contexts can have parent contexts.
- Context.Destroy() destroys child contexts and Injected GameObjects.
- Context classes are only hierarchy/lifetime anchors.
- Do not register Context classes or Context instances as dependencies.
- Every Context has a Builder; the Builder collects and performs dependency/type/factory/command registrations for that context.
- Keep registration code in the Builder, not in the Context class.
- Do not pass Context into services and do not resolve dependencies manually inside services.
- Service constructors list the dependencies they need. Those dependency types must be registered in the Builder, then the service itself is registered with RegisterType<TService>() or RegisterTypeAs<TService, TInterface>() for autocomposition.
- Scene `MonoBehaviour` objects are not registered directly as DI dependencies.
- For scene objects needed by DI, register a plain provider object in the Builder. The scene behaviour registers itself into that provider in `Awake`: `TargetProvider : object`; `TargetBehaviour : MonoBehaviour` calls `TargetProvider.Register(this)`.

Builder registration variants:
- RegisterDependency(instance)
- RegisterDependencyAs<Concrete, Interface>(instance)
- RegisterType<T>()
- RegisterTypeAs<Concrete, Interface>()
- RegisterFactory<T, TFactory>()
- RegisterFactory<T>(factoryInstance)
- RegisterAsFunction<TResult, TSource>(func)
- RegisterCommand<TEvent, TCommand>()

Injector:
- Injector is a MonoBehaviour.
- Injector injects dependencies on Awake.
- Injector must run after context creation and Builder registration, before dependent scripts.
- Injector injects members marked with attributes.

Injection:
- [Inject] resolves dependency from context.
- Works on fields, properties, methods, constructors.
- Method injection is preferred.

Autocomposition:
- Context can create objects automatically from types registered by the Builder.
- Prefer RegisterType<TService>()/RegisterTypeAs<TService, TInterface>() plus constructor dependencies over passing Context into the service.
- One instance per type per context.
- Constructor with [Inject] is preferred.
- If no [Inject], smallest constructor is used.

Factories:
- Factories create objects on each resolve.

Events:
- IEventDispatcher.DispatchEvent sends events.
- [InjectEvent] receives events.
- Inherit=true enables interface-based events.

Commands:
- Register commands in the Builder with `context.RegisterCommand<TEvent, TCommand>()`.
- Do not register `ICommand` implementations with `RegisterType<TCommand>()`; it creates a normal dependency and does not connect the command to BInject event dispatch.
- Use commands for event-driven workflow execution; dispatch the event instead of manually resolving and executing commands when a BInject command fits.
- Runtime/per-call command parameters are carried by the event object.
- Constructor dependencies are for registered services/state; per-call data belongs in the event payload.
- Commands receive event payload values with an `[InjectEvent]` method or member.
- Command flow: create event DTO -> `IEventDispatcher.DispatchEvent(event)` -> BInject autocomposes command -> `[InjectEvent]` injects payload -> `ICommand.Execute()`.

Example command event payload:
```csharp
public class StartSessionEvent
{
    public bool LoadSave;
}

public class StartSessionCommand : ICommand
{
    private readonly SessionLifecycle _sessionLifecycle;
    private StartSessionEvent _event;

    public StartSessionCommand(SessionLifecycle sessionLifecycle)
    {
        _sessionLifecycle = sessionLifecycle;
    }

    [InjectEvent]
    public void Init(StartSessionEvent startSessionEvent)
    {
        _event = startSessionEvent;
    }

    public void Execute()
    {
        _sessionLifecycle.StartSession(_event.LoadSave);
    }
}
```

Example registration and dispatch:
```csharp
context.RegisterCommand<StartSessionEvent, StartSessionCommand>();
_eventDispatcher.DispatchEvent(new StartSessionEvent { LoadSave = true });
```

Hierarchy:
- HierarchyContext provides local context for GameObject trees.
- Use Context.CreateLocal().