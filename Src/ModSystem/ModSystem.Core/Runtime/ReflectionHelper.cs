using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModSystem.Core
{
    /// <summary>
    /// 反射辅助工具类 - 帮助模组使用反射访问Unity API
    /// </summary>
    public static class ReflectionHelper
    {
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// 查找类型（支持Unity类型的智能查找）
        /// </summary>
        public static Type FindType(string typeName)
        {
            if (typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            Type type = null;

            // 1. 尝试直接获取（包含程序集限定名的情况）
            type = Type.GetType(typeName);

            // 2. 搜索所有已加载的程序集
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }

            // 3. 尝试常见的Unity命名空间
            if (type == null)
            {
                var unityAssemblies = new Dictionary<string, string[]>
                {
                    { "UnityEngine", new[] { "UnityEngine", "UnityEngine.CoreModule" } },
                    { "UnityEngine.UI", new[] { "UnityEngine.UI", "UnityEngine.UIModule" } },
                    { "UnityEngine.Events", new[] { "UnityEngine", "UnityEngine.CoreModule" } },
                    { "UnityEngine.EventSystems", new[] { "UnityEngine.UI", "UnityEngine.UIModule" } }
                };

                foreach (var kvp in unityAssemblies)
                {
                    var ns = kvp.Key;
                    var assemblies = kvp.Value;

                    foreach (var asm in assemblies)
                    {
                        // 尝试完整类型名
                        type = Type.GetType($"{ns}.{typeName}, {asm}");
                        if (type != null) break;
                    }

                    if (type != null) break;
                }
            }

            if (type != null)
                typeCache[typeName] = type;

            return type;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        public static object CreateInstance(string typeName, params object[] args)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                throw new TypeLoadException($"Cannot find type: {typeName}");
            }

            try
            {
                if (args == null || args.Length == 0)
                    return Activator.CreateInstance(type);
                else
                    return Activator.CreateInstance(type, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create instance of {typeName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 调用静态方法
        /// </summary>
        public static object InvokeStatic(string typeName, string methodName, params object[] args)
        {
            var type = FindType(typeName);
            if (type == null) return null;

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                // 尝试根据参数类型查找
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == methodName)
                    .ToArray();

                if (methods.Length == 1)
                {
                    method = methods[0];
                }
                else if (methods.Length > 1 && args != null)
                {
                    // 尝试匹配参数
                    method = methods.FirstOrDefault(m =>
                        m.GetParameters().Length == args.Length);
                }
            }

            return method?.Invoke(null, args);
        }

        /// <summary>
        /// 调用实例方法
        /// </summary>
        public static object InvokeMethod(object obj, string methodName, params object[] args)
        {
            if (obj == null) return null;

            var type = obj.GetType();
            var key = $"{type.FullName}.{methodName}";

            MethodInfo method = null;

            if (!methodCache.TryGetValue(key, out method))
            {
                // 特殊处理 GameObject.AddComponent
                if (methodName == "AddComponent" && args != null && args.Length == 1 && args[0] is Type)
                {
                    // 查找 AddComponent(Type) 重载
                    method = type.GetMethod(methodName, new Type[] { typeof(Type) });
                }
                else
                {
                    // 尝试直接获取
                    method = type.GetMethod(methodName);

                    if (method == null && args != null)
                    {
                        // 根据参数类型查找
                        var argTypes = args.Select(a => a?.GetType() ?? typeof(object)).ToArray();
                        method = type.GetMethod(methodName, argTypes);
                    }

                    if (method == null)
                    {
                        // 尝试查找所有同名方法
                        var methods = type.GetMethods()
                            .Where(m => m.Name == methodName)
                            .ToArray();

                        if (methods.Length == 1)
                        {
                            method = methods[0];
                        }
                        else if (methods.Length > 1 && args != null)
                        {
                            // 按参数数量匹配
                            method = methods.FirstOrDefault(m =>
                                m.GetParameters().Length == args.Length);
                        }
                    }
                }

                if (method != null)
                    methodCache[key] = method;
            }

            try
            {
                return method?.Invoke(obj, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke {methodName} on {type.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        public static object GetProperty(object obj, string propertyName)
        {
            if (obj == null) return null;
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        public static void SetProperty(object obj, string propertyName, object value)
        {
            if (obj == null) return;
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        public static object GetField(object obj, string fieldName)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(fieldName);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// 设置字段值
        /// </summary>
        public static void SetField(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(fieldName);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// 添加组件（GameObject专用）
        /// </summary>
        public static object AddComponent(object gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null) return null;

            var method = gameObject.GetType().GetMethod("AddComponent", new Type[] { typeof(Type) });
            return method?.Invoke(gameObject, new object[] { componentType });
        }

        /// <summary>
        /// 获取组件（GameObject专用）
        /// </summary>
        public static object GetComponent(object gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null) return null;

            var method = gameObject.GetType().GetMethod("GetComponent", new Type[] { typeof(Type) });
            return method?.Invoke(gameObject, new object[] { componentType });
        }
    }
}