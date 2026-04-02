using UnityEngine;
using Vuforia;
using System.Collections;

public class VuforiaTapToRefocus : MonoBehaviour
{
    [Header("Tap Refocus")]
    public bool enableTapToRefocus = true;
    public float extent = 0.25f;
    public float restoreContinuousAutoDelay = 0.4f;
    public bool logFocusEvents = true;

    private bool vuforiaStarted = false;
    private bool refocusInProgress = false;

    void Start()
    {
        if (VuforiaApplication.Instance != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
            VuforiaApplication.Instance.OnVuforiaPaused += OnVuforiaPaused;
        }
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
            VuforiaApplication.Instance.OnVuforiaPaused -= OnVuforiaPaused;
        }
    }

    void OnVuforiaStarted()
    {
        vuforiaStarted = true;
        SetContinuousAutoFocus();
    }

    void OnVuforiaPaused(bool paused)
    {
        if (!paused && vuforiaStarted)
            SetContinuousAutoFocus();
    }

    void Update()
    {
        if (!enableTapToRefocus || !vuforiaStarted || refocusInProgress)
            return;

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
                StartCoroutine(DoTapRefocus(touch.position));
        }
#if UNITY_EDITOR
        else if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(DoTapRefocus(Input.mousePosition));
        }
#endif
    }

    IEnumerator DoTapRefocus(Vector2 screenPos)
    {
        refocusInProgress = true;

        var cam = VuforiaBehaviour.Instance.CameraDevice;
        if (cam == null)
        {
            refocusInProgress = false;
            yield break;
        }

        if (logFocusEvents)
            Debug.Log($"[VuforiaTapToRefocus] Tap refocus at {screenPos}");

        if (cam.FocusRegionSupported)
        {
            var roi = new CameraRegionOfInterest(screenPos, extent);
            bool regionOk = cam.SetFocusRegion(roi);

            if (logFocusEvents)
                Debug.Log($"[VuforiaTapToRefocus] SetFocusRegion: {regionOk}");
        }

        bool triggerOk = cam.SetFocusMode(FocusMode.FOCUS_MODE_TRIGGERAUTO);

        if (logFocusEvents)
            Debug.Log($"[VuforiaTapToRefocus] TRIGGERAUTO: {triggerOk}");

        yield return new WaitForSeconds(restoreContinuousAutoDelay);

        SetContinuousAutoFocus();
        refocusInProgress = false;
    }

    void SetContinuousAutoFocus()
    {
        var cam = VuforiaBehaviour.Instance.CameraDevice;
        if (cam == null) return;

        bool ok = cam.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);

        if (logFocusEvents)
            Debug.Log($"[VuforiaTapToRefocus] CONTINUOUSAUTO: {ok}");
    }
}