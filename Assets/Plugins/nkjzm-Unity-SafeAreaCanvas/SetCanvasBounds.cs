using UnityEngine;

namespace nkjzm.SafeAreaCanvas
{
    [ExecuteInEditMode()]
    public class SetCanvasBounds : MonoBehaviour
    {
        public RectTransform panel;
        Rect lastSafeArea = new Rect(0, 0, 0, 0);

        void ApplySafeArea(Rect area)
        {
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = Vector2.zero;

            var anchorMin = area.position;
            var anchorMax = area.position + area.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;

            lastSafeArea = area;
        }

        void Update()
        {
            if (panel == null) { return; }

            Rect safeArea = Screen.safeArea;
#if UNITY_EDITOR
            if (Screen.width == 1125 && Screen.height == 2436)
            {
                safeArea.y = 102;
                safeArea.height = 2202;
            }
            if (Screen.width == 2436 && Screen.height == 1125)
            {
                safeArea.x = 132;
                safeArea.y = 63;
                safeArea.height = 1062;
                safeArea.width = 2172;
            }
#endif
            if (safeArea != lastSafeArea)
            {
                ApplySafeArea(safeArea);
            }
        }
    }
}