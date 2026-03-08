using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherFuel : MonoBehaviour
{
    [Header("Fuel Settings")]
    [SerializeField] private float maxFuel = 5f;              // e.g., 5 seconds worth
    [SerializeField] private float fuelUsePerSecond = 1f;     // fuel drained per second
    [SerializeField] private bool clearParticlesOnEmpty = true;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string fuelBarName = "FuelBar";  // ProgressBar "Name" in UI Builder

    [Header("Spray Effects (Optional)")]
    [SerializeField] private ParticleSystem sprayParticles;
    [SerializeField] private AudioSource sprayAudio;

    [Header("Input")]
    [SerializeField] private bool useTouchAndMouse = true;    // quick default for testing

    private ProgressBar fuelBar;
    private float fuel;

    void Awake()
    {
        fuel = maxFuel;

        if (!uiDocument)
        {
            Debug.LogError("[ExtinguisherFuel] UIDocument not assigned.");
            return;
        }

        // Find the ProgressBar
        var root = uiDocument.rootVisualElement;
        fuelBar = root.Q<ProgressBar>(fuelBarName);

        if (fuelBar == null)
        {
            Debug.LogError($"[ExtinguisherFuel] ProgressBar named '{fuelBarName}' not found in UXML.");
            return;
        }

        // Configure bar range
        fuelBar.lowValue = 0f;
        fuelBar.highValue = maxFuel;
        fuelBar.value = fuel;
    }

    void Update()
    {
        if (fuelBar == null) return;

        bool wantsToSpray = GetSprayInput();

        // Decide if we can actually spray
        bool canSpray = wantsToSpray && fuel > 0f;

        if (canSpray)
        {
            StartSpray();

            // Drain fuel
            fuel = Mathf.Max(0f, fuel - fuelUsePerSecond * Time.deltaTime);

            // If we just hit empty, stop immediately
            if (fuel <= 0f)
            {
                fuel = 0f;
                StopSpray(immediateClear: clearParticlesOnEmpty);
            }
        }
        else
        {
            // If user is holding input but fuel is empty, ensure we are stopped
            StopSpray(immediateClear: false);
        }

        // Update UI
        fuelBar.value = fuel;
    }

    private bool GetSprayInput()
    {
        if (!useTouchAndMouse) return false;

        // Mouse hold
        if (Input.GetMouseButton(0)) return true;

        // Touch hold
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            // treat Moved/Stationary as "holding"
            return t.phase == TouchPhase.Began ||
                   t.phase == TouchPhase.Moved ||
                   t.phase == TouchPhase.Stationary;
        }

        return false;
    }

    private void StartSpray()
    {
        if (sprayParticles && !sprayParticles.isEmitting)
        {
            sprayParticles.Play(true);
        }

        if (sprayAudio && !sprayAudio.isPlaying)
        {
            sprayAudio.Play();
        }
    }

    private void StopSpray(bool immediateClear)
    {
        if (sprayParticles)
        {
            // If you want it to disappear instantly (no lingering particles), clear them.
            if (immediateClear)
                sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (sprayAudio && sprayAudio.isPlaying)
        {
            sprayAudio.Stop();
        }
    }

    // Call this from a button or other script to refill
    public void RefillToFull()
    {
        fuel = maxFuel;
        if (fuelBar != null) fuelBar.value = fuel;
    }

    // Optional: Set fuel directly (0..maxFuel)
    public void SetFuel(float newFuel)
    {
        fuel = Mathf.Clamp(newFuel, 0f, maxFuel);
        if (fuelBar != null) fuelBar.value = fuel;

        if (fuel <= 0f)
            StopSpray(immediateClear: true);
    }

    public float GetFuel() => fuel;
    public bool HasFuel() => fuel > 0f;
}