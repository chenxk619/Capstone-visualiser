using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherExtinguish_CameraRay : MonoBehaviour
{
    [Header("Spray")]
    public ParticleSystem spray;
    public float sprayRange = 10f;

    [Header("Ray Source (AR Camera only)")]
    public Camera arCamera;
    public Vector3 rayViewportPoint = new Vector3(0.5f, 0.5f, 0f);

    [Header("Fire Target")]
    public LayerMask fireLayerMask;
    public float extinguishTime = 3f;
    public float extinguishDistance = 2.5f;

    [Header("Current Fire Root")]
    public GameObject fireRoot;
    public bool hideRenderersOnly = true;

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
    public float maxFuel = 5f;
    public float fuelUsePerSecond = 1f;
    public bool clearParticlesOnEmpty = true;

    [Header("Fuel UI Toolkit ProgressBar")]
    public UIDocument uiDocument;
    public string fuelBarName = "FuelBar";

    [Header("Challenge Manager")]
    public FireChallengeManager challengeManager;

    private float fuel;
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
                Debug.LogWarning($"[Extinguisher] ProgressBar named '{fuelBarName}' not found.");
            }
        }
    }

    void Update()
    {
        if (uiManager != null && (uiManager.IsWinShowing() || uiManager.IsLoseShowing()))
        {
            ForceStopSpray();
            return;
        }

        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.deltaTime;
            ForceStopSpray();
            UpdateFuelUI();
            return;
        }

        if (extinguished)
        {
            ForceStopSpray();
            return;
        }

        pressedNow = IsPressed();

        if (enableFuel && fuel <= 0f)
        {
            pressedNow = false;
            ForceStopSpray();
            UpdateFuelUI();
            return;
        }

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

        hitNow = false;

        if (pressedNow && RayHitsFire(out RaycastHit hit))
        {
            if (showDebug)
                Debug.Log($"Hit fire at distance: {hit.distance:F2} / limit: {extinguishDistance:F2}");

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

        return Physics.Raycast(
            ray,
            out hit,
            sprayRange,
            fireLayerMask,
            QueryTriggerInteraction.Collide
        );
    }

    void Extinguish()
    {
        extinguished = true;
        timer = 0f;
        hitNow = false;
        pressedNow = false;

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        HideCurrentFire();

        inputLockTimer = inputLockAfterWin;

        if (challengeManager != null)
            challengeManager.OnFireExtinguished();
        else if (uiManager != null)
            uiManager.ShowWin();
    }

    void HideCurrentFire()
    {
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
    }

    void ShowCurrentFire()
    {
        if (!fireRoot) return;

        if (hideRenderersOnly)
        {
            foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }
        else
        {
            fireRoot.SetActive(true);
        }
    }

    public void ResetGame()
    {
        timer = 0f;
        extinguished = false;
        hitNow = false;
        pressedNow = false;

        fuel = maxFuel;
        UpdateFuelUI();

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ShowCurrentFire();

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

    bool IsPressed()
    {
        if (Input.touchCount > 0) return true;
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

        if (newFireRoot != null)
            fireRoot = newFireRoot;

        ShowCurrentFire();

        inputLockTimer = inputLockAfterRestart;
    }
}