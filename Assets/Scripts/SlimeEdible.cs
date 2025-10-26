using UnityEngine;

[DisallowMultipleComponent]
public class SlimeEdible : Edible
{
    [Header("Auto-wired (overrideable)")]
    [SerializeField] SlimeController slime;          // 自動で親から拾う。必要なら手で上書え
    [SerializeField] GameObject destroyRootOverride; // 指定あればこれを消す。未指定ならslimeのルート
    [SerializeField] protected GameObject destroyTarget;

    void Reset()
    {
        AutoWire();
    }

    void OnValidate()
    {
        // エディタ上で変更しても極力自動で埋め直す（既に手で入れていたら尊重）
        if (!slime || slime.Equals(null)) slime = GetComponentInParent<SlimeController>();
        if (!destroyTarget && destroyRootOverride) destroyTarget = destroyRootOverride;
        if (!destroyTarget && slime) destroyTarget = slime.gameObject;
    }

    void Awake()
    {
        AutoWire();
    }

    void AutoWire()
    {
        if (!slime || slime.Equals(null)) slime = GetComponentInParent<SlimeController>();
        if (!destroyTarget)
        {
            if (destroyRootOverride) destroyTarget = destroyRootOverride;
            else if (slime) destroyTarget = slime.gameObject; // 既定＝スライム本体ごと消す
        }
    }

    public override bool CanBeEaten(PlayerController eater)
    {
        // 死亡している時だけ食べられる
        return slime && slime.IsDead;
    }
}
