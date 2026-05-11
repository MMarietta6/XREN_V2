using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to the HoldToAdvance UI GameObject on the tutorial canvas.
/// The player holds either controller trigger for holdDuration seconds
/// to advance the current tutorial step — works as a universal bypass for all steps.
/// </summary>
public class HoldToAdvance : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds the trigger must be held to advance.")]
    public float holdDuration = 2f;

    [Header("UI References")]
    [Tooltip("Fill image that shows hold progress (Image type = Filled, Horizontal).")]
    public Image progressRing;

    [Tooltip("Label text on the button.")]
    public TextMeshProUGUI label;

    [Header("Colors")]
    public Color idleColor    = new Color(0.12f, 0.12f, 0.18f, 0.92f);
    public Color holdingColor = new Color(0.9f, 0.65f, 0f, 1f);
    public Color doneColor    = new Color(0.1f, 0.85f, 0.2f, 1f);

    // Set by StartupTutorialManager
    [HideInInspector] public StartupTutorialManager manager;

    private float _holdTimer = 0f;
    private bool  _isHolding = false;
    private bool  _fired     = false;
    private Image _background;

    // New Input System actions — bound to both controller triggers
    private InputAction _triggerAction;

    void Awake()
    {
        _background = GetComponent<Image>();
        // Fully transparent at rest — secret feature
        if (_background != null) _background.color = Color.clear;
        if (progressRing != null) { progressRing.fillAmount = 0f; progressRing.color = Color.clear; }
        if (label != null) label.color = Color.clear;

        // Bind to both XR controller triggers using the new Input System
        _triggerAction = new InputAction("HoldTrigger", InputActionType.Value);
        _triggerAction.AddBinding("<XRController>{RightHand}/trigger");
        _triggerAction.AddBinding("<XRController>{LeftHand}/trigger");
        // Keyboard Space for editor testing
        _triggerAction.AddBinding("<Keyboard>/space");
        _triggerAction.Enable();
    }

    void OnDestroy()
    {
        _triggerAction?.Disable();
        _triggerAction?.Dispose();
    }

    void OnEnable()
    {
        Reset();
    }

    void Update()
    {
        if (_fired || manager == null) return;

        bool held = IsHeld();

        if (held)
        {
            if (!_isHolding)
            {
                _isHolding = true;
                // Only reveal the progress fill — background and label stay invisible
                if (progressRing != null) progressRing.color = new Color(1f, 0.75f, 0f, 0.5f);
            }

            _holdTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_holdTimer / holdDuration);
            if (progressRing != null) progressRing.fillAmount = progress;

            if (_holdTimer >= holdDuration)
                Advance();
        }
        else
        {
            if (_isHolding)
            {
                _isHolding = false;
                _holdTimer = 0f;
                // Hide everything again
                if (progressRing != null) { progressRing.fillAmount = 0f; progressRing.color = Color.clear; }
            }
        }
    }

    bool IsHeld()
    {
        if (_triggerAction == null) return false;
        // Trigger value > 0.5 counts as "held"
        return _triggerAction.ReadValue<float>() > 0.5f;
    }

    void Advance()
    {
        _fired = true;
        if (progressRing != null) { progressRing.fillAmount = 1f; progressRing.color = new Color(0.1f, 0.85f, 0.2f, 0.6f); }

        manager.StepCompleted();

        // Reset after short delay
        Invoke(nameof(Reset), 0.5f);
    }

    public void Reset()
    {
        _fired     = false;
        _isHolding = false;
        _holdTimer = 0f;
        // Return to fully invisible
        if (progressRing != null) { progressRing.fillAmount = 0f; progressRing.color = Color.clear; }
        if (_background  != null) _background.color = Color.clear;
        if (label        != null) label.color = Color.clear;
    }
}
