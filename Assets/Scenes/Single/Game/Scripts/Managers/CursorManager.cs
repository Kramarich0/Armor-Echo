using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    public InputActionReference toggleCursorAction;
    public GameObject pausePanel;

    private bool isCursorVisible = true;

    void OnEnable()
    {
        toggleCursorAction?.action?.Enable();
    }

    void OnDisable()
    {
        toggleCursorAction?.action?.Disable();
    }

    void Update()
    {
        if (pausePanel != null && pausePanel.activeSelf)
        {
            SetCursor(true);
            return;
        }

        if (toggleCursorAction?.action != null && toggleCursorAction.action.IsPressed())
            SetCursor(!isCursorVisible);
    }

    public void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        isCursorVisible = visible;
    }
}
