using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlaceCube : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    private bool isPlacing = false;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (!raycastManager || isPlacing) return;

        // Touch (mobile)
        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            isPlacing = true;
            PlaceObject(Touch.activeTouches[0].screenPosition);
            return;
        }

        // Mouse (Editor)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPlacing = true;
            PlaceObject(Mouse.current.position.ReadValue());
        }
    }

    void PlaceObject(Vector2 screenPos)
    {
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.AllTypes) && hits.Count > 0)
        {
            var pose = hits[0].pose;
            Instantiate(raycastManager.raycastPrefab, pose.position, pose.rotation);
        }

        StartCoroutine(ResetPlacing());
    }

    IEnumerator ResetPlacing()
    {
        yield return new WaitForSeconds(0.25f);
        isPlacing = false;
    }
}
