using UnityEngine;
public interface IInteractable
{
    // その相手が今インタラクト可能か（死体だけ可、距離条件など）
    bool CanInteract(PlayerController player);

    // UIに出す文言（例：「E で食べる」「E で話す」）
    string GetPrompt(PlayerController player);

    // 実行（食べる・会話開始など）
    void Interact(PlayerController player);

    // 競合したときの優先度（大きいほど優先）
    int Priority { get; }

    // インタラクト時のSFX（任意）
    bool TryGetSfx(out AudioClip clip, out float volume);
}
