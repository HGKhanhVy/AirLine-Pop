using UnityEngine;

public class UiNoInternet : Uibase
{
    public void Retry()
    {
        InternetChecker.Instance.CheckInternet();
    }
}
