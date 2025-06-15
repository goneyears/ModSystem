using UnityEngine;
using UnityEngine.UI;

namespace ModSystem.Unity.Testing
{
    /// <summary>
    /// 按钮测试辅助脚本
    /// 将此脚本添加到场景中，按T键创建测试按钮
    /// </summary>
    public class ButtonTestHelper : MonoBehaviour
    {
        void Start()
        {
            // 确保基础UI系统存在
            EnsureUISystem();
        }

        void Update()
        {
            // 按T键创建测试按钮
            if (Input.GetKeyDown(KeyCode.T))
            {
                CreateTestButton();
            }
        }

        void EnsureUISystem()
        {
            // 确保EventSystem
            if (!GameObject.Find("EventSystem"))
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[ButtonTest] Created EventSystem");
            }

            // 确保Canvas
            if (!GameObject.Find("TestCanvas"))
            {
                var canvas = new GameObject("TestCanvas");
                canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();
                Debug.Log("[ButtonTest] Created Canvas");
            }
        }

        void CreateTestButton()
        {
            var canvas = GameObject.Find("TestCanvas");
            if (!canvas) return;

            // 创建按钮
            var buttonGO = new GameObject("TestButton");
            buttonGO.transform.SetParent(canvas.transform, false);

            var rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 40);
            rect.anchoredPosition = new Vector2(0, 100);

            // 背景
            var image = buttonGO.AddComponent<Image>();
            image.color = Color.white;

            // 按钮组件
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // 文本
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var text = textGO.AddComponent<Text>();
            text.text = "Test Button";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = rect.sizeDelta;
            textRect.anchoredPosition = Vector2.zero;

            // 点击事件
            button.onClick.AddListener(() => 
            {
                Debug.Log("[ButtonTest] Test button clicked!");
                text.text = $"Clicked! {Time.time:F1}";
            });

            Debug.Log("[ButtonTest] Created test button - click it to verify UI system works");
        }
    }
}