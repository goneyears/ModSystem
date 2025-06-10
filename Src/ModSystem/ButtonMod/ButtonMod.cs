// ===================================================================
// ButtonMod.cs - 纯反射版本
// 不依赖Unity DLL，通过反射创建所有UI
// ===================================================================

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
        private object modRootObject; // 模组根对象

        // 状态
        private int clickCount = 0;

        public override void OnInitialize(IModContext context)
        {
            base.OnInitialize(context);
            Context.Log("[ButtonMod] Initialized with reflection");

            // 获取模组的GameObject作为父对象
            modRootObject = GetModRootObject();
        }

        // 获取模组的根GameObject
        private object GetModRootObject()
        {
            try
            {
                // 方法1：通过当前脚本所在的GameObject向上查找
                // 这假设ButtonMod实例与某个Unity组件有关联
                var currentType = this.GetType();

                // 方法2：通过场景查找
                var gameObjectType = FindType("GameObject");

                // 首先尝试通过Find查找完整路径
                var findMethod = gameObjectType.GetMethod("Find", new Type[] { typeof(string) });
                if (findMethod != null)
                {
                    // 尝试不同的路径格式
                    string[] possiblePaths = new[]
                    {
                        $"GameObject/Mods/Mod_{Context.ModId}_v1",
                        $"GameObject/Mods/Mod_{Context.ModId}_v2",
                        $"GameObject/Mods/Mod_{Context.ModId}_v3",
                        $"Mods/Mod_{Context.ModId}_v1",
                        $"Mods/Mod_{Context.ModId}_v2",
                        $"Mods/Mod_{Context.ModId}_v3",
                        $"Mod_{Context.ModId}_v1",
                        $"Mod_{Context.ModId}_v2",
                        $"Mod_{Context.ModId}_v3"
                    };

                    foreach (var path in possiblePaths)
                    {
                        var modObj = findMethod.Invoke(null, new object[] { path });
                        if (modObj != null)
                        {
                            Context.Log($"[ButtonMod] Found mod root object at: {path}");
                            return modObj;
                        }
                    }
                }

                // 方法3：通过FindObjectsOfType查找所有GameObject，然后筛选
                var objectType = FindType("UnityEngine.Object");
                var findObjectsMethod = objectType.GetMethod("FindObjectsOfType", new Type[] { typeof(Type) });
                if (findObjectsMethod != null)
                {
                    var allGameObjects = findObjectsMethod.Invoke(null, new object[] { gameObjectType }) as Array;
                    if (allGameObjects != null)
                    {
                        foreach (var go in allGameObjects)
                        {
                            var name = GetProperty(go, "name") as string;
                            if (name != null && name.Contains($"Mod_{Context.ModId}"))
                            {
                                Context.Log($"[ButtonMod] Found mod root object by searching: {name}");
                                return go;
                            }
                        }
                    }
                }

                Context.LogError("[ButtonMod] Could not find mod root object, UI will be created at scene root");
                return null;
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error getting mod root object: {ex.Message}");
                return null;
            }
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

                // 5. 验证UI元素
                VerifyUIElements();

                Context.Log("[ButtonMod] UI created successfully!");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in CreateUI: {ex.Message}");
                // 不重新抛出，让模组继续运行
            }
        }

        private void VerifyUIElements()
        {
            try
            {
                Context.Log("[ButtonMod] Verifying UI elements...");

                // 检查模组根对象
                if (modRootObject != null)
                {
                    var modRootName = GetProperty(modRootObject, "name");
                    Context.Log($"[ButtonMod] Mod root object: {modRootName}");
                }

                // 检查EventSystem的父对象
                if (eventSystemObj != null)
                {
                    var transform = GetProperty(eventSystemObj, "transform");
                    var parent = GetProperty(transform, "parent");
                    if (parent != null)
                    {
                        var parentGO = GetProperty(parent, "gameObject");
                        var parentName = GetProperty(parentGO, "name");
                        Context.Log($"[ButtonMod] EventSystem parent: {parentName}");
                    }
                    else
                    {
                        Context.Log("[ButtonMod] EventSystem has no parent (at scene root)");
                    }
                }

                // 检查Canvas的父对象
                if (canvasObj != null)
                {
                    var transform = GetProperty(canvasObj, "transform");
                    var parent = GetProperty(transform, "parent");
                    if (parent != null)
                    {
                        var parentGO = GetProperty(parent, "gameObject");
                        var parentName = GetProperty(parentGO, "name");
                        Context.Log($"[ButtonMod] Canvas parent: {parentName}");
                    }
                    else
                    {
                        Context.Log("[ButtonMod] Canvas has no parent (at scene root)");
                    }

                    var canvasType = FindType("Canvas");
                    var canvas = GetComponent(canvasObj, canvasType);
                    if (canvas != null)
                    {
                        var renderMode = GetProperty(canvas, "renderMode");
                        Context.Log($"[ButtonMod] Canvas render mode: {renderMode}");
                    }
                }

                // 检查按钮是否在正确的位置
                if (buttonObj != null)
                {
                    var transform = GetProperty(buttonObj, "transform");
                    var parent = GetProperty(transform, "parent");
                    if (parent != null)
                    {
                        var parentGO = GetProperty(parent, "gameObject");
                        var parentName = GetProperty(parentGO, "name");
                        Context.Log($"[ButtonMod] Button parent: {parentName}");
                    }

                    // 检查按钮是否激活
                    var activeInHierarchy = GetProperty(buttonObj, "activeInHierarchy");
                    Context.Log($"[ButtonMod] Button active: {activeInHierarchy}");

                    // 检查RectTransform
                    var rectTransformType = FindType("RectTransform");
                    var rectTransform = GetComponent(buttonObj, rectTransformType);
                    if (rectTransform != null)
                    {
                        var sizeDelta = GetProperty(rectTransform, "sizeDelta");
                        var position = GetProperty(rectTransform, "anchoredPosition");
                        Context.Log($"[ButtonMod] Button size: {sizeDelta}, position: {position}");
                    }
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error verifying UI: {ex.Message}");
            }
        }

        private void EnsureEventSystem()
        {
            try
            {
                // EventSystem在UnityEngine.EventSystems命名空间中
                var eventSystemType = FindType("EventSystem");
                if (eventSystemType == null)
                {
                    // 尝试直接从程序集中查找
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.FullName.Contains("UnityEngine.UI"))
                        {
                            eventSystemType = assembly.GetType("UnityEngine.EventSystems.EventSystem");
                            if (eventSystemType != null) break;
                        }
                    }
                }

                if (eventSystemType == null)
                {
                    Context.LogError("[ButtonMod] EventSystem type not found! Continuing without EventSystem...");
                    return;
                }

                var objectType = FindType("UnityEngine.Object");
                if (objectType == null)
                {
                    Context.LogError("[ButtonMod] UnityEngine.Object type not found!");
                    return;
                }

                // 尝试使用 FindObjectsOfType 而不是 FindObjectOfType
                // 因为在某些Unity版本中，FindObjectOfType(Type) 可能有问题
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
                else
                {
                    // 如果 FindObjectsOfType 不可用，尝试使用泛型版本
                    Context.Log("[ButtonMod] FindObjectsOfType not available, creating EventSystem anyway");
                }

                Context.Log("[ButtonMod] Creating EventSystem...");
                eventSystemObj = CreateInstance("GameObject", "ModEventSystem"); // 使用不同的名称以区分

                // 设置父对象为模组根对象
                if (modRootObject != null && eventSystemObj != null)
                {
                    var eventSystemTransform = GetProperty(eventSystemObj, "transform");
                    var modRootTransform = GetProperty(modRootObject, "transform");
                    SetParent(eventSystemTransform, modRootTransform, false);
                    Context.Log("[ButtonMod] EventSystem created under mod root");
                }
                else
                {
                    Context.Log("[ButtonMod] EventSystem created at scene root (mod root not found)");
                }

                // 添加EventSystem组件
                AddComponent(eventSystemObj, eventSystemType);

                // 添加输入模块 - StandaloneInputModule也在EventSystems命名空间中
                var inputModuleType = FindType("StandaloneInputModule");
                if (inputModuleType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.FullName.Contains("UnityEngine.UI"))
                        {
                            inputModuleType = assembly.GetType("UnityEngine.EventSystems.StandaloneInputModule");
                            if (inputModuleType != null) break;
                        }
                    }
                }

                if (inputModuleType != null)
                {
                    AddComponent(eventSystemObj, inputModuleType);
                }

                Context.Log("[ButtonMod] EventSystem created");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in EnsureEventSystem: {ex.Message}");
                // 继续执行，即使EventSystem创建失败
            }
        }

        private void CreateCanvas()
        {
            Context.Log("[ButtonMod] Creating Canvas...");

            try
            {
                // 创建Canvas GameObject
                canvasObj = CreateInstance("GameObject", "ModCanvas");
                if (canvasObj == null)
                {
                    Context.LogError("[ButtonMod] Failed to create Canvas GameObject");
                    return;
                }

                // 设置父对象为模组根对象
                if (modRootObject != null)
                {
                    var canvasTransform = GetProperty(canvasObj, "transform");
                    var modRootTransform = GetProperty(modRootObject, "transform");
                    SetParent(canvasTransform, modRootTransform, false);
                    Context.Log("[ButtonMod] Canvas created under mod root");
                }
                else
                {
                    Context.Log("[ButtonMod] Canvas created at scene root (mod root not found)");
                }

                // 添加Canvas组件
                var canvasType = FindType("Canvas");
                if (canvasType == null)
                {
                    Context.LogError("[ButtonMod] Canvas type not found!");
                    return;
                }

                var canvas = AddComponent(canvasObj, canvasType);
                if (canvas == null)
                {
                    Context.LogError("[ButtonMod] Failed to add Canvas component");
                    return;
                }

                // 设置渲染模式为ScreenSpaceOverlay
                SetProperty(canvas, "renderMode", 0);

                // 设置排序顺序，确保在最前面
                SetProperty(canvas, "sortingOrder", 100);

                // 添加CanvasScaler
                var canvasScalerType = FindType("CanvasScaler");
                if (canvasScalerType != null)
                {
                    AddComponent(canvasObj, canvasScalerType);
                }
                else
                {
                    Context.LogError("[ButtonMod] CanvasScaler type not found");
                }

                // 添加GraphicRaycaster
                var graphicRaycasterType = FindType("GraphicRaycaster");
                if (graphicRaycasterType != null)
                {
                    AddComponent(canvasObj, graphicRaycasterType);
                }
                else
                {
                    Context.LogError("[ButtonMod] GraphicRaycaster type not found");
                }

                Context.Log("[ButtonMod] Canvas created successfully");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in CreateCanvas: {ex.Message}");
                throw; // 重新抛出异常以便调试
            }
        }

        private void CreateButton()
        {
            Context.Log("[ButtonMod] Creating Button...");

            try
            {
                // 尝试使用DefaultControls创建按钮
                bool useDefaultControls = false;
                var defaultControlsType = FindType("DefaultControls");
                if (defaultControlsType != null)
                {
                    try
                    {
                        var resourcesType = defaultControlsType.GetNestedType("Resources");
                        if (resourcesType != null)
                        {
                            var resources = Activator.CreateInstance(resourcesType);
                            var createButtonMethod = defaultControlsType.GetMethod("CreateButton");
                            if (createButtonMethod != null)
                            {
                                buttonObj = createButtonMethod.Invoke(null, new object[] { resources });
                                if (buttonObj != null)
                                {
                                    Context.Log("[ButtonMod] Button created using DefaultControls");
                                    useDefaultControls = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Context.LogError($"[ButtonMod] DefaultControls failed: {ex.Message}");
                        buttonObj = null;
                    }
                }

                // 如果DefaultControls方法失败，手动创建
                if (buttonObj == null)
                {
                    Context.Log("[ButtonMod] Creating button manually");
                    buttonObj = CreateInstance("GameObject", "MyButton");
                    if (buttonObj == null)
                    {
                        Context.LogError("[ButtonMod] Failed to create button GameObject");
                        return;
                    }
                }

                // 设置父对象为Canvas - 这对两种创建方法都是必须的
                var buttonTransform = GetProperty(buttonObj, "transform");
                var canvasTransform = GetProperty(canvasObj, "transform");
                SetParent(buttonTransform, canvasTransform, false);

                // 获取RectTransform
                var rectTransformType = FindType("RectTransform");
                if (rectTransformType == null)
                {
                    Context.LogError("[ButtonMod] RectTransform type not found!");
                    return;
                }

                var rectTransform = GetComponent(buttonObj, rectTransformType);

                // 如果是手动创建的，需要添加必要的组件
                if (!useDefaultControls)
                {
                    if (rectTransform == null)
                    {
                        Context.Log("[ButtonMod] Adding RectTransform to button");
                        rectTransform = AddComponent(buttonObj, rectTransformType);
                    }

                    // 添加Image组件作为背景
                    var imageType = FindType("Image");
                    if (imageType != null)
                    {
                        var image = GetComponent(buttonObj, imageType);
                        if (image == null)
                        {
                            image = AddComponent(buttonObj, imageType);

                            // 设置初始颜色（白色）
                            var colorType = FindType("Color");
                            if (colorType != null && image != null)
                            {
                                var whiteColor = Activator.CreateInstance(colorType, 1f, 1f, 1f, 1f);
                                SetProperty(image, "color", whiteColor);
                            }
                        }
                    }

                    // 添加Button组件
                    var buttonType = FindType("Button");
                    if (buttonType != null)
                    {
                        var button = GetComponent(buttonObj, buttonType);
                        if (button == null)
                        {
                            button = AddComponent(buttonObj, buttonType);
                        }
                    }

                    // 手动创建需要添加文本
                    CreateButtonText();
                }

                // 设置位置和大小（对两种方法都适用）
                var vector2Type = FindType("Vector2");
                if (vector2Type != null && rectTransform != null)
                {
                    // 设置锚点到中心
                    var half = Activator.CreateInstance(vector2Type, 0.5f, 0.5f);
                    SetProperty(rectTransform, "anchorMin", half);
                    SetProperty(rectTransform, "anchorMax", half);
                    SetProperty(rectTransform, "pivot", half);

                    // 设置大小
                    var sizeDelta = Activator.CreateInstance(vector2Type, 200f, 150f);
                    SetProperty(rectTransform, "sizeDelta", sizeDelta);

                    // 设置位置（屏幕中心）
                    var position = Activator.CreateInstance(vector2Type, 0f, 0f);
                    SetProperty(rectTransform, "anchoredPosition", position);
                }

                Context.Log("[ButtonMod] Button created successfully");
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error creating button: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CreateButtonText()
        {
            Context.Log("[ButtonMod] Creating Button Text...");

            try
            {
                // 首先检查按钮是否已经有Text子对象（DefaultControls创建的按钮可能已经有了）
                var textType = FindType("Text");
                if (textType != null && buttonObj != null)
                {
                    // 获取按钮的所有子对象
                    var buttonTransform = GetProperty(buttonObj, "transform");
                    var childCount = GetProperty(buttonTransform, "childCount");
                    if (childCount != null && (int)childCount > 0)
                    {
                        // 查找现有的Text组件
                        var getChildMethod = buttonTransform.GetType().GetMethod("GetChild", new Type[] { typeof(int) });
                        if (getChildMethod != null)
                        {
                            for (int i = 0; i < (int)childCount; i++)
                            {
                                var childTransform = getChildMethod.Invoke(buttonTransform, new object[] { i });
                                if (childTransform != null)
                                {
                                    var childGO = GetProperty(childTransform, "gameObject");
                                    var existingText = GetComponent(childGO, textType);
                                    if (existingText != null)
                                    {
                                        Context.Log("[ButtonMod] Found existing text component, using it");
                                        textObj = childGO;

                                        // 更新现有文本的属性
                                        SetProperty(existingText, "text", "Click Me!");
                                        SetProperty(existingText, "fontSize", 18);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // 如果没有找到现有的Text组件，创建新的
                Context.Log("[ButtonMod] No existing text found, creating new one");

                // 创建文本GameObject
                textObj = CreateInstance("GameObject", "Text");
                if (textObj == null)
                {
                    Context.LogError("[ButtonMod] Failed to create text GameObject");
                    return;
                }

                // 设置父对象为按钮 - 使用辅助方法
                var textTransform = GetProperty(textObj, "transform");
                var parentTransform = GetProperty(buttonObj, "transform");
                SetParent(textTransform, parentTransform, false);

                // 获取或添加RectTransform
                var rectTransformType = FindType("RectTransform");
                if (rectTransformType == null)
                {
                    Context.LogError("[ButtonMod] RectTransform type not found!");
                    return;
                }

                var textRect = GetComponent(textObj, rectTransformType);
                if (textRect == null)
                {
                    textRect = AddComponent(textObj, rectTransformType);
                }

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
                if (textType != null)
                {
                    var text = AddComponent(textObj, textType);

                    if (text != null)
                    {
                        // 设置文本内容
                        SetProperty(text, "text", "Click Me!");

                        // 设置文本对齐（居中）
                        SetProperty(text, "alignment", 4); // TextAnchor.MiddleCenter

                        // 设置字体大小
                        SetProperty(text, "fontSize", 18);

                        // 设置文本颜色（黑色）
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
                if (buttonType == null)
                {
                    Context.LogError("[ButtonMod] Button type not found!");
                    return;
                }

                var button = GetComponent(buttonObj, buttonType);

                if (button == null)
                {
                    Context.LogError("[ButtonMod] Button component not found!");
                    return;
                }

                // 获取onClick事件
                var onClick = GetProperty(button, "onClick");
                if (onClick == null)
                {
                    Context.LogError("[ButtonMod] onClick property not found!");
                    return;
                }

                // 创建UnityAction - 使用更简单的方法
                try
                {
                    // 方法1：尝试直接使用 UnityAction
                    var unityActionType = FindType("UnityEngine.Events.UnityAction");
                    if (unityActionType != null)
                    {
                        Action clickAction = OnButtonClick;
                        var unityAction = Delegate.CreateDelegate(unityActionType, this, clickAction.Method);
                        InvokeMethod(onClick, "AddListener", unityAction);
                        Context.Log("[ButtonMod] Button click handler added using UnityAction");
                    }
                }
                catch (Exception ex)
                {
                    Context.LogError($"[ButtonMod] Failed to add click listener: {ex.Message}");

                    // 方法2：如果方法1失败，尝试使用反射创建委托
                    try
                    {
                        var addListenerMethod = onClick.GetType().GetMethod("AddListener");
                        if (addListenerMethod != null)
                        {
                            var paramType = addListenerMethod.GetParameters()[0].ParameterType;
                            var clickDelegate = Delegate.CreateDelegate(paramType, this, "OnButtonClick");
                            addListenerMethod.Invoke(onClick, new object[] { clickDelegate });
                            Context.Log("[ButtonMod] Button click handler added using reflection");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Context.LogError($"[ButtonMod] Alternative method also failed: {ex2.Message}");
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

            // 改变按钮颜色
            try
            {
                var imageType = FindType("Image");
                if (imageType == null)
                {
                    Context.LogError("[ButtonMod] Image type not found!");
                    return;
                }

                var image = GetComponent(buttonObj, imageType);

                if (image != null)
                {
                    // 使用预定义的颜色值而不是解析HTML颜色
                    var colorValues = new[]
                    {
                        new float[] {1f, 0f, 0f, 1f},    // Red
                        new float[] {0f, 1f, 0f, 1f},    // Green
                        new float[] {0f, 0f, 1f, 1f},    // Blue
                        new float[] {1f, 1f, 0f, 1f},    // Yellow
                        new float[] {1f, 0f, 1f, 1f},    // Magenta
                        new float[] {0f, 1f, 1f, 1f}     // Cyan
                    };

                    var colorData = colorValues[clickCount % colorValues.Length];
                    var colorType = FindType("Color");
                    var color = Activator.CreateInstance(colorType, colorData[0], colorData[1], colorData[2], colorData[3]);

                    SetProperty(image, "color", color);
                    Context.Log($"[ButtonMod] Changed button color");
                }

                // 更新按钮文本
                UpdateButtonText();

                // 可选：改变按钮大小
                if (clickCount % 5 == 0)
                {
                    AnimateButton();
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error in OnButtonClick: {ex.Message}");
            }
        }

        private void UpdateButtonText()
        {
            try
            {
                var textType = FindType("Text");
                if (textType == null)
                {
                    Context.LogError("[ButtonMod] Text type not found!");
                    return;
                }

                var text = GetComponent(textObj, textType);

                if (text != null)
                {
                    SetProperty(text, "text", $"Clicked {clickCount} times!");
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error updating text: {ex.Message}");
            }
        }

        private void AnimateButton()
        {
            try
            {
                var rectTransformType = FindType("RectTransform");
                if (rectTransformType == null)
                {
                    Context.LogError("[ButtonMod] RectTransform type not found!");
                    return;
                }

                var rectTransform = GetComponent(buttonObj, rectTransformType);

                if (rectTransform != null)
                {
                    var vector2Type = FindType("Vector2");
                    var newSize = Activator.CreateInstance(vector2Type, 250f, 60f);
                    SetProperty(rectTransform, "sizeDelta", newSize);

                    Context.Log("[ButtonMod] Button size increased!");

                    // 可以在这里添加协程来恢复大小，但需要更复杂的反射
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"[ButtonMod] Error animating button: {ex.Message}");
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

        // 辅助方法 - 获取组件，避免GetComponent的歧义
        private object GetComponent(object gameObject, Type componentType)
        {
            if (gameObject == null || componentType == null) return null;

            var getComponentMethod = gameObject.GetType().GetMethod("GetComponent", new Type[] { typeof(Type) });
            return getComponentMethod?.Invoke(gameObject, new object[] { componentType });
        }

        // 辅助方法 - 添加组件
        private object AddComponent(object gameObject, Type componentType)
        {
            return ReflectionHelper.AddComponent(gameObject, componentType);
        }

        // 辅助方法 - 设置Transform的父对象
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
                else
                {
                    Context.LogError("[ButtonMod] SetParent method not found!");
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
            Context.Log("[ButtonMod] OnBeforeReload - preparing for reload");
            // 可以在这里保存状态
        }

        public void OnAfterReload()
        {
            Context.Log("[ButtonMod] OnAfterReload - reload completed");
            // 可以在这里恢复状态
        }

        #endregion
    }
}