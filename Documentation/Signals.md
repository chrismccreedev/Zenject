
## Signals

## <a id="theory"></a>Motivation / Theory

Given two classes A and B that need to communicate, your options are usually:

1. Directly call a method on B from A.  In this case, A is strongly coupled with B.
2. Inverse the dependency by having B observe an event on A.  In this case, B is strongly coupled with A.

So, often you have to ask yourself, should A know about B or should B know about A?

As a third option, in some cases it might actually be better for neither one to know about the other. This way your code is kept as loosely coupled as possible.  You can achieve this by having A and B interact with an intermediary object (in this case, zenject signals) instead of directly with each other.

Note also that while the result will be more loosely coupled, this isn't always going to be better.  Signals can be misused just like any programming pattern, so you have to consider each case for whether it's a good candidate for them or not.

## <a id="quick-start"></a>Signals Quick Start

If you just want to get up and running immediately, see the following example which shows basic usage:

```csharp

public class UserJoinedSignal : ISignal
{
    public UserJoinedSignal(string username)
    {
        Username = username;
    }

    public string Username
    {
        get; private set;
    }
}

public class GameInitializer : IInitializable
{
    readonly SignalBus _signalBus;

    public GameInitializer(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.Fire(new UserJoinedSignal("Bob"));
    }
}

public class Greeter
{
    public void SayHello(string userName)
    {
        Debug.Log("Hello " + userName + "!");
    }
}

public class GameInstaller : MonoInstaller<GameInstaller>
{
    public override void InstallBindings()
    {
        SignalRootInstaller.Install(Container);

        Container.DeclareSignal<UserJoinedSignal>();

        Container.Bind<Greeter>().AsSingle();

        Container.BindSignal<UserJoinedSignal>()
            .ToMethod<Greeter>((x, s) => x.SayHello(s.Username)).FromResolve();

        Container.BindInterfacesTo<GameInitializer>().AsSingle();
    }
}
```

To run, just create copy and paste the code above into a new file named GameInstaller then create an empty scene with a new scene context and attach the new installer.

There are several ways of creating signal handlers.  Another approach would be the following

```csharp
public class Greeter : IInitializable, IDisposable
{
    readonly SignalBus _signalBus;

    public Greeter(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<UserJoinedSignal>(OnUserJoined);
    }

    public void Dispose()
    {
        _signalBus.Unsubscribe<UserJoinedSignal>(OnUserJoined);
    }

    void OnUserJoined(UserJoinedSignal args)
    {
        SayHello(args.Username);
    }

    public void SayHello(string userName)
    {
        Debug.Log("Hello " + userName + "!");
    }
}

public class GameInstaller : MonoInstaller<GameInstaller>
{
    public override void InstallBindings()
    {
        SignalRootInstaller.Install(Container);

        Container.DeclareSignal<UserJoinedSignal>();

        // Here, we can get away with just binding the interfaces since they don't refer
        // to each other
        Container.BindInterfacesTo<Greeter>().AsSingle();
        Container.BindInterfacesTo<GameInitializer>().AsSingle();
    }
}
```

Note that the UserJoinedSignal and GameInitializer were not included here because they are the same as in the first example.  As one final alternative approach, you could also combine zenject signals with the UniRx library and do it like this instead:


```csharp
public class Greeter : IInitializable, IDisposable
{
    readonly SignalBus _signalBus;
    readonly CompositeDisposable _disposables = new CompositeDisposable();

    public Greeter(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.GetStream<UserJoinedSignal>()
            .Subscribe(x => SayHello(x.Username)).AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void SayHello(string userName)
    {
        Debug.Log("Hello " + userName + "!");
    }
}
```

As you can see in the the above examples, you can either directly bind a handler method to a signal in an installer using BindSignal (first example) or you can have your signal handler attach and detach itself to the signal (second and third examples)

Details of how this works will be explained in the following sections.

## <a id="declaration"></a>Signals Declaration

Signals are defined like this:

```csharp
public class PlayerDiedSignal : ISignal
{
    // Add parameters here
}
```

Any parameters passed along with the signal should be added as public members or properties.  For example:

```csharp
public class WeaponEquippedSignal : ISignal
{
    public Player Player;
    public IWeapon Weapon;
}
```

However - it is usually best practice to make the signal classes immutable, so our WeaponEquippedSignal might be better written as this instead:

```csharp
public class WeaponEquippedSignal : ISignal
{
    public WeaponEquippedSignal(Player player, IWeapon weapon)
    {
        Player = player;
        Weapon = weapon;
    }

    public IWeapon Weapon
    {
        get; private set;
    }

    public Player Player
    {
        get; private set;
    }
}
```

This isn't necessary but is good practice because it will ensure that any signal handlers do not attempt to change the signal parameter values, which could negatively affect other signal handler behaviour.

After we have created our signal class we just need to declare it in an installer somewhere:

```csharp
public override void InstallBindings()
{
    Container.DeclareSignal<PlayerDiedSignal>();
}
```

Note that the declaration is the same regardless of the parameter list.

The format of the DeclareSignal statement is the following:

<pre>
Container.DeclareSignal&lt;<b>SignalType</b>&gt;()
    .<b>(RequireHandler|OptionalHandler)</b>()
    .<b>(RunAsync|RunSync)</b>();
</pre>

Where:

- **RequireHandler**/**OptionalHandler** - These values control how to behave when the signal is fired and there is no handler associated with it.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default is OptionalHandler, which will allow signals to fire with zero handlers.  When RequireHandler is set, exceptions will be thrown in the case where the signal is fired with zero handlers.  Which one you choose depends on how strict you prefer the system to be.  When it's optional, it can sometimes be 

- **RunAsync**/**RunSync** - These values control whether the signal is fired synchronously or asynchronously.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default value is to run synchronously.  When RunAsync is used, this means that when a signal is fired by calling `SignalBus.Fire`, the handlers will not actually be invoked until the beginning of the next frame.  See <a href="#async-vs-sync">here</a> for a discussion of this feature.

Note that the defaults for both of these values can be overridden by changing <a href="../README.md#zenjectsettings">ZenjectSettings</a>.

## <a id="firing"></a>Signal Firing

Firing the signal is as simple as just adding a reference to it and calling Fire

```csharp
public class Bar : ITickable
{
    readonly DoSomethingSignal _signal;

    public Bar(DoSomethingSignal signal)
    {
        _signal = signal;
    }

    public void DoSomething()
    {
        _signal.Fire();
    }
}
```

## <a id="handlers"></a>Signal Handlers

There are three ways of adding handlers to a signal:

1. C# events
2. UniRx Observable
3. Installer Binding

### <a id="handler-events"></a>C# Event Signal Handler

Probably the easiest method to add a handler is to add it directly from within the handler class.  For example:

```csharp
public class Greeter : IInitializable, IDisposable
{
    AppStartedSignal _appStartedSignal;

    public Greeter(AppStartedSignal appStartedSignal)
    {
        _appStartedSignal = appStartedSignal;
    }

    public void Initialize()
    {
        _appStartedSignal += OnAppStarted;
    }

    public void Dispose()
    {
        _appStartedSignal -= OnAppStarted;
    }

    void OnAppStarted()
    {
        Debug.Log("Hello world!");
    }
}
```

Or, equivalently:

```csharp
public class Greeter : IInitializable, IDisposable
{
    AppStartedSignal _appStartedSignal;

    public Greeter(AppStartedSignal appStartedSignal)
    {
        _appStartedSignal = appStartedSignal;
    }

    public void Initialize()
    {
        _appStartedSignal.Listen(OnAppStarted);
    }

    public void Dispose()
    {
        _appStartedSignal.Unlisten(OnAppStarted);
    }

    void OnAppStarted()
    {
        Debug.Log("Hello world!");
    }
}
```

### <a id="handler-unirx"></a>UniRx Signal Handler

If you are a fan of <a href="https://github.com/neuecc/UniRx">UniRx</a>, as we are, then you might also want to treat the signal as a UniRx observable.  For example:

```csharp
public class Greeter : MonoBehaviour
{
    [Inject]
    AppStartedSignal _appStartedSignal;

    public void Start()
    {
        _appStartedSignal.AsObservable.Subscribe(OnAppStarted).AddTo(this);
    }

    void OnAppStarted()
    {
        Debug.Log("Hello World!");
    }
}
```

NOTE:  Integration with UniRx is disabled by default.  To enable, you must add the define `ZEN_SIGNALS_ADD_UNIRX` to your project, which you can do by selecting Edit -> Project Settings -> Player and then adding `ZEN_SIGNALS_ADD_UNIRX` in the "Scripting Define Symbols" section

### <a id="handler-binding"></a>Installer Binding Signal Handler

Finally, you can also add signal handlers directly within an installer. There are three ways to do this:

1.  Instance Method

    ```csharp
    public class Greeter1
    {
        public void SayHello()
        {
            Debug.Log("Hello!");
        }
    }

    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<AppStartedSignal>()
                .To<Greeter1>(x => x.SayHello).AsSingle();
        }
    }
    ```

    Or, when the signal has parameters:

    ```csharp
    public class Greeter1
    {
        public void SayHello(string name)
        {
            Debug.Log("Hello " + name + "!");
        }
    }

    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<string, AppStartedSignal>()
                .To<Greeter1>(x => x.SayHello).AsSingle();
        }
    }
    ```

2.  Static Method

    ```csharp

    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<AppStartedSignal>()
                .To(() => Debug.Log("Hello!")).AsSingle();
        }
    }
    ```

    Or, when the signal has parameters:

    ```csharp
    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<string, AppStartedSignal>()
                .To(name => Debug.Log("Hello " + name + "!")).AsSingle();
        }
    }
    ```

3.  Static Method With Instance

    This approach is similar to 1 except allows you to implement a static method that contains both the list of parameters, and a handler class that you can either call or make use of somehow in the method.  This approach is particularly useful if you need to apply some kind of transformation to the parameters before forwarding it to the handler class

    ```csharp
    public class Greeter1
    {
        public void SayHello()
        {
            Debug.Log("Hello!");
        }
    }

    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<AppStartedSignal>()
                .To<Greeter1>(x => x.SayHello(x)).AsSingle();
        }
    }
    ```

    Or, when the signal has parameters:

    ```csharp
    public class Greeter1
    {
        public void SayHello(string name)
        {
            Debug.Log("Hello " + name + "!");
        }
    }

    public class GreeterInstaller : MonoInstaller<GreeterInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindSignal<string, AppStartedSignal>()
                .To<Greeter1>((x, name) => x.SayHello(name)).AsSingle();
        }
    }
    ```

Installer bindings for signals have the following advantages:
- More flexible, because you can wire it up in the installer and can have multiple installer configurations
- More loosely coupled, because the handler class can remain completely ignorant of the signal
- Less error prone, because you don't have to remember to unsubscribe.  The signal will automatically be unsubscribed when the 'context' is disposed of.  This means that if you add a handler within a sub-container, the handler will automatically unsubscribe when the sub-container is disposed of
- You can more easily control which classes are allowed to fire the signal.  You can do this by adding a When() conditional to the declaration.  (You can't do this with the other handler types because the listener also needs access to the signal to add itself to it)

However, it might also be harder to follow just by reading the code, because you will have to check the installers to see what handlers a given has.

Which approach to signal handlers depends on the specifics of each case and personal preference.

### Signals With Subcontainers

One interesting feature of signals is that the signal handlers do not need to be in the same container as the signal declaration.  The declaration can either be in the same container, a parent container, or a sub-container, and it should trigger the handlers regardless of where they are declared.  Note that the declaration will however determine which container the signal can be fired from (the signal itself will be accessible as a dependency for the container it is declared in and all sub-containers just like other bindings)

For example, you can declare a signal in your ProjectContext and then add signal handlers for each particular scene.  Then, when each scene exits, the signal handler that was added in that scene will no longer be called when the signal is fired.

Or, you could add signal handlers in the ProjectContext and then declare the signal in some particular scene.

For example, You might use this to implement your GUI entirely in its own scene, loaded alongside the main backend scene.  Then you could have the GUI scene strictly fire Signals, which would then have method bindings in the game scene.

### Signals With Identifiers

If you want to define multiple instances of the same signal, you would need to use identifiers.  This works identically to how normal zenject binding identifiers work. For example:

```csharp
Container.DeclareSignal<FooSignal>().WithId("foo");
```

Then for installer handlers:

```csharp
Container.BindSignal<FooSignal>().WithId("foo").To<Bar>(x => x.DoSomething).AsSingle();
```

Then to access it to fire it, or to add a C# event / unirx handlers:

```csharp
public class Qux
{
    FooSignal _signal;

    public Qux(
        [Inject(Id = "foo")] FooSignal signal)
    {
        _signal = signal;
    }

    public void Run()
    {
        _signal.Fire();
    }
}
```

## <a id="async-vs-sync"></a>Asynchronous Versus Synchronous Events

