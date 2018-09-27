using System.Diagnostics;
using System.Linq.Expressions;
using ModestTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if !NOT_UNITY3D
using UnityEngine;
#endif

using Zenject.Internal;

namespace Zenject
{
    public delegate InjectTypeInfo ZenTypeInfoGetter();

    public enum ReflectionBakingCoverageModes
    {
        FallbackToDirectReflection,
        NoCheckAssumeFullCoverage,
        FallbackToDirectReflectionWithWarning,
    }

    public static class TypeAnalyzer
    {
        static Dictionary<Type, InjectTypeInfo> _typeInfo = new Dictionary<Type, InjectTypeInfo>();

#if UNITY_EDITOR
        // We store this separately from InjectTypeInfo because this flag is needed for contract
        // types whereas InjectTypeInfo is only needed for types that are instantiated, and
        // we want to minimize the types that generate InjectTypeInfo for
        static Dictionary<Type, bool> _allowDuringValidation = new Dictionary<Type, bool>();
#endif

        // Use double underscores for generated methods since this is also what the C# compiler does
        // for things like anonymous methods
        public const string ReflectionBakingGetInjectInfoMethodName = "__zenCreateInjectTypeInfo";
        public const string ReflectionBakingFactoryMethodName = "__zenCreate";
        public const string ReflectionBakingInjectMethodPrefix = "__zenInjectMethod";
        public const string ReflectionBakingFieldSetterPrefix = "__zenFieldSetter";
        public const string ReflectionBakingPropertySetterPrefix = "__zenPropertySetter";

        public static ReflectionBakingCoverageModes ReflectionBakingCoverageMode
        {
            get; set;
        }

        public static bool ShouldAllowDuringValidation<T>()
        {
            return ShouldAllowDuringValidation(typeof(T));
        }

#if !UNITY_EDITOR
        public static bool ShouldAllowDuringValidation(Type type)
        {
            return false;
        }
#else
        public static bool ShouldAllowDuringValidation(Type type)
        {
            bool shouldAllow;

            if (!_allowDuringValidation.TryGetValue(type, out shouldAllow))
            {
                shouldAllow = ShouldAllowDuringValidationInternal(type);
                _allowDuringValidation.Add(type, shouldAllow);
            }

            return shouldAllow;
        }

        static bool ShouldAllowDuringValidationInternal(Type type)
        {
            // During validation, do not instantiate or inject anything except for
            // Installers, IValidatable's, or types marked with attribute ZenjectAllowDuringValidation
            // You would typically use ZenjectAllowDuringValidation attribute for data that you
            // inject into factories

            if (type.DerivesFrom<IInstaller>() || type.DerivesFrom<IValidatable>())
            {
                return true;
            }

#if !NOT_UNITY3D
            if (type.DerivesFrom<Context>())
            {
                return true;
            }
#endif

            return type.HasAttribute<ZenjectAllowDuringValidationAttribute>();
        }
#endif

        public static InjectTypeInfo GetInfo<T>()
        {
            return GetInfo(typeof(T));
        }

        public static InjectTypeInfo GetInfo(Type type)
        {
            var info = TryGetInfo(type);
            Assert.IsNotNull(info, "Unable to get type info for type '{0}'", type);
            return info;
        }

        public static InjectTypeInfo TryGetInfo<T>()
        {
            return TryGetInfo(typeof(T));
        }

        public static InjectTypeInfo TryGetInfo(Type type)
        {
            InjectTypeInfo info;

#if ZEN_MULTITHREADING
            lock (_typeInfo)
#endif
            {
                if (_typeInfo.TryGetValue(type, out info))
                {
                    return info;
                }
            }

#if UNITY_EDITOR
            using (ProfileBlock.Start("Zenject Reflection"))
#endif
            {
                info = GetInfoInternal(type);
            }

            if (info != null)
            {
                Assert.IsEqual(info.Type, type);
                Assert.IsNull(info.BaseTypeInfo);

                var baseType = type.BaseType();

                if (baseType != null && ShouldAnalyzeType(baseType))
                {
                    info.BaseTypeInfo = TryGetInfo(baseType);
                }
            }

#if ZEN_MULTITHREADING
            lock (_typeInfo)
#endif
            {
                _typeInfo.Add(type, info);
            }

            return info;
        }

        static InjectTypeInfo GetInfoInternal(Type type)
        {
            if (!ShouldAnalyzeType(type))
            {
                return null;
            }

#if ZEN_INTERNAL_PROFILING
            // Make sure that the static constructor logic doesn't inflate our profile measurements
            using (ProfileTimers.CreateTimedBlock("User Code"))
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
#endif

#if ZEN_INTERNAL_PROFILING
            using (ProfileTimers.CreateTimedBlock("Type Analysis - Calling Baked Reflection Getter"))
#endif
            {
                var getInfoMethod = type.GetMethod(
                    ReflectionBakingGetInjectInfoMethodName,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (getInfoMethod != null)
                {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
                    var infoGetter = (ZenTypeInfoGetter)getInfoMethod.CreateDelegate(
                        typeof(ZenTypeInfoGetter), null);
#else
                    var infoGetter = ((ZenTypeInfoGetter)Delegate.CreateDelegate(
                        typeof(ZenTypeInfoGetter), getInfoMethod));
#endif

                    if (infoGetter != null)
                    {
                        return infoGetter();
                    }
                }
            }

            if (ReflectionBakingCoverageMode == ReflectionBakingCoverageModes.NoCheckAssumeFullCoverage)
            {
                // If we are confident that the reflection baking supplies all the injection information,
                // then we can avoid the costs of doing reflection on types that were not covered
                // by the baking
                return null;
            }

#if !(UNITY_WSA && ENABLE_DOTNET) || UNITY_EDITOR
            if (ReflectionBakingCoverageMode == ReflectionBakingCoverageModes.FallbackToDirectReflectionWithWarning)
            {
                Log.Warn("No reflection baking information found for type '{0}' - using more costly direct reflection instead", type);
            }
#endif

#if ZEN_INTERNAL_PROFILING
            using (ProfileTimers.CreateTimedBlock("Type Analysis - Direct Reflection"))
#endif
            {
                return CreateTypeInfoFromReflection(type);
            }
        }

        public static bool ShouldAnalyzeType(Type type)
        {
            if (type == null || type.IsEnum() || type.IsArray || type.IsInterface()
                || type.ContainsGenericParameters() || IsStaticType(type)
                || type == typeof(object))
            {
                return false;
            }

            return ShouldAnalyzeNamespace(type.Namespace);
        }

        static bool IsStaticType(Type type)
        {
            // Apparently this is unique to static classes
            return type.IsAbstract() && type.IsSealed();
        }

        public static bool ShouldAnalyzeNamespace(string ns)
        {
            if (ns == null)
            {
                return true;
            }

            return ns != "System" && !ns.StartsWith("System.")
                && ns != "UnityEngine" && !ns.StartsWith("UnityEngine.")
                && ns != "UnityEditor" && !ns.StartsWith("UnityEditor.")
                && ns != "UnityStandardAssets" && !ns.StartsWith("UnityStandardAssets.");
        }

        static InjectTypeInfo CreateTypeInfoFromReflection(Type type)
        {
            var reflectionInfo = ReflectionTypeAnalyzer.GetReflectionInfo(type);

            var injectConstructor = ReflectionInfoTypeInfoConverter.ConvertConstructor(
                reflectionInfo.InjectConstructor, type);

            var injectMethods = reflectionInfo.InjectMethods.Select(
                ReflectionInfoTypeInfoConverter.ConvertMethod).ToArray();

            var memberInfos = reflectionInfo.InjectFields.Select(
                x => ReflectionInfoTypeInfoConverter.ConvertField(type, x)).Concat(
                    reflectionInfo.InjectProperties.Select(
                        x => ReflectionInfoTypeInfoConverter.ConvertProperty(type, x))).ToArray();

            return new InjectTypeInfo(
                type, injectConstructor, injectMethods, memberInfos);
        }
    }
}
