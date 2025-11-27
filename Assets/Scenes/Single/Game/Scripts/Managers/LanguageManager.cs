using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

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
        if (dropdown == null)
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

            if (dropdown == null)
                Debug.LogError("LanguageSelector: Dropdown не найден!");
        }


        if (dropdown != null)
            dropdown.gameObject.SetActive(false);
        if (loadingText != null) loadingText.SetActive(true);

        yield return LocalizationSettings.InitializationOperation;

        dropdown.ClearOptions();

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            string code = locale.Identifier.Code;
            if (niceNames.TryGetValue(code, out string nice))
                dropdown.options.Add(new TMP_Dropdown.OptionData(nice));
            else
                dropdown.options.Add(new TMP_Dropdown.OptionData(locale.LocaleName));
        }

        int savedIndex = PlayerPrefs.GetInt("LanguageIndex", -1);
        if (savedIndex >= 0 && savedIndex < dropdown.options.Count)
        {
            dropdown.value = savedIndex;
            dropdown.RefreshShownValue();
            StartCoroutine(SetLocale(savedIndex));
        }
        else
        {
            var current = LocalizationSettings.SelectedLocale;
            int index = LocalizationSettings.AvailableLocales.Locales.IndexOf(current);
            dropdown.value = index;
            dropdown.RefreshShownValue();
        }


        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        dropdown.gameObject.SetActive(true);
        if (loadingText != null) loadingText.SetActive(false);
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

    private void InitializeDropdown()
    {
        dropdown.ClearOptions();

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            string code = locale.Identifier.Code;
            if (niceNames.TryGetValue(code, out string nice))
                dropdown.options.Add(new TMP_Dropdown.OptionData(nice));
            else
                dropdown.options.Add(new TMP_Dropdown.OptionData(locale.LocaleName));
        }

        int savedIndex = PlayerPrefs.GetInt("LanguageIndex", 0);
        dropdown.value = Mathf.Clamp(savedIndex, 0, dropdown.options.Count - 1);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        dropdown = null;

        var allDropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var dd in allDropdowns)
        {
            if (dd.name.ToLower().Contains("language"))
            {
                dropdown = dd;
                break;
            }
        }

        if (dropdown == null)
        {
            Debug.LogWarning("LanguageSelector: Dropdown не найден на новой сцене!");
            return;
        }

        InitializeDropdown();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
