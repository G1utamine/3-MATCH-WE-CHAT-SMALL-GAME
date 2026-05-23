using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AdRewardPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button watchButton;
    [SerializeField] private Button cancelButton;

    private Action onWatchComplete;

    private void Awake()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (watchButton != null)
            watchButton.onClick.AddListener(OnWatchButtonClick);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClick);
    }

    public void Show(string itemType, Action onComplete)
    {
        onWatchComplete = onComplete;

        // 修改这里：使用 GetItemName 将 itemType 转换成中文
        if (messageText != null)
            messageText.text = $"观看广告获得 10 个{GetItemName(itemType)}？";
        if (titleText != null)
            titleText.text = "道具不足";

        if (popupPanel != null)
            popupPanel.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    // 给 Cancel 按钮绑定的方法
    public void OnCancelButtonClick()
    {
        AudioManager.Instance?.ButtonClick();
        Hide();
    }

    public void Hide()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        onWatchComplete = null;
    }

    private void OnWatchButtonClick()
    {
        AudioManager.Instance?.ButtonClick();
        PlayRewardedVideo();
    }

    private void PlayRewardedVideo()
    {
        GrantReward();
        return;
#if UNITY_WEBGL && !UNITY_EDITOR
        // WeChat Mini Game environment
        WeChatWASM.WX.CreateRewardedVideoAd(new WeChatWASM.WXCreateRewardedVideoAdParam
        {
            adUnitId = "YOUR_AD_UNIT_ID",
            multiton = false
        }).Show();
        // Note: You need to listen to ad close event to call GrantReward()
#else
        // Editor test environment
        SimulateAdReward();
#endif
    }

    private void SimulateAdReward()
    {
        Invoke(nameof(DelayedReward), 0.5f);
    }

    private void DelayedReward()
    {
        var callback = onWatchComplete;
        Hide();
        callback?.Invoke();
    }

    public void GrantReward()
    {
        var callback = onWatchComplete;
        Hide();
        callback?.Invoke();
    }

    private string GetItemName(string itemType)
    {
        switch (itemType)
        {
            case "Bomb": return "炸弹";
            case "Lightning": return "闪电";
            case "Potion": return "药水";
            default: return "道具";
        }
    }
}