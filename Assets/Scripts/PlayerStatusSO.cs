using UnityEngine;

[CreateAssetMenu]
public class PlayerStatusSO : ScriptableObject
{
    [SerializeField] int hP;
    [SerializeField] int poisonResistance;
    [SerializeField] int attack;
    [SerializeField] int defence;

    // getがないとほかのファイルから参照できない。setがないとほかのファイルから書き換えができない
    // setはでもよろしくない。
    // SpecializeFieldではなく普通にpublicと上で定義しても同じ感じになるが、こっちの方法がいいらしい
    public int HP { get => hP; }
    public int PoisonR { get => poisonResistance; }
    public int ATTACK { get => attack; }
    public int DEFENCE { get => defence; }
}
