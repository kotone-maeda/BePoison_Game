using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerBars : MonoBehaviour
{
    [SerializeField] PlayerController player; // Player をドラッグ
    [SerializeField] Slider hpBar;
    [SerializeField] Slider poisonBar;
    [SerializeField] TextMeshProUGUI hpLabel;
    [SerializeField] TextMeshProUGUI poisonLabel;

    void OnEnable()
    {
        if (!player) return;
        player.OnStatsChanged += Refresh;

        // 初期セットアップ
        if (hpBar)     { hpBar.minValue = 0; hpBar.maxValue = player.maxHP; }
        if (poisonBar) { poisonBar.minValue = 0; poisonBar.maxValue = player.maxPoisonRes; }

        Refresh();
    }

    void OnDisable()
    {
        if (player) player.OnStatsChanged -= Refresh;
    }

    void Refresh()
    {
        if (!player) return;
        if (hpBar)     hpBar.value = player.currentHP;
        if (poisonBar) poisonBar.value = player.poisonRes;

        if (hpLabel)     hpLabel.text     = $"HP {player.currentHP}/{player.maxHP}";
        if (poisonLabel) poisonLabel.text = $"PoisonRes {player.poisonRes}/{player.maxPoisonRes}";
    }
}
