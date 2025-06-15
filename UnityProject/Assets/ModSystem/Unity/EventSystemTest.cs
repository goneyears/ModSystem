using UnityEngine;
using ModSystem.Unity;

namespace ModSystem.Unity.Examples
{
    /// <summary>
    /// 事件系统测试
    /// </summary>
    public class EventSystemTest : MonoBehaviour
    {
        void Start()
        {
            // 创建简单的信息UI
            var canvas = new GameObject("InfoCanvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            
            var text = new GameObject("InfoText");
            text.transform.SetParent(canvas.transform, false);
            var txt = text.AddComponent<UnityEngine.UI.Text>();
            txt.text = "ModSystem V2\nPress R to reload mods";
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            txt.color = Color.white;
            var rect = txt.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 100);
            rect.anchoredPosition = new Vector2(-150, 150);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Reloading mods...");
                ModSystemController.Instance?.LoadMods();
            }
        }
    }
}