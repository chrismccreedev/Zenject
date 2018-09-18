using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ModestTree;
using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;
//using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using Zenject.Internal;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject.ReflectionBaking
{
    public class ReflectionBakingCodeWeaver
    {
        ModuleDefinition _module;

        MethodReference _zenjectTypeInfoConstructor;
        MethodReference _injectableInfoConstructor;
        MethodReference _injectMethodInfoConstructor;
        MethodReference _injectMemberInfoConstructor;
        MethodReference _constructorInfoConstructor;
        MethodReference _getTypeInfoMethod;
        MethodReference _getTypeFromHandleMethod;
        MethodReference _funcConstructor;
        MethodReference _funcPostInject;
        MethodReference _funcMemberSetter;

        TypeReference _injectMethodInfoType;
        TypeReference _injectMemberInfoType;
        TypeReference _injectableInfoType;
        TypeReference _objectArrayType;
        TypeReference _zenjectTypeInfoRef;

        public const string FactoryMethodName = "__zenCreate";

        public ReflectionBakingCodeWeaver(ModuleDefinition module)
        {
            _module = module;

            _zenjectTypeInfoRef = module.Import(typeof(InjectTypeInfo));
            _zenjectTypeInfoConstructor = module.Import(
                _zenjectTypeInfoRef.Resolve().GetMethod(".ctor"));

            var injectableInfoReference = module.Import(typeof(InjectableInfo));
            TypeDefinition injectableInfoDef = injectableInfoReference.Resolve();
            _injectableInfoConstructor = module.Import(injectableInfoDef.GetMethod(".ctor"));

            var typeAnalyzerReference = module.Import(typeof(Zenject.TypeAnalyzer));
            TypeDefinition typeAnalyzerInfo = typeAnalyzerReference.Resolve();

            _getTypeInfoMethod = module.Import(typeAnalyzerInfo.GetMethod("TryGetInfo", 1));

            var typeTypeReference = module.Import(typeof(Type));
            TypeDefinition typeTypeDef = typeTypeReference.Resolve();

            _getTypeFromHandleMethod = module.Import(typeTypeDef.GetMethod("GetTypeFromHandle", 1));

            _injectMethodInfoType = module.Import(typeof(InjectTypeInfo.InjectMethodInfo));
            _injectMethodInfoConstructor = module.Import(
                _injectMethodInfoType.Resolve().GetMethod(".ctor"));

            _injectMemberInfoType = module.Import(typeof(InjectTypeInfo.InjectMemberInfo));
            _injectMemberInfoConstructor = module.Import(
                _injectMemberInfoType.Resolve().GetMethod(".ctor"));

            var constructorInfoType = module.Import(typeof(InjectTypeInfo.InjectConstructorInfo));
            _constructorInfoConstructor = module.Import(
                constructorInfoType.Resolve().GetMethod(".ctor"));

            _injectableInfoType = module.Import(typeof(InjectableInfo));

            _objectArrayType = module.Import(typeof(object[]));

            var factoryMethodType = module.Import(typeof(ZenFactoryMethod));

            _funcConstructor = module.Import(
                factoryMethodType.Resolve().GetMethod(".ctor", 2));

            var postInjectMethodType = module.Import(typeof(ZenInjectMethod));

            _funcPostInject = module.Import(
                postInjectMethodType.Resolve().GetMethod(".ctor", 2));

            var setterMethodType = module.Import(typeof(ZenMemberSetterMethod));

            _funcMemberSetter = module.Import(
                setterMethodType.Resolve().GetMethod(".ctor", 2));
        }

        public bool EditType(TypeDefinition typeDef, Type actualType)
        {
            if (actualType.IsEnum || actualType.IsValueType || actualType.IsInterface
                    || actualType.HasAttribute<NoReflectionCodeWeavingAttribute>()
                    || IsStaticClass(actualType))
            {
                return false;
            }

            // Allow running on the same dll multiple times without causing problems
            if (IsTypeProcessed(typeDef))
            {
                return false;
            }

            try
            {
                var typeInfo = ReflectionTypeAnalyzer.GetReflectionInfo(actualType);

                // Should be false when defining a static constructor according to msdn
                typeDef.IsBeforeFieldInit = false;

                var factoryMethod = TryAddFactoryMethod(typeDef, typeInfo);

                var genericTypeDef = CreateGenericInstanceWithParameters(typeDef);
                var fieldSetMethods = AddFieldSetters(typeDef, genericTypeDef, typeInfo);
                var propertySetMethods = AddPropertySetters(typeDef, genericTypeDef, typeInfo);
                var postInjectMethods = AddPostInjectMethods(typeDef, genericTypeDef, typeInfo);

                CreateGetInfoMethod(
                    typeDef, genericTypeDef, typeInfo,
                    factoryMethod, fieldSetMethods, propertySetMethods, postInjectMethods);
            }
            catch (Exception e)
            {
                Log.ErrorException("Error when modifying type '{0}'".Fmt(actualType), e);
                throw;
            }

            return true;
        }

        static bool IsStaticClass(Type type)
        {
            // Apparently this is unique to static classes
            return type.IsAbstract && type.IsSealed;
        }

        // We are already processed if our static constructor calls TypeAnalyzer
        bool IsTypeProcessed(TypeDefinition typeDef)
        {
            return typeDef.GetMethod(TypeAnalyzer.ReflectionBakingGetInjectInfoMethodName) != null;
        }

        void EmitCastOperation(ILProcessor processor, Type type, Collection<GenericParameter> genericParams)
        {
            if (type.IsGenericParameter)
            {
                processor.Emit(OpCodes.Unbox_Any, genericParams[type.GenericParameterPosition]);
            }
            else if (type.IsEnum)
            {
                processor.Emit(OpCodes.Unbox_Any, _module.TypeSystem.Int32);
            }
            else if (type.IsValueType)
            {
                processor.Emit(OpCodes.Unbox_Any, _module.Import(type));
            }
            else
            {
                processor.Emit(OpCodes.Castclass, CreateGenericInstanceIfNecessary(type, genericParams));
            }
        }

        TypeReference CreateGenericInstanceWithParameters(TypeDefinition typeDef)
        {
            if (typeDef.GenericParameters.Any())
            {
                var genericInstance = new GenericInstanceType(typeDef);

                foreach (var parameter in typeDef.GenericParameters)
                {
                    genericInstance.GenericArguments.Add(parameter);
                }

                return genericInstance;
            }

            return typeDef;
        }

        MethodDefinition TryAddFactoryMethod(
            TypeDefinition typeDef, ReflectionTypeInfo typeInfo)
        {
#if !NOT_UNITY3D
            if (typeInfo.Type.DerivesFromOrEqual<Component>())
            {
                Assert.That(typeInfo.InjectConstructor.Parameters.IsEmpty());
                return null;
            }
#endif

            if (typeInfo.InjectConstructor.ConstructorInfo == null)
            {
                // static classes, abstract types
                return null;
            }

            var factoryMethod = new MethodDefinition(
                FactoryMethodName,
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig |
                Mono.Cecil.MethodAttributes.Static,
                _module.TypeSystem.Object);

            var p1 = new ParameterDefinition(_objectArrayType);
            p1.Name = "P_0";
            factoryMethod.Parameters.Add(p1);

            var body = factoryMethod.Body;
            body.InitLocals = true;

            var processor = body.GetILProcessor();

            var returnValueVar = new VariableDefinition(_module.TypeSystem.Object);
            body.Variables.Add(returnValueVar);

            processor.Emit(OpCodes.Nop);

            Assert.IsNotNull(typeInfo.InjectConstructor);

            var args = typeInfo.InjectConstructor.Parameters;

            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldc_I4, i);
                processor.Emit(OpCodes.Ldelem_Ref);

                EmitCastOperation(
                    processor, arg.ParameterInfo.ParameterType, typeDef.GenericParameters);
            }

            processor.Emit(OpCodes.Newobj, _module.Import(typeInfo.InjectConstructor.ConstructorInfo));

            processor.Emit(OpCodes.Stloc_0);
            processor.Emit(OpCodes.Ldloc_S, returnValueVar);
            processor.Emit(OpCodes.Ret);

            typeDef.Methods.Add(factoryMethod);

            return factoryMethod;
        }

        void AddPostInjectMethodBody(
            ILProcessor processor, ReflectionTypeInfo.InjectMethodInfo postInjectInfo, TypeDefinition typeDef, TypeReference genericTypeDef)
        {
            processor.Emit(OpCodes.Nop);

            TypeReference declaringTypeDef;
            MethodReference actualMethodDef;

            if (!TryFindLocalMethod(
                genericTypeDef, postInjectInfo.MethodInfo.Name, out declaringTypeDef, out actualMethodDef))
            {
                throw Assert.CreateException();
            }

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Castclass, declaringTypeDef);

            for (int k = 0; k < postInjectInfo.Parameters.Count; k++)
            {
                var injectInfo = postInjectInfo.Parameters[k];

                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Ldc_I4, k);
                processor.Emit(OpCodes.Ldelem_Ref);

                EmitCastOperation(processor, injectInfo.ParameterInfo.ParameterType, typeDef.GenericParameters);
            }

            processor.Emit(OpCodes.Callvirt, actualMethodDef);
            processor.Emit(OpCodes.Ret);
        }

        MethodDefinition AddPostInjectMethod(
            string name, ReflectionTypeInfo.InjectMethodInfo postInjectInfo, TypeDefinition typeDef, TypeReference genericTypeDef)
        {
            var methodDef = new MethodDefinition(
                name,
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig |
                Mono.Cecil.MethodAttributes.Static,
                _module.TypeSystem.Void);

            var p1 = new ParameterDefinition(_module.TypeSystem.Object);
            p1.Name = "P_0";
            methodDef.Parameters.Add(p1);

            var p2 = new ParameterDefinition(_objectArrayType);
            p2.Name = "P_1";
            methodDef.Parameters.Add(p2);

            var body = methodDef.Body;
            var processor = body.GetILProcessor();

            AddPostInjectMethodBody(processor, postInjectInfo, typeDef, genericTypeDef);

            typeDef.Methods.Add(methodDef);

            return methodDef;
        }

        List<MethodDefinition> AddPostInjectMethods(
            TypeDefinition typeDef, TypeReference genericTypeDef, ReflectionTypeInfo typeInfo)
        {
            var postInjectMethods = new List<MethodDefinition>();

            for (int i = 0; i < typeInfo.InjectMethods.Count; i++)
            {
                postInjectMethods.Add(
                    AddPostInjectMethod(
                        "__zenInjectMethod" + i.ToString(), typeInfo.InjectMethods[i], typeDef, genericTypeDef));
            }

            return postInjectMethods;
        }

        void EmitSetterMethod(
            ILProcessor processor, MemberInfo memberInfo, TypeDefinition typeDef, TypeReference genericTypeDef)
        {
            processor.Emit(OpCodes.Nop);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Castclass, genericTypeDef);

            processor.Emit(OpCodes.Ldarg_1);

            if (memberInfo is FieldInfo)
            {
                var fieldInfo = (FieldInfo)memberInfo;

                EmitCastOperation(processor, fieldInfo.FieldType, typeDef.GenericParameters);

                processor.Emit(OpCodes.Stfld, FindLocalField(genericTypeDef, fieldInfo.Name));
            }
            else
            {
                var propertyInfo = (PropertyInfo)memberInfo;

                EmitCastOperation(processor, propertyInfo.PropertyType, typeDef.GenericParameters);

                processor.Emit(OpCodes.Callvirt, FindLocalPropertySetMethod(genericTypeDef, propertyInfo.Name));
            }

            processor.Emit(OpCodes.Ret);
        }

        MethodDefinition AddSetterMethod(
            string name, MemberInfo memberInfo, TypeDefinition typeDef, TypeReference genericTypeDef)
        {
            var methodDef = new MethodDefinition(
                name,
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig |
                Mono.Cecil.MethodAttributes.Static,
                _module.TypeSystem.Void);

            var p1 = new ParameterDefinition(_module.TypeSystem.Object);
            p1.Name = "P_0";
            methodDef.Parameters.Add(p1);

            var p2 = new ParameterDefinition(_module.TypeSystem.Object);
            p2.Name = "P_1";
            methodDef.Parameters.Add(p2);

            methodDef.Body.InitLocals = true;

            EmitSetterMethod(
                methodDef.Body.GetILProcessor(), memberInfo, typeDef, genericTypeDef);

            typeDef.Methods.Add(methodDef);

            return methodDef;
        }

        List<MethodDefinition> AddPropertySetters(
            TypeDefinition typeDef, TypeReference genericTypeDef, ReflectionTypeInfo typeInfo)
        {
            var methodDefs = new List<MethodDefinition>();

            for (int i = 0; i < typeInfo.InjectProperties.Count; i++)
            {
                methodDefs.Add(
                    AddSetterMethod(
                        "__zenPropertySetter" + i.ToString(),
                        typeInfo.InjectProperties[i].PropertyInfo, typeDef, genericTypeDef));
            }

            return methodDefs;
        }

        List<MethodDefinition> AddFieldSetters(
            TypeDefinition typeDef, TypeReference genericTypeDef, ReflectionTypeInfo typeInfo)
        {
            var methodDefs = new List<MethodDefinition>();

            for (int i = 0; i < typeInfo.InjectFields.Count; i++)
            {
                methodDefs.Add(
                    AddSetterMethod(
                        "__zenFieldSetter" + i.ToString(),
                        typeInfo.InjectFields[i].FieldInfo, typeDef, genericTypeDef));
            }

            return methodDefs;
        }

        void CreateGetInfoMethod(
            TypeDefinition typeDef, TypeReference genericTypeDef, ReflectionTypeInfo typeInfo,
            MethodDefinition factoryMethod, List<MethodDefinition> fieldSetMethods,
            List<MethodDefinition> propertySetMethods, List<MethodDefinition> postInjectMethods)
        {
            var getInfoMethodDef = new MethodDefinition(
                TypeAnalyzer.ReflectionBakingGetInjectInfoMethodName,
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig |
                Mono.Cecil.MethodAttributes.Static,
                _zenjectTypeInfoRef);

            typeDef.Methods.Add(getInfoMethodDef);

            var returnValueVar = new VariableDefinition(_module.TypeSystem.Object);

            var body = getInfoMethodDef.Body;

            body.Variables.Add(returnValueVar);
            body.InitLocals = true;

            var instructions = new List<Instruction>();

            instructions.Add(Instruction.Create(OpCodes.Ldtoken, genericTypeDef));
            instructions.Add(Instruction.Create(OpCodes.Call, _getTypeFromHandleMethod));

            if (factoryMethod == null)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
            }
            else
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
                instructions.Add(Instruction.Create(OpCodes.Ldftn, ChangeDeclaringType(factoryMethod, genericTypeDef)));
                instructions.Add(Instruction.Create(OpCodes.Newobj, _funcConstructor));
            }

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, typeInfo.InjectConstructor.Parameters.Count));
            instructions.Add(Instruction.Create(OpCodes.Newarr, _injectableInfoType));

            for (int i = 0; i < typeInfo.InjectConstructor.Parameters.Count; i++)
            {
                var injectableInfo = typeInfo.InjectConstructor.Parameters[i].InjectableInfo;

                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));

                EmitNewInjectableInfoInstructions(
                    instructions, injectableInfo, typeDef, genericTypeDef);

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            instructions.Add(Instruction.Create(OpCodes.Newobj, _constructorInfoConstructor));

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, typeInfo.InjectMethods.Count));
            instructions.Add(Instruction.Create(OpCodes.Newarr, _injectMethodInfoType));

            Assert.IsEqual(postInjectMethods.Count, typeInfo.InjectMethods.Count);

            for (int i = 0; i < typeInfo.InjectMethods.Count; i++)
            {
                var injectMethodInfo = typeInfo.InjectMethods[i];

                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));

                AddInjectableMethodInstructions(
                    instructions, injectMethodInfo, typeDef, genericTypeDef, postInjectMethods[i]);

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, fieldSetMethods.Count + propertySetMethods.Count));
            instructions.Add(Instruction.Create(OpCodes.Newarr, _injectMemberInfoType));

            for (int i = 0; i < fieldSetMethods.Count; i++)
            {
                var injectField = typeInfo.InjectFields[i];

                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));

                AddInjectableMemberInstructions(
                    instructions,
                    injectField.InjectableInfo, injectField.FieldInfo.Name,
                    typeDef, genericTypeDef, fieldSetMethods[i]);

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            for (int i = 0; i < propertySetMethods.Count; i++)
            {
                var injectProperty = typeInfo.InjectProperties[i];

                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, fieldSetMethods.Count + i));

                AddInjectableMemberInstructions(
                    instructions,
                    injectProperty.InjectableInfo,
                    injectProperty.PropertyInfo.Name, typeDef, genericTypeDef,
                    propertySetMethods[i]);

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            var baseType = typeInfo.Type.BaseType;

            if (baseType == null || baseType == typeof(object)
                || !TypeAnalyzer.ShouldAnalyzeNamespace(baseType.Namespace))
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
            }
            else
            {
                var specificBaseType = TryGetSpecificBaseTypeRef(typeDef, genericTypeDef);

                if (specificBaseType == null)
                {
                    instructions.Add(Instruction.Create(OpCodes.Ldnull));
                }
                else
                {
                    instructions.Add(Instruction.Create(OpCodes.Ldtoken, specificBaseType));
                    instructions.Add(Instruction.Create(OpCodes.Call, _getTypeFromHandleMethod));
                    instructions.Add(Instruction.Create(OpCodes.Call, _getTypeInfoMethod));
                }
            }

            instructions.Add(Instruction.Create(OpCodes.Newobj, _zenjectTypeInfoConstructor));

            instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            instructions.Add(Instruction.Create(OpCodes.Ldloc_S, returnValueVar));
            instructions.Add(Instruction.Create(OpCodes.Ret));

            var processor = body.GetILProcessor();

            foreach (var instruction in instructions)
            {
                processor.Append(instruction);
            }
        }

        class TypeDefPair
        {
            public readonly TypeDefinition TypeDefinition;
            public readonly TypeReference TypeReference;

            public TypeDefPair(
                TypeDefinition typeDefinition,
                TypeReference typeReference)
            {
                TypeDefinition = typeDefinition;
                TypeReference = typeReference;
            }
        }

        IEnumerable<TypeDefPair> IterateBaseTypesAndSelf(TypeReference specificTypeRef)
        {
            var typeDef = specificTypeRef.Resolve();

            yield return new TypeDefPair(typeDef, specificTypeRef);

            var specificBaseTypeRef = TryGetSpecificBaseTypeRef(typeDef, specificTypeRef);

            if (specificBaseTypeRef != null)
            {
                foreach (var typePair in IterateBaseTypesAndSelf(specificBaseTypeRef))
                {
                    yield return typePair;
                }
            }
        }

        TypeReference TryGetSpecificBaseTypeRef(TypeDefinition typeDef, TypeReference specificTypeRef)
        {
            Assert.IsNotNull(typeDef.BaseType);
            Assert.That(typeDef.BaseType.FullName != "System.Object");

            var specificBaseTypeRef = typeDef.BaseType;

            if (specificBaseTypeRef.ContainsGenericParameter)
            {
                var genericArgMap = new Dictionary<string, TypeReference>();
                var specificTypeRefGenericInstance = (GenericInstanceType)specificTypeRef;

                for (int i = 0; i < typeDef.GenericParameters.Count; i++)
                {
                    genericArgMap[typeDef.GenericParameters[i].Name] = specificTypeRefGenericInstance.GenericArguments[i];
                }

                specificBaseTypeRef = FillInGenericParameters(specificBaseTypeRef, genericArgMap);
            }

            return specificBaseTypeRef;
        }

        MethodReference FindLocalPropertySetMethod(
            TypeReference specificTypeRef, string memberName)
        {
            foreach (var typePair in IterateBaseTypesAndSelf(specificTypeRef))
            {
                var candidatePropertyDef = typePair.TypeDefinition.Properties
                    .Where(x => x.Name == memberName).SingleOrDefault();

                if (candidatePropertyDef != null)
                {
                    return ChangeDeclaringType(
                        candidatePropertyDef.SetMethod, typePair.TypeReference);
                }
            }

            throw Assert.CreateException();
        }

        FieldReference FindLocalField(
            TypeReference specificTypeRef, string fieldName)
        {
            foreach (var typePair in IterateBaseTypesAndSelf(specificTypeRef))
            {
                var candidateFieldDef = typePair.TypeDefinition.Fields
                    .Where(x => x.Name == fieldName).SingleOrDefault();

                if (candidateFieldDef != null)
                {
                    return ChangeDeclaringType(
                        candidateFieldDef, typePair.TypeReference);
                }
            }

            throw Assert.CreateException();
        }

        bool TryFindLocalMethod(
            TypeReference specificTypeRef, string methodName, out TypeReference declaringTypeRef, out MethodReference methodRef)
        {
            foreach (var typePair in IterateBaseTypesAndSelf(specificTypeRef))
            {
                var candidateMethodDef = typePair.TypeDefinition.Methods
                    .Where(x => x.Name == methodName).SingleOrDefault();

                if (candidateMethodDef != null)
                {
                    declaringTypeRef = typePair.TypeReference;
                    methodRef = ChangeDeclaringType(
                        candidateMethodDef, typePair.TypeReference);
                    return true;
                }
            }

            declaringTypeRef = null;
            methodRef = null;
            return false;
        }

        TypeReference FillInGenericParameters(
            TypeReference type, Dictionary<string, TypeReference> genericArgMap)
        {
            var genericType = type as GenericInstanceType;
            Assert.IsNotNull(genericType);

            var genericTypeClone = new GenericInstanceType(_module.Import(type.Resolve()));

            for (int i = 0; i < genericType.GenericArguments.Count; i++)
            {
                var arg = genericType.GenericArguments[i];

                if (arg.IsGenericParameter)
                {
                    Assert.That(genericArgMap.ContainsKey(arg.Name), "Could not find key '{0}' for type '{1}'", arg.Name, type.FullName);

                    genericTypeClone.GenericArguments.Add(genericArgMap[arg.Name]);
                }
                else
                {
                    genericTypeClone.GenericArguments.Add(arg);
                }
            }

            return genericTypeClone;
        }

        MethodReference ChangeDeclaringType(MethodDefinition methodDef, TypeReference typeRef)
        {
            var newMethodRef = new MethodReference(
                methodDef.Name, methodDef.ReturnType, typeRef);

            newMethodRef.HasThis = methodDef.HasThis;

            foreach (var arg in methodDef.Parameters)
            {
                var paramDef = new ParameterDefinition(arg.ParameterType);

                newMethodRef.Parameters.Add(paramDef);
            }

            return newMethodRef;
        }

        FieldReference ChangeDeclaringType(FieldReference fieldDef, TypeReference typeRef)
        {
            return new FieldReference(
                fieldDef.Name, fieldDef.FieldType, typeRef);
        }

        void AddObjectInstructions(
            List<Instruction> instructions,
            object identifier)
        {
            if (identifier == null)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
            }
            else if (identifier is string)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldstr, (string)identifier));
            }
            else if (identifier is int)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)identifier));
                instructions.Add(Instruction.Create(OpCodes.Box, _module.Import(typeof(int))));
            }
            else if (identifier.GetType().IsEnum)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)identifier));
                instructions.Add(Instruction.Create(OpCodes.Box, _module.Import(identifier.GetType())));
            }
            else
            {
                throw Assert.CreateException(
                    "Cannot process values with type '{0}' currently.  Feel free to add support for this and submit a pull request to github.", identifier.GetType());
            }
        }

        TypeReference CreateGenericInstanceIfNecessary(
            Type memberType, Collection<GenericParameter> genericParams)
        {
            if (!memberType.ContainsGenericParameters)
            {
                return _module.Import(memberType);
            }

            if (memberType.IsGenericParameter)
            {
                return genericParams[memberType.GenericParameterPosition];
            }

            if (memberType.IsArray)
            {
                return new ArrayType(
                    CreateGenericInstanceIfNecessary(memberType.GetElementType(), genericParams), memberType.GetArrayRank());
            }

            var genericMemberType = memberType.GetGenericTypeDefinition();

            var genericInstance = new GenericInstanceType(_module.Import(genericMemberType));

            foreach (var arg in memberType.GenericArguments())
            {
                genericInstance.GenericArguments.Add(
                    CreateGenericInstanceIfNecessary(arg, genericParams));
            }

            return genericInstance;
        }

        void AddInjectableMemberInstructions(
            List<Instruction> instructions,
            InjectableInfo injectableInfo, string name,
            TypeDefinition typeDef, TypeReference genericTypeDef,
            MethodDefinition methodDef)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Ldftn, ChangeDeclaringType(methodDef, genericTypeDef)));
            instructions.Add(Instruction.Create(OpCodes.Newobj, _funcMemberSetter));

            EmitNewInjectableInfoInstructions(
                instructions, injectableInfo, typeDef, genericTypeDef);

            instructions.Add(Instruction.Create(OpCodes.Newobj, _injectMemberInfoConstructor));
        }

        void AddInjectableMethodInstructions(
            List<Instruction> instructions,
            ReflectionTypeInfo.InjectMethodInfo injectMethod,
            TypeDefinition typeDef, TypeReference genericTypeDef,
            MethodDefinition methodDef)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Ldftn, ChangeDeclaringType(methodDef, genericTypeDef)));
            instructions.Add(Instruction.Create(OpCodes.Newobj, _funcPostInject));

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, injectMethod.Parameters.Count));
            instructions.Add(Instruction.Create(OpCodes.Newarr, _injectableInfoType));

            for (int i = 0; i < injectMethod.Parameters.Count; i++)
            {
                var injectableInfo = injectMethod.Parameters[i].InjectableInfo;

                instructions.Add(Instruction.Create(OpCodes.Dup));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));

                EmitNewInjectableInfoInstructions(
                    instructions, injectableInfo, typeDef, genericTypeDef);

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            instructions.Add(Instruction.Create(OpCodes.Ldstr, injectMethod.MethodInfo.Name));

            instructions.Add(Instruction.Create(OpCodes.Newobj, _injectMethodInfoConstructor));
        }

        void EmitNewInjectableInfoInstructions(
            List<Instruction> instructions,
            InjectableInfo injectableInfo,
            TypeDefinition typeDef, TypeReference genericTypeDef)
        {
            if (injectableInfo.Optional)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            }
            else
            {
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            }

            AddObjectInstructions(instructions, injectableInfo.Identifier);

            instructions.Add(Instruction.Create(OpCodes.Ldstr, injectableInfo.MemberName));

            instructions.Add(Instruction.Create(OpCodes.Ldtoken, CreateGenericInstanceIfNecessary(injectableInfo.MemberType, typeDef.GenericParameters)));

            instructions.Add(Instruction.Create(OpCodes.Call, _getTypeFromHandleMethod));

            instructions.Add(Instruction.Create(OpCodes.Ldtoken, genericTypeDef));
            instructions.Add(Instruction.Create(OpCodes.Call, _getTypeFromHandleMethod));

            AddObjectInstructions(instructions, injectableInfo.DefaultValue);

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)injectableInfo.SourceType));

            instructions.Add(Instruction.Create(OpCodes.Newobj, _injectableInfoConstructor));
        }
    }
}
