
## Signals

NOTE: If you are upgrading from zenject 5.x and want to continue using that version of signals, you can find a zenject-6 compatible version of that [here](https://github.com/svermeulen/ZenjectSignalsOld).  So to use that, just import zenject and make sure to uncheck the `OptionalExtras/Signals` folder, and then add the `ZenjectSignalsOld` folder to your project.

## Table Of Contents

* Introduction
    * <a href="#theory">Theory</a>
    * <a href="#quick-start">Signals Quick Start</a>
    * <a href="#declaration">Signals Declaration</a>
    * <a href="#firing">Signal Firing</a>
    * <a href="#when-to-use-signals">When To Use Signals</a>
* Advanced
    * <a href="#use-with-subcontainers">Signals With Subcontainers</a>
    * <a href="#async-signals">Asynchronous Signals</a>

## <a id="theory"></a>Motivation / Theory

Given two classes A and B that need to communicate, your options are usually:

1. Directly call a method on B from A.  In this case, A is strongly coupled with B.
2. Inverse the dependency by having B observe an event on A.  In this case, B is strongly coupled with A

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
    public void SayHello(UserJoinedSignal userJoinedInfo)
    {
        Debug.Log("Hello " + userJoinedInfo.Username + "!");
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
            .ToMethod<Greeter>(x => x.SayHello).FromResolve();

        Container.BindInterfacesTo<GameInitializer>().AsSingle();
    }
}
```

To run, just create copy and paste the code above into a new file named `GameInstaller` then create an empty scene with a new scene context and attach the new installer.

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

As one final alternative approach, you could also combine zenject signals with the UniRx library and do it like this instead:


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

Details of how this works are explained in the following sections.

## <a id="declaration"></a>Signals Declaration

Before declaring a signal you need to create a class that will represent it.  For example:

```csharp
public class PlayerDiedSignal
{
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

The format of the DeclareSignal statement is the following:

<pre>
Container.DeclareSignal&lt;<b>SignalType</b>&gt;()
    .<b>(RequiredSubscriber|OptionalSubscriber|OptionalSubscriberWithWarning)</b>()
    .<b>(RunAsync|RunSync)</b>()
    .(<b>Copy</b>|<b>Move</b>)Into(<b>All</b>|<b>Direct</b>)SubContainers();
</pre>

Where:

- **SignalType** - The custom class that represents the signal

- **RequiredSubscriber**/**OptionalSubscriber**/**OptionalSubscriberWithWarning** - These values control how the signal should behave when it fired and yet there are no subscribers associated with it.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default is OptionalSubscriber, which will do nothing in this case.  When RequiredSubscriber is set, exceptions will be thrown in the case of zero subscribers.  OptionalSubscriberWithWarning is half way in between where it will issue a console log warning instead of an exception.  Which one you choose depends on how strict you prefer your application to be, and whether it matters if the given signal is actually handled or not.

- **RunAsync**/**RunSync** - These values control whether the signal is fired synchronously or asynchronously.  Unless it is over-ridden in <a href="../README.md#zenjectsettings">ZenjectSettings</a>, the default value is to run synchronously, which means that when the signal is fired by calling `SignalBus.Fire`, that all the subscribers are immediately notified.  When `RunAsync` is used instead, this means that when a signal is fired, the subscribers will not actually be notified until the end of the current frame.  Which one you choose comes down to a matter of preference.  See here for a discussion of asynchronous signals.  See  for a discussion.

* (**Copy**|**Move**)Into(**All**|**Direct**)SubContainers = Same as behaviour as described in <a href="../README.md#binding">main section on binding</a>.

Note that the default value for RunSync/RunAsync and RequiredSubscriber/OptionalSubscriber can be overridden by changing <a href="../README.md#zenjectsettings">ZenjectSettings</a>

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

As mentioned above, in addition to being able to directly subscribe to signals on the signal bus (via `SignalBus.Subscribe` or `SignalBus.GetStream`) you can also directly bind a signal to a handling class inside an installer.  This approach has advantages and disadvantages compared to directly subscribing in a handling class so again comes down to personal preference.

The format of the BindSignal command is:

<pre>
Container.BindSignal&lt;<b>SignalType</b>&gt;()
    .ToMethod(<b>Handler</b>)
    .From(<b>ConstructionMethod</b>)
    .(<b>Copy</b>|<b>Move</b>)Into(<b>All</b>|<b>Direct</b>)SubContainers();
</pre>

Where:

- **SignalType** - The custom class that represents the signal

- **ConstructionMethod** - When binding to an instance method above, you also need to define where this instance comes from.  See the section on Handler below for more detail

* (**Copy**|**Move**)Into(**All**|**Direct**)SubContainers = Same as behaviour as described in <a href="../README.md#binding">main section on binding</a>.

- **Handler** - The method that should be triggered when the signal fires.  This has several variations:

**1. Static method**

```csharp
Container.BindSignal<UserJoinedSignal>().ToMethod(s => Debug.Log("Hello user " + s.Username));
```

Note that the method can also be parameterless:

```csharp
Container.BindSignal<UserJoinedSignal>().ToMethod(() => Debug.Log("Received UserJoinedSignal signal"))
```

Note also that in this case, there is no option to provide a value for `From` since there is no instance needed

**2. Instance method directly**

For example:

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

In this case we want the signal to trigger the `Greeter.SayHello` method.  Note that we need to supply a value for `From` in thise case so that there is an instance that can be called with the given method.

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

We are using `FromResolve` however we could use any kind of construction method we want as well.  Under the hood, `FromResolve` actually expands to the following:

```csharp
Container.BindSignal<UserJoinedSignal>().ToMethod<Greeter>(x => x.SayHello).From(x => x.FromResolve().AsCached());
```

So, if we didn't need the Greeter class to be injected anywhere else, we could have also implemented it as follows:

```csharp
public class Greeter
{
    public void SayHello(UserJoinedSignal signal)
    {
        Debug.Log("Hello " + signal.Username + "!");
    }
}

Container.BindSignal<UserJoinedSignal>().ToMethod<Greeter>(x => x.SayHello).From(x => x.AsCached());
```

This way, we don't need a separate binding for Greeter at all.   You can provide many other kinds of arguments to `From` as well, including binding to a lazily instantiated MonoBehaviour, a factory method, a custom factory, a facade in a subcontainer, etc.

**3. Instance method with mapping**

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

## <a id="when-to-use-signals"></a>When To Use Signals

Signals are most appropriate as a communication mechanism when:

1. There might be multiple interested receivers listening to the signal
2. The sender doesn't need to get a result back from the receiver
3. The sender doesn't even really care if it gets received.  In other words, the sender should not rely on some state changing when the signal is called for subsequent sender logic to work correctly
4. The sender triggers the signal infrequently or at unpredictable times

These are just rules of thumb, but useful to keep in mind when using signals.  The less logically coupled the sender is to the response behaviour of the receivers, the more appropriate it is compared to other forms of communication such as direct method calls, interfaces, C# event class members, etc.  This is also one reason you might consider using <a href="#async-vs-sync-signals">asynchronous signals</a>

## <a id="use-with-subcontainers"></a>Signals With Subcontainers

Signals are only visible at the container level where they are declared and below.  For example, you might use Unity's multi-scene support and split up your game into a GUI scene and an Environment scene.  In the GUI scene you might fire a signal indicating that the GUI popup overlay has been opened/closed, so that the Environment scene can pause/resume activity.  One way of achieving this would be to declare a signal in a ProjectContext installer, then subscribe to it in the Environment scene, and then fire it from the GUI scene.  Or, alternatively, you could use a scene that is the parent of both the Environment scene and the GUI scene and put the signal declaration there.

## <a id="async-signals"></a>Asynchronous Signals

Synchronous events have the following drawbacks:

1. The order that the signal handler is triggered in is not always predictable when compared to normal update logic inside ITickables or MonoBehaviour.Update

For example, let's say you have a clas

For example, you might have a class Foo that updates its state in Foo.Tick.  Then Foo might also subscribe to a signal that affects this same state.  This signal could be fired at any point during the frame, both before and after the Foo.Tick method gets called.

You can change to make your signal

To take one example, an object A might trigger a signal which would perform some logic that would eventually cause A to be deleted.  If the signal was executed synchronously, 

This is not to say that asynchronous events are superious to synchronous events.  Asynchronous events have their own risks as well.

If you use async events for an object delete signal, you might have events on the queue that assume the object still exists, so you 

Fire and forget

