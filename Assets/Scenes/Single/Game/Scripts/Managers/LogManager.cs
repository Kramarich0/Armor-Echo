using UnityEngine;
using System;
using Serilog;
using Serilog.Sinks.Unity3D;

public class LocalManager : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_EDITOR
        var minLevel = Serilog.Events.LogEventLevel.Debug;
#else
        var minLevel = Serilog.Events.LogEventLevel.Warning; 
#endif
        var logDir = System.IO.Path.Combine(Application.persistentDataPath, "Logs");
        if (!System.IO.Directory.Exists(logDir))
            System.IO.Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Is(minLevel)

          .Enrich.WithProperty("Timestamp", DateTime.Now)
          .Enrich.WithProperty("GameVersion", Application.version)
          .Enrich.WithProperty("SceneName", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)

#if UNITY_EDITOR
            .WriteTo.Unity3D(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {Message}{NewLine}{Exception}"
            )
#endif

      .WriteTo.Async(a => a.File(
            System.IO.Path.Combine(Application.persistentDataPath, "Logs/log_.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}{NewLine}{Exception}"
        ))


          .CreateLogger();

        // Application.logMessageReceived += HandleUnityLog;

        Log.Information("Local logger initialized.");
    }

    private void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                Log.Error("UnityError: {Msg}\nStack: {Stack}", condition, stackTrace);
                break;

            case LogType.Warning:
                Log.Warning("UnityWarn: {Msg}", condition);
                break;

            default:
                Log.Information("UnityInfo: {Msg}", condition);
                break;
        }
    }

    private void OnDestroy()
    {
        // Application.logMessageReceived -= HandleUnityLog;
        Log.CloseAndFlush();
    }
}
