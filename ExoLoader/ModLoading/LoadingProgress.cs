using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ExoLoader;
public class LoadingProgress : MonoBehaviour
{
    private static LoadingProgress _instance;
    public static LoadingProgress Instance => _instance;

    private TextMeshProUGUI modLoadingText;
    private Slider progressBar;
    private Canvas targetCanvas;

    private bool isLoading = false;
    private LoadingScene loadingScene;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUI();
        }
        else
        {
            ModInstance.log("LoadingProgress instance already exists, destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        ModInstance.log("Setting up LoadingProgress UI...");

        loadingScene = FindObjectOfType<LoadingScene>();
        if (loadingScene != null)
        {
            ModInstance.log("Found LoadingScene, setting up mod loading UI...");
            CreateModLoadingUI();
        }

        SetUIVisibility(false);
    }

    private void CreateModLoadingUI()
    {
        try
        {
            targetCanvas = loadingScene.GetComponentInParent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = loadingScene.GetComponentInChildren<Canvas>();
            if (targetCanvas == null)
                targetCanvas = FindObjectOfType<Canvas>();

            if (targetCanvas == null)
            {
                ModInstance.log("Could not find any Canvas to attach mod loading UI to!");
                return;
            }

            ModInstance.log($"Using canvas: {targetCanvas.name}");

            Transform existingLoadingText = loadingScene.loadingText.transform;

            GameObject modUIContainer = new GameObject("ModLoadingUI");
            modUIContainer.transform.SetParent(targetCanvas.transform, false);

            RectTransform containerRect = modUIContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(400, 100);
            containerRect.anchoredPosition = new Vector2(0, -330);

            GameObject modTextObj = new GameObject("ModLoadingText");
            modTextObj.transform.SetParent(modUIContainer.transform, false);

            modLoadingText = modTextObj.AddComponent<TextMeshProUGUI>();
            modLoadingText.text = "Loading mod assets...";
            modLoadingText.font = loadingScene.loadingText.font;
            modLoadingText.fontSize = 30;
            modLoadingText.color = Color.white;
            modLoadingText.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = modLoadingText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.7f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            GameObject progressObj = new GameObject("ModProgressBar");
            progressObj.transform.SetParent(modUIContainer.transform, false);

            progressBar = progressObj.AddComponent<Slider>();
            progressBar.value = 0f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(progressObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progressObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);

            RectTransform fillRect = fillImage.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            progressBar.fillRect = fillRect;

            RectTransform progressRect = progressBar.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.1f, 0.2f);
            progressRect.anchorMax = new Vector2(0.9f, 0.5f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            ModInstance.log("Successfully created mod loading UI elements.");
        }
        catch (System.Exception e)
        {
            ModInstance.log($"Error creating mod loading UI: {e.Message}");
        }
    }

    private void SetUIVisibility(bool visible)
    {
        if (modLoadingText != null)
            modLoadingText.gameObject.SetActive(visible);
        if (progressBar != null)
            progressBar.gameObject.SetActive(visible);
    }

    public void Show(string message = "Loading mod assets...", Color? color = null)
    {
        if (color == null)
            color = Color.white;

        ModInstance.log($"Showing loading overlay: {message}");
        if (_instance == null) return;

        // If we haven't set up UI yet, try again
        if (modLoadingText == null)
        {
            SetupUI();
        }

        isLoading = true;
        SetUIVisibility(true);

        if (modLoadingText != null)
        {
            modLoadingText.text = message;
            modLoadingText.color = color.Value;
        }
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        // Force canvas update
        if (targetCanvas != null)
            Canvas.ForceUpdateCanvases();
    }

    public void UpdateProgress(float progress, string message = null)
    {
        ModInstance.log($"Updating loading overlay progress: {progress:F2} with message: {message}");
        if (_instance == null || !isLoading) return;

        if (progressBar != null)
        {
            progressBar.value = Mathf.Clamp01(progress);
        }

        if (!string.IsNullOrEmpty(message) && modLoadingText != null)
            modLoadingText.text = message;

        if (targetCanvas != null)
            Canvas.ForceUpdateCanvases();
    }
}
