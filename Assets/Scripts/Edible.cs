using UnityEngine;

public abstract class Edible : MonoBehaviour, IInteractable
{
    [Header("Edible")]
    [SerializeField] protected int poisonAmount = 0;   // 0なら無毒
    [SerializeField] protected int healAmount   = 0;   // 回復量（任意）
    [SerializeField] protected bool destroyOnEat = true;

    [Header("Interact Settings")]
    [SerializeField] int priority = 0;                 // 競合時の優先度（会話より下なら0のまま）

    [Header("SFX")]
    [SerializeField] AudioClip eatClip;
    [Range(0f,1f)] [SerializeField] float eatVolume = 1f;

    private bool consumed = false; // 二重食い防止

    // ===== “食べ物として” の基本ロジック =====

    /// いま食べられる状態か？（派生で条件を絞る）
    public virtual bool CanBeEaten(PlayerController eater) => true;

    /// 食べたときの処理（毒耐性アップ＋超過ダメージ、回復など）
    public virtual void OnEaten(PlayerController eater)
    {
        if (!eater) return;

        if (healAmount   > 0) eater.Heal(healAmount);
        if (poisonAmount > 0) eater.EatPoison(poisonAmount);

        if (destroyOnEat) Destroy(gameObject);
    }

    /// 直接“食べ”を呼びたい場合の入口（必要なら）
    public bool TryEat(PlayerController eater)
    {
        if (consumed) return false;
        if (!CanBeEaten(eater)) return false;

        consumed = true;
        OnEaten(eater);
        return true;
    }

    // ===== IInteractable 実装 =====

    public virtual bool CanInteract(PlayerController player) => CanBeEaten(player);

    public virtual string GetPrompt(PlayerController player) => "E で食べる";

    // Interact は “食べる” を呼ぶだけ（InteractBox から呼ばれる）
    public virtual void Interact(PlayerController player)
    {
        TryEat(player);
    }

    public int Priority => priority;

    public virtual bool TryGetSfx(out AudioClip clip, out float volume)
    {
        clip = eatClip;
        volume = eatVolume;
        return clip != null;
    }
}
