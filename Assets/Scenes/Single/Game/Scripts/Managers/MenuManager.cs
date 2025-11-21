using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void DirectToLevelSelect()
    {
        SceneManager.LoadScene("SelectLevel");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
