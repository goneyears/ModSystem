using System;
using ModSystem.Core.Reflection;

namespace ModSystem.Core.Unity
{
    /// <summary>
    /// Unity便捷访问助手 - 提供常用Unity操作的简化方法
    /// </summary>
    public static class UnityHelper
    {
        /// <summary>
        /// 创建立方体
        /// </summary>
        public static object CreateCube(string name = "Cube")
        {
            var primitiveType = ReflectionHelper.FindType("UnityEngine.PrimitiveType");
            if (primitiveType != null)
            {
                var cubeValue = Enum.Parse(primitiveType, "Cube");
                var cube = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "CreatePrimitive", cubeValue);
                if (cube != null && !string.IsNullOrEmpty(name))
                {
                    ReflectionHelper.SetProperty(cube, "name", name);
                }
                return cube;
            }
            return null;
        }

        /// <summary>
        /// 创建球体
        /// </summary>
        public static object CreateSphere(string name = "Sphere")
        {
            var primitiveType = ReflectionHelper.FindType("UnityEngine.PrimitiveType");
            if (primitiveType != null)
            {
                var sphereValue = Enum.Parse(primitiveType, "Sphere");
                var sphere = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "CreatePrimitive", sphereValue);
                if (sphere != null && !string.IsNullOrEmpty(name))
                {
                    ReflectionHelper.SetProperty(sphere, "name", name);
                }
                return sphere;
            }
            return null;
        }

        /// <summary>
        /// 创建平面
        /// </summary>
        public static object CreatePlane(string name = "Plane")
        {
            var primitiveType = ReflectionHelper.FindType("UnityEngine.PrimitiveType");
            if (primitiveType != null)
            {
                var planeValue = Enum.Parse(primitiveType, "Plane");
                var plane = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "CreatePrimitive", planeValue);
                if (plane != null && !string.IsNullOrEmpty(name))
                {
                    ReflectionHelper.SetProperty(plane, "name", name);
                }
                return plane;
            }
            return null;
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        public static void SetPosition(object gameObject, float x, float y, float z)
        {
            var transform = ReflectionHelper.GetProperty(gameObject, "transform");
            if (transform != null)
            {
                var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                var position = Activator.CreateInstance(vector3Type, x, y, z);
                ReflectionHelper.SetProperty(transform, "position", position);
            }
        }

        /// <summary>
        /// 设置旋转（欧拉角）
        /// </summary>
        public static void SetRotation(object gameObject, float x, float y, float z)
        {
            var transform = ReflectionHelper.GetProperty(gameObject, "transform");
            if (transform != null)
            {
                var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                var rotation = Activator.CreateInstance(vector3Type, x, y, z);
                ReflectionHelper.SetProperty(transform, "eulerAngles", rotation);
            }
        }

        /// <summary>
        /// 设置缩放
        /// </summary>
        public static void SetScale(object gameObject, float x, float y, float z)
        {
            var transform = ReflectionHelper.GetProperty(gameObject, "transform");
            if (transform != null)
            {
                var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                var scale = Activator.CreateInstance(vector3Type, x, y, z);
                ReflectionHelper.SetProperty(transform, "localScale", scale);
            }
        }

        /// <summary>
        /// 旋转对象（增量旋转）
        /// </summary>
        public static void Rotate(object gameObject, float x, float y, float z)
        {
            var transform = ReflectionHelper.GetProperty(gameObject, "transform");
            if (transform != null)
            {
                var vector3Type = ReflectionHelper.FindType("UnityEngine.Vector3");
                var rotation = Activator.CreateInstance(vector3Type, x, y, z);
                ReflectionHelper.InvokeMethod(transform, "Rotate", rotation);
            }
        }

        /// <summary>
        /// 设置材质颜色
        /// </summary>
        public static void SetColor(object gameObject, float r, float g, float b, float a = 1.0f)
        {
            var renderer = ReflectionHelper.GetComponent(gameObject, "UnityEngine.MeshRenderer");
            if (renderer != null)
            {
                var material = ReflectionHelper.GetProperty(renderer, "material");
                if (material != null)
                {
                    var colorType = ReflectionHelper.FindType("UnityEngine.Color");
                    var color = Activator.CreateInstance(colorType, r, g, b, a);
                    ReflectionHelper.SetProperty(material, "color", color);
                }
            }
        }

        /// <summary>
        /// 创建点光源
        /// </summary>
        public static object CreatePointLight(string name = "PointLight", float intensity = 1.0f, float range = 10.0f)
        {
            var lightGO = ReflectionHelper.CreateGameObject(name);
            if (lightGO != null)
            {
                var light = ReflectionHelper.AddComponent(lightGO, "UnityEngine.Light");
                if (light != null)
                {
                    var lightType = ReflectionHelper.FindType("UnityEngine.LightType");
                    if (lightType != null)
                    {
                        var pointLight = Enum.Parse(lightType, "Point");
                        ReflectionHelper.SetProperty(light, "type", pointLight);
                    }
                    ReflectionHelper.SetProperty(light, "intensity", intensity);
                    ReflectionHelper.SetProperty(light, "range", range);
                }
                return lightGO;
            }
            return null;
        }

        /// <summary>
        /// 创建方向光
        /// </summary>
        public static object CreateDirectionalLight(string name = "DirectionalLight", float intensity = 1.0f)
        {
            var lightGO = ReflectionHelper.CreateGameObject(name);
            if (lightGO != null)
            {
                var light = ReflectionHelper.AddComponent(lightGO, "UnityEngine.Light");
                if (light != null)
                {
                    var lightType = ReflectionHelper.FindType("UnityEngine.LightType");
                    if (lightType != null)
                    {
                        var directionalLight = Enum.Parse(lightType, "Directional");
                        ReflectionHelper.SetProperty(light, "type", directionalLight);
                    }
                    ReflectionHelper.SetProperty(light, "intensity", intensity);
                }
                return lightGO;
            }
            return null;
        }

        /// <summary>
        /// 创建相机
        /// </summary>
        public static object CreateCamera(string name = "Camera")
        {
            var cameraGO = ReflectionHelper.CreateGameObject(name);
            if (cameraGO != null)
            {
                ReflectionHelper.AddComponent(cameraGO, "UnityEngine.Camera");
                ReflectionHelper.AddComponent(cameraGO, "UnityEngine.AudioListener");
                return cameraGO;
            }
            return null;
        }

        /// <summary>
        /// 创建UI画布
        /// </summary>
        public static object CreateCanvas(string name = "Canvas")
        {
            var canvasGO = ReflectionHelper.CreateGameObject(name);
            if (canvasGO != null)
            {
                var canvas = ReflectionHelper.AddComponent(canvasGO, "UnityEngine.Canvas");
                if (canvas != null)
                {
                    // 设置为屏幕空间覆盖模式
                    var renderMode = ReflectionHelper.FindType("UnityEngine.RenderMode");
                    if (renderMode != null)
                    {
                        var screenSpaceOverlay = Enum.Parse(renderMode, "ScreenSpaceOverlay");
                        ReflectionHelper.SetProperty(canvas, "renderMode", screenSpaceOverlay);
                    }
                }

                ReflectionHelper.AddComponent(canvasGO, "UnityEngine.UI.CanvasScaler");
                ReflectionHelper.AddComponent(canvasGO, "UnityEngine.UI.GraphicRaycaster");

                return canvasGO;
            }
            return null;
        }

        /// <summary>
        /// 创建文本UI
        /// </summary>
        public static object CreateText(object parent, string text, float x = 0, float y = 0)
        {
            var textGO = ReflectionHelper.CreateGameObject("Text");
            if (textGO != null && parent != null)
            {
                // 设置父对象
                var transform = ReflectionHelper.GetProperty(textGO, "transform");
                if (transform != null)
                {
                    ReflectionHelper.SetProperty(transform, "parent", ReflectionHelper.GetProperty(parent, "transform"));
                }

                // 添加RectTransform和Text组件
                var rectTransform = ReflectionHelper.GetComponent(textGO, "UnityEngine.RectTransform");
                if (rectTransform != null)
                {
                    var vector2Type = ReflectionHelper.FindType("UnityEngine.Vector2");
                    var anchoredPosition = Activator.CreateInstance(vector2Type, x, y);
                    ReflectionHelper.SetProperty(rectTransform, "anchoredPosition", anchoredPosition);
                }

                var textComponent = ReflectionHelper.AddComponent(textGO, "UnityEngine.UI.Text");
                if (textComponent != null)
                {
                    ReflectionHelper.SetProperty(textComponent, "text", text);

                    // 设置默认字体
                    var font = ReflectionHelper.InvokeStatic("UnityEngine.Font", "CreateDynamicFontFromOSFont", "Arial", 14);
                    if (font != null)
                    {
                        ReflectionHelper.SetProperty(textComponent, "font", font);
                    }
                }

                return textGO;
            }
            return null;
        }

        /// <summary>
        /// 查找带标签的对象
        /// </summary>
        public static object FindObjectWithTag(string tag)
        {
            return ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "FindWithTag", tag);
        }

        /// <summary>
        /// 查找所有带标签的对象
        /// </summary>
        public static object[] FindObjectsWithTag(string tag)
        {
            var result = ReflectionHelper.InvokeStatic("UnityEngine.GameObject", "FindGameObjectsWithTag", tag);
            return result as object[];
        }
    }
}