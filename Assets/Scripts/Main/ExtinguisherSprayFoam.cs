using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherExtinguish_CameraRay : MonoBehaviour
{
    [Header("Safety Pin")]
    public string pinButtonName = "PinButton";
    private Button pinButton;
    private bool isPinPulled = false;

    [Header("Spray")]
    public ParticleSystem spray;
    public float sprayRange = 10f;

    [Header("Spray Lifetime")]
    public float normalStartLifetime = 0.1f;
    public float pressureStartLifetime = 0.3f;

    [Header("Pressure Mode")]
    public bool isPressureMode = false;

    [Header("Ray Source (AR Camera only)")]
    public Camera arCamera;

    [Header("Ray Origin (Extinguisher Nozzle)")]
    public Transform rayOrigin;

    [Header("Fire Target")]
    public LayerMask fireLayerMask;
    public float extinguishTime = 1f;

    [Header("Extinguish Distance")]
    public float normalExtinguishDistance = 2f;
    public float pressureExtinguishDistance = 4f;

    [Header("Debug Overlay")]
    public bool showDebug = true;

    [Header("UI Manager")]
    public GameUIManager uiManager;

    [Header("Range UI Toolkit Label")]
    public string rangeLabelName = "Range";
    private Label rangeLabel;

    [Header("Input Lock")]
    public float inputLockAfterWin = 0.3f;
    public float inputLockAfterRestart = 0.2f;
    private float inputLockTimer = 0f;

    [Header("Fuel")]
    public bool enableFuel = true;
    public float maxFuel = 60f;
    public float fuelUsePerSecond = 1f;
    public bool clearParticlesOnEmpty = true;

    [Header("Fuel UI Toolkit ProgressBar")]
    public UIDocument uiDocument;
    public string fuelBarName = "FuelBar";

    [Header("Challenge Manager")]
    public FireChallengeManager challengeManager;

    [Header("Extinguisher Switcher")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;

    [Header("Comms Input")]
    public bool showCommsLogs = true;
    private bool commsSprayHeld = false;

    [Header("Wrong Type Feedback")]
    public bool showWrongTypeLogs = true;
    public string wrongTypeText = "Wrong Type";

    private float fuel;
    private ProgressBar fuelBar;

    private float timer = 0f;
    private bool pressedNow = false;
    private bool hitNow = false;

    private MiniFireUnit currentMiniTarget = null;
    private bool uiBound = false;

    void Awake()
    {
        if (!spray) spray = GetComponent<ParticleSystem>();
        if (!arCamera) arCamera = Camera.main;

        ApplySprayModeSettings();
        fuel = maxFuel;

        CacheUIReferences();
        UpdateFuelUI();
        UpdateRangeUI("");
        RefreshPinButtonText();
    }

    void OnEnable()
    {
        CacheUIReferences();
        BindPinButton();
        RefreshPinButtonText();
        UpdateFuelUI();
        UpdateRangeUI("");
    }

    void OnDisable()
    {
        UnbindPinButton();
        ForceStopSpray();
    }

    void OnDestroy()
    {
        UnbindPinButton();
    }

    void CacheUIReferences()
    {
        if (uiDocument == null)
            return;

        var root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        pinButton = root.Q<Button>(pinButtonName);
        fuelBar = root.Q<ProgressBar>(fuelBarName);
        rangeLabel = root.Q<Label>(rangeLabelName);

        if (fuelBar != null)
        {
            fuelBar.lowValue = 0f;
            fuelBar.highValue = maxFuel;
        }

        if (pinButton == null)
            Debug.LogWarning($"[Extinguisher] Button '{pinButtonName}' not found.");

        if (fuelBar == null)
            Debug.LogWarning($"[Extinguisher] ProgressBar named '{fuelBarName}' not found.");

        if (rangeLabel == null)
            Debug.LogWarning($"[Extinguisher] Label named '{rangeLabelName}' not found.");
    }

    void BindPinButton()
    {
        if (pinButton == null || uiBound)
            return;

        pinButton.clicked += TogglePin;
        uiBound = true;
    }

    void UnbindPinButton()
    {
        if (pinButton == null || !uiBound)
            return;

        pinButton.clicked -= TogglePin;
        uiBound = false;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            ForceStopSpray();
            return;
        }

        if (uiManager != null && (uiManager.IsWinShowing() || uiManager.IsLoseShowing()))
        {
            ForceStopSpray();
            UpdateRangeUI("");
            return;
        }

        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.deltaTime;
            ForceStopSpray();
            UpdateFuelUI();
            UpdateRangeUI("");
            return;
        }

        pressedNow = IsPressed() && isPinPulled;

        if (!pressedNow)
        {
            UpdateRangeUI("");
            timer = 0f;
            currentMiniTarget = null;
        }

        if (enableFuel && fuel <= 0f)
        {
            pressedNow = false;
            ForceStopSpray();
            UpdateFuelUI();
            UpdateRangeUI("");
            return;
        }

        if (spray)
        {
            if (pressedNow)
            {
                if (!spray.isEmitting)
                    spray.Play();
            }
            else
            {
                if (spray.isEmitting || spray.isPlaying)
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
                UpdateRangeUI("");
                return;
            }
        }

        UpdateFuelUI();
        hitNow = false;

        if (pressedNow)
        {
            if (RayHitsFire(out RaycastHit hit))
            {
                float currentExtinguishDistance = GetCurrentExtinguishDistance();

                if (showDebug)
                {
                    Debug.Log($"[{gameObject.name}] Ray hit: {hit.collider.name}, distance: {hit.distance:F2}, limit: {currentExtinguishDistance:F2}");
                }

                if (hit.distance <= currentExtinguishDistance)
                {
                    if (IsCorrectExtinguisherForCurrentFire())
                    {
                        MiniFireUnit miniFire = hit.collider.GetComponentInParent<MiniFireUnit>();

                        if (miniFire != null)
                        {
                            hitNow = true;
                            UpdateRangeUI("In Range");

                            if (currentMiniTarget != miniFire)
                            {
                                currentMiniTarget = miniFire;
                                timer = 0f;
                            }
                        }
                        else
                        {
                            hitNow = false;
                            timer = 0f;
                            currentMiniTarget = null;
                            UpdateRangeUI("Missed");
                        }
                    }
                    else
                    {
                        hitNow = false;
                        timer = 0f;
                        currentMiniTarget = null;
                        UpdateRangeUI(wrongTypeText);

                        if (showWrongTypeLogs && challengeManager != null)
                        {
                            Debug.Log($"[{gameObject.name}] Wrong extinguisher type for current fire index {challengeManager.CurrentFireIndex}.");
                        }
                    }
                }
                else
                {
                    hitNow = false;
                    timer = 0f;
                    currentMiniTarget = null;
                    UpdateRangeUI("Out of Range");
                }
            }
            else
            {
                hitNow = false;
                timer = 0f;
                currentMiniTarget = null;
                UpdateRangeUI("Missed");

                if (showDebug)
                    Debug.Log($"[{gameObject.name}] Ray did not hit fire.");
            }
        }

        if (hitNow && currentMiniTarget != null)
        {
            timer += Time.deltaTime;

            if (timer >= extinguishTime)
            {
                timer = 0f;

                if (challengeManager != null)
                    challengeManager.TryStartMiniFireExtinguish(currentMiniTarget);

                currentMiniTarget = null;
            }
        }
        else
        {
            if (!pressedNow)
                timer = 0f;
        }
    }

    bool RayHitsFire(out RaycastHit hit)
    {
        hit = default;

        if (rayOrigin == null || arCamera == null)
            return false;

        Vector3 start = rayOrigin.position;
        Vector3 dir = arCamera.transform.forward;

        return Physics.Raycast(
            start,
            dir,
            out hit,
            sprayRange,
            fireLayerMask,
            QueryTriggerInteraction.Collide
        );
    }

    private bool IsCorrectExtinguisherForCurrentFire()
    {
        if (challengeManager == null || extinguisherSwitcher == null)
            return true;

        int requiredFireIndex = challengeManager.CurrentFireIndex;

        if (requiredFireIndex < 0)
            return false;

        return extinguisherSwitcher.CanCurrentExtinguisherPutOutFire(requiredFireIndex);
    }

    public void ResetGame()
    {
        timer = 0f;
        hitNow = false;
        pressedNow = false;
        currentMiniTarget = null;

        fuel = maxFuel;
        isPinPulled = false;
        commsSprayHeld = false;

        UpdateFuelUI();
        UpdateRangeUI("");
        RefreshPinButtonText();

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        inputLockTimer = inputLockAfterRestart;
    }

    public void ResetForNextFire(GameObject newFireRoot, bool refillFuel)
    {
        timer = 0f;
        hitNow = false;
        pressedNow = false;
        commsSprayHeld = false;
        currentMiniTarget = null;

        // Keep this extinguisher's own pin state
        // DO NOT reset isPinPulled here

        if (refillFuel)
        {
            fuel = maxFuel;
            UpdateFuelUI();
        }

        UpdateRangeUI("");

        if (spray)
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        RefreshPinButtonText();
        inputLockTimer = inputLockAfterRestart;
    }

    void UpdateFuelUI()
    {
        if (fuelBar != null)
            fuelBar.value = fuel;
    }

    void UpdateRangeUI(string text)
    {
        if (rangeLabel == null) return;

        rangeLabel.text = text;

        if (text == "In Range")
            rangeLabel.style.color = new StyleColor(Color.green);
        else if (text == "Out of Range" || text == wrongTypeText)
            rangeLabel.style.color = new StyleColor(Color.red);
        else
            rangeLabel.style.color = new StyleColor(Color.white);
    }

    void ForceStopSpray()
    {
        pressedNow = false;
        commsSprayHeld = false;
        currentMiniTarget = null;

        if (spray && (spray.isPlaying || spray.isEmitting))
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    bool IsPressed()
    {
        bool localPressed = false;

        if (Input.touchCount > 0) localPressed = true;
        if (Input.GetMouseButton(0)) localPressed = true;

        return localPressed || commsSprayHeld;
    }

    void TogglePin()
    {
        isPinPulled = !isPinPulled;
        RefreshPinButtonText();

        if (showDebug || showCommsLogs)
            Debug.Log($"[Extinguisher] {gameObject.name} pin pulled = {isPinPulled}");
    }

    void RefreshPinButtonText()
    {
        if (pinButton == null)
            return;

        pinButton.text = isPinPulled ? "Pin: OFF" : "Pin: ON";
    }

    public void PullPinFromComms()
    {
        isPinPulled = true;
        RefreshPinButtonText();

        if (showCommsLogs)
            Debug.Log($"[Extinguisher] {gameObject.name} pin pulled by comms.");
    }

    public void InsertPinFromComms()
    {
        isPinPulled = false;
        commsSprayHeld = false;
        RefreshPinButtonText();

        if (showCommsLogs)
            Debug.Log($"[Extinguisher] {gameObject.name} pin inserted by comms.");
    }

    public void SetCommsSprayHeld(bool held)
    {
        commsSprayHeld = held;

        if (showCommsLogs)
            Debug.Log($"[Extinguisher] {gameObject.name} comms spray held = {held}");
    }

    public void SetPressureMode(bool enabled)
    {
        isPressureMode = enabled;
        ApplySprayModeSettings();

        if (showDebug || showCommsLogs)
        {
            Debug.Log($"[Extinguisher] {gameObject.name} pressure mode set to {enabled}. Distance = {GetCurrentExtinguishDistance():F2}");
        }
    }

    public bool IsPressureMode()
    {
        return isPressureMode;
    }

    public bool IsPinPulled()
    {
        return isPinPulled;
    }

    public float GetCurrentExtinguishDistance()
    {
        return isPressureMode ? pressureExtinguishDistance : normalExtinguishDistance;
    }

    public float GetCurrentStartLifetime()
    {
        return isPressureMode ? pressureStartLifetime : normalStartLifetime;
    }

    private void ApplySprayModeSettings()
    {
        if (spray == null) return;

        var main = spray.main;
        main.startLifetime = GetCurrentStartLifetime();
    }
}