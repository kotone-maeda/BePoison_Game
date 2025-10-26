using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerBars : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [Header("Bars")]
    [SerializeField] Slider hpBar;      // 腹痛バー
    [SerializeField] Slider poisonBar;  // 毒耐性バー
    [Header("Labels")]
    [SerializeField] TextMeshProUGUI hpLabel;
    [SerializeField] TextMeshProUGUI poisonLabel;

    void OnEnable()
    {
        if (!player) return;

        // 二重登録ガード
        player.OnStatsChanged -= Refresh;
        player.OnStatsChanged += Refresh;

        // 初期セットアップ（min は 0 に固定）
        if (hpBar)
        {
            hpBar.minValue = 0;
            hpBar.maxValue = Mathf.Max(1, player.maxStomachache); // 0ガード
        }
        if (poisonBar)
        {
            poisonBar.minValue = 0; // ← ここが重要。以前 player.poisonRes になってた
            poisonBar.maxValue = Mathf.Max(1, player.maxPoisonRes);
        }

        Refresh();
    }

    void OnDisable()
    {
        if (player) player.OnStatsChanged -= Refresh;
    }

    void Refresh()
    {
        if (!player) return;

        // max を毎回同期（初期化順で後から上書きされても追随）
        if (hpBar)     hpBar.maxValue     = Mathf.Max(1, player.maxStomachache);
        if (poisonBar) poisonBar.maxValue = Mathf.Max(1, player.maxPoisonRes);

        // 値
        if (hpBar)     hpBar.value     = Mathf.Clamp(player.stomachache, 0, hpBar.maxValue);
        if (poisonBar) poisonBar.value = Mathf.Clamp(player.poisonRes,   0, poisonBar.maxValue);

        // 表示
        if (hpLabel)     hpLabel.text     = $"腹痛 {player.stomachache}/{player.maxStomachache}";
        if (poisonLabel) poisonLabel.text = $"毒耐性 {player.poisonRes}/{player.maxPoisonRes}";
    }
}
