// PlayerEater.cs
using UnityEngine;

public class PlayerEater : MonoBehaviour
{
    [SerializeField] PlayerController player;     // プレイヤー本体
    [Header("Feedback")]
    [SerializeField] AudioSource sfxSource;       // プレイヤーに付けた AudioSource をドラッグ
    [SerializeField] AudioClip eatSfx;            // 食べる効果音
    [SerializeField] ParticleSystem eatVfxPrefab; // 省略可：小さなキラッ等（Prefab）

    void OnTriggerEnter(Collider other)
    {
        if (!player) return;

        // Edible を探す
        Edible edible = other.GetComponent<Edible>() ?? other.GetComponentInParent<Edible>();
        if (edible == null) return;

        // 食べを試行
        if (edible.TryEat(player))
        {
            // --- フィードバック ---
            if (eatSfx && sfxSource) sfxSource.PlayOneShot(eatSfx);

            if (eatVfxPrefab)
            {
                var vfx = Instantiate(eatVfxPrefab, other.transform.position, Quaternion.identity);
                Destroy(vfx.gameObject, 2f);
            }

            // （任意）UIちらっと光らせるなどの演出はここでコルーチン呼び出し
            // StartCoroutine(FlashPoisonBar());
        }
    }
}
