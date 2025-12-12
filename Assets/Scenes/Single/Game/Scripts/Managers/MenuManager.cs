using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void BackToMainMenu()
    {
        SceneManager.LoadSceneAsync(SceneNames.MainMenu);
    }
    public void DirectToLevelSelect()
    {
        SceneManager.LoadSceneAsync(SceneNames.SelectLevel);
    }

    public void TankSelectionScene()
    {
        SceneManager.LoadSceneAsync(SceneNames.TankSelection);
    }

    public void SettingsScene()
    {
        SceneManager.LoadSceneAsync(SceneNames.Settings);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
