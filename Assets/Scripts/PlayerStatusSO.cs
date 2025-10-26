using UnityEngine;
using UnityEngine.Serialization; // 旧フィールド名を引き継ぐ用

[CreateAssetMenu]
public class PlayerStatusSO : ScriptableObject
{
    // 旧 hP をそのまま「腹痛の最大値」に流用する（データ移行不要）
    [FormerlySerializedAs("hP")]
    [SerializeField] int stomachacheMax = 100;

    [SerializeField] int poisonResistance;
    [SerializeField] int attack;
    [SerializeField] int defence;

    // 読み取り用プロパティ
    public int StomachacheMax => stomachacheMax;
    public int PoisonR       => poisonResistance;
    public int ATTACK        => attack;
    public int DEFENCE       => defence;
}
