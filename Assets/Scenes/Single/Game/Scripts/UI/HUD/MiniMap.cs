using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;











public class StaticMiniMapRealtimeAccurate : MonoBehaviour
{
    public enum MapMode { Center, BottomLeft, CustomPoints }

    [Header("UI")]
    public RectTransform mapRect;            
    public Image playerIconPrefab;
    public Image enemyIconPrefab;
    public Image allyIconPrefab;

    [Header("Objects")]
    public Transform player;
    public List<Transform> enemies = new List<Transform>();
    public List<Transform> allies = new List<Transform>();

    [Header("Map Settings")]
    public MapMode mapMode = MapMode.Center;
    [Tooltip("Center: мировые координаты, соответствующие центру текстуры")]
    public Vector3 mapWorldCenter = Vector3.zero;
    [Tooltip("BottomLeft: мировые координаты нижнего левого угла текстуры")]
    public Vector3 mapWorldBottomLeft = Vector3.zero;
    [Tooltip("Размер области мира, которую покрывает текстура. X->world X, Y->world Z")]
    public Vector2 mapWorldSize = new Vector2(1000f, 1000f);

    [Header("Custom points (precision)")]
    [Tooltip("Custom: две точки в мире и их UV(0..1) на текстуре (UV origin: left-bottom)")]
    public Vector3 worldA = Vector3.zero;
    [Range(0f, 1f)] public Vector2 uvA = Vector2.zero;
    public Vector3 worldB = new Vector3(100f, 0f, 0f);
    [Range(0f, 1f)] public Vector2 uvB = new Vector2(1f, 0f);

    [Header("Options")]
    public bool flipZ = false; 
    public bool rotateWithPlayer = true;
    public bool debugMode = false;

    [Header("Zoom Settings")]
    [Tooltip("Кнопочный/шаговый коэффициент зума для Ctrl + / - (меньше = более плавный)")]
    public float keyZoomFactor = 0.85f; 
    [Tooltip("Чем больше, тем быстрее зум при одном 'шаге' колеса")]
    public float wheelZoomSpeed = 0.15f; 
    [Tooltip("Минимальные размеры области (по X и Y)")]
    public Vector2 minMapWorldSize = new Vector2(10f, 10f);
    [Tooltip("Максимальные размеры области (по X и Y)")]
    public Vector2 maxMapWorldSize = new Vector2(5000f, 5000f);

    
    private RectTransform playerIcon;
    private List<RectTransform> enemyIcons = new List<RectTransform>();
    private List<RectTransform> allyIcons = new List<RectTransform>();

    
    private bool haveCustomMatrix = false;
    private float m00, m01, m10, m11; 
    private Vector2 offsetPixels = Vector2.zero;

    
    private Canvas parentCanvas;
    private Camera uiCamera;

    
    private RawImage rawImage;
    private Image imageComponent;
    private Material imageMaterial; 

    
    private Vector2 initialMapWorldSize;
    private Vector3 initialMapWorldCenter;
    private Vector3 initialMapWorldBottomLeft;

    void Start()
    {
        if (mapRect == null)
        {
            Debug.LogError("[StaticMiniMapRealtimeAccurate] mapRect не задан!");
            enabled = false;
            return;
        }

        parentCanvas = mapRect.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera;
        else
            uiCamera = null;

        
        rawImage = mapRect.GetComponent<RawImage>();
        imageComponent = mapRect.GetComponent<Image>();
        if (imageComponent != null && imageComponent.material != null)
            imageMaterial = imageComponent.material;

        
        initialMapWorldSize = mapWorldSize;
        initialMapWorldCenter = mapWorldCenter;
        initialMapWorldBottomLeft = mapWorldBottomLeft;

        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.localRotation = Quaternion.identity;

        playerIcon = InstantiateIcon(playerIconPrefab, "PlayerIcon");
        RebuildIconsList(enemies, enemyIcons, enemyIconPrefab, "EnemyIcon");
        RebuildIconsList(allies, allyIcons, allyIconPrefab, "AllyIcon");

        RecalculateMapping();
        ApplyTextureZoom(); 
    }

    void OnValidate()
    {
        if (!Application.isPlaying && mapRect != null)
            RecalculateMapping();
    }

    void RecalculateMapping()
    {
        haveCustomMatrix = false;

        Vector2 sizePx = mapRect.rect.size;

        if (mapMode == MapMode.CustomPoints)
        {
            
            Vector2 pA = new Vector2(uvA.x * sizePx.x, uvA.y * sizePx.y);
            Vector2 pB = new Vector2(uvB.x * sizePx.x, uvB.y * sizePx.y);

            Vector2 wA2 = new Vector2(worldA.x, worldA.z);
            Vector2 wB2 = new Vector2(worldB.x, worldB.z);

            Vector2 v1 = wB2 - wA2;
            Vector2 m1 = pB - pA;

            float len2 = v1.x * v1.x + v1.y * v1.y;
            if (len2 < 1e-6f)
            {
                Debug.LogWarning("[MiniMap] Custom points слишком близко — fallback.");
                haveCustomMatrix = false;
                return;
            }

            Vector2 v2 = new Vector2(-v1.y, v1.x); 
            Vector2 m2 = new Vector2(-m1.y, m1.x);

            float a = v1.x, b = v2.x, c = v1.y, d = v2.y;
            float det = a * d - b * c;
            if (Mathf.Abs(det) < 1e-9f)
            {
                Debug.LogWarning("[MiniMap] Det слишком мал для CustomPoints.");
                haveCustomMatrix = false;
                return;
            }

            float invDet = 1f / det;
            float i00 = d * invDet;
            float i01 = -b * invDet;
            float i10 = -c * invDet;
            float i11 = a * invDet;

            m00 = m1.x * i00 + m2.x * i10;
            m01 = m1.x * i01 + m2.x * i11;
            m10 = m1.y * i00 + m2.y * i10;
            m11 = m1.y * i01 + m2.y * i11;

            offsetPixels = pA - new Vector2(m00 * wA2.x + m01 * wA2.y, m10 * wA2.x + m11 * wA2.y);

            haveCustomMatrix = true;
            if (debugMode) Debug.Log($"[MiniMap] Custom matrix: M=[[{m00:F4},{m01:F4}],[{m10:F4},{m11:F4}]] offset={offsetPixels}");
        }
        else
        {
            
            haveCustomMatrix = true;
            if (debugMode) Debug.Log("[MiniMap] Simple mapping (Center/BottomLeft) ready.");
        }
    }

    void Update()
    {
        
        if (rotateWithPlayer && player != null)
            mapRect.localEulerAngles = new Vector3(0f, 0f, -player.eulerAngles.y);
        else
            mapRect.localEulerAngles = Vector3.zero;

        HandleZoomInput();

        UpdateAllIcons();
    }

    
    void HandleZoomInput()
    {
        if (mapRect == null) return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        
        if (ctrl && (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)))
        {
            PerformKeyboardZoom(true);
            return;
        }
        
        if (ctrl && (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)))
        {
            PerformKeyboardZoom(false);
            return;
        }

        
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 1e-6f) return;
        if (!ctrl) return;

        
        if (!RectTransformUtility.RectangleContainsScreenPoint(mapRect, Input.mousePosition, uiCamera)) return;

        Vector2 sizePx = mapRect.rect.size;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, uiCamera, out localPoint);

        
        Vector2 uv = (localPoint + sizePx * 0.5f);
        if (sizePx.x != 0f) uv.x /= sizePx.x; else uv.x = 0.5f;
        if (sizePx.y != 0f) uv.y /= sizePx.y; else uv.y = 0.5f;
        uv = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

        float factor = 1f - scroll * wheelZoomSpeed;
        factor = Mathf.Clamp(factor, 0.05f, 10f);

        if (mapMode == MapMode.Center)
        {
            float left = mapWorldCenter.x - mapWorldSize.x * 0.5f;
            float bottom = mapWorldCenter.z - mapWorldSize.y * 0.5f;
            float worldX = left + uv.x * mapWorldSize.x;
            float worldZ = bottom + uv.y * mapWorldSize.y;

            Vector2 newSize = mapWorldSize * factor;
            newSize.x = Mathf.Clamp(newSize.x, minMapWorldSize.x, maxMapWorldSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minMapWorldSize.y, maxMapWorldSize.y);

            float newLeft = worldX - uv.x * newSize.x;
            float newBottom = worldZ - uv.y * newSize.y;

            mapWorldSize = newSize;
            mapWorldCenter = new Vector3(newLeft + newSize.x * 0.5f, mapWorldCenter.y, newBottom + newSize.y * 0.5f);
        }
        else if (mapMode == MapMode.BottomLeft)
        {
            float left = mapWorldBottomLeft.x;
            float bottom = mapWorldBottomLeft.z;
            float worldX = left + uv.x * mapWorldSize.x;
            float worldZ = bottom + uv.y * mapWorldSize.y;

            Vector2 newSize = mapWorldSize * factor;
            newSize.x = Mathf.Clamp(newSize.x, minMapWorldSize.x, maxMapWorldSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minMapWorldSize.y, maxMapWorldSize.y);

            float newLeft = worldX - uv.x * newSize.x;
            float newBottom = worldZ - uv.y * newSize.y;

            mapWorldSize = newSize;
            mapWorldBottomLeft = new Vector3(newLeft, mapWorldBottomLeft.y, newBottom);
        }
        else
        {
            
            Vector3 mid = (worldA + worldB) * 0.5f;
            Vector3 dirA = worldA - mid;
            Vector3 dirB = worldB - mid;
            Vector3 newA = mid + dirA * factor;
            Vector3 newB = mid + dirB * factor;
            worldA = newA;
            worldB = newB;
        }

        
        ApplyTextureZoom();
        RecalculateMapping();
    }

    void PerformKeyboardZoom(bool zoomIn)
    {
        
        float factor = zoomIn ? keyZoomFactor : (1f / keyZoomFactor);

        if (mapMode == MapMode.Center)
        {
            Vector2 newSize = mapWorldSize * factor;
            newSize.x = Mathf.Clamp(newSize.x, minMapWorldSize.x, maxMapWorldSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minMapWorldSize.y, maxMapWorldSize.y);

            
            mapWorldSize = newSize;
        }
        else if (mapMode == MapMode.BottomLeft)
        {
            
            Vector2 newSize = mapWorldSize * factor;
            newSize.x = Mathf.Clamp(newSize.x, minMapWorldSize.x, maxMapWorldSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minMapWorldSize.y, maxMapWorldSize.y);

            
            Vector3 centerBefore = new Vector3(mapWorldBottomLeft.x + mapWorldSize.x * 0.5f, 0f, mapWorldBottomLeft.z + mapWorldSize.y * 0.5f);
            Vector3 newBottomLeft = new Vector3(centerBefore.x - newSize.x * 0.5f, mapWorldBottomLeft.y, centerBefore.z - newSize.y * 0.5f);

            mapWorldSize = newSize;
            mapWorldBottomLeft = newBottomLeft;
        }
        else 
        {
            Vector3 mid = (worldA + worldB) * 0.5f;
            Vector3 dirA = worldA - mid;
            Vector3 dirB = worldB - mid;
            worldA = mid + dirA * factor;
            worldB = mid + dirB * factor;
        }

        ApplyTextureZoom();
        RecalculateMapping();
    }

    
    void ApplyTextureZoom()
    {
        
        if (mapMode == MapMode.CustomPoints)
        {
            if (debugMode) Debug.Log("[MiniMap] Texture zoom не поддерживается для CustomPoints. Только world-зум.");
            return;
        }

        
        if (initialMapWorldSize.x <= 0f || initialMapWorldSize.y <= 0f)
        {
            if (debugMode) Debug.LogWarning("[MiniMap] initialMapWorldSize некорректен.");
            return;
        }

        
        float left = 0f, bottom = 0f, initLeft = 0f, initBottom = 0f;
        if (mapMode == MapMode.Center)
        {
            left = mapWorldCenter.x - mapWorldSize.x * 0.5f;
            bottom = mapWorldCenter.z - mapWorldSize.y * 0.5f;

            initLeft = initialMapWorldCenter.x - initialMapWorldSize.x * 0.5f;
            initBottom = initialMapWorldCenter.z - initialMapWorldSize.y * 0.5f;
        }
        else 
        {
            left = mapWorldBottomLeft.x;
            bottom = mapWorldBottomLeft.z;

            initLeft = initialMapWorldBottomLeft.x;
            initBottom = initialMapWorldBottomLeft.z;
        }

        
        float uLeft = (left - initLeft) / (initialMapWorldSize.x != 0f ? initialMapWorldSize.x : 1f);
        float vBottom = (bottom - initBottom) / (initialMapWorldSize.y != 0f ? initialMapWorldSize.y : 1f);
        float uWidth = mapWorldSize.x / (initialMapWorldSize.x != 0f ? initialMapWorldSize.x : 1f);
        float vHeight = mapWorldSize.y / (initialMapWorldSize.y != 0f ? initialMapWorldSize.y : 1f);

        
        uLeft = Mathf.Clamp01(uLeft);
        vBottom = Mathf.Clamp01(vBottom);
        uWidth = Mathf.Clamp(uWidth, 1e-6f, 1f);
        vHeight = Mathf.Clamp(vHeight, 1e-6f, 1f);

        
        if (flipZ)
        {
            vBottom = 1f - vBottom - vHeight;
            vBottom = Mathf.Clamp01(vBottom);
        }

        
        if (rawImage != null)
        {
            rawImage.uvRect = new Rect(uLeft, vBottom, uWidth, vHeight);
            if (debugMode) Debug.Log($"[MiniMap] Applied RawImage.uvRect = {rawImage.uvRect}");
            return;
        }

        
        if (imageMaterial != null)
        {
            
            try
            {
                imageMaterial.SetTextureOffset("_MainTex", new Vector2(uLeft, vBottom));
                imageMaterial.SetTextureScale("_MainTex", new Vector2(uWidth, vHeight));
                
            }
            catch (System.Exception ex)
            {
                if (debugMode) Debug.LogWarning("[MiniMap] Не удалось установить material texture offset/scale: " + ex.Message);
            }
            return;
        }

        
        if (debugMode) Debug.LogWarning("[MiniMap] Для видимого зума рекомендую использовать RawImage на mapRect. Image без кастомного материала может не показать zoom.");
    }

    RectTransform InstantiateIcon(Image prefab, string name)
    {
        if (prefab == null) return null;
        Image img = Instantiate(prefab, mapRect, false);
        img.name = name;
        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        img.gameObject.SetActive(true);
        return rt;
    }

    void RebuildIconsList(List<Transform> source, List<RectTransform> iconsList, Image prefab, string baseName)
    {
        while (iconsList.Count > source.Count)
        {
            Destroy(iconsList[iconsList.Count - 1].gameObject);
            iconsList.RemoveAt(iconsList.Count - 1);
        }
        while (iconsList.Count < source.Count)
        {
            iconsList.Add(InstantiateIcon(prefab, baseName + iconsList.Count));
        }
    }

    void UpdateAllIcons()
    {
        RebuildIconsList(enemies, enemyIcons, enemyIconPrefab, "EnemyIcon");
        RebuildIconsList(allies, allyIcons, allyIconPrefab, "AllyIcon");

        Vector2 sizePx = mapRect.rect.size;
        Vector2 halfPx = sizePx * 0.5f;

        UpdateIcon(playerIcon, player, sizePx, halfPx);
        for (int i = 0; i < enemies.Count; i++) UpdateIcon(enemyIcons[i], enemies[i], sizePx, halfPx);
        for (int i = 0; i < allies.Count; i++) UpdateIcon(allyIcons[i], allies[i], sizePx, halfPx);
    }

    
    Vector2 MapPixelsFromWorldAccurate(Vector3 worldPos, Vector2 sizePx)
    {
        float wx = worldPos.x;
        float wz = worldPos.z;

        if (mapMode == MapMode.CustomPoints && haveCustomMatrix)
        {
            if (flipZ) wz = -wz;
            float px = m00 * wx + m01 * wz + offsetPixels.x;
            float py = m10 * wx + m11 * wz + offsetPixels.y;
            return new Vector2(px - sizePx.x * 0.5f, py - sizePx.y * 0.5f);
        }
        else if (mapMode == MapMode.Center)
        {
            
            float left = mapWorldCenter.x - mapWorldSize.x * 0.5f;
            float bottom = mapWorldCenter.z - mapWorldSize.y * 0.5f;

            float nx = (wx - left) / (mapWorldSize.x != 0f ? mapWorldSize.x : 1f); 
            float ny = (wz - bottom) / (mapWorldSize.y != 0f ? mapWorldSize.y : 1f); 

            if (flipZ) ny = 1f - ny;

            float px = Mathf.Lerp(0f, sizePx.x, nx);
            float py = Mathf.Lerp(0f, sizePx.y, ny);
            return new Vector2(px - sizePx.x * 0.5f, py - sizePx.y * 0.5f);
        }
        else 
        {
            float left = mapWorldBottomLeft.x;
            float bottom = mapWorldBottomLeft.z;

            float nx = (wx - left) / (mapWorldSize.x != 0f ? mapWorldSize.x : 1f);
            float ny = (wz - bottom) / (mapWorldSize.y != 0f ? mapWorldSize.y : 1f);

            if (flipZ) ny = 1f - ny;

            float px = Mathf.Lerp(0f, sizePx.x, nx);
            float py = Mathf.Lerp(0f, sizePx.y, ny);
            return new Vector2(px - sizePx.x * 0.5f, py - sizePx.y * 0.5f);
        }
    }

    void UpdateIcon(RectTransform icon, Transform obj, Vector2 sizePx, Vector2 halfPx)
    {
        if (icon == null || obj == null) return;

        if (!haveCustomMatrix)
        {
            RecalculateMapping();
            if (!haveCustomMatrix) return;
        }

        Vector2 anchored = MapPixelsFromWorldAccurate(obj.position, sizePx);

        
        anchored.x = Mathf.Clamp(anchored.x, -halfPx.x, halfPx.x);
        anchored.y = Mathf.Clamp(anchored.y, -halfPx.y, halfPx.y);

        icon.anchoredPosition = anchored;

        
        if (rotateWithPlayer && player != null)
            icon.localEulerAngles = new Vector3(0f, 0f, player.eulerAngles.y);
        else
            icon.localEulerAngles = Vector3.zero;

        if (debugMode && obj == player)
        {
            Debug.Log($"[MiniMap] player world={obj.position} -> anchored={anchored} (pixels from center)");
        }
    }

    
    void OnDrawGizmosSelected()
    {
        if (!debugMode || mapRect == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(worldA, 0.5f);
        Gizmos.DrawSphere(worldB, 0.5f);

        if (haveCustomMatrix)
        {
            Vector2 pA_px = new Vector2(uvA.x * mapRect.rect.width, uvA.y * mapRect.rect.height);
            Vector2 pB_px = new Vector2(uvB.x * mapRect.rect.width, uvB.y * mapRect.rect.height);
            Debug.Log($"[MiniMap Debug] UV->pixels A={pA_px} B={pB_px}");
        }
    }
}
