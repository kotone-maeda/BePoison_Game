using UnityEngine;
// Talkable.cs（あなたのコードに追記）
public class Talkable : MonoBehaviour, IInteractable
{
    [Header("Priority")]
    [SerializeField] int priority = 10;
    public int Priority => priority;

    [Header("SFX")]
    [SerializeField] AudioClip talkClip;
    [Range(0f, 1f)][SerializeField] float talkVolume = 0.9f;

    [Header("Talk")]
    [SerializeField] TalkData talkData;
    [SerializeField] bool facePlayerOnTalk = true;
    [SerializeField] DialogueAsset dialogue;

    public bool CanInteract(PlayerController player) => true;
    public string GetPrompt(PlayerController player) => "E で話す";

    public void Interact(PlayerController player)
    {
        // 向き合わせは任意
        Vector3 dir = player.transform.position - transform.position; dir.y = 0;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

        DialogueManager.Instance?.Open(dialogue);
    }

    public bool TryGetSfx(out AudioClip clip, out float volume)
    {
        clip = talkClip; volume = talkVolume; return clip != null;
    }
}
