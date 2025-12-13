using UnityEngine;

public class TrackController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody tankRigidbody;
    public Renderer leftTrackRenderer;
    public Renderer rightTrackRenderer;

    [Header("Settings")]
    public float turnScrollFactor = 0.7f;
    public float wheelRadius = 0.3f;

    [Header("Wheels (optional)")]
    public Transform[] leftWheels;
    public Transform[] rightWheels;
    private Material leftTrackMat;
    private Material rightTrackMat;

    private float leftDistance = 0f;
    private float rightDistance = 0f;

    void Start()
    {
        if (!tankRigidbody) tankRigidbody = GetComponentInParent<Rigidbody>();
        if (leftTrackRenderer) leftTrackMat = leftTrackRenderer.material;
        if (rightTrackRenderer) rightTrackMat = rightTrackRenderer.material;
    }

    void Update()
    {
        if (!tankRigidbody) return;

        float forwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, tankRigidbody.transform.forward);
        float turnSpeed = tankRigidbody.angularVelocity.y;

        float leftSpeed = forwardSpeed - turnSpeed * turnScrollFactor * tankRigidbody.transform.localScale.x;
        float rightSpeed = forwardSpeed + turnSpeed * turnScrollFactor * tankRigidbody.transform.localScale.x;

        // и еще тут мб трабл с интегралом пока не тестил но может быть текстура сломается
        leftDistance += -leftSpeed * Time.deltaTime / (2f * Mathf.PI * wheelRadius);
        rightDistance += -rightSpeed * Time.deltaTime / (2f * Mathf.PI * wheelRadius);

        if (Application.isPlaying)
        {
            if (leftTrackMat) leftTrackMat.SetFloat("_Distance", leftDistance);
            if (rightTrackMat) rightTrackMat.SetFloat("_Distance", rightDistance);
        }
        // вот это [1]
        float leftRotation = leftSpeed * Time.deltaTime / (2f * Mathf.PI * wheelRadius) * 360f;
        RotateWheels(leftWheels, leftRotation);

        float rightRotation = rightSpeed * Time.deltaTime / (2f * Mathf.PI * wheelRadius) * 360f;
        RotateWheels(rightWheels, rightRotation);

        // float avgSpeed = (forwardSpeed + turnSpeed * 0.5f) * Time.deltaTime; если будут проблемы с ездой то удали то что выше[1] и все что тут закоменчено разкоменть
        // float rotationDegrees = avgSpeed / (2f * Mathf.PI * wheelRadius) * 360f;

        // RotateWheels(leftWheels, rotationDegrees); 
        // RotateWheels(rightWheels, rotationDegrees);
    }

    void RotateWheels(Transform[] wheels, float degrees)
    {
        if (wheels == null) return;
        foreach (var w in wheels)
        {
            if (w != null)
                w.Rotate(Vector3.right, degrees, Space.Self);
        }
    }
}
