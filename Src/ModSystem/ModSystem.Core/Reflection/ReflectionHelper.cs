using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModSystem.Core.Reflection
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

                        // 如果typeName已经包含命名空间，直接尝试
                        if (typeName.Contains("."))
                        {
                            type = Type.GetType($"{typeName}, {asm}");
                            if (type != null) break;
                        }
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

            // 对于缓存，需要包含参数类型信息
            var argTypesStr = args == null ? "void" : string.Join(",", args.Select(a => a?.GetType().Name ?? "null"));
            var key = $"{type.FullName}.{methodName}({argTypesStr})";

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
                    // 获取所有同名方法
                    var methods = type.GetMethods()
                        .Where(m => m.Name == methodName)
                        .ToArray();

                    if (methods.Length == 0)
                    {
                        throw new Exception($"Method {methodName} not found on type {type.Name}");
                    }
                    else if (methods.Length == 1)
                    {
                        method = methods[0];
                    }
                    else
                    {
                        // 多个重载，需要根据参数匹配
                        if (args == null || args.Length == 0)
                        {
                            // 无参数方法
                            method = methods.FirstOrDefault(m => m.GetParameters().Length == 0);
                        }
                        else
                        {
                            // 首先尝试精确匹配参数类型
                            var argTypes = args.Select(a => a?.GetType() ?? typeof(object)).ToArray();
                            method = type.GetMethod(methodName, argTypes);

                            if (method == null)
                            {
                                // 尝试找到最佳匹配
                                var candidates = methods.Where(m => m.GetParameters().Length == args.Length).ToList();

                                if (candidates.Count == 1)
                                {
                                    method = candidates[0];
                                }
                                else if (candidates.Count > 1)
                                {
                                    // 评分系统：找到最匹配的方法
                                    method = FindBestMethodMatch(candidates, args);
                                }
                            }
                        }
                    }
                }

                if (method != null)
                    methodCache[key] = method;
            }

            if (method == null)
            {
                throw new Exception($"Could not find suitable method {methodName} on type {type.Name} with {args?.Length ?? 0} arguments");
            }

            try
            {
                return method.Invoke(obj, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke {methodName} on {type.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 找到最匹配的方法
        /// </summary>
        private static MethodInfo FindBestMethodMatch(List<MethodInfo> candidates, object[] args)
        {
            MethodInfo bestMatch = null;
            int bestScore = -1;

            foreach (var candidate in candidates)
            {
                var parameters = candidate.GetParameters();
                int score = 0;

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                    {
                        // null可以匹配任何引用类型
                        if (!parameters[i].ParameterType.IsValueType)
                            score += 1;
                    }
                    else
                    {
                        var argType = args[i].GetType();
                        var paramType = parameters[i].ParameterType;

                        if (paramType == argType)
                        {
                            score += 3; // 精确匹配得分最高
                        }
                        else if (paramType.IsAssignableFrom(argType))
                        {
                            score += 2; // 可以赋值
                        }
                        else if (IsNumericMatch(argType, paramType))
                        {
                            score += 1; // 数值类型可以转换
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = candidate;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// 检查是否是兼容的数值类型
        /// </summary>
        private static bool IsNumericMatch(Type from, Type to)
        {
            // 常见的数值类型转换
            var numericTypes = new[] { typeof(int), typeof(float), typeof(double), typeof(long), typeof(short), typeof(byte) };
            return numericTypes.Contains(from) && numericTypes.Contains(to);
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        public static object GetProperty(object obj, string propertyName)
        {
            if (obj == null) return null;

            var type = obj.GetType();

            // 支持嵌套属性 (如 "transform.position")
            if (propertyName.Contains("."))
            {
                var parts = propertyName.Split('.');
                object current = obj;

                foreach (var part in parts)
                {
                    if (current == null) return null;
                    current = GetPropertyOrField(current, part);
                }

                return current;
            }

            return GetPropertyOrField(obj, propertyName);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        public static void SetProperty(object obj, string propertyName, object value)
        {
            if (obj == null) return;

            // 支持嵌套属性
            if (propertyName.Contains("."))
            {
                var parts = propertyName.Split('.');
                object current = obj;

                // 导航到最后一个属性的父对象
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (current == null) return;
                    current = GetPropertyOrField(current, parts[i]);
                }

                if (current != null)
                {
                    SetPropertyOrField(current, parts[parts.Length - 1], value);
                }
            }
            else
            {
                SetPropertyOrField(obj, propertyName, value);
            }
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
        /// 添加组件（GameObject专用，使用类型名）
        /// </summary>
        public static object AddComponent(object gameObject, string componentTypeName)
        {
            var componentType = FindType(componentTypeName);
            return AddComponent(gameObject, componentType);
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

        /// <summary>
        /// 获取组件（GameObject专用，使用类型名）
        /// </summary>
        public static object GetComponent(object gameObject, string componentTypeName)
        {
            var componentType = FindType(componentTypeName);
            return GetComponent(gameObject, componentType);
        }

        /// <summary>
        /// 创建GameObject
        /// </summary>
        public static object CreateGameObject(string name = "GameObject")
        {
            return CreateInstance("UnityEngine.GameObject", name);
        }

        /// <summary>
        /// 查找GameObject
        /// </summary>
        public static object FindGameObject(string name)
        {
            return InvokeStatic("UnityEngine.GameObject", "Find", name);
        }

        /// <summary>
        /// 销毁Unity对象
        /// </summary>
        public static void Destroy(object obj)
        {
            if (obj == null) return;

            try
            {
                // 获取UnityEngine.Object类型
                var objectType = FindType("UnityEngine.Object");
                if (objectType != null)
                {
                    // 明确指定调用 Destroy(UnityEngine.Object) 重载
                    var destroyMethod = objectType.GetMethod("Destroy", new Type[] { objectType });
                    if (destroyMethod != null)
                    {
                        destroyMethod.Invoke(null, new[] { obj });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to destroy object: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 立即销毁Unity对象
        /// </summary>
        public static void DestroyImmediate(object obj)
        {
            if (obj == null) return;

            try
            {
                // 获取UnityEngine.Object类型
                var objectType = FindType("UnityEngine.Object");
                if (objectType != null)
                {
                    // 明确指定调用 DestroyImmediate(UnityEngine.Object) 重载
                    var destroyMethod = objectType.GetMethod("DestroyImmediate", new Type[] { objectType });
                    if (destroyMethod != null)
                    {
                        destroyMethod.Invoke(null, new[] { obj });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to destroy object immediately: {ex.Message}", ex);
            }
        }

        // 辅助方法
        private static object GetPropertyOrField(object obj, string name)
        {
            var type = obj.GetType();

            // 先尝试属性
            var property = type.GetProperty(name);
            if (property != null && property.CanRead)
                return property.GetValue(obj);

            // 再尝试字段
            var field = type.GetField(name);
            if (field != null)
                return field.GetValue(obj);

            return null;
        }

        private static void SetPropertyOrField(object obj, string name, object value)
        {
            var type = obj.GetType();

            // 先尝试属性
            var property = type.GetProperty(name);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, ConvertValue(value, property.PropertyType));
                return;
            }

            // 再尝试字段
            var field = type.GetField(name);
            if (field != null)
            {
                field.SetValue(obj, ConvertValue(value, field.FieldType));
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            // 处理Unity特殊类型
            if (targetType.Name == "Vector3" && value is float[] floatArray && floatArray.Length >= 3)
            {
                var vector3Type = FindType("UnityEngine.Vector3");
                if (vector3Type != null)
                {
                    return Activator.CreateInstance(vector3Type, floatArray[0], floatArray[1], floatArray[2]);
                }
            }

            if (targetType.Name == "Color" && value is float[] colorArray && colorArray.Length >= 3)
            {
                var colorType = FindType("UnityEngine.Color");
                if (colorType != null)
                {
                    var a = colorArray.Length > 3 ? colorArray[3] : 1f;
                    return Activator.CreateInstance(colorType, colorArray[0], colorArray[1], colorArray[2], a);
                }
            }

            // 尝试常规转换
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }
    }
}