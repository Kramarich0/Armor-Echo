using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TeamMarker : MonoBehaviour
{
    [Header("Target / Pivot")]
    
    public Transform pivot;

    [Header("References")]
    public TeamComponent teamComp;
    public TankAI tankAI;

    [Header("Sprites by Tank Class")]
    public Sprite lightSprite;
    public Sprite mediumSprite;
    public Sprite heavySprite;

    [Header("Appearance")]
    public float size = 1f;
    public Vector3 localOffset = Vector3.zero;
    public bool faceCamera = true;
    private SpriteRenderer sr;
    private Camera mainCamera;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        FindMainCamera();
        UpdateReferences();
        UpdateAppearance();
    }

    void Update()
    {
        if (pivot == null) return;
        FindMainCamera();

        transform.position = pivot.position + pivot.TransformDirection(localOffset);
        transform.localScale = Vector3.one * Mathf.Max(0.001f, size);

        if (faceCamera && mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(
                     transform.position - mainCamera.transform.position,
                     Vector3.up
                 );
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        UpdateAppearance();
    }

    void UpdateReferences()
    {
        if (teamComp == null)
            teamComp = GetComponentInParent<TeamComponent>(true);

        if (tankAI == null)
            tankAI = GetComponentInParent<TankAI>(true);
    }

    void UpdateAppearance()
    {
        if (sr == null || teamComp == null || tankAI == null)
            return;

        sr.sprite = tankAI.CurrentTankClass switch
        {
            TankClass.Light => lightSprite,
            TankClass.Medium => mediumSprite,
            TankClass.Heavy => heavySprite,
            _ => null,
        };
        sr.color = (teamComp.team == TeamEnum.Friendly) ? Color.green : Color.red;
    }

    void FindMainCamera()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
            return;
        }

        var brain = FindFirstObjectByType<Unity.Cinemachine.CinemachineBrain>(); if (brain != null && brain.OutputCamera != null)
        {
            mainCamera = brain.OutputCamera;
            return;
        }

        mainCamera = Camera.current != null ? Camera.current : Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
    }

    void OnDrawGizmosSelected()
    {
        if (pivot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pivot.position, transform.position);
            Gizmos.DrawSphere(pivot.position, 0.05f);
        }
    }
}
