using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zenject.ReflectionBaking
{
    public class CustomAssemblyResolver : BaseAssemblyResolver
    {
        readonly IDictionary<string, AssemblyDefinition> _cache;

        public CustomAssemblyResolver(params string[] directoryPaths)
        {
            _cache = new Dictionary<string, AssemblyDefinition>();

            foreach (var path in directoryPaths)
            {
                AddSearchDirectory(path);
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assemblyDef = FindAssemblyDefinition(name.FullName, null);

            if (assemblyDef == null)
            {
                assemblyDef = base.Resolve(name);
                _cache[name.FullName] = assemblyDef;
            }

            return assemblyDef;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition assemblyDef = FindAssemblyDefinition(name.FullName, parameters);

            if (assemblyDef == null)
            {
                assemblyDef = base.Resolve(name, parameters);
                _cache[name.FullName] = assemblyDef;
            }

            return assemblyDef;
        }

        AssemblyDefinition FindAssemblyDefinition(string fullName, ReaderParameters parameters)
        {
            if (fullName == null)
            {
                throw new ArgumentNullException("fullName");
            }

            AssemblyDefinition assemblyDefinition;

            if (_cache.TryGetValue(fullName, out assemblyDefinition))
            {
                return assemblyDefinition;
            }

            return null;
        }
    }
}
