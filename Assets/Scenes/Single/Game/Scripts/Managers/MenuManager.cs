using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    public void DirectToLevelSelect()
    {
        SceneManager.LoadScene(SceneNames.SelectLevel);
    }

    public void TankSelectionScene()
    {
        SceneManager.LoadScene(SceneNames.TankSelection);
    }

    public void SettingsScene()
    {
        SceneManager.LoadScene(SceneNames.Settings);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
