using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using Serilog;

public class LanguageSelector : MonoBehaviour
{
    public static LanguageSelector instance;
    public TMP_Dropdown dropdown;
    public GameObject loadingText;
    private bool isChanging = false;

    private readonly Dictionary<string, string> niceNames = new()
    {
        { "en", "English" },
        { "ru", "Русский" },
        { "de", "Deutsch" },
        { "fr", "Français" }
    };

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private IEnumerator Start()
    {
        if (loadingText != null) loadingText.SetActive(true);

        yield return LocalizationSettings.InitializationOperation;

        ApplySavedLanguage();

        if (loadingText != null) loadingText.SetActive(false);

        if (dropdown != null)
        {
            SetupDropdown();
        }
    }

    private void ApplySavedLanguage()
    {
        int savedIndex = PlayerPrefs.GetInt("LanguageIndex", -1);
        var locales = LocalizationSettings.AvailableLocales.Locales;

        if (savedIndex >= 0 && savedIndex < locales.Count)
        {
            LocalizationSettings.SelectedLocale = locales[savedIndex];
        }
    }

    private void SetupDropdown()
    {
        dropdown.gameObject.SetActive(false);
        dropdown.ClearOptions();

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            string code = locale.Identifier.Code;
            string nice = niceNames.GetValueOrDefault(code, locale.LocaleName);
            dropdown.options.Add(new TMP_Dropdown.OptionData(nice));
        }

        int savedIndex = PlayerPrefs.GetInt("LanguageIndex", -1);
        var locales = LocalizationSettings.AvailableLocales.Locales;

        if (savedIndex >= 0 && savedIndex < locales.Count)
        {
            dropdown.value = savedIndex;
        }
        else
        {
            int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
            dropdown.value = Mathf.Max(0, currentIndex);
        }

        dropdown.RefreshShownValue();
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        dropdown.gameObject.SetActive(true);
    }

    private void OnDropdownChanged(int index)
    {
        if (!isChanging)
            StartCoroutine(SetLocale(index));
    }

    private IEnumerator SetLocale(int index)
    {
        isChanging = true;

        var locale = LocalizationSettings.AvailableLocales.Locales[index];
        yield return LocalizationSettings.InitializationOperation;

        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetInt("LanguageIndex", index);
        PlayerPrefs.Save();

        isChanging = false;
    }

    private void FindDropdownOnScene()
    {
        var allDropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var dd in allDropdowns)
        {
            if (dd.name.ToLower().Contains("language"))
            {
                dropdown = dd;
                break;
            }
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.Settings) 
        {
            FindDropdownOnScene();
            if (dropdown != null)
            {
                SetupDropdown();
            }
        }
    }

}
