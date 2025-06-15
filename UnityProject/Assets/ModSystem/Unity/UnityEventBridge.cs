using UnityEngine;
using ModSystem.Core.Interfaces;
using ModSystem.Core.Events;

namespace ModSystem.Unity.Events
{
    /// <summary>
    /// Unity事件桥接器
    /// </summary>
    public class UnityEventBridge : MonoBehaviour
    {
        private IEventBus _eventBus;

        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<CreateUIRequestEvent>(OnCreateUI);
        }

        private void OnCreateUI(CreateUIRequestEvent e)
        {
            UnityEventDispatcher.Instance.RunOnMainThread(() => CreateSimpleUI(e));
        }

        private void CreateSimpleUI(CreateUIRequestEvent request)
        {
            // 创建Canvas
            if (!GameObject.Find("ModUICanvas"))
            {
                var canvasGO = new GameObject("ModUICanvas");
                canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // 创建面板
            var panel = new GameObject($"{request.SenderId}_Panel");
            panel.transform.SetParent(GameObject.Find("ModUICanvas").transform, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 150);
            rect.anchoredPosition = Vector2.zero;

            panel.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // 创建按钮
            if (request.Buttons != null)
            {
                float y = 30;
                foreach (var btn in request.Buttons)
                {
                    CreateButton(panel, btn, y);
                    y -= 40;
                }
            }
        }

        private void CreateButton(GameObject parent, ButtonConfig config, float y)
        {
            var button = new GameObject(config.Id);
            button.transform.SetParent(parent.transform, false);

            var rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 30);
            rect.anchoredPosition = new Vector2(0, y);

            var image = button.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;  // 按钮背景白色
            
            var btn = button.AddComponent<UnityEngine.UI.Button>();
            
            // 添加按钮过渡效果
            btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            btn.targetGraphic = image;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;
            
            // 文本
            var text = new GameObject("Text");
            text.transform.SetParent(button.transform, false);
            var txt = text.AddComponent<UnityEngine.UI.Text>();
            txt.text = config.Text;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            txt.color = Color.black;  // 文本黑色，与白色背景形成对比
            txt.alignment = TextAnchor.MiddleCenter;
            var textRect = text.GetComponent<RectTransform>();
            textRect.sizeDelta = rect.sizeDelta;
            textRect.anchoredPosition = Vector2.zero;

            // 点击事件
            btn.onClick.AddListener(() => 
            {
                UnityEngine.Debug.Log($"Button clicked: {config.Id}");  // 添加调试日志
                var clickEvent = new ButtonClickedEvent 
                { 
                    ButtonId = config.Id,
                    SenderId = "Unity",
                    Timestamp = System.DateTime.Now
                };
                _eventBus.Publish(clickEvent);
            });
        }
    }
}