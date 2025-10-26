using UnityEngine;

[CreateAssetMenu(fileName = "FoodStatus", menuName = "Game/Food Status", order = 0)]
public class FoodStatusSO : ScriptableObject
{
    [Header("Meta")]
    public string displayName = "食べ物";
    public string promptText = "E で食べる";
    public int priority = 0;

    [Header("Effects")]
    [Tooltip("腹痛をどれだけ減らすか（正の値で回復）。例：30")]
    public int healStomachache = 0;

    [Tooltip("毒量。>0 ならプレイヤーに毒耐性処理（超過分ダメージ）")]
    public int poison = 0;

    [Header("Behavior")]
    public bool destroyOnEat = true;

    [Header("SFX")]
    public AudioClip sfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;
}
