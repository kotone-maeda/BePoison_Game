using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerBars : MonoBehaviour
{
    [SerializeField] PlayerController player; // Player をドラッグ
    [Header("Bars")]
    [SerializeField] Slider hpBar;           // ← 腹痛バーとして使う（0→最大で満タン）
    [SerializeField] Slider poisonBar;       // 毒耐性バー（上がるほど耐える）
    [Header("Labels")]
    [SerializeField] TextMeshProUGUI hpLabel;     // 「腹痛 x/y」
    [SerializeField] TextMeshProUGUI poisonLabel; // 「毒耐性 x/y」

    void OnEnable()
    {
        if (player)
        {
            player.OnStatsChanged -= Refresh; // 二重登録ガード
            player.OnStatsChanged += Refresh;

            // 初期セットアップ
            if (hpBar)
            {
                hpBar.minValue = 0;
                hpBar.maxValue = player.maxStomachache; // 最大腹痛
            }
            if (poisonBar)
            {
                poisonBar.minValue = 0;
                poisonBar.maxValue = player.maxPoisonRes;
            }
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

        // 腹痛（0→増えるとつらい）
        if (hpBar) hpBar.value = player.stomachache;
        if (hpLabel) hpLabel.text = $"腹痛 {player.stomachache}/{player.maxStomachache}";

        // 毒耐性（高いほど強い）
        if (poisonBar) poisonBar.value = player.poisonRes;
        if (poisonLabel) poisonLabel.text = $"毒耐性 {player.poisonRes}/{player.maxPoisonRes}";
    }
}
