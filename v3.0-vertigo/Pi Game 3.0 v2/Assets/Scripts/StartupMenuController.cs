using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartupMenuController : MonoBehaviour
{
    [Header("Loading")]
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Image blackOverlay;
    [SerializeField] private GameObject loadingBarRoot;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private float minLoadSeconds = 4f;
    [SerializeField] private float maxLoadSeconds = 6f;
    [SerializeField] private float overlayFadeSeconds = 0.5f;

    [Header("Menu")]
    [SerializeField] private GameObject menuButtonsRoot;
    [SerializeField] private GameObject roomPanelRoot;
    [SerializeField] private Graphic menuBackgroundGraphic;
    [SerializeField] private Graphic[] menuGraphics;
    [SerializeField] private Graphic[] roomGraphics;
    [SerializeField] private float menuFadeSeconds = 0.25f;
    [SerializeField] private float panelFadeSeconds = 0.15f;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button roomBackButton;

    private bool isPanelTransitioning;

    private void Awake()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (loadingRoot != null)
            loadingRoot.SetActive(true);

        if (loadingBarRoot != null)
            loadingBarRoot.SetActive(true);

        SetAlpha(blackOverlay, 1f);

        if (loadingBar != null)
        {
            loadingBar.minValue = 0f;
            loadingBar.maxValue = 1f;
            loadingBar.value = 0f;
        }

        if (menuButtonsRoot != null)
            menuButtonsRoot.SetActive(true);

        if (roomPanelRoot != null)
            roomPanelRoot.SetActive(false);

        if ((menuGraphics == null || menuGraphics.Length == 0) && menuButtonsRoot != null)
            menuGraphics = menuButtonsRoot.GetComponentsInChildren<Graphic>(true);

        if ((roomGraphics == null || roomGraphics.Length == 0) && roomPanelRoot != null)
            roomGraphics = roomPanelRoot.GetComponentsInChildren<Graphic>(true);

        menuGraphics = FilterMenuGraphics(menuGraphics);

        if (menuBackgroundGraphic != null)
            SetAlpha(menuBackgroundGraphic, 1f);

        SetGraphicsAlpha(menuGraphics, 0f);
        SetButtonsInteractable(false);

        if (playButton != null)
            playButton.onClick.AddListener(OpenRoomPanel);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);

        if (roomBackButton != null)
            roomBackButton.onClick.AddListener(OpenMenuPanel);
    }

    private void Start()
    {
        StartCoroutine(RunStartup());
    }

    private IEnumerator RunStartup()
    {
        float min = Mathf.Max(0.1f, Mathf.Min(minLoadSeconds, maxLoadSeconds));
        float max = Mathf.Max(min, Mathf.Max(minLoadSeconds, maxLoadSeconds));
        float duration = Random.Range(min, max);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (loadingBar != null)
                loadingBar.value = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (loadingBar != null)
            loadingBar.value = 1f;

        if (loadingBarRoot != null)
            loadingBarRoot.SetActive(false);

        yield return FadeGraphic(blackOverlay, 1f, 0f, overlayFadeSeconds);

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        yield return FadeGraphics(menuGraphics, 0f, 1f, menuFadeSeconds);
        SetButtonsInteractable(true);
    }

    private void OpenRoomPanel()
    {
        if (isPanelTransitioning)
            return;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        StartCoroutine(FadeBetweenPanels(menuButtonsRoot, menuGraphics, roomPanelRoot, roomGraphics));
    }

    private void OpenMenuPanel()
    {
        if (isPanelTransitioning)
            return;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        StartCoroutine(FadeBetweenPanels(roomPanelRoot, roomGraphics, menuButtonsRoot, menuGraphics));
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null)
            yield break;

        if (duration <= 0f)
        {
            SetAlpha(graphic, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(graphic, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(graphic, to);
    }

    private IEnumerator FadeBetweenPanels(GameObject fromRoot, Graphic[] fromGraphics, GameObject toRoot, Graphic[] toGraphics)
    {
        isPanelTransitioning = true;

        if (toRoot != null)
            toRoot.SetActive(true);

        SetGraphicsAlpha(toGraphics, 0f);

        float duration = Mathf.Max(0f, panelFadeSeconds);
        if (duration <= 0f)
        {
            SetGraphicsAlpha(fromGraphics, 0f);
            SetGraphicsAlpha(toGraphics, 1f);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetGraphicsAlpha(fromGraphics, Mathf.Lerp(1f, 0f, t));
                SetGraphicsAlpha(toGraphics, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }

            SetGraphicsAlpha(fromGraphics, 0f);
            SetGraphicsAlpha(toGraphics, 1f);
        }

        if (fromRoot != null)
            fromRoot.SetActive(false);

        isPanelTransitioning = false;
    }

    private static IEnumerator FadeGraphics(Graphic[] graphics, float from, float to, float duration)
    {
        if (graphics == null || graphics.Length == 0)
            yield break;

        if (duration <= 0f)
        {
            SetGraphicsAlpha(graphics, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetGraphicsAlpha(graphics, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetGraphicsAlpha(graphics, to);
    }

    private static void SetGraphicsAlpha(Graphic[] graphics, float alpha)
    {
        if (graphics == null)
            return;

        for (int i = 0; i < graphics.Length; i++)
            SetAlpha(graphics[i], alpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private Graphic[] FilterMenuGraphics(Graphic[] graphics)
    {
        if (graphics == null || graphics.Length == 0)
            return new Graphic[0];

        List<Graphic> filtered = new List<Graphic>(graphics.Length);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            if (graphic == menuBackgroundGraphic)
                continue;

            filtered.Add(graphic);
        }

        return filtered.ToArray();
    }

    private void SetButtonsInteractable(bool enabled)
    {
        if (playButton != null)
            playButton.interactable = enabled;

        if (quitButton != null)
            quitButton.interactable = enabled;

        if (settingsButton != null)
            settingsButton.interactable = enabled;
    }
}
