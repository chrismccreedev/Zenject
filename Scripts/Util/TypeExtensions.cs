using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModestTree
{
    public static class TypeExtensions
    {
        public static bool DerivesFrom<T>(this Type a)
        {
            return DerivesFrom(a, typeof(T));
        }

        // This seems easier to think about than IsAssignableFrom
        public static bool DerivesFrom(this Type a, Type b)
        {
            return b.IsAssignableFrom(a);
        }

        public static AttributeType GetAttribute<AttributeType>(this ParameterInfo paramInfo) where AttributeType : class
        {
            var attributes = paramInfo.GetCustomAttributes(typeof(AttributeType), true);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes.Cast<AttributeType>().Single();
        }

        public static AttributeType GetAttribute<AttributeType>(this Type type) where AttributeType : class
        {
            var attributes = type.GetCustomAttributes(typeof(AttributeType), true);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes.Cast<AttributeType>().Single();
        }

        public static AttributeType GetAttribute<AttributeType>(this FieldInfo fieldInfo) where AttributeType : class
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(AttributeType), true);

            if (attributes.Length == 0)
            {
                return null;
            }

            Assert.IsEqual(attributes.Length, 1, "Expected non-muliple attribute");
            return attributes[0] as AttributeType;
        }

        public static bool HasAttribute<AttributeType>(this FieldInfo fieldInfo) where AttributeType : class
        {
            return fieldInfo.GetCustomAttributes(typeof(AttributeType), true).Any();
        }

        public static bool HasAttribute<AttributeType>(this Type type) where AttributeType : class
        {
            return type.GetCustomAttributes(typeof(AttributeType), true).Any();
        }

        public static object GetDefaultValue(this Type type)
        {
            if(type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        // Returns name without generic arguments
        public static string GetSimpleName(this Type type)
        {
            var name = type.Name;

            var quoteIndex = name.IndexOf("`");

            if (quoteIndex == -1)
            {
                return name;
            }

            // Remove the backtick
            return name.Substring(0, quoteIndex);
        }

        public static string GetPrettyName(this Type type)
        {
            if (type.GetGenericArguments().Length == 0)
            {
                return type.Name;
            }

            var genericArguments = type.GetGenericArguments();
            var typeDefinition = type.Name;
            var quoteIndex = typeDefinition.IndexOf("`");

            if (quoteIndex < 0)
            {
                // Not sure why this happens sometimes
                return type.Name;
            }

            var unmangledName = typeDefinition.Substring(0, quoteIndex);
            return unmangledName + "<" + String.Join(",", genericArguments.Select<Type,string>(GetPrettyName).ToArray()) + ">";
        }

        public static IEnumerable<FieldInfo> GetFieldsWithAttribute<T>(this Type type)
        {
            foreach (var fieldInfo in GetAllFields(type))
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(T), true);

                if (attrs.Length > 0)
                {
                    yield return fieldInfo;
                }
            }
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            // Recursion is necessary since otherwise we won't get private members in base classes
            var baseClassFields = type.BaseType == null ? Enumerable.Empty<FieldInfo>() : type.BaseType.GetAllFields();

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return type.GetFields(flags).Concat(baseClassFields);
        }
    }
}
