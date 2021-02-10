using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Zenject.Tests.Installers
{
    public class TestCompositeInstallerExtensions
    {
        TestCompositeInstaller _parentInstaller1;
        List<TestCompositeInstaller> _parentInstallers;

        TestInstaller _dummyInstaller1;
        TestInstaller _dummyInstaller2;
        TestInstaller _dummyInstaller3;

        [SetUp]
        public void SetUp()
        {
            _parentInstaller1 = new TestCompositeInstaller
            {
                _leafInstallers = new List<TestInstaller>()
            };

            _parentInstallers = new List<TestCompositeInstaller>
            {
                _parentInstaller1,
            };

            _dummyInstaller1 = new TestInstaller();
            _dummyInstaller2 = new TestInstaller();
            _dummyInstaller3 = new TestInstaller();
        }

        [Test]
        public void TestValidateAsCompositeWithInstaller()
        {
            var installer = new TestInstaller();

            bool actual = installer.ValidateAsComposite(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerWithoutCircularRef()
        {
            var installer = new TestCompositeInstaller
            {
                _leafInstallers = new List<TestInstaller>(),
            };

            bool actual = installer.ValidateAsComposite(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerWithoutCircularRefDeep()
        {
            var installer1 = new TestCompositeInstaller();
            var installer2 = new TestCompositeInstaller();
            var installer3 = new TestCompositeInstaller();

            installer1._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller1,
                installer2,
                _dummyInstaller2,
            };
            installer2._leafInstallers = new List<TestInstaller>
            {
                installer3,
            };
            installer3._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller3,
            };

            bool actual = installer1.ValidateAsComposite(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndParentAsSelf()
        {
            var installer = new TestCompositeInstaller();
            _parentInstallers = new List<TestCompositeInstaller>
            {
                installer,
            };

            bool actual = installer.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndSelfCircularRef()
        {
            var installer = new TestCompositeInstaller();

            installer._leafInstallers = new List<TestInstaller>
            {
                installer,
            };

            bool actual = installer.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndSelfCircularRefDeep()
        {
            var installer1 = new TestCompositeInstaller();
            var installer2 = new TestCompositeInstaller();
            var installer3 = new TestCompositeInstaller();

            installer1._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller1,
                installer2,
                _dummyInstaller2,
            };
            installer2._leafInstallers = new List<TestInstaller>
            {
                installer3,
            };
            installer3._leafInstallers = new List<TestInstaller>
            {
                installer1,  // a circular reference
                _dummyInstaller3,
            };

            bool actual = installer1.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndParentCircularRef()
        {
            var installer = new TestCompositeInstaller();

            installer._leafInstallers = new List<TestInstaller>
            {
                _parentInstaller1,
            };

            bool actual = installer.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndParentCircularRefDeep()
        {
            var installer1 = new TestCompositeInstaller();
            var installer2 = new TestCompositeInstaller();
            var installer3 = new TestCompositeInstaller();

            installer1._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller1,
                installer2,
                _dummyInstaller2,
            };
            installer2._leafInstallers = new List<TestInstaller>
            {
                installer3,
            };
            installer3._leafInstallers = new List<TestInstaller>
            {
                _parentInstaller1,  // a circular reference
                _dummyInstaller3,
            };

            bool actual = installer1.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndAnotherCircularRef()
        {
            var installer1 = new TestCompositeInstaller();
            var installer2 = new TestCompositeInstaller();
            var installer3 = new TestCompositeInstaller();

            installer1._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller1,
                installer2,
                _dummyInstaller2,
            };
            installer2._leafInstallers = new List<TestInstaller>
            {
                installer3,
            };
            installer3._leafInstallers = new List<TestInstaller>
            {
                installer2,  // a circular reference
                _dummyInstaller3,
            };

            bool actual = installer1.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateAsCompositeWithCompositeInstallerAndAnotherCircularRefDeep()
        {
            var installer1 = new TestCompositeInstaller();
            var installer2 = new TestCompositeInstaller();
            var installer3 = new TestCompositeInstaller();
            var installer4 = new TestCompositeInstaller();
            var installer5 = new TestCompositeInstaller();

            installer1._leafInstallers = new List<TestInstaller>
            {
                _dummyInstaller1,
                installer2,
                _dummyInstaller2,
            };
            installer2._leafInstallers = new List<TestInstaller>
            {
                installer3,
            };
            installer3._leafInstallers = new List<TestInstaller>
            {
                installer4,
                _dummyInstaller3,
            };
            installer4._leafInstallers = new List<TestInstaller>
            {
                installer5,
            };
            installer5._leafInstallers = new List<TestInstaller>
            {
                installer3,  // a circular reference
            };

            bool actual = installer1.ValidateAsComposite(_parentInstallers);

            Assert.False(actual);
        }

        public class TestInstaller : IInstaller
        {
            public bool IsEnabled => false;
            public void InstallBindings() { }
        }

        public class TestCompositeInstaller : TestInstaller, ICompositeInstaller<TestInstaller>
        {
            public List<TestInstaller> _leafInstallers;
            public IReadOnlyList<TestInstaller> LeafInstallers => _leafInstallers;
        }
    }
}