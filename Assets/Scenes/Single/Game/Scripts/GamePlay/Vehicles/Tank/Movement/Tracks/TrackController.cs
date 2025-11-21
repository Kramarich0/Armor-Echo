using UnityEngine;

public class TrackController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody tankRigidbody;
    public Renderer leftTrackRenderer;
    public Renderer rightTrackRenderer;

    [Header("Settings")]
    public string textureProperty = "_MainTex";
    public float scrollSpeedFactor = 0.05f;
    public float turnScrollFactor = 0.7f;
    public float wheelRadius = 0.3f;

    [Header("Wheels (optional)")]
    public Transform[] leftWheels;
    public Transform[] rightWheels;

    private Vector2 leftOffset, rightOffset;

    void Start()
    {
        if (!tankRigidbody) tankRigidbody = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (!tankRigidbody) return;

        float forwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, tankRigidbody.transform.forward);
        float turnSpeed = tankRigidbody.angularVelocity.y;

        float leftSpeed = forwardSpeed - turnSpeed * turnScrollFactor * tankRigidbody.transform.localScale.x;
        float rightSpeed = forwardSpeed + turnSpeed * turnScrollFactor * tankRigidbody.transform.localScale.x;

        leftOffset.y += leftSpeed * scrollSpeedFactor * Time.deltaTime;
        rightOffset.y += rightSpeed * scrollSpeedFactor * Time.deltaTime;

        if (leftTrackRenderer)
            leftTrackRenderer.material.SetTextureOffset(textureProperty, leftOffset);
        if (rightTrackRenderer)
            rightTrackRenderer.material.SetTextureOffset(textureProperty, rightOffset);

        float avgSpeed = (forwardSpeed + (turnSpeed * 0.5f)) * Time.deltaTime;
        float distance = avgSpeed;
        float rotationDegrees = distance / (2f * Mathf.PI * wheelRadius) * 360f;

        RotateWheels(leftWheels, rotationDegrees);
        RotateWheels(rightWheels, rotationDegrees);
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
