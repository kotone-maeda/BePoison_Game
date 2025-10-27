// DialogueManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public DialogueUI ui;
    public PlayerController player; // 必要なら移動ロック等に使用

    DialogueAsset _asset;
    int _idx;
    bool _open;

    void Awake()
    {
        if (Instance != null && Instance != this){ Destroy(gameObject); return; }
        Instance = this;
        ui.HideImmediate();
    }

    public void Open(DialogueAsset asset)
    {
        _asset = asset;
        _idx = 0; _open = true;

        if (_asset == null || _asset.lines == null || _asset.lines.Length == 0) { Close(); return; }

        ui.SetupNames(_asset.playerDisplayName, _asset.npcDisplayName);
        ui.Show();
        ShowCurrentLine();
        // 任意: player側で入力ロック
        // player.SetDialogueLock(true);
    }

    void ShowCurrentLine()
    {
        var line = _asset.lines[_idx];
        ui.SetSpeaker(line.speaker, line.portrait);
        ui.PlayLine(line.text);

        // 任意: 音声
        if (line.voice) AudioSource.PlayClipAtPoint(line.voice, Camera.main.transform.position);

        // 任意: 自動進行
        if (line.autoAdvanceAfter > 0) StartCoroutine(AutoNext(line.autoAdvanceAfter));
    }

    IEnumerator AutoNext(float secs){ yield return new WaitForSecondsRealtime(secs); Next(); }

    public void Next()
    {
        if (!_open) return;

        if (ui.IsTyping) { ui.CompleteTyping(); return; }

        _idx++;
        if (_idx >= _asset.lines.Length) { Close(); return; }

        ShowCurrentLine();
    }

    public void Close()
    {
        _open = false;
        ui.Hide();
        // player.SetDialogueLock(false);
        _asset = null; _idx = 0;
    }

    void Update()
    {
        var k = Keyboard.current; var gp = Gamepad.current;
        if (!_open) return;

        if (k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
            Next();
        if (gp != null && gp.buttonSouth.wasPressedThisFrame)
            Next();
    }
}
