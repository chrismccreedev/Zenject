<img src="/Documentation/ZenjectLogo.png?raw=true" alt="Zenject" width="600px" height="134px"/>
Dependency Injection Framework for Unity3D

# Introduction

Zenject is a lightweight dependency injection framework built specifically to target Unity 3D.  It can be used to turn your Unity 3D application into a collection of loosely-coupled parts with highly segmented responsibilities.  Zenject can then glue the parts together in many different configurations to allow you to easily write, re-use, refactor and test your code in a scalable and extremely flexible way.

This project is open source.  You can find the official repository [here](https://github.com/modesttree/Zenject).  If you would like to contribute to the project pull requests are welcome!

# Features

* Injection into normal C# classes or MonoBehaviours
* Constructor injection (can tag constructor if there are multiple)
* Field injection
* Property injection
* Named injections (string, enum, etc.)
* Auto-Mocking using the Moq library
* Injection across different Unity scenes
* Ability to print entire object graph as a UML image automatically

# History

Unity is a fantastic game engine, however the approach that new developers are encouraged to take does not lend itself well to writing large, flexible, or scalable code bases.  In particular, the default way that Unity manages dependencies between different game components can often be awkward and error prone.

Having worked on non-unity projects that use dependency management frameworks (such as Ninject, which Zenject takes a lot of inspiration from), the problem irked me enough that I decided a custom framework was in order.  Upon googling for solutions, I found a series of great articles by Sebastiano Mandalà outlining the problem, which I strongly recommend that everyone read before firing up Zenject:

* http://blog.sebaslab.com/ioc-container-for-unity3d-part-1/
* http://blog.sebaslab.com/ioc-container-for-unity3d-part-2/

Sebastiano even wrote a proof of concept and open sourced it, which became the basis for this library.

I will not go into detail here about why dependency injection is a great pattern (other people have done a good enough job of that).   I will just say that if you don't have experience with DI frameworks, and are writing object oriented code, then trust me, you will thank me later!  Once you learn how to write properly loosely coupled code using DI, there is simply no going back.

I also highly recommend anything written by Mark Seeman on the subject, in particular his book 'Dependency Injection in .NET'.

# Theory

When writing an individual class to achieve some functionality, it will likely need to interact with other classes in the system to achieve its goals.  One way to do this is to have the class itself create its dependencies, by calling concrete constructors:

    public class Foo
    {
        ISomeService _service;

        public Foo()
        {
            _service = new SomeService();
        }

        public void DoSomething()
        {
            _service.PerformTask();
            ...
        }
    }

This works fine for small projects, but as your project grows it starts to get unwieldy.  The class Foo is tightly coupled to class 'SomeService'.  If we decide later that we want to use a different concrete implementation then we have to go back into the Foo class to change it.

After thinking about this, often you come to the realization that ultimately, Foo shouldn't bother itself with the details of choosing the specific implementation of the service.  All Foo should care about is fulfilling its own specific responsibilities.  As long as the service fulfills the abstract interface required by Foo, Foo is happy.  Our class then becomes:

    public class Foo
    {
        ISomeService _service;

        public Foo(ISomeService service)
        {
            _service = service;
        }

        public void DoSomething()
        {
            _service.PerformTask();
            ...
        }
    }

This is better, but now whatever class is creating Foo (let's call it Bar) has the problem of filling in Foo's extra dependencies:

    public class Bar
    {
        public void DoSomething()
        {
            var foo = new Foo(new SomeService());
            foo.DoSomething();
            ...
        }
    }

And class Bar probably also doesn't really care about what specific implementation of SomeService Foo uses.  Therefore we push the dependency up again:

    public class Bar
    {
        ISomeService _service;

        public Bar(ISomeService service)
        {
            _service = service;
        }

        public void DoSomething()
        {
            var foo = new Foo(_service);
            foo.DoSomething();
            ...
        }
    }

So we find that it is useful to push the responsibility of deciding which specific implementations of which classes to use further and further up in the 'object graph' of the application.  Taking this to an extreme, we arrive at the entry point of the application, at which point all dependencies must be satisfied before things start.  The dependency injection lingo for this part of the application is called the 'composition root'.

# Misconceptions

There are many misconceptions about DI, due to the fact that it can be tricky to fully wrap your head around at first.  It will take time and experience before it fully 'clicks'.

As shown in the above example, DI frameworks can be used to easily swap different implementations of a given interface (in the example this was ISomeService).  However, this is only one of many benefits that DI offers.  In most cases the various responsibilities of an application only have individual classes implementing them, so you will be injecting concrete references in those cases rather than interfaces (especially if you're like me and follow the Reused Abstraction Principle).

More important than that is the fact that using a dependency injection framework like Zenject allows you to more easily follow the 'Single Responsibility Principle'.  By letting Zenject worry about wiring up the classes, the classes themselves can just focus on fulfilling their specific responsibilities.

* Testability - Writing automated unit tests or user-driven tests becomes very easy, because it is just a matter of writing a different 'composition root' which wires up the dependencies in a different way.  Want to only test one subsystem?  Simply create a new composition root which creates 'mocks' for all other systems in the application.
* Refactorability - When code is loosely coupled, as is the case when using DI properly, the entire code base is much more resilient to changes.  You can completely change parts of the code base without having those changes wreak havoc on other parts.
* Encourages modular code - When using a DI framework you will naturally follow better design practices, because it forces you to think about the interfaces between classes.

# How should I get started?

Once you have an understanding of the theory behind Zenject (if not see the previous sections), then I recommend diving in to the included sample application to get started, by extracting the included unity package "ZenjectSampleGame.unitypackage" into your unity project.  The sample is well documented and conveys proper usage of Zenject better than I could explain here.

# Overview Of The Zenject API

What follows is a general overview of how DI patterns are applied using Zenject.  However, the best documentation right now is probably the included sample project itself.  I would recommend using that for reference when reading over these concepts.

## Composition Root / Installers

If you look at the sample application (a kind of asteroids clone) you will see that at the top of the scene heirarchy we have a game object with the name CompositionRoot.   This is where Zenject resolves all dependencies before kicking off your application.

To add dependency bindings to your application, you need to write what is referred to in Zenject as an 'Installer' which usually looks something like this:

    [Serializable]
    public class GameInstaller : Installer
    {
        public string SomeSetting;

        public override void RegisterBindings()
        {
            ...
            _container.Bind<IDependencyRoot>().ToSingle<GameRoot>();
            ...
        }
    }

    public class GameInstallerWrapper : InstallerMonoBehaviourWrapper<GameInstaller>
    {
    }

The RegisterBindings() method is called once at the entry point of the application by the composition root.  Note here that the Installer class is not a MonoBehaviour and therefore cannot be dragged onto unity game objects.  This is to allow installers to easily trigger other installers and also to allow installers to be used in non-unity contexts (eg: NUnit tests).  However, it is also very useful to be able to simply drag and drop different sets of installers into a given unity scene, which is why in many cases you will want to provide the extra wrapper class.

Once RegisterBindings() is called the installer can begin mapping out the object graph to be used in the application.  The syntax here will be familiar to users of many other DI frameworks.

Like many other DI frameworks, dependency mapping done by adding the binding to something called the container.  The container should then 'know' how to create all the object instances in our application, by recursively resolving all dependencies for a given object.  You can do this by calling the Resolve method:

    Foo foo = _container.Resolve<Foo>()

However, any use of the container should be restricted to the composition root or factory classes (see rules/guidelines section below)

# The dependency root

Every Zenject app has one root object.  The dependencies of this object generates the full object graph for the application/game.  For example, in the sample project this is the GameRoot class which is declared as below:

    _container.Bind<IDependencyRoot>().ToSingle<GameRoot>();

A Zenject driven application is executed by the following steps:

* Composition Root is started (via Awake() method)
* Composition Root calls RegisterBindings() on all installers that are attached below it in the scene heirarchy
* Each Installer registers different sets of dependencies directly on to the DiContainer by calling Bind<> and BindValue<> methods.  Note that the order that this binding occurs should not matter.
* The CR then traverses the scene heirarchy again and injects all MonoBehaviours with their dependencies.  Since MonoBehaviours are instantiated by Unity we cannot use constructor injection in this case and therefore field or property injection must be used (which is done by adding a [Inject] attribute to any member)
* After filling in the scene dependencies the CR then calls `_container.Resolve` on the root object (that is, whatever is bound to IDependencyRoot).  In most cases code does not need to be in MonoBehaviours and will be resolved this way
* If a dependency cannot be resolved, a ZenjectResolveException is thrown

# Tickables / IInitializables

I prefer to avoid MonoBehaviours when possible in favour of just normal C# classes.  Zenject allows you to do this much more easily by providing interfaces that mirror functionality that you would normally need to use a MonoBehaviour for.

For example, if you have code that needs to run per frame, then you can implement the ITickable interface:

    public class Ship : ITickable
    {
        public void Tick()
        {
            // Perform per frame tasks
        }
    }

Then it's just a matter of including the following in one of your installers (as long as you also include a few dependencies as outlined in the hello world example below)

    _container.Bind<ITickable>().ToSingle<Ship>();

The same goes for IInitializable, for cases where you have code that you want to run on startup.  (side note: using IInitializable is generally better than putting too much work in constructors).  IInitializable can also be used for objects that are created via factories (in which case Initialize() is called automatically, as long as you use one of the built in Zenject factory classes).

# Zenject Hello World

    public class TestInstallerWrapper : InstallerMonoBehaviourWrapper<TestInstaller>
    {
    }

    [Serializable]
    public class TestInstaller : Installer
    {
        public string Name;

        public override void RegisterBindings()
        {
            Install<StandardUnityInstaller>();

            _container.Bind<IDependencyRoot>().ToSingle<DependencyRootStandard>();

            _container.Bind<ITickable>().ToSingle<TestRunner>();
            _container.Bind<IInitializable>().ToSingle<TestRunner>();
            _container.Bind<string>().ToSingle(Name).WhenInjectedInto<TestRunner>();
        }
    }

    public class TestRunner : ITickable, IInitializable
    {
        string _name;

        public TestRunner(string name)
        {
            _name = name;
        }

        public void Initialize()
        {
            Debug.Log("Hello " + _name + "!");
        }

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Exiting!");
                Application.Quit();
            }
        }
    }

You can run this example by copying and pasting the above code into a file named 'TestInstallerWrapper'.  Then create a new scene, add a GameObject. Attach CompositionRoot to the GameObject.  Attach TestInstallerWrapper.  Run.  Observe unity console.

Some notes:

* The `Install<StandardUnityInstaller>()` line is necessary to tell zenject to initialize some basic unity helper classes (including the Zenject class which updates all ITickables and the class which calls Initialize on all IInitializables).  It is done this way because in some cases you might not want to use the whole ITickable/IInitializable approach at all.  Or maybe you aren't even using Unity. Etc.
* You will also need to define a dependency root otherwise Zenject will not create your object graph
* Note that all Installers use the [Serializable] attribute.  This is so that Installers can expose settings to their MonoBehaviour wrapper.  In this case, we expose a "Name" variable.
* Note the usage of WhenInjectedInto.  This is good because otherwise any class which had a string parameter in its constructor would get our Name parameter.

# Update / Initialization Order

In many cases, especially for small projects, the order that classes update or initialize in does not matter.  This is why Unity does not have an easy way to control this (besides in Edit -> Project Settings -> Script Execution Order but that is pretty awkward to use).  In Unity, after adding a bunch of MonoBehaviours to your scene, it can be difficult to predict in what order the Start(), Awake(), or Update() methods will be called in.

By default, ITickables and IInitializables are updated in the order that they are added, however for cases where the update or initialization order matters, there is a much better way.  By specifying their priorities explicitly in the installer.  For example, in the sample project you can find this code:

        public override void RegisterBindings()
        {
            ...
            new TickablePrioritiesInstaller(_container, Tickables).RegisterBindings();
            new InitializablePrioritiesInstaller(_container, Initializables).RegisterBindings();
        }

        static List<Type> Tickables = new List<Type>()
        {
            // Re-arrange this list to control update order
            typeof(AsteroidManager),
            typeof(GameController),
        };

        static List<Type> Initializables = new List<Type>()
        {
            // Re-arrange this list to control init order
            typeof(GameController),
        };

This way, you won't hit a wall at the end of the project due to some unforseen order-dependency.

Any ITickables or IInitializables that aren't given an explicit order are updated after everything else.

# Injecting data across scenes

In some cases it's useful to pass arguments from one scene to another.  The way Unity allows us to do this by default is fairly awkward.  Your options are to create a persistent GameObject and call DontDestroyOnLoad() to keep it alive when changing scenes, or use global static classes to temporarily store the data.

Let's pretend you want to specify a 'level' string to the next scene.  You have the following class that requires the input:

    public class LevelHandler : IInitializable
    {
        readonly string _startLevel;

        public LevelHandler(
            [InjectOptional]
            [Inject("StartLevelName")]
            string startLevel)
        {
            if (startLevel == null)
            {
                _startLevel = "level01";
            }
            else
            {
                _startLevel = startLevel;
            }
        }

        public void Initialize()
        {
            ...
            [Load level]
            ...
        }
    }

You can load the scene containing `LessonStandaloneStart` and specify a particular level by using the following syntax:

    ZenUtil.LoadLevel("NameOfSceneToLoad",
        delegate (DiContainer container)
        {
            container.Bind<string>().ToSingle("level02").WhenInjectedInto<LevelHandler>("StartLevelName");
        });

Note that you can still run the scene directly, in which case it will default to using "level01".  This is possible because we are using the InjectOptional flag.

# Rules / Guidelines / Recommendations

* The container should *only* be referenced in the composition root layer.  Note that factories are part of this layer and the container can be referenced there (which is necessary to create objects at runtime).  For example, see ShipStateFactory in the sample project.
* Prefer constructor injection to field or property injection.
    * Constructor injection forces the dependency to only be resolved once, at class creation, which is usually what you want.  In many cases you don't want to expose a public property with your internal dependencies
    * Constructor injection guarantees no circular dependencies between classes, which is generally a bad thing to do
    * Constructor injection is more portable for cases where you decide to re-use the code without a DI framework such as Zenject.  You can do the same with public properties but it's more error prone.  It's possible to forget to initialize one field and leave the object in an invalid state
    * Finally, Constructor injection makes it clear what all the dependencies of a class are when another programmer is reading the code.  They can simply look at the parameter list of the constructor.

# How is this different from Strange IoC?

Zenject is a pure dependency injection framework and does not offer the suite of features that Strange IoC does.  It is kept extremely lightweight to focus on its single purpose: Simple, reliable, and flexible dependency management.

****
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
