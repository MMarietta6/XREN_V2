using UnityEngine;

/// <summary>
/// Attach to the highlight arrow GameObject.
/// Bobs up and down and rotates to point at the target.
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobAmplitude = 0.015f;
    public float bobSpeed     = 2.5f;

    [Header("Rotation")]
    public float rotationSpeed = 90f;   // degrees per second around Y

    private Vector3 _startLocalPos;

    void Start()
    {
        _startLocalPos = transform.localPosition;
    }

    void Update()
    {
        // Bob
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = _startLocalPos + Vector3.up * yOffset;

        // Spin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
