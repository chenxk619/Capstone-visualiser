using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherExtinguish_CameraRay : MonoBehaviour
{
    [Header("Spray")]
    public ParticleSystem spray;            // your spray particle system
    public float sprayRange = 10f;          // try 20 on iPhone while testing

    [Header("Ray Source (AR Camera)")]
    public Camera arCamera;                // drag ARCamera here
    public Vector3 rayViewportPoint = new Vector3(0.5f, 0.5f, 0f); // center of screen

    [Header("Fire Target")]
    public LayerMask fireLayerMask;        // set to Fire layer only
    public float extinguishTime = 3f;      // seconds of continuous hit

    [Header("What to hide when extinguished")]
    public GameObject fireRoot;            // drag your fire model root (the augmentation object)
    public bool hideRenderersOnly = true;  // recommended for Vuforia objects

    [Header("Debug Overlay")]
    public bool showDebug = true;

    [Header("UI Manager")]
    public GameUIManager uiManager;

    [Header("Input Lock")]
    public float inputLockAfterWin = 0.3f;
    public float inputLockAfterRestart = 0.2f;
    private float inputLockTimer = 0f;

    [Header("Fuel")]
    public bool enableFuel = true;
    public float maxFuel = 5f;                 // seconds worth (if fuelUsePerSecond=1)
    public float fuelUsePerSecond = 1f;        // drain per second while spraying
    public bool clearParticlesOnEmpty = true;

    [Header("Fuel UI Toolkit ProgressBar")]
    public UIDocument uiDocument;              // drag your UIDocument here
    public string fuelBarName = "FuelBar";     // ProgressBar Name in UI Builder

    // Fuel state
    private float fuel;

    // UI state
    private ProgressBar fuelBar;

    private float timer = 0f;
    private bool pressedNow = false;
    private bool hitNow = false;
    private bool extinguished = false;

    void Start()
    {
        if (!spray) spray = GetComponent<ParticleSystem>();
        if (!arCamera) arCamera = Camera.main;

        fuel = maxFuel;

        if (enableFuel && uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            fuelBar = root.Q<ProgressBar>(fuelBarName);

            if (fuelBar != null)
            {
                fuelBar.lowValue = 0f;
                fuelBar.highValue = maxFuel;
                fuelBar.value = fuel;
            }
            else
            {
                Debug.LogWarning($"[Extinguisher] ProgressBar named '{fuelBarName}' not found. Fuel UI won't update.");
            }
        }
    }

    void Update()
    {
        // If win panel is showing, force spray OFF and ignore input
        if (uiManager != null && uiManager.IsWinShowing())
        {
            ForceStopSpray();
            KeepFireHidden();
            return;
        }

        // Lock input briefly after win/restart (extra safety)
        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.deltaTime;
            ForceStopSpray();
            UpdateFuelUI();
            return;
        }

        // Read input
        pressedNow = IsPressed();

        // Fuel gate: if empty, behave like locked state
        if (enableFuel && fuel <= 0f)
        {
            pressedNow = false;
            ForceStopSpray();
            UpdateFuelUI();
            return;
        }

        // Spray control
        if (spray)
        {
            if (pressedNow)
            {
                if (!spray.isEmitting) spray.Play();
            }
            else
            {
                if (spray.isEmitting)
                    spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Drain fuel while spraying
        if (enableFuel && pressedNow)
        {
            fuel -= fuelUsePerSecond * Time.deltaTime;

            if (fuel <= 0f)
            {
                fuel = 0f;

                if (spray && (spray.isPlaying || spray.isEmitting))
                {
                    var mode = clearParticlesOnEmpty
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting;

                    spray.Stop(true, mode);
                }

                pressedNow = false;
                UpdateFuelUI();
                return;
            }
        }

        UpdateFuelUI();

        if (extinguished)
        {
            KeepFireHidden();
            return;
        }

        hitNow = pressedNow && RayHitsFire(out RaycastHit hit);

        if (hitNow)
        {
            timer += Time.deltaTime;
            if (timer >= extinguishTime)
                Extinguish();
        }
        else
        {
            timer = 0f;
        }
    }

    bool RayHitsFire(out RaycastHit hit)
    {
        hit = default;
        if (!arCamera) return false;

        Ray ray = arCamera.ViewportPointToRay(rayViewportPoint);
        return Physics.Raycast(ray, out hit, sprayRange, fireLayerMask, QueryTriggerInteraction.Collide);
    }

    void Extinguish()
    {
        extinguished = true;

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        inputLockTimer = inputLockAfterWin;

        if (!fireRoot) return;

        if (hideRenderersOnly)
        {
            foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        }
        else
        {
            fireRoot.SetActive(false);
        }

        if (uiManager) uiManager.ShowWin();
    }

    // CALLED BY YOUR RESTART BUTTON VIA GameUIManager
    public void ResetGame()
    {
        timer = 0f;
        extinguished = false;
        hitNow = false;
        pressedNow = false;

        // REFILL FUEL + UPDATE BAR
        fuel = maxFuel;
        UpdateFuelUI();

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        inputLockTimer = inputLockAfterRestart;
    }

    void UpdateFuelUI()
    {
        if (fuelBar != null)
            fuelBar.value = fuel;
    }

    void ForceStopSpray()
    {
        pressedNow = false;

        if (spray && (spray.isPlaying || spray.isEmitting))
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void KeepFireHidden()
    {
        if (!fireRoot) return;

        if (hideRenderersOnly)
        {
            foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        }
        else
        {
            if (fireRoot.activeSelf) fireRoot.SetActive(false);
        }
    }

    bool IsPressed()
    {
        // iPhone touch
        if (Input.touchCount > 0) return true;

        // Editor mouse testing
        if (Input.GetMouseButton(0)) return true;

        return false;
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 40;
        style.normal.textColor = Color.white;

        string msg =
            $"Pressed: {(pressedNow ? "YES" : "no")}\n" +
            $"Hit Fire: {(hitNow ? "YES" : "no")}\n" +
            $"Timer: {timer:F2} / {extinguishTime:F2}\n" +
            $"Range: {sprayRange:F1}\n" +
            $"Fuel: {fuel:F2} / {maxFuel:F2}\n" +
            $"ARCamera: {(arCamera ? arCamera.name : "None")}\n" +
            $"FireRoot: {(fireRoot ? fireRoot.name : "None")}\n" +
            $"Extinguished: {(extinguished ? "YES" : "no")}\n" +
            $"WinShowing: {(uiManager != null && uiManager.IsWinShowing() ? "YES" : "no")}";
        
        GUI.Label(new Rect(30, 30, 900, 520), msg, style);
    }
}