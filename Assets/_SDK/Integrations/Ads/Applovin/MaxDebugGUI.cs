using UnityEngine;
#if APPLOVIN
using AppLovinMax;
#endif

public class MaxDebugGUI : MonoBehaviour
{
#if MAX_DEBUG
    public bool showButton = true;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!showButton) return;

        GUI.backgroundColor = new Color(0, 0, 0, 0.6f);
        GUI.contentColor = Color.white;

        // Kích thước nút
        float width = 180f;
        float height = 70f;

        // Vị trí góc trái trên
        float x = 20f;
        float y = 20f;

        if (GUI.Button(new Rect(x, y, width, height), "MAX DEBUG"))
        {
            Debug.Log("[MAX] Opening Mediation Debugger");
            MaxSdk.ShowMediationDebugger();
        }
#endif
    }
#endif
}
