using UnityEngine;

public class FireGroupController : MonoBehaviour
{
    [Header("Fire Info")]
    public int fireIndex = -1;

    [Header("Mini Fires in any order")]
    public MiniFireUnit[] miniFires = new MiniFireUnit[3];

    [Header("Reset Rule")]
    public float resetTimeIfPaused = 10f;

    [Header("References")]
    public FireChallengeManager challengeManager;

    private bool[] extinguishedFlags = new bool[3];
    private float sweepTimer = 0f;
    private bool startedAny = false;
    private bool completed = false;
    private int extinguishedCount = 0;

    void Awake()
    {
        BindMiniFires();
    }

    void Update()
    {
        if (!startedAny || completed)
            return;

        sweepTimer -= Time.deltaTime;
        if (sweepTimer <= 0f)
        {
            Debug.Log($"[FireGroupController] Reset timeout for fireIndex {fireIndex}. Resetting mini fires.");
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
            if (miniFires[i] != null && !extinguishedFlags[i])
                miniFires[i].ShowMiniFire();
        }
    }

    public void HideGroup()
    {
        gameObject.SetActive(false);
    }

    public void FullReset(bool hideGroup)
    {
        completed = false;
        startedAny = false;
        sweepTimer = 0f;
        extinguishedCount = 0;

        if (extinguishedFlags == null || extinguishedFlags.Length != 3)
            extinguishedFlags = new bool[3];

        for (int i = 0; i < 3; i++)
            extinguishedFlags[i] = false;

        BindMiniFires();

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] != null)
            {
                miniFires[i].ResetMiniFire();
                miniFires[i].gameObject.SetActive(!hideGroup);
            }
        }

        gameObject.SetActive(!hideGroup);
    }

    public bool TrySprayMiniFire(MiniFireUnit mini, float sprayAmount)
    {
        if (mini == null || completed)
            return false;

        int idx = mini.miniIndex;
        if (idx < 0 || idx >= 3)
            return false;

        if (extinguishedFlags[idx])
            return false;

        if (!startedAny)
            startedAny = true;

        sweepTimer = resetTimeIfPaused;
        mini.AddSprayProgress(sprayAmount);

        return true;
    }

    public void OnMiniFireFullyExtinguished(MiniFireUnit mini)
    {
        if (mini == null || completed)
            return;

        int idx = mini.miniIndex;
        if (idx < 0 || idx >= 3)
            return;

        if (extinguishedFlags[idx])
            return;

        extinguishedFlags[idx] = true;
        extinguishedCount++;

        if (!startedAny)
            startedAny = true;

        sweepTimer = resetTimeIfPaused;

        Debug.Log($"[FireGroupController] Mini fire complete: fireIndex={fireIndex}, miniIndex={idx}, count={extinguishedCount}/3");

        if (extinguishedCount >= 3)
        {
            completed = true;
            Debug.Log($"[FireGroupController] Fire group completed: fireIndex={fireIndex}");

            if (challengeManager != null)
                challengeManager.OnFireGroupCompleted(fireIndex, gameObject);
        }
    }

    void ResetSweep(bool hideGroup)
    {
        startedAny = false;
        sweepTimer = 0f;
        extinguishedCount = 0;

        for (int i = 0; i < 3; i++)
            extinguishedFlags[i] = false;

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