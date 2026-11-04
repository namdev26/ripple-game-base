using UnityEngine;

namespace RippleGame.UI
{
    /// <summary>Keeps a full-screen RectTransform inside the device safe area, away from notches and home bars.</summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void Awake() => rect = (RectTransform)transform;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != appliedArea || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            appliedArea = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (appliedScreen.x <= 0 || appliedScreen.y <= 0) return;

            rect.anchorMin = new Vector2(appliedArea.xMin / appliedScreen.x, appliedArea.yMin / appliedScreen.y);
            rect.anchorMax = new Vector2(appliedArea.xMax / appliedScreen.x, appliedArea.yMax / appliedScreen.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
