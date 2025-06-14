// UnityProject/Assets/Scripts/UI/ModUIFactory.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModSystem.Core;
using IModLogger = ModSystem.Core.ILogger;
namespace ModSystem.Unity
{
    /// <summary>
    /// UI工厂类，用于创建和管理模组的UI元素
    /// </summary>
    public class ModUIFactory
    {
        private readonly Canvas canvas;
        private readonly IModLogger logger;
        private readonly Dictionary<string, GameObject> uiCache;
        
        /// <summary>
        /// 创建UI工厂
        /// </summary>
        public ModUIFactory(Canvas canvas, IModLogger logger)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.uiCache = new Dictionary<string, GameObject>();
        }
        
        /// <summary>
        /// 创建按钮
        /// </summary>
        public Button CreateButton(string name, string text, Vector2 position)
        {
            try
            {
                // 创建按钮GameObject
                var buttonObj = new GameObject(name);
                buttonObj.transform.SetParent(canvas.transform, false);
                
                // 添加RectTransform
                var rectTransform = buttonObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = new Vector2(160, 30);
                
                // 添加Image组件（按钮背景）
                var image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.6f, 0.8f, 1f);
                
                // 添加Button组件
                var button = buttonObj.AddComponent<Button>();
                button.targetGraphic = image;
                
                // 创建文本子对象
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                
                // 添加Text组件
                var textComponent = textObj.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.fontSize = 14;
                textComponent.color = Color.white;
                textComponent.alignment = TextAnchor.MiddleCenter;
                
                // 设置文本RectTransform
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                
                // 缓存UI对象
                uiCache[name] = buttonObj;
                
                logger?.Log($"Created button: {name}");
                return button;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create button: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 创建文本标签
        /// </summary>
        public Text CreateLabel(string name, string text, Vector2 position)
        {
            try
            {
                var labelObj = new GameObject(name);
                labelObj.transform.SetParent(canvas.transform, false);
                
                var rectTransform = labelObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = new Vector2(200, 30);
                
                var textComponent = labelObj.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.fontSize = 14;
                textComponent.color = Color.white;
                textComponent.alignment = TextAnchor.MiddleLeft;
                
                uiCache[name] = labelObj;
                
                logger?.Log($"Created label: {name}");
                return textComponent;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create label: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 创建面板
        /// </summary>
        public GameObject CreatePanel(string name, Vector2 position, Vector2 size)
        {
            try
            {
                var panelObj = new GameObject(name);
                panelObj.transform.SetParent(canvas.transform, false);
                
                var rectTransform = panelObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = size;
                
                var image = panelObj.AddComponent<Image>();
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                
                uiCache[name] = panelObj;
                
                logger?.Log($"Created panel: {name}");
                return panelObj;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create panel: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 获取缓存的UI对象
        /// </summary>
        public GameObject GetUIObject(string name)
        {
            return uiCache.TryGetValue(name, out var obj) ? obj : null;
        }
        
        /// <summary>
        /// 销毁UI对象
        /// </summary>
        public void DestroyUIObject(string name)
        {
            if (uiCache.TryGetValue(name, out var obj))
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                uiCache.Remove(name);
                logger?.Log($"Destroyed UI object: {name}");
            }
        }
        
        /// <summary>
        /// 清理所有UI对象
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in uiCache)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                }
            }
            uiCache.Clear();
            logger?.Log("Cleared all UI objects");
        }
    }
}