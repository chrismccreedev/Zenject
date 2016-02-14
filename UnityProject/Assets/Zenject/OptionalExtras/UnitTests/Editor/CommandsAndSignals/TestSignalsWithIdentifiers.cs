using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ModestTree;
using Assert=ModestTree.Assert;
using Zenject.Commands;

namespace Zenject.Tests
{
    [TestFixture]
    public class TestSignalsWithIdentifiers : TestWithContainer
    {
        [Test]
        public void RunTest()
        {
            Binder.BindSignal<SomethingHappenedSignal>();
            Binder.BindTrigger<SomethingHappenedSignal.Trigger>();

            Binder.Bind<Foo>().ToSingle();
            Binder.Bind<Bar>().ToSingle();

            Binder.BindSignal<SomethingHappenedSignal>("special");
            Binder.BindTrigger<SomethingHappenedSignal.Trigger>("special");

            Binder.Bind<FooSpecial>().ToSingle();
            Binder.Bind<BarSpecial>().ToSingle();

            var foo = Resolver.Resolve<Foo>();
            var bar = Resolver.Resolve<Bar>();

            var fooSpecial = Resolver.Resolve<FooSpecial>();
            var barSpecial = Resolver.Resolve<BarSpecial>();

            bar.Initialize();
            barSpecial.Initialize();

            Assert.IsNull(bar.ReceivedValue);
            Assert.IsNull(barSpecial.ReceivedValue);

            foo.DoSomething("asdf");

            Assert.IsEqual(bar.ReceivedValue, "asdf");
            Assert.IsNull(barSpecial.ReceivedValue);

            bar.ReceivedValue = null;

            fooSpecial.DoSomething("zxcv");

            Assert.IsEqual(barSpecial.ReceivedValue, "zxcv");
            Assert.IsNull(bar.ReceivedValue);

            bar.Dispose();
            barSpecial.Dispose();
        }

        public class SomethingHappenedSignal : Signal<string>
        {
            public class Trigger : TriggerBase
            {
            }
        }

        public class Foo
        {
            readonly SomethingHappenedSignal.Trigger _trigger;

            public Foo(
                SomethingHappenedSignal.Trigger trigger)
            {
                _trigger = trigger;
            }

            public void DoSomething(string value)
            {
                _trigger.Fire(value);
            }
        }

        public class Bar
        {
            readonly SomethingHappenedSignal _signal;

            public Bar(SomethingHappenedSignal signal)
            {
                _signal = signal;
            }

            public string ReceivedValue
            {
                get;
                set;
            }

            public void Initialize()
            {
                _signal.Event += OnStarted;
            }

            public void Dispose()
            {
                _signal.Event -= OnStarted;
            }

            void OnStarted(string value)
            {
                ReceivedValue = value;
            }
        }

        public class FooSpecial
        {
            readonly SomethingHappenedSignal.Trigger _trigger;

            public FooSpecial(
                [Inject("special")]
                SomethingHappenedSignal.Trigger trigger)
            {
                _trigger = trigger;
            }

            public void DoSomething(string value)
            {
                _trigger.Fire(value);
            }
        }

        public class BarSpecial
        {
            readonly SomethingHappenedSignal _signal;

            public BarSpecial(
                [Inject("special")]
                SomethingHappenedSignal signal)
            {
                _signal = signal;
            }

            public string ReceivedValue
            {
                get;
                set;
            }

            public void Initialize()
            {
                _signal.Event += OnStarted;
            }

            public void Dispose()
            {
                _signal.Event -= OnStarted;
            }

            void OnStarted(string value)
            {
                ReceivedValue = value;
            }
        }
    }
}

