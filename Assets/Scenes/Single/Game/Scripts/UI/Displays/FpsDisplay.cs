using UnityEngine;
using TMPro;

public class FpsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float timeLeft;
    private int frameCount;

    void Start()
    {
        timeLeft = updateInterval;
    }

    void Update()
    {
        timeLeft -= Time.unscaledDeltaTime;
        frameCount++;

        if (timeLeft <= 0f)
        {
            float fps = frameCount / updateInterval;
            fpsText.text = $"{fps:0.0} FPS";

            frameCount = 0;
            timeLeft = updateInterval;
        }
    }
}
