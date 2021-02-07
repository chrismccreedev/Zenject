using System.Collections;
using ModestTree;
using UnityEngine.TestTools;
using Zenject;
using Zenject.Tests;
using Zenject.Tests.Installers.CompositeScriptableObjectInstallers;

namespace Zenject.Tests.Installers
{
    public class TestCompositeScriptableObjectInstaller : ZenjectIntegrationTestFixture
    {
        [UnityTest]
        public IEnumerator TestZeroParameters()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectFooInstaller", Container);
            PostInstall();

            FixtureUtil.AssertResolveCount<Foo>(Container, 1);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestZeroParametersDeep()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectDeepFooInstaller1", Container);
            PostInstall();

            FixtureUtil.AssertResolveCount<Foo>(Container, 1);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestOneParameter()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/BarInstaller/TestCompositeScriptableObjectBarInstaller", Container);
            PostInstall();

            Assert.IsEqual(Container.Resolve<string>(), "composite scriptable object installer blurg");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestOneParameterDeep()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/BarInstaller/TestCompositeScriptableObjectDeepBarInstaller1", Container);
            PostInstall();

            Assert.IsEqual(Container.Resolve<string>(), "composite scriptable object installer blurg");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestThreeParameters()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/QuxInstaller/TestCompositeScriptableObjectQuxInstaller", Container);
            PostInstall();

            Assert.IsEqual(Container.Resolve<string>(), "composite scriptable object installer string");
            Assert.IsEqual(Container.Resolve<float>(), 1.234f);
            Assert.IsEqual(Container.Resolve<int>(), 5678);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestThreeParametersDeep()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/QuxInstaller/TestCompositeScriptableObjectDeepQuxInstaller1", Container);
            PostInstall();

            Assert.IsEqual(Container.Resolve<string>(), "composite scriptable object installer string");
            Assert.IsEqual(Container.Resolve<float>(), 1.234f);
            Assert.IsEqual(Container.Resolve<int>(), 5678);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestMultipleInstallers()
        {
            PreInstall();
            FooInjecteeInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInjecteeInstaller/FooInjecteeInstaller", Container);
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectFooInstaller", Container);
            PostInstall();

            FixtureUtil.AssertResolveCount<Foo>(Container, 1);
            FixtureUtil.AssertResolveCount<FooInjectee>(Container, 1);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestMultipleInstallersDeep()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInjecteeInstaller/TestCompositeSOFooInjecteeInstaller", Container);
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectFooInstaller", Container);
            PostInstall();

            FixtureUtil.AssertResolveCount<Foo>(Container, 1);
            FixtureUtil.AssertResolveCount<FooInjectee>(Container, 1);
            yield break;
        }

        [UnityTest]
        public IEnumerator TestDuplicateInstallers()
        {
            PreInstall();
            CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectDeepFooInstaller1", Container);
            Assert.Throws<ZenjectException>(() =>
            {
                CompositeScriptableObjectInstaller.InstallFromResource("TestCompositeScriptableObjectInstaller/FooInstaller/TestCompositeScriptableObjectDeepFooInstaller2", Container);
            });
            PostInstall();

            yield break;
        }
    }
}
