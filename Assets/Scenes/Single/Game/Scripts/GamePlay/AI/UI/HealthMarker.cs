using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HealthMarkerSimple : MonoBehaviour
{
    [Header("Target")]
    public AITankHealth targetHealth;
    public TeamComponent targetTeam;

    [Header("Size")]
    public float width = 1f;
    public float height = 0.15f;

    // кешы
    private MeshRenderer fillRenderer;
    private MeshRenderer fillBackRenderer;
    private Transform fillBarTransform;
    private Transform fillBarBackTransform;
    private Camera mainCamera;
    private float cameraSearchTimer = 0f;
    private const float CameraSearchInterval = 0.5f;

    static Material s_unlitColorMat;
    private MaterialPropertyBlock mpb;


    void Start()
    {
        mpb = new MaterialPropertyBlock();

        if (targetHealth == null) return;

        EnsureSharedMaterial();
        CreateMarker();
        CacheChildren();
        FindMainCamera();
    }


    void Update()
    {
        if (targetHealth == null) return;

        if (mainCamera == null)
        {
            cameraSearchTimer -= Time.deltaTime;
            if (cameraSearchTimer <= 0f)
            {
                cameraSearchTimer = CameraSearchInterval;
                FindMainCamera();
            }
        }

        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0f, 180f, 0f);
        }

        float ratio = 0f;
        if (targetHealth.maxHealth > 0f)
            ratio = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);

        var fillScale = new Vector3(width * ratio, height, 1f);
        var fillPosition = new Vector3(-width * 0.5f + (width * ratio) * 0.5f, 0f, -0.01f);

        if (fillBarTransform != null)
        {
            fillBarTransform.localScale = fillScale;
            fillBarTransform.localPosition = fillPosition;
        }

        if (fillBarBackTransform != null)
        {
            fillBarBackTransform.localScale = fillScale;
            fillBarBackTransform.localPosition = fillPosition + new Vector3(0f, 0f, 0.001f);
        }

        Color newColor = (targetTeam != null && targetTeam.team == TeamEnum.Friendly) ? Color.green : Color.red;
        if (fillRenderer != null)
        {
            fillRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", newColor);
            fillRenderer.SetPropertyBlock(mpb);
        }
        if (fillBackRenderer != null)
        {
            fillBackRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", newColor);
            fillBackRenderer.SetPropertyBlock(mpb);
        }
    }

    void EnsureSharedMaterial()
    {
        if (s_unlitColorMat == null)
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default"); // fallback
            s_unlitColorMat = new Material(shader);
            s_unlitColorMat.hideFlags = HideFlags.DontSave;
            s_unlitColorMat.enableInstancing = true;
        }
    }

    void CreateMarker()
    {
        // Background
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        background.name = "Background";
        background.SetParent(transform, false);
        background.localScale = new Vector3(width, height, 1f);
        var bgR = background.GetComponent<MeshRenderer>();
        bgR.sharedMaterial = s_unlitColorMat;
        bgR.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", Color.black);
        bgR.SetPropertyBlock(mpb);
        DestroyImmediate(background.GetComponent<Collider>());

        // FillBar
        var fillBar = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBar.name = "FillBar";
        fillBar.SetParent(transform, false);
        fillBar.localPosition = new Vector3(-width * 0.5f, 0f, -0.01f);
        fillRenderer = fillBar.GetComponent<MeshRenderer>();
        fillRenderer.sharedMaterial = s_unlitColorMat;
        DestroyImmediate(fillBar.GetComponent<Collider>());

        // FillBarBack
        var fillBarBack = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBarBack.name = "FillBarBack";
        fillBarBack.SetParent(transform, false);
        fillBackRenderer = fillBarBack.GetComponent<MeshRenderer>();
        fillBarBack.GetComponent<MeshFilter>().mesh = fillBar.GetComponent<MeshFilter>().mesh;
        fillBarBack.localScale = Vector3.one;
        fillBarBack.localRotation = Quaternion.Euler(0f, 180f, 0f);
        fillBackRenderer.sharedMaterial = s_unlitColorMat;
        DestroyImmediate(fillBarBack.GetComponent<Collider>());
    }

    void CacheChildren()
    {
        fillBarTransform = transform.Find("FillBar");
        fillBarBackTransform = transform.Find("FillBarBack");
    }

    void FindMainCamera()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
            return;
        }

        var brain = FindFirstObjectByType<Unity.Cinemachine.CinemachineBrain>();
        mainCamera = brain != null ? brain.OutputCamera : null;
    }

    void OnDestroy()
    {
    }
}
