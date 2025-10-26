using UnityEngine;

public abstract class Edible : MonoBehaviour, IInteractable
{
    [Header("Edible Effects")]
    [Tooltip("毒量。毒耐性を上げるだけ（PlayerController.EatPoison を呼ぶ）。")]
    [SerializeField] protected int poisonAmount = 0;

    [Tooltip("腹痛を回復させる量（現在の腹痛を減らす）。")]
    [SerializeField] protected int healAmount = 0;

    [Tooltip("食べた瞬間に腹痛を増やす量（ダメージ的な扱い）。")]
    [SerializeField] protected int stomachacheIncrease = 0;

    [Tooltip("食べたらこのオブジェクトを消す。")]
    [SerializeField] protected bool destroyOnEat = true;

    [Header("Interact Settings")]
    [SerializeField] int priority = 0; // 競合時の優先度

    [Header("SFX")]
    [SerializeField] AudioClip eatClip;
    [Range(0f, 1f)] [SerializeField] float eatVolume = 1f;

    private bool consumed = false; // 二重食い防止

    // いま食べられる状態か？（派生で条件を絞るならオーバーライド）
    public virtual bool CanBeEaten(PlayerController eater) => true;

    /// <summary>
    /// 食べた時の処理
    /// - stomachacheIncrease 分だけ腹痛を加算
    /// - poisonAmount 分の毒を摂取（毒耐性アップ、超過分は腹痛に加算は PlayerController 側で処理）
    /// - healAmount 分だけ腹痛を回復
    /// - 必要なら自身を Destroy
    /// </summary>
    public virtual void OnEaten(PlayerController eater)
    {
        if (!eater) return;

        // 先に“腹痛を上げる”を適用（食べ物そのものが重い/辛い等の表現）
        if (stomachacheIncrease > 0)
            eater.AddStomachache(stomachacheIncrease);

        // 次に毒：耐性アップ（溢れ分は PlayerController.EatPoison 内で腹痛へ）
        if (poisonAmount > 0)
            eater.EatPoison(poisonAmount);

        // 最後に回復：食後に少し楽になる等
        if (healAmount > 0)
            eater.RelieveStomachache(healAmount);

        if (destroyOnEat)
            Destroy(gameObject);
    }

    // PlayerEater / InteractBox から呼ばれるエントリ
    public bool TryEat(PlayerController eater)
    {
        if (consumed) return false;
        if (!CanBeEaten(eater)) return false;

        consumed = true;
        OnEaten(eater);
        return true;
    }

    // ===== IInteractable =====
    public virtual bool CanInteract(PlayerController player) => CanBeEaten(player);

    public virtual string GetPrompt(PlayerController player) => "E で食べる";

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
