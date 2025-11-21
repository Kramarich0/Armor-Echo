using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (TryGetComponent<AudioListener>(out var listener)) Destroy(listener);

            audioSource = GetComponent<AudioSource>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource?.Stop();
    }

    public void PlayClip(AudioClip clip)
    {
        if (audioSource == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level"))
        {
            if (audioSource.isPlaying) StopMusic();
        }
        else
        {
            if (!audioSource.isPlaying && audioSource.clip != null)
                Start();
        }
    }
}
