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

    [Header("Challenge Manager")]
    public FireChallengeManager challengeManager;

    // Fuel state
    private float fuel;

    // UI state
    private ProgressBar fuelBar;

    private float timer = 0f;
    private bool pressedNow = false;
    private bool hitNow = false;
    private bool extinguished = false;

    //fire extinguishing range
    public float range = 10f;
    public float extinguishDistance = 0.2f;   // maximum distance allowed
    public Camera cam;

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

        if (uiManager != null && (uiManager.IsWinShowing() || uiManager.IsLoseShowing()))
        {
            ForceStopSpray();
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

        // Extinguish only if spraying, ray hits fire, and hit is close enough
        hitNow = false;

        if (pressedNow && RayHitsFire(out RaycastHit hit))
        {
            if (showDebug)
                Debug.Log($"Hit fire at distance: {hit.distance:F2} when range: {extinguishDistance} m");

            if (hit.distance <= extinguishDistance)
            {
                hitNow = true;
            }
        }

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

        // advance the challenge
        if (challengeManager)
            challengeManager.OnFireExtinguished();
        else if (uiManager)
            uiManager.ShowWin();
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

        if (hideRenderersOnly)
        {
            foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }
        else
        {
            if (fireRoot.activeSelf) fireRoot.SetActive(true);
        }

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

    public void ResetForNextFire(GameObject newFireRoot, bool refillFuel)
    {
        timer = 0f;
        extinguished = false;
        hitNow = false;
        pressedNow = false;

        if (refillFuel)
        {
            fuel = maxFuel;
            UpdateFuelUI();
        }

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Switch target fireRoot
        if (newFireRoot != null)
            fireRoot = newFireRoot;

        inputLockTimer = inputLockAfterRestart;
    }
}