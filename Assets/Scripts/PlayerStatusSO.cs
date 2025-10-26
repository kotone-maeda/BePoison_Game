using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatusSO", menuName = "Game/PlayerStatusSO")]
public class PlayerStatusSO : ScriptableObject
{
    [Header("Max Values")]
    [SerializeField] int maxStomachache = 100;  // 腹痛の最大（ここに集約）
    [SerializeField] int maxPoisonRes = 100;  // 毒耐性の上限

    [Header("Start Values")]
    [SerializeField] int basePoisonRes = 10;    // 開始時の毒耐性

    [Header("Combat")]
    [SerializeField] int attack = 10;
    [SerializeField] int defence = 5;

    public int MaxStomachache => maxStomachache;
    public int MaxPoisonRes => maxPoisonRes;
    public int BasePoisonRes => basePoisonRes;
    public int ATTACK => attack;
    public int DEFENCE => defence;
}
