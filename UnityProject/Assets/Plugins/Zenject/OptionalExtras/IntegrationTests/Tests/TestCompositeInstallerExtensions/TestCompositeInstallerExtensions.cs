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
        public void TestValidateLeafInstallerWithInstaller()
        {
            var installer = new TestInstaller();

            bool actual = installer.ValidateLeafInstaller(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerWithoutCircularRef()
        {
            var installer = new TestCompositeInstaller
            {
                _leafInstallers = new List<TestInstaller>(),
            };

            bool actual = installer.ValidateLeafInstaller(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerWithoutCircularRefDeep()
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

            bool actual = installer1.ValidateLeafInstaller(_parentInstallers);

            Assert.True(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndParentAsSelf()
        {
            var installer = new TestCompositeInstaller();
            _parentInstallers = new List<TestCompositeInstaller>
            {
                installer,
            };

            bool actual = installer.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndSelfCircularRef()
        {
            var installer = new TestCompositeInstaller();

            installer._leafInstallers = new List<TestInstaller>
            {
                installer,
            };

            bool actual = installer.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndSelfCircularRefDeep()
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

            bool actual = installer1.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndParentCircularRef()
        {
            var installer = new TestCompositeInstaller();

            installer._leafInstallers = new List<TestInstaller>
            {
                _parentInstaller1,
            };

            bool actual = installer.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndParentCircularRefDeep()
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

            bool actual = installer1.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndAnotherCircularRef()
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

            bool actual = installer1.ValidateLeafInstaller(_parentInstallers);

            Assert.False(actual);
        }

        [Test]
        public void TestValidateLeafInstallerWithCompositeInstallerAndAnotherCircularRefDeep()
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

            bool actual = installer1.ValidateLeafInstaller(_parentInstallers);

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