﻿//-----------------------------------------------------------------------
// <copyright file="ReflectionUtils.cs" company="The Outercurve Foundation">
//    Copyright (c) 2011, The Outercurve Foundation. 
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <author>Prabir Shrestha (prabir.me)</author>
// <website>https://github.com/facebook-csharp-sdk/ReflectionUtils</website>
//-----------------------------------------------------------------------

#if SILVERLIGHT || WINDOWS_PHONE || NETFX_CORE
#undef REFLECTION_UTILS_REFLECTION_EMIT
#endif

#if NETFX_CORE
#define REFLECTION_UTILS_TYPEINFO
#endif

using System;
using System.Collections;
using System.Collections.Generic;
#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION
using System.Linq.Expressions;
#endif
using System.Globalization;
using System.Reflection;
#if REFLECTION_UTILS_REFLECTIONEMIT
using System.Reflection.Emit;
#endif

namespace ReflectionUtils
{
#if REFLECTION_UTILS_INTERNAL
    internal
#else
    public
#endif
 class ReflectionUtils
    {
        private static readonly object[] EmptyObjects = new object[] { };

        public static Attribute GetAttribute(MemberInfo info, Type type)
        {
#if NETFX_CORE
            if (info == null || type == null || !info.IsDefined(type))
                return null;
            return info.GetCustomAttribute(type);
#else
            if (info == null || type == null || !Attribute.IsDefined(info, type))
                return null;
            return Attribute.GetCustomAttribute(info, type);
#endif
        }

        public static Attribute GetAttribute(Type objectType, Type attributeType)
        {

#if NETFX_CORE
            if (objectType == null || attributeType == null || !objectType.GetTypeInfo().IsDefined(attributeType))
                return null;
            return objectType.GetTypeInfo().GetCustomAttribute(attributeType);
#else
            if (objectType == null || attributeType == null || !Attribute.IsDefined(objectType, attributeType))
                return null;
            return Attribute.GetCustomAttribute(objectType, attributeType);
#endif
        }

        public static bool IsTypeGenericeCollectionInterface(Type type)
        {
#if NETFX_CORE
            if (!type.GetTypeInfo().IsGenericType)
#else
            if (!type.IsGenericType)
#endif
                return false;

            Type genericDefinition = type.GetGenericTypeDefinition();

            return (genericDefinition == typeof(IList<>) || genericDefinition == typeof(ICollection<>) || genericDefinition == typeof(IEnumerable<>));
        }

        public static bool IsTypeDictionary(Type type)
        {
#if NETFX_CORE
            if (typeof(IDictionary<,>).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                return true;

            if (!type.GetTypeInfo().IsGenericType)
                return false;
#else
                if (typeof(IDictionary).IsAssignableFrom(type))
                    return true;

                if (!type.IsGenericType)
                    return false;
#endif
            Type genericDefinition = type.GetGenericTypeDefinition();
            return genericDefinition == typeof(IDictionary<,>);
        }

        public static bool IsNullableType(Type type)
        {
            return
#if NETFX_CORE
                type.GetTypeInfo().IsGenericType
#else
                type.IsGenericType
#endif
                && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static object ToNullableType(object obj, Type nullableType)
        {
            return obj == null ? null : Convert.ChangeType(obj, Nullable.GetUnderlyingType(nullableType), CultureInfo.InvariantCulture);
        }

        public static bool IsValueType(Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        public static IEnumerable<ConstructorInfo> GetConstructors(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredConstructors;
#else
            return type.GetConstructors();
#endif
        }

        public static ConstructorInfo GetConstructorInfo(Type type, params Type[] argsType)
        {
            IEnumerable<ConstructorInfo> constructorInfos = GetConstructors(type);
            int i;
            bool matches;
            foreach (ConstructorInfo constructorInfo in constructorInfos)
            {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (argsType.Length != parameters.Length)
                    continue;

                i = 0;
                matches = true;
                foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
                {
                    if (parameterInfo.ParameterType != argsType[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return constructorInfo;
            }

            return null;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredProperties;
#else
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static IEnumerable<FieldInfo> GetFields(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredFields;
#else
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static MethodInfo GetGetterMethodInfo(PropertyInfo propertyInfo)
        {
#if REFLECTION_UTILS_TYPEINFO
            return propertyInfo.GetMethod;
#else
            return propertyInfo.GetGetMethod(true);
#endif
        }

        public static MethodInfo GetSetterMethodInfo(PropertyInfo propertyInfo)
        {
#if NETFX_CORE
            return propertyInfo.SetMethod;
#else
            return propertyInfo.GetSetMethod(true);
#endif
        }

        public static ConstructorDelegate GetContructor(ConstructorInfo constructorInfo)
        {
            return delegate(object[] args) { return constructorInfo.Invoke(args); };
        }

        public static ConstructorDelegate GetConstructor(Type type, params  Type[] argsType)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            return constructorInfo == null ? null : GetContructor(constructorInfo);
        }

#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION

        public static ConstructorDelegate GetConstructorByExpression(ConstructorInfo constructorInfo)
        {
            ParameterInfo[] paramsInfo = constructorInfo.GetParameters();
            ParameterExpression param = Expression.Parameter(typeof(object[]), "args");
            Expression[] argsExp = new Expression[paramsInfo.Length];
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;
                Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                argsExp[i] = paramCastExp;
            }
            NewExpression newExp = Expression.New(constructorInfo, argsExp);
            Expression<Func<object[], object>> lambda = Expression.Lambda<Func<object[], object>>(newExp, param);
            Func<object[], object> compiledLambda = lambda.Compile();
            return delegate(object[] args) { return compiledLambda(args); };
        }

        public static ConstructorDelegate GetConstructorByExpression(Type type, params Type[] argsType)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            return constructorInfo == null ? null : GetConstructorByExpression(constructorInfo);
        }

#endif

#if REFLECTION_UTILS_REFLECTION_EMIT
        // todo: GetConstructorByReflectionEmit
#endif

        public static GetHandler GetGetMethod(PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo = GetGetterMethodInfo(propertyInfo);
            return delegate(object source) { return methodInfo.Invoke(source, EmptyObjects); };
        }

        public static GetHandler GetGetMethod(FieldInfo fieldInfo)
        {
            return delegate(object source) { return fieldInfo.GetValue(source); };
        }
    }
}

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 delegate object GetHandler(object source);

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 delegate void SetHandler(object source, object value);

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 delegate object ConstructorDelegate(params object[] args);

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 delegate void MemberMapLoader(Type type, SafeDictionary<string, CacheResolver.MemberMap> memberMaps);

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 class CacheResolver
{
    private static readonly Type[] EmptyTypes = new Type[0];

    private readonly MemberMapLoader _memberMapLoader;
    private readonly SafeDictionary<Type, SafeDictionary<string, MemberMap>> _memberMapsCache = new SafeDictionary<Type, SafeDictionary<string, MemberMap>>();

    delegate object CtorDelegate();
    readonly static SafeDictionary<Type, CtorDelegate> ConstructorCache = new SafeDictionary<Type, CtorDelegate>();

    public CacheResolver(MemberMapLoader memberMapLoader)
    {
        _memberMapLoader = memberMapLoader;
    }

    public static object GetNewInstance(Type type)
    {
        CtorDelegate c;
        if (ConstructorCache.TryGetValue(type, out c))
            return c();
#if REFLECTION_UTILS_REFLECTIONEMIT
                DynamicMethod dynamicMethod = new DynamicMethod("Create" + type.FullName, typeof(object), Type.EmptyTypes, type, true);
                dynamicMethod.InitLocals = true;
                ILGenerator generator = dynamicMethod.GetILGenerator();
                if (type.IsValueType)
                {
                    generator.DeclareLocal(type);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Box, type);
                }
                else
                {
                    ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (constructorInfo == null)
                        throw new Exception(string.Format("Could not get constructor for {0}.", type));
                    generator.Emit(OpCodes.Newobj, constructorInfo);
                }
                generator.Emit(OpCodes.Ret);
                c = (CtorDelegate)dynamicMethod.CreateDelegate(typeof(CtorDelegate));
                ConstructorCache.Add(type, c);
                return c();
#else
#if NETFX_CORE
        IEnumerable<ConstructorInfo> constructorInfos = type.GetTypeInfo().DeclaredConstructors;
        ConstructorInfo constructorInfo = null;
        foreach (ConstructorInfo item in constructorInfos) // FirstOrDefault()
        {
            if (item.GetParameters().Length == 0) // Default ctor - make sure it doesn't contain any parameters
            {
                constructorInfo = item;
                break;
            }
        }
#else
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, EmptyTypes, null);
#endif
        c = delegate { return constructorInfo.Invoke(null); };
        ConstructorCache.Add(type, c);
        return c();
#endif
    }

    public SafeDictionary<string, MemberMap> LoadMaps(Type type)
    {
        if (type == null || type == typeof(object))
            return null;
        SafeDictionary<string, MemberMap> maps;
        if (_memberMapsCache.TryGetValue(type, out maps))
            return maps;
        maps = new SafeDictionary<string, MemberMap>();
        _memberMapLoader(type, maps);
        _memberMapsCache.Add(type, maps);
        return maps;
    }

#if REFLECTION_UTILS_REFLECTIONEMIT
            static DynamicMethod CreateDynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner)
            {
                DynamicMethod dynamicMethod = !owner.IsInterface
                  ? new DynamicMethod(name, returnType, parameterTypes, owner, true)
                  : new DynamicMethod(name, returnType, parameterTypes, (Module)null, true);

                return dynamicMethod;
            }
#endif

    static GetHandler CreateGetHandler(FieldInfo fieldInfo)
    {
#if REFLECTION_UTILS_REFLECTIONEMIT
                Type type = fieldInfo.FieldType;
                DynamicMethod dynamicGet = CreateDynamicMethod("Get" + fieldInfo.Name, fieldInfo.DeclaringType, new Type[] { typeof(object) }, fieldInfo.DeclaringType);
                ILGenerator getGenerator = dynamicGet.GetILGenerator();

                getGenerator.Emit(OpCodes.Ldarg_0);
                getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                if (type.IsValueType)
                    getGenerator.Emit(OpCodes.Box, type);
                getGenerator.Emit(OpCodes.Ret);

                return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
#else
        return delegate(object instance) { return fieldInfo.GetValue(instance); };
#endif
    }

    static SetHandler CreateSetHandler(FieldInfo fieldInfo)
    {
        if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
            return null;
#if REFLECTION_UTILS_REFLECTIONEMIT
                Type type = fieldInfo.FieldType;
                DynamicMethod dynamicSet = CreateDynamicMethod("Set" + fieldInfo.Name, null, new Type[] { typeof(object), typeof(object) }, fieldInfo.DeclaringType);
                ILGenerator setGenerator = dynamicSet.GetILGenerator();

                setGenerator.Emit(OpCodes.Ldarg_0);
                setGenerator.Emit(OpCodes.Ldarg_1);
                if (type.IsValueType)
                    setGenerator.Emit(OpCodes.Unbox_Any, type);
                setGenerator.Emit(OpCodes.Stfld, fieldInfo);
                setGenerator.Emit(OpCodes.Ret);

                return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
#else
        return delegate(object instance, object value) { fieldInfo.SetValue(instance, value); };
#endif
    }

    static GetHandler CreateGetHandler(PropertyInfo propertyInfo)
    {
#if NETFX_CORE
        MethodInfo getMethodInfo = propertyInfo.GetMethod;
#else
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
#endif
        if (getMethodInfo == null)
            return null;
#if REFLECTION_UTILS_REFLECTIONEMIT
                Type type = propertyInfo.PropertyType;
                DynamicMethod dynamicGet = CreateDynamicMethod("Get" + propertyInfo.Name, propertyInfo.DeclaringType, new Type[] { typeof(object) }, propertyInfo.DeclaringType);
                ILGenerator getGenerator = dynamicGet.GetILGenerator();

                getGenerator.Emit(OpCodes.Ldarg_0);
                getGenerator.Emit(OpCodes.Call, getMethodInfo);
                if (type.IsValueType)
                    getGenerator.Emit(OpCodes.Box, type);
                getGenerator.Emit(OpCodes.Ret);

                return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
#else
#if NETFX_CORE
        return delegate(object instance) { return getMethodInfo.Invoke(instance, new Type[] { }); };
#else
            return delegate(object instance) { return getMethodInfo.Invoke(instance, EmptyTypes); };
#endif
#endif
    }

    static SetHandler CreateSetHandler(PropertyInfo propertyInfo)
    {
#if NETFX_CORE
        MethodInfo setMethodInfo = propertyInfo.SetMethod;
#else
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
#endif
        if (setMethodInfo == null)
            return null;
#if REFLECTION_UTILS_REFLECTIONEMIT
                Type type = propertyInfo.PropertyType;
                DynamicMethod dynamicSet = CreateDynamicMethod("Set" + propertyInfo.Name, null, new Type[] { typeof(object), typeof(object) }, propertyInfo.DeclaringType);
                ILGenerator setGenerator = dynamicSet.GetILGenerator();

                setGenerator.Emit(OpCodes.Ldarg_0);
                setGenerator.Emit(OpCodes.Ldarg_1);
                if (type.IsValueType)
                    setGenerator.Emit(OpCodes.Unbox_Any, type);
                setGenerator.Emit(OpCodes.Call, setMethodInfo);
                setGenerator.Emit(OpCodes.Ret);
                return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
#else
        return delegate(object instance, object value) { setMethodInfo.Invoke(instance, new object[] { value }); };
#endif
    }

#if REFLECTION_UTILS_INTERNAL
    internal
#else
    public
#endif
 sealed class MemberMap
    {
        public readonly MemberInfo MemberInfo;
        public readonly Type Type;
        public readonly GetHandler Getter;
        public readonly SetHandler Setter;

        public MemberMap(PropertyInfo propertyInfo)
        {
            MemberInfo = propertyInfo;
            Type = propertyInfo.PropertyType;
            Getter = CreateGetHandler(propertyInfo);
            Setter = CreateSetHandler(propertyInfo);
        }

        public MemberMap(FieldInfo fieldInfo)
        {
            MemberInfo = fieldInfo;
            Type = fieldInfo.FieldType;
            Getter = CreateGetHandler(fieldInfo);
            Setter = CreateSetHandler(fieldInfo);
        }
    }
}

#if REFLECTION_UTILS_INTERNAL
    internal
#else
public
#endif
 class SafeDictionary<TKey, TValue>
{
    private readonly object _padlock = new object();
    private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get { return _dictionary[key]; }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).GetEnumerator();
    }

    public void Add(TKey key, TValue value)
    {
        lock (_padlock)
        {
            if (_dictionary.ContainsKey(key) == false)
                _dictionary.Add(key, value);
        }
    }
}