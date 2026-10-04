using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates and maintains the bottom-left status text for the current possession.
/// </summary>
public sealed class PossessionHud : MonoBehaviour
{
    [SerializeField] private PossessionManager possessionManager;

    private GameObject statusPanel;
    private Text statusText;

    private void Awake()
    {
        if (possessionManager == null)
            possessionManager = GetComponent<PossessionManager>();

        CreateHud();
    }

    private void OnEnable()
    {
        if (possessionManager != null)
            possessionManager.PossessionChanged += Refresh;
    }

    private void Start()
    {
        Refresh(possessionManager != null ? possessionManager.Current : null);
    }

    private void OnDisable()
    {
        if (possessionManager != null)
            possessionManager.PossessionChanged -= Refresh;
    }

    private void Refresh(IControllable controllable)
    {
        bool isPossessing = controllable != null;
        statusPanel.SetActive(isPossessing);

        if (isPossessing)
            statusText.text = $"You are a <b>{EscapeRichText(controllable.DisplayName)}</b>";
    }

    private void CreateHud()
    {
        GameObject canvasObject = new("Possession HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        statusPanel = new GameObject("Possession Status", typeof(RectTransform), typeof(Image));
        statusPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = statusPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = new Vector2(32f, 32f);
        panelRect.sizeDelta = new Vector2(460f, 68f);
        statusPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(statusPanel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 0f);
        textRect.offsetMax = new Vector2(-18f, 0f);

        statusText = textObject.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 30;
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;
        statusText.supportRichText = true;
        statusText.color = Color.white;
    }

    private static string EscapeRichText(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
