using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Zenject
{
    public static class CompositeInstallerExtensions
    {
        public static bool ValidateLeafInstaller<T>(
            this T leafInstaller,
            IReadOnlyList<ICompositeInstaller<T>> parentInstallers)
            where T : IInstaller
        {
            var compositeInstaller = leafInstaller as ICompositeInstaller<T>;
            if (compositeInstaller == null)
            {
                return true;
            }

            if (parentInstallers.Contains(compositeInstaller))
            {
                // Found a circular reference
                return false;
            }

            var childParentInstallers = new List<ICompositeInstaller<T>>(parentInstallers)
            {
                compositeInstaller
            };

            bool result = compositeInstaller
                .LeafInstallers
                .All(installer => installer.ValidateLeafInstallerSavedAlloc(childParentInstallers));
            return result;
        }

        public static bool ValidateLeafInstallerSavedAlloc<T>(
            this T leafInstaller,
            List<ICompositeInstaller<T>> reusableParentInstallers)
            where T : IInstaller
        {
            var compositeInstaller = leafInstaller as ICompositeInstaller<T>;
            if (compositeInstaller == null)
            {
                return true;
            }

            if (reusableParentInstallers.Contains(compositeInstaller))
            {
                // Found a circular reference
                return false;
            }

            bool result = true;

            int compositeInstallerIndex = reusableParentInstallers.Count;
            reusableParentInstallers.Add(compositeInstaller);

            var leafInstallers = compositeInstaller.LeafInstallers;
            for (int i = 0; i < leafInstallers.Count; ++i)
            {
                var installer = leafInstallers[i];
                result &= installer.ValidateLeafInstallerSavedAlloc(reusableParentInstallers);

                if (!result)
                {
                    break;
                }
            }

            reusableParentInstallers.RemoveAt(compositeInstallerIndex);

            return result;
        }
    }
}