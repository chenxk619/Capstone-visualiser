using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    public ExtinguisherModelSwitcher switcher;
    public FireChallengeManager challengeManager;

    private Button nextButton;
    private Button prevButton;

    void Start()
    {
        var root = uiDocument.rootVisualElement;

        nextButton = root.Q<Button>("NextExtinguisherButton");
        prevButton = root.Q<Button>("PrevExtinguisherButton");

        if (nextButton != null)
        {
            nextButton.clicked += () =>
            {
                Debug.Log("[UI] Next extinguisher pressed");
                switcher.NextModel();

                if (challengeManager != null)
                    challengeManager.RefreshCurrentExtinguisher();
            };
        }

        if (prevButton != null)
        {
            prevButton.clicked += () =>
            {
                Debug.Log("[UI] Previous extinguisher pressed");
                switcher.PreviousModel();

                if (challengeManager != null)
                    challengeManager.RefreshCurrentExtinguisher();
            };
        }
    }
}