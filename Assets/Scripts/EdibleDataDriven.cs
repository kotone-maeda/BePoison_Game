using UnityEngine;

[DisallowMultipleComponent]
public class EdibleDataDriven : Edible
{
    [Header("Data Driven Options")]
    [Tooltip("食べた時に消したい対象（未指定なら自分）")]
    [SerializeField] GameObject targetToDestroy;

    [Header("Optional Slime Gating")]
    [Tooltip("スライムの死体のみ食べられる仕様にする場合 ON")]
    [SerializeField] bool requireDeadSlime = false;
    [SerializeField] SlimeController slime; // 自動取得を試みる

    void Reset()
    {
        if (!slime) slime = GetComponentInParent<SlimeController>();
        if (!targetToDestroy) targetToDestroy = gameObject;
    }

    void OnValidate()
    {
        if (!slime) slime = GetComponentInParent<SlimeController>();
        if (!targetToDestroy) targetToDestroy = gameObject;
    }

    public override bool CanBeEaten(PlayerController eater)
    {
        if (requireDeadSlime)
        {
            // スライム参照がある場合のみチェック。無ければ食べられない想定でも良い。
            if (!slime) return false;
            return slime.IsDead;
        }
        return true;
    }

    public override void OnEaten(PlayerController eater)
    {
        base.OnEaten(eater); // heal/poison/destroyOnEat を先に実行（destroyOnEat=true なら自分が消える点に注意）

        // base では自分（Edibleが付いたGO）しか消さない。
        // スライムのルートなど別オブジェクトも消したい場合はここで消す。
        if (targetToDestroy && targetToDestroy != gameObject)
        {
            Destroy(targetToDestroy);
        }
    }
}
