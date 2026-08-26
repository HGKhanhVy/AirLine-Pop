#if TOPON
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AnyThinkAds.Api;

public abstract class BaseAdOperator
{
    abstract public void initializeAd(string ID);

    abstract public void destroyAd();

    abstract public void loadAd();

    abstract public void showAd();

    abstract public bool isAdReady();

    protected string adsId;
}
#endif