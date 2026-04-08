using UnityEngine;
using System.Collections.Generic;

public class FireGroupController : MonoBehaviour
{
    [Header("Fire Info")]
    public int fireIndex = -1;

    [Header("Mini Fires in Left -> Middle -> Right order")]
    public MiniFireUnit[] miniFires = new MiniFireUnit[3];

    [Header("Sweep Rule")]
    public float resetTimeIfPaused = 4f;

    [Tooltip("Allowed sweep paths are Left->Middle->Right or Right->Middle->Left")]
    public bool allowBothDirections = true;

    [Header("References")]
    public FireChallengeManager challengeManager;

    private List<int> extinguishedSequence = new List<int>();
    private bool[] extinguishedFlags = new bool[3];
    private bool[] extinguishingFlags = new bool[3];
    private float sweepTimer = 0f;
    private bool startedSweep = false;
    private bool completed = false;

    void Awake()
    {
        BindMiniFires();
    }

    void Update()
    {
        if (!startedSweep || completed)
            return;

        sweepTimer -= Time.deltaTime;
        if (sweepTimer <= 0f)
        {
            Debug.Log($"[FireGroupController] Sweep timed out for fireIndex {fireIndex}. Resetting.");
            ResetSweep(false);
        }
    }

    void BindMiniFires()
    {
        if (miniFires == null)
            return;

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] == null)
                continue;

            miniFires[i].miniIndex = i;
            miniFires[i].groupController = this;
        }
    }

    public void ShowGroup()
    {
        gameObject.SetActive(true);

        BindMiniFires();

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] != null && !extinguishedFlags[i] && !extinguishingFlags[i])
                miniFires[i].ResetMiniFire();
        }
    }

    public void HideGroup()
    {
        gameObject.SetActive(false);
    }

    public void FullReset(bool hideGroup)
    {
        completed = false;
        startedSweep = false;
        sweepTimer = 0f;
        extinguishedSequence.Clear();

        if (extinguishedFlags == null || extinguishedFlags.Length != 3)
            extinguishedFlags = new bool[3];

        if (extinguishingFlags == null || extinguishingFlags.Length != 3)
            extinguishingFlags = new bool[3];

        for (int i = 0; i < 3; i++)
        {
            extinguishedFlags[i] = false;
            extinguishingFlags[i] = false;
        }

        BindMiniFires();

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] != null)
            {
                miniFires[i].ResetMiniFire();
                miniFires[i].gameObject.SetActive(!hideGroup);
            }
        }

        if (hideGroup)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    public bool TryStartMiniFire(MiniFireUnit mini)
    {
        if (mini == null || completed)
            return false;

        int idx = mini.miniIndex;
        if (idx < 0 || idx >= 3)
            return false;

        if (extinguishedFlags[idx] || extinguishingFlags[idx])
            return false;

        extinguishingFlags[idx] = true;
        mini.StartShrinkAndExtinguish();

        Debug.Log($"[FireGroupController] Start mini fire extinguish: fireIndex={fireIndex}, miniIndex={idx}");
        return true;
    }

    public void OnMiniFireFullyExtinguished(MiniFireUnit mini)
    {
        if (mini == null || completed)
            return;

        int idx = mini.miniIndex;
        if (idx < 0 || idx >= 3)
            return;

        extinguishingFlags[idx] = false;
        extinguishedFlags[idx] = true;

        if (!startedSweep)
            startedSweep = true;

        sweepTimer = resetTimeIfPaused;
        extinguishedSequence.Add(idx);

        Debug.Log($"[FireGroupController] Mini fire complete: fireIndex={fireIndex}, sequence={string.Join(",", extinguishedSequence)}");

        if (!IsValidSweepSoFar())
        {
            Debug.Log($"[FireGroupController] Invalid sweep for fireIndex {fireIndex}. Resetting.");
            ResetSweep(false);
            return;
        }

        if (extinguishedSequence.Count >= 3)
        {
            completed = true;
            Debug.Log($"[FireGroupController] Fire group completed: fireIndex={fireIndex}");

            if (challengeManager != null)
                challengeManager.OnFireGroupCompleted(fireIndex, gameObject);
        }
    }

    bool IsValidSweepSoFar()
    {
        if (extinguishedSequence.Count == 0)
            return true;

        if (allowBothDirections)
        {
            return MatchesPrefix(extinguishedSequence, new int[] { 0, 1, 2 }) ||
                   MatchesPrefix(extinguishedSequence, new int[] { 2, 1, 0 });
        }

        return MatchesPrefix(extinguishedSequence, new int[] { 0, 1, 2 });
    }

    bool MatchesPrefix(List<int> actual, int[] target)
    {
        if (actual.Count > target.Length)
            return false;

        for (int i = 0; i < actual.Count; i++)
        {
            if (actual[i] != target[i])
                return false;
        }

        return true;
    }

    void ResetSweep(bool hideGroup)
    {
        startedSweep = false;
        sweepTimer = 0f;
        extinguishedSequence.Clear();

        for (int i = 0; i < 3; i++)
        {
            extinguishedFlags[i] = false;
            extinguishingFlags[i] = false;
        }

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] != null)
            {
                miniFires[i].ResetMiniFire();
                miniFires[i].gameObject.SetActive(!hideGroup);
            }
        }

        if (hideGroup)
            gameObject.SetActive(false);
    }
}