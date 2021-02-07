using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Zenject
{
    // Use `Create -> Zenject -> Composite Scriptable Object Installer`
    public class CompositeScriptableObjectInstaller : ScriptableObjectInstaller<CompositeScriptableObjectInstaller>, ICompositeInstaller<ScriptableObjectInstallerBase>
    {
        [SerializeField]
        List<ScriptableObjectInstallerBase> _leafInstallers = new List<ScriptableObjectInstallerBase>();
        public IReadOnlyList<ScriptableObjectInstallerBase> LeafInstallers => _leafInstallers;

        public override void InstallBindings()
        {
            foreach (var installer in _leafInstallers)
            {
                Container.Inject(installer);

#if ZEN_INTERNAL_PROFILING
                using (ProfileTimers.CreateTimedBlock("User Code"))
#endif
                {
                    installer.InstallBindings();
                }
            }
        }
    }
}