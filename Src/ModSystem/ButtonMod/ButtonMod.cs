// Src/ModSystem/ButtonMod/Source/ButtonMod.cs
// 这是一个简化版的ButtonMod，展示如何在项目中使用

using ModSystem.Core;
using System;
using static ModSystem.Core.ReflectionHelper;

namespace ExampleMods
{
    /// <summary>
    /// 按钮模组 - 使用纯反射创建Unity UI
    /// </summary>
    public class ButtonMod : ModBase, IReloadable
    {
        // UI对象引用
        private object canvasObj;
        private object buttonObj;
        private object textObj;
        private object eventSystemObj;

        // 状态
        private int clickCount = 0;

        public override void OnInitialize(IModContext context)
        {
            base.OnInitialize(context);
            Context.Log("[ButtonMod] Initialized with reflection");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            CreateUI();
        }

        private void CreateUI()
        {
            try
            {
                // 1. 确保有EventSystem
                EnsureEventSystem();

                // 2. 创建Canvas
                CreateCanvas();

                // 3. 创建按钮
                CreateButton();

                // 4. 设置按钮点击事件
                SetupButtonClick();

                Context.Log("[ButtonMod] UI created successfully!");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in CreateUI: {ex.Message}");
            }
        }

        private void EnsureEventSystem()
        {
            try
            {
                var eventSystemType = FindType("EventSystem");
                if (eventSystemType == null)
                {
                    Context.LogError("[ButtonMod] EventSystem type not found!");
                    return;
                }

                var objectType = FindType("UnityEngine.Object");
                var findObjectsMethod = objectType.GetMethod("FindObjectsOfType", new Type[] { typeof(Type) });

                if (findObjectsMethod != null)
                {
                    var allEventSystems = findObjectsMethod.Invoke(null, new object[] { eventSystemType }) as Array;
                    if (allEventSystems != null && allEventSystems.Length > 0)
                    {
                        Context.Log("[ButtonMod] EventSystem already exists");
                        return;
                    }
                }

                Context.Log("[ButtonMod] Creating EventSystem...");
                eventSystemObj = CreateInstance("GameObject", "ModEventSystem");

                // 添加EventSystem组件
                AddComponent(eventSystemObj, eventSystemType);

                // 添加输入模块
                var inputModuleType = FindType("StandaloneInputModule");
                if (inputModuleType != null)
                {
                    AddComponent(eventSystemObj, inputModuleType);
                }

                Context.Log("[ButtonMod] EventSystem created");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in EnsureEventSystem: {ex.Message}");
            }
        }

        private void CreateCanvas()
        {
            Context.Log("[ButtonMod] Creating Canvas...");

            try
            {
                // 创建Canvas GameObject
                canvasObj = CreateInstance("GameObject", "ModCanvas");

                // 添加Canvas组件
                var canvasType = FindType("Canvas");
                var canvas = AddComponent(canvasObj, canvasType);

                // 设置渲染模式为ScreenSpaceOverlay
                SetProperty(canvas, "renderMode", 0);

                // 设置排序顺序
                SetProperty(canvas, "sortingOrder", 100);

                // 添加CanvasScaler
                var canvasScalerType = FindType("CanvasScaler");
                if (canvasScalerType != null)
                {
                    AddComponent(canvasObj, canvasScalerType);
                }

                // 添加GraphicRaycaster
                var graphicRaycasterType = FindType("GraphicRaycaster");
                if (graphicRaycasterType != null)
                {
                    AddComponent(canvasObj, graphicRaycasterType);
                }

                Context.Log("[ButtonMod] Canvas created successfully");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in CreateCanvas: {ex.Message}");
            }
        }

        private void CreateButton()
        {
            Context.Log("[ButtonMod] Creating Button...");

            try
            {
                // 创建按钮GameObject
                buttonObj = CreateInstance("GameObject", "MyButton");

                // 设置父对象为Canvas
                var buttonTransform = GetProperty(buttonObj, "transform");
                var canvasTransform = GetProperty(canvasObj, "transform");
                SetParent(buttonTransform, canvasTransform, false);

                // 添加RectTransform（如果还没有）
                var rectTransformType = FindType("RectTransform");
                var rectTransform = GetComponent(buttonObj, rectTransformType);
                if (rectTransform == null)
                {
                    rectTransform = AddComponent(buttonObj, rectTransformType);
                }

                // 设置位置和大小
                var vector2Type = FindType("Vector2");
                if (vector2Type != null && rectTransform != null)
                {
                    // 设置锚点到中心
                    var half = Activator.CreateInstance(vector2Type, 0.5f, 0.5f);
                    SetProperty(rectTransform, "anchorMin", half);
                    SetProperty(rectTransform, "anchorMax", half);
                    SetProperty(rectTransform, "pivot", half);

                    // 设置大小
                    var sizeDelta = Activator.CreateInstance(vector2Type, 200f, 60f);
                    SetProperty(rectTransform, "sizeDelta", sizeDelta);

                    // 设置位置（屏幕中心）
                    var position = Activator.CreateInstance(vector2Type, 0f, 0f);
                    SetProperty(rectTransform, "anchoredPosition", position);
                }

                // 添加Image组件作为背景
                var imageType = FindType("Image");
                if (imageType != null)
                {
                    var image = AddComponent(buttonObj, imageType);

                    // 设置初始颜色
                    var colorType = FindType("Color");
                    if (colorType != null && image != null)
                    {
                        var whiteColor = Activator.CreateInstance(colorType, 1f, 1f, 1f, 1f);
                        SetProperty(image, "color", whiteColor);
                    }
                }

                // 添加Button组件
                var buttonType = FindType("Button");
                if (buttonType != null)
                {
                    AddComponent(buttonObj, buttonType);
                }

                // 创建按钮文本
                CreateButtonText();

                Context.Log("[ButtonMod] Button created successfully");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error creating button: {ex.Message}");
            }
        }

        private void CreateButtonText()
        {
            Context.Log("[ButtonMod] Creating Button Text...");

            try
            {
                // 创建文本GameObject
                textObj = CreateInstance("GameObject", "Text");

                // 设置父对象为按钮
                var textTransform = GetProperty(textObj, "transform");
                var buttonTransform = GetProperty(buttonObj, "transform");
                SetParent(textTransform, buttonTransform, false);

                // 添加RectTransform
                var rectTransformType = FindType("RectTransform");
                var textRect = AddComponent(textObj, rectTransformType);

                // 设置RectTransform充满父对象
                var vector2Type = FindType("Vector2");
                if (vector2Type != null && textRect != null)
                {
                    var zeroVector = Activator.CreateInstance(vector2Type, 0f, 0f);
                    var oneVector = Activator.CreateInstance(vector2Type, 1f, 1f);

                    SetProperty(textRect, "anchorMin", zeroVector);
                    SetProperty(textRect, "anchorMax", oneVector);
                    SetProperty(textRect, "sizeDelta", zeroVector);
                    SetProperty(textRect, "anchoredPosition", zeroVector);
                }

                // 添加Text组件
                var textType = FindType("Text");
                if (textType != null)
                {
                    var text = AddComponent(textObj, textType);

                    if (text != null)
                    {
                        // 设置文本内容
                        SetProperty(text, "text", "Click Me!");

                        // 设置文本对齐（居中）
                        SetProperty(text, "alignment", 4);

                        // 设置字体大小
                        SetProperty(text, "fontSize", 18);

                        // 设置文本颜色
                        var colorType = FindType("Color");
                        if (colorType != null)
                        {
                            var blackColor = Activator.CreateInstance(colorType, 0f, 0f, 0f, 1f);
                            SetProperty(text, "color", blackColor);
                        }
                    }
                }

                Context.Log("[ButtonMod] Button text created");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error creating button text: {ex.Message}");
            }
        }

        private void SetupButtonClick()
        {
            Context.Log("[ButtonMod] Setting up button click...");

            try
            {
                var buttonType = FindType("Button");
                var button = GetComponent(buttonObj, buttonType);

                if (button != null)
                {
                    // 获取onClick事件
                    var onClick = GetProperty(button, "onClick");
                    if (onClick != null)
                    {
                        // 创建UnityAction
                        var unityActionType = FindType("UnityEngine.Events.UnityAction");
                        if (unityActionType != null)
                        {
                            Action clickAction = OnButtonClick;
                            var unityAction = Delegate.CreateDelegate(unityActionType, this, clickAction.Method);
                            InvokeMethod(onClick, "AddListener", unityAction);
                            Context.Log("[ButtonMod] Button click handler added");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Failed to setup button click: {ex.Message}");
            }
        }

        public void OnButtonClick()
        {
            clickCount++;
            Context.Log($"[ButtonMod] Button clicked {clickCount} times!");

            // 更新按钮文本
            try
            {
                var textType = FindType("Text");
                var text = GetComponent(textObj, textType);

                if (text != null)
                {
                    SetProperty(text, "text", $"Clicked {clickCount} times!");
                }

                // 改变按钮颜色
                var imageType = FindType("Image");
                var image = GetComponent(buttonObj, imageType);

                if (image != null)
                {
                    var colorValues = new[]
                    {
                        new float[] {1f, 0f, 0f, 1f},    // Red
                        new float[] {0f, 1f, 0f, 1f},    // Green
                        new float[] {0f, 0f, 1f, 1f},    // Blue
                        new float[] {1f, 1f, 0f, 1f},    // Yellow
                    };

                    var colorData = colorValues[clickCount % colorValues.Length];
                    var colorType = FindType("Color");
                    var color = Activator.CreateInstance(colorType, colorData[0], colorData[1], colorData[2], colorData[3]);

                    SetProperty(image, "color", color);
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in OnButtonClick: {ex.Message}");
            }
        }

        public override void OnDisable()
        {
            Context.Log("[ButtonMod] OnDisable called");
            base.OnDisable();
        }

        public override void OnDestroy()
        {
            Context.Log("[ButtonMod] OnDestroy called - cleaning up UI");

            // 销毁创建的对象
            DestroyGameObject(canvasObj);
            DestroyGameObject(eventSystemObj);

            base.OnDestroy();
        }

        private void DestroyGameObject(object gameObject)
        {
            if (gameObject != null)
            {
                try
                {
                    var objectType = FindType("UnityEngine.Object");
                    var destroyMethod = objectType.GetMethod("Destroy", new Type[] { typeof(object) });
                    destroyMethod.Invoke(null, new object[] { gameObject });
                }
                catch (Exception ex)
                {
                    Context.LogError($"[ButtonMod] Error destroying object: {ex.Message}");
                }
            }
        }

        #region 辅助方法

        private object GetComponent(object gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null) return null;

            var getComponentMethod = gameObject.GetType().GetMethod("GetComponent", new Type[] { typeof(Type) });
            return getComponentMethod?.Invoke(gameObject, new object[] { componentType });
        }

        private void SetParent(object childTransform, object parentTransform, bool worldPositionStays = false)
        {
            if (childTransform == null || parentTransform == null) return;

            try
            {
                var transformType = childTransform.GetType();
                var setParentMethod = transformType.GetMethod("SetParent", new Type[] { transformType, typeof(bool) });
                if (setParentMethod != null)
                {
                    setParentMethod.Invoke(childTransform, new object[] { parentTransform, worldPositionStays });
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in SetParent: {ex.Message}");
            }
        }

        #endregion

        #region IReloadable 实现

        public void OnBeforeReload()
        {
            Context.Log("[ButtonMod] OnBeforeReload - saving state");
            // 可以在这里保存状态
        }

        public void OnAfterReload()
        {
            Context.Log("[ButtonMod] OnAfterReload - restoring state");
            // 可以在这里恢复状态
        }

        #endregion
    }
}