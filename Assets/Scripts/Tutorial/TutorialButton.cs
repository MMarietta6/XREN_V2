using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to any tutorial interactable GameObject.
/// Fires OnActivated when the player interacts with it (select / activate).
/// Also supports a fallback "confirm" via keyboard/controller button for E-stop twist.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class TutorialButton : MonoBehaviour
{
    public event Action OnActivated;

    [Header("Settings")]
    [Tooltip("If true, this button also listens for a twist gesture (E-stop).")]
    public bool requiresTwist = false;

    [Tooltip("Highlight renderer to pulse when this step is active.")]
    public Renderer highlightRenderer;

    [Tooltip("Material to apply when this step is active.")]
    public Material activeMaterial;

    [Tooltip("Material to apply when this step is inactive.")]
    public Material inactiveMaterial;

    private XRSimpleInteractable _interactable;
    private bool _isActive = false;
    private bool _fired = false;

    // For twist detection
    private bool _isGrabbed = false;
    private Quaternion _grabStartRotation;
    private const float TwistThresholdDegrees = 30f;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.activated.AddListener(OnActivate);
    }

    void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.activated.RemoveListener(OnActivate);
        }
    }

    void Update()
    {
        if (!_isActive || _fired) return;

        // Twist detection: check rotation delta from grab start
        if (requiresTwist && _isGrabbed)
        {
            float angle = Quaternion.Angle(_grabStartRotation, transform.rotation);
            if (angle >= TwistThresholdDegrees)
            {
                Fire();
            }
        }

        // Pulse highlight
        if (highlightRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            highlightRenderer.material.SetFloat("_EmissiveIntensity", Mathf.Lerp(0.5f, 2.5f, pulse));
        }
    }

    // Called by StartupTutorialManager to enable/disable this step
    public void SetActive(bool active)
    {
        _isActive = active;
        _fired = false;
        _interactable.enabled = active;

        if (highlightRenderer != null)
        {
            highlightRenderer.material = active ? activeMaterial : inactiveMaterial;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!_isActive || _fired) return;

        if (requiresTwist)
        {
            // Record rotation at grab start for twist detection
            _isGrabbed = true;
            _grabStartRotation = transform.rotation;
        }
        else
        {
            // Simple press — fire immediately on select
            Fire();
        }
    }

    private void OnActivate(ActivateEventArgs args)
    {
        if (!_isActive || _fired) return;
        Fire();
    }

    // Public so the UI "Confirm" button can also call this
    public void ConfirmManually()
    {
        if (!_isActive || _fired) return;
        Fire();
    }

    private void Fire()
    {
        if (_fired) return;
        _fired = true;
        _isGrabbed = false;
        OnActivated?.Invoke();
    }
}
