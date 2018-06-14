
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

public class UserJoinedSignal
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

Note that if you go this route that you need to enable UniRx integration as described <a href="../README.md#unirx-integration">here</a>.

As you can see in the the above examples, you can either directly bind a handler method to a signal in an installer using BindSignal (first example) or you can have your signal handler attach and detach itself to the signal (second and third examples)

Details of how this works will be explained in the following sections.

## <a id="declaration"></a>Signals Declaration

Signals are defined like this:

```csharp
public class PlayerDiedSignal
{
    // Add parameters here
}
```

Any parameters passed along with the signal should be added as public members or properties.  For example:

```csharp
public class WeaponEquippedSignal
{
    public Player Player;
    public IWeapon Weapon;
}
```

However - it is usually best practice to make the signal classes immutable, so our WeaponEquippedSignal might be better written as this instead:

```csharp
public class WeaponEquippedSignal
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
    .<b>(RequiredSubscriber|OptionalSubscriber|OptionalSubscriberWithWarning)</b>()
    .<b>(RunAsync|RunSync)</b>()
    .(**Copy**|**Move**)Into(**All**|**Direct**)SubContainers();
</pre>

Where:

- **SignalType** - The custom class that represents the signal

- **RequiredSubscriber**/**OptionalSubscriber**/**OptionalSubscriberWithWarning** - These values control how the signal should behave when it fired but there are no subscribers associated with it.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default is OptionalSubscriber, which will allow signals to fire with zero subscribers.  When RequiredSubscriber is set, exceptions will be thrown in the case where the signal is fired with zero subscribers.  Which one you choose depends on how strict you prefer your application to be.

- **RunAsync**/**RunSync** - These values control whether the signal is fired synchronously or asynchronously.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default value is to run synchronously, which means that when the signal is fired by calling `SignalBus.Fire`, that all the subscribers are immediately notified.  When `RunAsync` is used instead, this means that when a signal is fired, the subscribers will not actually be notified until the end of the current frame.  Which one you choose comes down to a matter of preference.  Asynchronous events and synchronous events both have their advantages and disadvantages.  See <a href="#async-vs-sync">here</a> for a discussion.

* (**Copy**|**Move**)Into(**All**|**Direct**)SubContainers = Same as behaviour as described in <a href="../README.md#binding">main section on binding</a>.

Note that the defaults for both of these values can be overridden by changing <a href="../README.md#zenjectsettings">ZenjectSettings</a>.

## <a id="firing"></a>Signal Firing

To fire the signal, you add a reference to the `SignalBus` class, and then call the `Fire` method like this:

```csharp
public class UserJoinedSignal
{
}

public class UserManager
{
    readonly SignalBus _signalBus;

    public UserManager(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void DoSomething()
    {
        _signalBus.Fire<UserJoinedSignal>();
    }
}
```

Or, if the signal has parameters then you will want to create a new instance of it, like this:

```csharp
public class UserJoinedSignal
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

public class UserManager
{
    readonly SignalBus _signalBus;

    public UserManager(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void DoSomething()
    {
        _signalBus.Fire(new UserJoinedSignal("Bob"));
    }
}
```

### Binding Signals with BindSignal

As mentioned above, in addition to being able to directly subscribe to signals on the event bus (via `SignalBus.Subscribe` or `SignalBus.GetStream`) you can also directly bind a signal to a handling class inside an installer.  This approach has advantages and disadvantages compared to directly subscribing in a handling class.

The format of the BindSignal command is:

<pre>
Container.BindSignal&lt;<b>SignalType</b>&gt;()
    .ToMethod(<b>Handler</b>)
    .From(<b>ConstructionMethod</b>)
    .(**Copy**|**Move**)Into(**All**|**Direct**)SubContainers();
</pre>

Where:

- **SignalType** - The custom class that represents the signal

- **Handler** - The method that should be triggered when the signal fires.  This has several variations:

1. Static method

```csharp
Container.BindSignal<UserJoinedSignal>().ToMethod(s => Debug.Log("Hello user " + s.Username));
```

Note that the method can also be parameterless:

```csharp
Container.BindSignal<UserJoinedSignal>().ToMethod(() => Debug.Log("Received UserJoinedSignal signal"))
```

Note also that in this case, there is no option to provide a value for `From` since there is no instance needed

1. Instance method directly

```csharp
public class Greeter
{
    public void SayHello(UserJoinedSignal signal)
    {
        Debug.Log("Hello " + signal.Username + "!");
    }
}

Container.Bind<Greeter>().AsSingle();
Container.BindSignal<UserJoinedSignal>().ToMethod<Greeter>(x => x.SayHello).FromResolve();
```

In this case we want to fire the `Greeter.SayHello` method.  In this case we also need to supply a value for `From` so that there is an instance that can be called with the given method.

Similar to static methods you could also bind to a method without parameters:

```csharp
public class Greeter
{
    public void SayHello()
    {
        Debug.Log("Hello there!");
    }
}

Container.Bind<Greeter>().AsSingle();
Container.BindSignal<UserJoinedSignal>().ToMethod<Greeter>(x => x.SayHello).FromResolve();
```

1. Instance method with mapping

There might also be cases where the arguments to the handling method directly contain the signal arguments.  For example:

```csharp
public class Greeter
{
    public void SayHello(string username)
    {
        Debug.Log("Hello " + username + "!");
    }
}
```

In this case you could bind the signal to a method that does a mapping of the parameters for us:

```csharp
Container.Bind<Greeter>().AsSingle();
Container.BindSignal<UserJoinedSignal>().ToMethod<Greeter>((x, s) => x.SayHello(s.Username)).FromResolve()
```

- **ConstructionMethod** - After binding to an instance method above, you also need to define where this instance comes from.

* (**Copy**|**Move**)Into(**All**|**Direct**)SubContainers = Same as behaviour as described in <a href="../README.md#binding">main section on binding</a>.

### Signals With Subcontainers

Signals are only visible at the container level where they are declared and below.  For example, you might use Unity's multi-scene support and split up your game into a GUI scene and an Environment scene.  In the GUI scene you might fire a signal indicating that the GUI popup overlay has been opened/closed, so that the Environment scene can pause/resume activity.  One way of achieving this would be to declare a signal in a ProjectContext installer, then subscribe to it in the Environment scene, and then fire it from the GUI scene.  Or, alternatively, you could use a scene that is the parent of both the Environment scene and the GUI scene and put the signal declaration there.

## <a id="signal-naming-convention"></a>Signal Naming Convention

TBD

## <a id="async-vs-sync"></a>Asynchronous Versus Synchronous Events

TBD

