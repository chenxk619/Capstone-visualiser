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

    [Header("Ray Source")]
    public Camera arCamera;

    [Header("Ray Origin (Extinguisher Nozzle)")]
    public Transform rayOrigin;

    [Header("Raycast Close-Range Fix")]
    public float rayStartOffset = 0.05f;

    [Header("Fire Target")]
    public LayerMask fireLayerMask;

    [Header("Extinguish Distance")]
    public float normalExtinguishDistance = 2f;
    public float pressureExtinguishDistance = 8f;

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

    bool HasInfiniteFuel()
    {
        return challengeManager != null && challengeManager.cheatMode;
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
            currentMiniTarget = null;
        }

        if (enableFuel && !HasInfiniteFuel() && fuel <= 0f)
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

        if (enableFuel)
        {
            if (HasInfiniteFuel())
            {
                fuel = maxFuel;
            }
            else if (pressedNow)
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
                    Vector3 start = rayOrigin != null
                        ? rayOrigin.position + rayOrigin.forward * rayStartOffset
                        : Vector3.zero;

                    Debug.Log(
                        $"[{gameObject.name}] Ray hit: {hit.collider.name}" +
                        $" | hitDistance={hit.distance:F2}" +
                        $" | allowedDistance={currentExtinguishDistance:F2}" +
                        $" | sprayRange={sprayRange:F2}" +
                        $" | pressureMode={isPressureMode}" +
                        $" | rayStart={start.ToString("F3")}" +
                        $" | rayDir={(rayOrigin != null ? rayOrigin.forward.ToString("F3") : "null")}"
                    );
                }

                if (hit.distance <= currentExtinguishDistance)
                {
                    if (IsCorrectExtinguisherForCurrentFire())
                    {
                        MiniFireUnit miniFire = hit.collider.GetComponentInParent<MiniFireUnit>();

                        if (miniFire != null)
                        {
                            hitNow = true;
                            currentMiniTarget = miniFire;
                            UpdateRangeUI("In Range");

                            if (challengeManager != null)
                                challengeManager.TrySprayMiniFire(currentMiniTarget, Time.deltaTime);
                        }
                        else
                        {
                            hitNow = false;
                            currentMiniTarget = null;

                            if (showDebug)
                            {
                                Debug.Log($"[{gameObject.name}] Hit collider has no MiniFireUnit in parent. Collider = {hit.collider.name}");
                            }

                            UpdateRangeUI("Missed");
                        }
                    }
                    else
                    {
                        hitNow = false;
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
                    currentMiniTarget = null;

                    if (showDebug)
                    {
                        Debug.Log(
                            $"[{gameObject.name}] OUT OF RANGE" +
                            $" | hit={hit.collider.name}" +
                            $" | hitDistance={hit.distance:F2}" +
                            $" | allowedDistance={currentExtinguishDistance:F2}" +
                            $" | pressureMode={isPressureMode}"
                        );
                    }

                    UpdateRangeUI("Out of Range");
                }
            }
            else
            {
                hitNow = false;
                currentMiniTarget = null;
                UpdateRangeUI("Missed");

                if (showDebug)
                {
                    Vector3 start = rayOrigin != null
                        ? rayOrigin.position + rayOrigin.forward * rayStartOffset
                        : Vector3.zero;

                    Debug.Log(
                        $"[{gameObject.name}] Ray did not hit fire." +
                        $" | sprayRange={sprayRange:F2}" +
                        $" | pressureMode={isPressureMode}" +
                        $" | rayStart={start.ToString("F3")}" +
                        $" | rayDir={(rayOrigin != null ? rayOrigin.forward.ToString("F3") : "null")}"
                    );
                }
            }
        }
    }

    bool RayHitsFire(out RaycastHit hit)
    {
        hit = default;

        if (rayOrigin == null)
            return false;

        Vector3 dir = rayOrigin.forward;
        Vector3 start = rayOrigin.position + dir * rayStartOffset;

        if (showDebug)
            Debug.DrawRay(start, dir * sprayRange, Color.red);

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
        hitNow = false;
        pressedNow = false;
        commsSprayHeld = false;
        currentMiniTarget = null;

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
        {
            fuelBar.highValue = maxFuel;
            fuelBar.value = fuel;
            fuelBar.title = HasInfiniteFuel() ? "Fuel: ∞" : "";
        }
    }

    void UpdateRangeUI(string text)
    {
        if (rangeLabel == null)
        {
            if (showDebug)
            {
                Debug.Log(
                    $"[RangeUI] {gameObject.name} | label missing" +
                    $" | requestedText='{text}'" +
                    $" | pressureMode={isPressureMode}" +
                    $" | currentExtinguishDistance={GetCurrentExtinguishDistance():F2}" +
                    $" | sprayRange={sprayRange:F2}" +
                    $" | pinPulled={isPinPulled}" +
                    $" | pressedNow={pressedNow}" +
                    $" | commsSprayHeld={commsSprayHeld}"
                );
            }
            return;
        }

        rangeLabel.text = text;

        if (text == "In Range")
            rangeLabel.style.color = new StyleColor(Color.green);
        else if (text == "Out of Range" || text == wrongTypeText)
            rangeLabel.style.color = new StyleColor(Color.red);
        else
            rangeLabel.style.color = new StyleColor(Color.white);

        if (showDebug)
        {
            string rayOriginPos = rayOrigin != null ? rayOrigin.position.ToString("F3") : "null";
            string rayForward = rayOrigin != null ? rayOrigin.forward.ToString("F3") : "null";

            Debug.Log(
                $"[RangeUI] {gameObject.name}" +
                $" | UI='{text}'" +
                $" | pressureMode={isPressureMode}" +
                $" | currentExtinguishDistance={GetCurrentExtinguishDistance():F2}" +
                $" | sprayRange={sprayRange:F2}" +
                $" | pinPulled={isPinPulled}" +
                $" | pressedNow={pressedNow}" +
                $" | commsSprayHeld={commsSprayHeld}" +
                $" | rayOrigin={rayOriginPos}" +
                $" | rayForward={rayForward}"
            );
        }
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