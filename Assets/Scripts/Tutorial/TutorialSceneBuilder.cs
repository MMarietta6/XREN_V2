using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility. Run via Tools > UMC Tutorial > Build Tutorial Scene.
/// Creates all GameObjects, materials, UI, and wires up the StartupTutorialManager.
/// </summary>
public static class TutorialSceneBuilder
{
    // ── World positions (measured from scene data) ────────────────────────────
    // Control panel world pos: (3.47, 0.96, -1.16)  lossyScale ~0.001
    // The panel face is roughly at Z = -1.16, facing -Z direction in world space
    // We place buttons slightly in front of the panel face

    // Control panel approximate world bounds (from screenshot: tall vertical panel)
    // Panel top ~y=1.35, bottom ~y=0.60, face at z ~ -1.13
    private static readonly Vector3 PanelPos    = new Vector3(3.47f, 0.96f, -1.16f);
    private static readonly Vector3 ValvePos    = new Vector3(3.673f, 0.92f, 0.165f);

    // Button positions on the panel face (world space)
    // Status light: top protrusion of panel
    private static readonly Vector3 StatusLightOffset  = new Vector3(0f,    0.38f,  0.04f);
    // Power ON: upper face of panel
    private static readonly Vector3 PowerOnOffset      = new Vector3(0f,    0.18f,  0.04f);
    // E-Stop: side protrusion (left side of panel in screenshot)
    private static readonly Vector3 EStopOffset        = new Vector3(-0.06f, 0.05f, 0.04f);
    // Door confirm button (placed near door area)
    private static readonly Vector3 DoorOffset         = new Vector3(0f,   -0.05f,  0.04f);
    // Power Up
    private static readonly Vector3 PowerUpOffset      = new Vector3(0f,   -0.12f,  0.04f);
    // Handle Job
    private static readonly Vector3 HandleJobOffset    = new Vector3(0f,   -0.19f,  0.04f);
    // Diagnostic
    private static readonly Vector3 DiagnosticOffset   = new Vector3(0f,   -0.26f,  0.04f);

    [MenuItem("Tools/UMC Tutorial/Build Tutorial Scene")]
    public static void Build()
    {
        // ── Materials ─────────────────────────────────────────────────────────
        Material redMat    = CreateEmissiveMaterial("RedLightMat",    new Color(1f, 0.05f, 0.05f), 3f);
        Material greenMat  = CreateEmissiveMaterial("GreenLightMat",  new Color(0.05f, 1f, 0.1f),  3f);
        Material activeMat = CreateEmissiveMaterial("ButtonActiveMat",new Color(1f, 0.85f, 0f),    2f);
        Material inactMat  = CreateEmissiveMaterial("ButtonInactMat", new Color(0.2f, 0.2f, 0.2f), 0f);
        Material arrowMat  = CreateEmissiveMaterial("ArrowMat",       new Color(1f, 0.6f, 0f),     2.5f);

        // ── Status Light ──────────────────────────────────────────────────────
        GameObject statusLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        statusLight.name = "StatusLight";
        statusLight.transform.position = PanelPos + StatusLightOffset;
        statusLight.transform.localScale = Vector3.one * 0.025f;
        Object.DestroyImmediate(statusLight.GetComponent<SphereCollider>());
        statusLight.GetComponent<Renderer>().material = redMat;

        // ── Buttons ───────────────────────────────────────────────────────────
        TutorialButton valveBtn      = CreateButton("ValveInteractable",   ValvePos,                  0.04f,  activeMat, inactMat, false);
        TutorialButton powerOnBtn    = CreateButton("PowerOnButton",       PanelPos + PowerOnOffset,  0.025f, activeMat, inactMat, false);
        TutorialButton eStopBtn      = CreateButton("EmergencyStopButton", PanelPos + EStopOffset,    0.04f,  activeMat, inactMat, true);
        TutorialButton doorBtn       = CreateButton("DoorButton",          PanelPos + DoorOffset,     0.025f, activeMat, inactMat, false);
        TutorialButton powerUpBtn    = CreateButton("PowerUpButton",       PanelPos + PowerUpOffset,  0.025f, activeMat, inactMat, false);
        TutorialButton handleJobBtn  = CreateButton("HandleJobButton",     PanelPos + HandleJobOffset,0.025f, activeMat, inactMat, false);
        TutorialButton diagnosticBtn = CreateButton("DiagnosticButton",    PanelPos + DiagnosticOffset,0.025f,activeMat, inactMat, false);

        // Add labels above each button
        AddWorldLabel(powerOnBtn.gameObject,    "POWER ON",       0.03f);
        AddWorldLabel(eStopBtn.gameObject,      "E-STOP\n(Twist RIGHT or Press)", 0.05f);
        AddWorldLabel(doorBtn.gameObject,       "DOOR CLOSED",    0.03f);
        AddWorldLabel(powerUpBtn.gameObject,    "POWER UP",       0.03f);
        AddWorldLabel(handleJobBtn.gameObject,  "HANDLE JOB",     0.03f);
        AddWorldLabel(diagnosticBtn.gameObject, "DIAGNOSTIC",     0.03f);
        AddWorldLabel(valveBtn.gameObject,      "VALVE CHECK",    0.05f);

        // ── Highlight Arrow ───────────────────────────────────────────────────
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "HighlightArrow";
        arrow.transform.localScale = new Vector3(0.01f, 0.025f, 0.01f);
        Object.DestroyImmediate(arrow.GetComponent<CapsuleCollider>());
        arrow.GetComponent<Renderer>().material = arrowMat;
        arrow.AddComponent<TutorialHighlighter>();
        arrow.SetActive(false);

        // ── Tutorial Canvas (World Space) ─────────────────────────────────────
        GameObject canvasGO = new GameObject("TutorialCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Size: 0.4m wide x 0.25m tall in world space
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400f, 250f);
        canvasGO.transform.localScale = Vector3.one * 0.001f; // 1 unit = 1mm → 0.001 scale = 1m

        // Background panel
        GameObject bg = CreateUIPanel(canvasGO.transform, "Background",
            new Vector2(0, 0), new Vector2(400, 250),
            new Color(0.05f, 0.05f, 0.1f, 0.88f));

        // Step counter (top right)
        TextMeshProUGUI counterTMP = CreateTMPText(canvasGO.transform, "StepCounter",
            new Vector2(120, 110), new Vector2(160, 30),
            "Step 1 / 7", 14, TextAlignmentOptions.Right, Color.white);

        // Title
        TextMeshProUGUI titleTMP = CreateTMPText(canvasGO.transform, "StepTitle",
            new Vector2(0, 70), new Vector2(360, 40),
            "Step 1 — Check the Valve", 18, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f));

        // Instruction
        TextMeshProUGUI instrTMP = CreateTMPText(canvasGO.transform, "StepInstruction",
            new Vector2(0, -10), new Vector2(360, 100),
            "Locate and inspect the coolant valve.", 13, TextAlignmentOptions.Center, Color.white);

        // ── Completion Panel ──────────────────────────────────────────────────
        GameObject completionPanel = CreateUIPanel(canvasGO.transform, "CompletionPanel",
            new Vector2(0, 0), new Vector2(400, 250),
            new Color(0.02f, 0.18f, 0.04f, 0.95f));
        completionPanel.SetActive(false);

        TextMeshProUGUI completeTMP = CreateTMPText(completionPanel.transform, "CompleteText",
            new Vector2(0, 20), new Vector2(360, 80),
            "✓  STARTUP COMPLETE", 22, TextAlignmentOptions.Center, new Color(0.2f, 1f, 0.3f));

        CreateTMPText(completionPanel.transform, "CompleteSubText",
            new Vector2(0, -30), new Vector2(360, 50),
            "The UMC-500 is ready for operation.", 13, TextAlignmentOptions.Center, Color.white);

        // ── TutorialManager ───────────────────────────────────────────────────
        GameObject managerGO = new GameObject("TutorialManager");
        StartupTutorialManager mgr = managerGO.AddComponent<StartupTutorialManager>();

        // Wire up references
        mgr.tutorialCanvas       = canvas;
        mgr.stepTitleText        = titleTMP;
        mgr.stepInstructionText  = instrTMP;
        mgr.stepCounterText      = counterTMP;
        mgr.completionPanel      = completionPanel;
        mgr.completionText       = completeTMP;
        mgr.highlightArrow       = arrow;

        mgr.valveInteractable    = valveBtn;
        mgr.powerOnButton        = powerOnBtn;
        mgr.emergencyStopButton  = eStopBtn;
        mgr.doorButton           = doorBtn;
        mgr.powerUpButton        = powerUpBtn;
        mgr.handleJobButton      = handleJobBtn;
        mgr.diagnosticButton     = diagnosticBtn;

        mgr.statusLightRenderer  = statusLight.GetComponent<Renderer>();
        mgr.redLightMaterial     = redMat;
        mgr.greenLightMaterial   = greenMat;

        // Find player camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
            mgr.playerCamera = mainCam.transform;
        else
            Debug.LogWarning("[TutorialSceneBuilder] Could not find Main Camera. Assign playerCamera manually.");

        // ── Save materials to Assets ──────────────────────────────────────────
        SaveMaterial(redMat,    "Assets/Scripts/Tutorial/Materials/RedLightMat.mat");
        SaveMaterial(greenMat,  "Assets/Scripts/Tutorial/Materials/GreenLightMat.mat");
        SaveMaterial(activeMat, "Assets/Scripts/Tutorial/Materials/ButtonActiveMat.mat");
        SaveMaterial(inactMat,  "Assets/Scripts/Tutorial/Materials/ButtonInactMat.mat");
        SaveMaterial(arrowMat,  "Assets/Scripts/Tutorial/Materials/ArrowMat.mat");

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[TutorialSceneBuilder] Tutorial scene built successfully. Assign playerCamera if not auto-detected.");
        Selection.activeGameObject = managerGO;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static TutorialButton CreateButton(string name, Vector3 worldPos, float radius,
        Material activeMat, Material inactMat, bool requiresTwist)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = worldPos;
        go.transform.localScale = new Vector3(radius, radius * 0.4f, radius);

        // Remove default collider, add sphere collider for better XR grab feel
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.radius = 1f;

        // Add Rigidbody (required by XRSimpleInteractable)
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // XR Interactable
        go.AddComponent<XRSimpleInteractable>();

        // Tutorial button component
        TutorialButton btn = go.AddComponent<TutorialButton>();
        btn.requiresTwist    = requiresTwist;
        btn.activeMaterial   = activeMat;
        btn.inactiveMaterial = inactMat;
        btn.highlightRenderer = go.GetComponent<Renderer>();
        go.GetComponent<Renderer>().material = inactMat;

        return btn;
    }

    static void AddWorldLabel(GameObject parent, string text, float yOffset)
    {
        GameObject labelGO = new GameObject("Label_" + parent.name);
        labelGO.transform.SetParent(parent.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, yOffset, 0f);
        labelGO.transform.localScale    = Vector3.one * 0.012f;

        TextMeshPro tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = text;
        tmp.fontSize  = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        RectTransform rt = labelGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10f, 4f);
    }

    static Material CreateEmissiveMaterial(string name, Color emissiveColor, float intensity)
    {
        // Use URP Lit shader if available, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;

        // URP emissive
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissiveColor * intensity);
        }
        mat.color = emissiveColor;
        return mat;
    }

    static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI CreateTMPText(Transform parent, string name, Vector2 anchoredPos,
        Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.color     = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return tmp;
    }

    static void SaveMaterial(Material mat, string path)
    {
        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        if (!AssetDatabase.Contains(mat))
            AssetDatabase.CreateAsset(mat, path);
    }
}
#endif
