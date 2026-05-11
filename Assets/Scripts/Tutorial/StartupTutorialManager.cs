using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the UMC-500 Startup Procedure tutorial as a linear state machine.
/// Attach to the TutorialManager GameObject in the scene.
/// </summary>
public class StartupTutorialManager : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────────

    [Header("UI References")]
    public Canvas tutorialCanvas;
    public TextMeshProUGUI stepTitleText;
    public TextMeshProUGUI stepInstructionText;
    public TextMeshProUGUI stepCounterText;
    public GameObject completionPanel;
    public TextMeshProUGUI completionText;
    public GameObject highlightArrow;

    [Header("Player / Camera")]
    public Transform playerCamera;          // Main Camera transform (XR Origin)

    [Header("UI Follow Settings")]
    public float followDistance = 0.6f;     // metres in front of camera
    public float followHeight   = -0.15f;   // offset below camera centre
    public float followSmoothing = 4f;      // lerp speed

    [Header("Tutorial Interactables")]
    public TutorialButton valveInteractable;
    public TutorialButton powerOnButton;
    public TutorialButton emergencyStopButton;   // also accepts twist
    public TutorialButton doorButton;
    public TutorialButton powerUpButton;
    public TutorialButton handleJobButton;
    public TutorialButton diagnosticButton;

    [Header("Status Light")]
    public Renderer statusLightRenderer;
    public Material redLightMaterial;
    public Material greenLightMaterial;

    [Header("Hold-to-Advance")]
    [Tooltip("The HoldToAdvance component on the tutorial canvas.")]
    public HoldToAdvance holdToAdvance;

    // ── Step Data ─────────────────────────────────────────────────────────────

    private struct TutorialStep
    {
        public string title;
        public string instruction;
        public TutorialButton button;       // null = auto-advance
    }

    private TutorialStep[] steps;
    private int currentStep = -1;
    private bool tutorialComplete = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        BuildSteps();
        SetupButtonCallbacks();

        if (completionPanel != null) completionPanel.SetActive(false);

        // Wire hold-to-advance
        if (holdToAdvance != null)
            holdToAdvance.manager = this;

        AdvanceStep();
    }

    void Update()
    {
        if (tutorialComplete) return;
        FollowCamera();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    void BuildSteps()
    {
        steps = new TutorialStep[]
        {
            new TutorialStep
            {
                title       = "Step 1 — Check the Valve",
                instruction = "Locate and inspect the coolant valve on the side of the machine.\nPress the valve to confirm it is open.",
                button      = valveInteractable
            },
            new TutorialStep
            {
                title       = "Step 2 — Power ON",
                instruction = "Press the POWER ON button on the Control Panel.",
                button      = powerOnButton
            },
            new TutorialStep
            {
                title       = "Step 3 — Emergency Stop",
                instruction = "Twist the EMERGENCY STOP button to the RIGHT to release it.\nYou can also press it to confirm.",
                button      = emergencyStopButton
            },
            new TutorialStep
            {
                title       = "Step 4 — Close the Door",
                instruction = "Grab and slide the Auto Door fully closed.\nPress the door button to confirm.",
                button      = doorButton
            },
            new TutorialStep
            {
                title       = "Step 5 — Power Up",
                instruction = "Press the POWER UP button on the Control Panel.",
                button      = powerUpButton
            },
            new TutorialStep
            {
                title       = "Step 6 — Handle Job",
                instruction = "Press the HANDLE JOB button on the Control Panel.",
                button      = handleJobButton
            },
            new TutorialStep
            {
                title       = "Step 7 — Diagnostic",
                instruction = "Press the DIAGNOSTIC button to complete the startup procedure.",
                button      = diagnosticButton
            }
        };
    }

    void SetupButtonCallbacks()
    {
        // Each button calls StepCompleted when activated
        TutorialButton[] allButtons = new TutorialButton[]
        {
            valveInteractable, powerOnButton, emergencyStopButton,
            doorButton, powerUpButton, handleJobButton, diagnosticButton
        };

        foreach (var btn in allButtons)
        {
            if (btn != null)
                btn.OnActivated += StepCompleted;
        }
    }

    // ── Step Logic ────────────────────────────────────────────────────────────

    public void StepCompleted()
    {
        if (tutorialComplete) return;
        AdvanceStep();
    }

    void AdvanceStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            CompleteTutorial();
            return;
        }

        var step = steps[currentStep];

        // Update UI
        if (stepTitleText       != null) stepTitleText.text       = step.title;
        if (stepInstructionText != null) stepInstructionText.text = step.instruction;
        if (stepCounterText     != null) stepCounterText.text     = $"Step {currentStep + 1} / {steps.Length}";

        // Status light: red for steps 0-5, green triggered at completion
        if (statusLightRenderer != null && currentStep == 0)
            statusLightRenderer.material = redLightMaterial;

        // Move highlight arrow to current target button
        if (highlightArrow != null && step.button != null)
        {
            highlightArrow.SetActive(true);
            highlightArrow.transform.position = step.button.transform.position + Vector3.up * 0.05f;
        }

        // Activate only the current step's button
        ActivateOnlyCurrentButton();

        // Reset hold-to-advance for the new step
        if (holdToAdvance != null) holdToAdvance.Reset();
    }

    void ActivateOnlyCurrentButton()
    {
        TutorialButton[] allButtons = new TutorialButton[]
        {
            valveInteractable, powerOnButton, emergencyStopButton,
            doorButton, powerUpButton, handleJobButton, diagnosticButton
        };

        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
                allButtons[i].SetActive(i == currentStep);
        }
    }

    void CompleteTutorial()
    {
        tutorialComplete = true;

        // Switch status light to green
        if (statusLightRenderer != null && greenLightMaterial != null)
            statusLightRenderer.material = greenLightMaterial;

        // Hide step UI, show completion panel
        if (stepTitleText       != null) stepTitleText.gameObject.SetActive(false);
        if (stepInstructionText != null) stepInstructionText.gameObject.SetActive(false);
        if (stepCounterText     != null) stepCounterText.gameObject.SetActive(false);
        if (highlightArrow      != null) highlightArrow.SetActive(false);

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            StartCoroutine(AnimateCompletion());
        }

        // Hide hold-to-advance on completion
        if (holdToAdvance != null) holdToAdvance.gameObject.SetActive(false);
    }

    IEnumerator AnimateCompletion()
    {
        // Pulse the completion panel scale for a satisfying pop
        if (completionPanel == null) yield break;

        completionPanel.transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float scale = Mathf.SmoothStep(0f, 1f, t);
            // Overshoot for a bouncy feel
            float overshoot = scale < 0.9f ? scale * 1.1f : 1f;
            completionPanel.transform.localScale = Vector3.one * overshoot;
            yield return null;
        }
        completionPanel.transform.localScale = Vector3.one;
    }

    // ── Camera Follow ─────────────────────────────────────────────────────────

    void FollowCamera()
    {
        if (playerCamera == null || tutorialCanvas == null) return;

        // Target position: in front of and slightly below the camera
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 targetPos = playerCamera.position
                          + forward * followDistance
                          + Vector3.up * followHeight;

        tutorialCanvas.transform.position = Vector3.Lerp(
            tutorialCanvas.transform.position,
            targetPos,
            Time.deltaTime * followSmoothing
        );

        // Always face the player
        tutorialCanvas.transform.rotation = Quaternion.Lerp(
            tutorialCanvas.transform.rotation,
            Quaternion.LookRotation(tutorialCanvas.transform.position - playerCamera.position),
            Time.deltaTime * followSmoothing
        );
    }
}
