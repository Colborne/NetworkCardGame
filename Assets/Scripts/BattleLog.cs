using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public sealed class BattleLog : MonoBehaviour
{
    public const float DefaultActionDelaySeconds = 0.65f;

    private const int MaximumEntries = 100;
    private const float MessageIntervalSeconds = 0.08f;

    private static BattleLog instance;

    private readonly List<string> entries = new List<string>();
    private readonly Queue<string> pendingEntries = new Queue<string>();

    private GameObject panel;
    private TMP_Text body;
    private ScrollRect scrollRect;
    private Coroutine messageRoutine;
    private int sequence;

    public static void BeginBattle(string heading)
    {
        BattleLog log = EnsureExists();
        log.Clear();
        log.panel.SetActive(true);
        log.Enqueue($"<b>{heading}</b>");
    }

    public static void Record(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || Application.isBatchMode)
            return;

        BattleLog log = EnsureExists();
        log.panel.SetActive(true);
        log.Enqueue(message);
    }

    private static BattleLog EnsureExists()
    {
        if (instance)
            return instance;

        BattleLog existing = FindFirstObjectByType<BattleLog>();
        if (existing)
            return existing;

        GameObject root = new GameObject("Battle Log");
        return root.AddComponent<BattleLog>();
    }

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildInterface();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildInterface()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 960f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateUIObject("Action Log Panel", transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-12f, 0f);
        panelRect.sizeDelta = new Vector2(310f, -24f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.88f);
        background.raycastTarget = false;

        GameObject titleObject = CreateUIObject("Title", panel.transform);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(-20f, 44f);

        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = "ACTION LOG";
        title.fontSize = 23f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.94f, 0.84f, 0.46f, 1f);
        title.raycastTarget = false;

        GameObject viewport = CreateUIObject("Viewport", panel.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-12f, -48f);
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUIObject("Messages", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI messageText = content.AddComponent<TextMeshProUGUI>();
        messageText.text = string.Empty;
        messageText.fontSize = 18f;
        messageText.alignment = TextAlignmentOptions.TopLeft;
        messageText.color = new Color(0.93f, 0.95f, 1f, 1f);
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.overflowMode = TextOverflowModes.Overflow;
        messageText.margin = new Vector4(4f, 4f, 4f, 4f);
        messageText.raycastTarget = false;
        body = messageText;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private void Clear()
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = null;
        pendingEntries.Clear();
        entries.Clear();
        sequence = 0;
        if (body)
            body.text = string.Empty;
    }

    private void Enqueue(string message)
    {
        pendingEntries.Enqueue(message);
        if (messageRoutine == null)
            messageRoutine = StartCoroutine(PresentMessages());
    }

    private IEnumerator PresentMessages()
    {
        while (pendingEntries.Count > 0)
        {
            string message = pendingEntries.Dequeue();
            sequence++;
            entries.Add($"<color=#8191A8>{sequence:00}</color>  {message}");
            if (entries.Count > MaximumEntries)
                entries.RemoveAt(0);

            body.text = string.Join("\n\n", entries);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            yield return new WaitForSecondsRealtime(MessageIntervalSeconds);
        }

        messageRoutine = null;
    }
}
