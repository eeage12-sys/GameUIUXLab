using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OriginTitleMenuActions : MonoBehaviour
{
    public OriginUIFlowController flow;

    [Header("Settings")]
    public GameObject settingsPanel;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Button applyButton;
    public Button closeButton;

    private readonly List<Vector2Int> resolutions = new()
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720)
    };

    private void Start() => SetupSettings();

    private void Update()
    {
        if (settingsPanel != null && settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseSettings();
    }

    public void StartGame()
    {
        if (flow != null) flow.StartFromTitle();
    }

    public void ContinueGame()
    {
        if (flow != null) flow.StartFromTitle();
    }

    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();

        if (EventSystem.current != null && resolutionDropdown != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(resolutionDropdown.gameObject);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ApplySettings()
    {
        if (resolutionDropdown != null)
        {
            int index = Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1);
            Vector2Int r = resolutions[index];
            bool fullscreen = fullscreenToggle == null || fullscreenToggle.isOn;
            Screen.SetResolution(r.x, r.y, fullscreen);
        }

        if (qualityDropdown != null && QualitySettings.names.Length > 0)
        {
            int index = Mathf.Clamp(qualityDropdown.value, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(index, true);
        }

        CloseSettings();
    }

    public void ExitGame()
    {
        if (flow != null) flow.QuitGame();
    }

    private void SetupSettings()
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();

            List<string> options = new();
            int currentIndex = 0;

            for (int i = 0; i < resolutions.Count; i++)
            {
                Vector2Int r = resolutions[i];
                options.Add($"{r.x} × {r.y}");

                if (Screen.width == r.x && Screen.height == r.y)
                    currentIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.RefreshShownValue();
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        if (applyButton != null)
        {
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(ApplySettings);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}
