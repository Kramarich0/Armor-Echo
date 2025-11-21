using Serilog;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(RectTransform))]
public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;
    public Camera cameraToUse;
    public float smoothSpeed = 15f;

    private Image crosshairImage;
    private RectTransform rectTransform;
    private Vector2 currentAnchoredPos;
    private Canvas canvas;

    void Start()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (cameraToUse == null) cameraToUse = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Log.Warning("Crosshair: Canvas не найден!");
        currentAnchoredPos = rectTransform.anchoredPosition;
    }

    void LateUpdate()
    {
        if (cameraToUse == null || gunEnd == null) return;

        Vector3 forwardPoint = gunEnd.position + gunEnd.forward * 1000f;
        Vector3 screenPoint = cameraToUse.WorldToScreenPoint(forwardPoint);
        if (screenPoint.z <= 0f) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cameraToUse,
            out Vector2 localPoint))
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            currentAnchoredPos = Vector2.Lerp(currentAnchoredPos, localPoint, t);
            rectTransform.anchoredPosition = currentAnchoredPos;
        }
    }

    public void SetCamera(Camera cam)
    {
        cameraToUse = cam;
    }
}
