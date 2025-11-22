using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void DirectToLevelSelect()
    {
        SceneManager.LoadScene("SelectLevel");
    }

    public void SettingsScene()
    {
        SceneManager.LoadScene("Settings");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
