using UnityEngine;

public class Talkable : MonoBehaviour, IInteractable
{
    [Header("Priority")]
    [SerializeField] int priority = 10; // 食べより会話を優先したいなら食べより高く
    public int Priority => priority;

    [Header("SFX")]
    [SerializeField] AudioClip talkClip;
    [Range(0f,1f)] [SerializeField] float talkVolume = 0.9f;

    public bool CanInteract(PlayerController player)
    {
        // 例：距離・向き・クエスト条件などを後で追加
        return true;
    }

    public string GetPrompt(PlayerController player) => "E で話す";

    public void Interact(PlayerController player)
    {
        // 会話UIを開く、向きを合わせる、SFX鳴らす等
        Debug.Log("Start Conversation");
        // 例）DialogueManager.Instance.Open(this);
    }

    public bool TryGetSfx(out AudioClip clip, out float volume)
    {
        clip = talkClip;
        volume = talkVolume;
        return clip != null;
    }
}
