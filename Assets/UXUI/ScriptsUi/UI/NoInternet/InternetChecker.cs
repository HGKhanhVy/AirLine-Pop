using UnityEngine;

public class InternetChecker : MonoBehaviour
{
    public static InternetChecker Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CheckInternet();
    }

    public void CheckInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            UiManager.Instance.ShowNoInternet();
        }
        else
        {
            UiManager.Instance.HideNoInternet();
        }
    }
}