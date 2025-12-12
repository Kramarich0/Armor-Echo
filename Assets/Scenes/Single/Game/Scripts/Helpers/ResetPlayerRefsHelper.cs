using Serilog;
using UnityEditor;
using UnityEngine;

public class ResetPlayerRefsHelper
{
    [MenuItem("Tools/Reset PlayerPrefs")]
    static void Reset()
    {
        PlayerPrefs.DeleteAll();
        Log.Debug("PlayerPrefs сброшены!");
    }
}