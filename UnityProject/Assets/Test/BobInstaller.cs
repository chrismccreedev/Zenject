using UnityEngine;
using Zenject;

public class Jim
{
}

public class BobInstaller : MonoInstaller<BobInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<Jim>().AsSingle();
    }
}
