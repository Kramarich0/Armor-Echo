using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalizationHelper
{
    public static void SetLocalizedText(LocalizeStringEvent lse, string tableEntry, params object[] args)
    {
        if (lse == null) return;
        if (string.IsNullOrEmpty(tableEntry)) return;

        lse.StringReference.TableEntryReference = tableEntry;
        lse.StringReference.Arguments = args;
        lse.RefreshString();
    }

    public static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    public static string GetLocalizedString(string entryKey, params object[] args)
    {
        return GetLocalizedStringSync("GameStrings", entryKey, args);
    }

    private static string GetLocalizedStringSync(string tableCollectionName, string entryKey, params object[] args)
    {
        if (string.IsNullOrEmpty(tableCollectionName) || string.IsNullOrEmpty(entryKey))
            return string.Empty;

        if (!LocalizationSettings.InitializationOperation.IsDone)
            LocalizationSettings.InitializationOperation.WaitForCompletion();

        AsyncOperationHandle<string> handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableCollectionName, entryKey, args);
        handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result;

        Debug.LogWarning($"LocalizationHelper: Не удалось получить строку '{entryKey}' из '{tableCollectionName}' (status: {handle.Status})");
        return entryKey;
    }
}
