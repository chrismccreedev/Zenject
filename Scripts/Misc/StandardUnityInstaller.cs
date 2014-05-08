using System;

namespace ModestTree.Zenject
{
    public class StandardUnityInstaller : Installer
    {
        // Install basic functionality for most unity apps
        public override void RegisterBindings()
        {
            _container.Bind<UnityKernel>().ToSingleGameObject();

            _container.Bind<StandardKernel>().ToSingle();
            // Uncomment this once you remove dependency in PlayerSandboxWrapper
            //_container.Bind<StandardKernel>().ToTransient().WhenInjectedInto<UnityKernel>();

            _container.Bind<InitializableHandler>().ToSingle();
            _container.Bind<ITickable>().ToLookup<UnityEventManager>();
        }
    }
}
