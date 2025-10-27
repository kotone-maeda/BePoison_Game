// DialogueUI.cs（左右スロット対応/タイプライターは簡略）
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("Common")]
    public CanvasGroup group;
    public float charsPerSecond = 45f;

    [Header("Left (Player)")]
    public Image  leftPortrait;
    public TMP_Text leftName;
    public TMP_Text leftLine;

    [Header("Right (NPC)")]
    public Image  rightPortrait;
    public TMP_Text rightName;
    public TMP_Text rightLine;

    [Header("Style")]
    public Color activeName = Color.white;
    public Color inactiveName = new Color(1,1,1,0.5f);

    Coroutine _typing;
    string _full;
    TMP_Text _activeLine;
    TMP_Text _inactiveLine;
    TMP_Text _activeName;

    void Awake() => HideImmediate();

    public void SetupNames(string playerName, string npcName)
    {
        if (leftName)  leftName.text  = playerName;
        if (rightName) rightName.text = npcName;
    }

    public void SetSpeaker(DialogueAsset.Speaker spk, Sprite portrait = null)
    {
        bool player = spk == DialogueAsset.Speaker.Player;

        // 左=Player, 右=NPC とする
        _activeLine   = player ? leftLine  : rightLine;
        _inactiveLine = player ? rightLine : leftLine;
        _activeName   = player ? leftName  : rightName;

        if (leftPortrait)  leftPortrait.sprite  = player ? portrait : leftPortrait.sprite;
        if (rightPortrait) rightPortrait.sprite = !player ? portrait : rightPortrait.sprite;

        // 強調（名前の色/非アクティブ側の吹き出し透明にする等、好みで）
        if (leftName)  leftName.color  = player ? activeName : inactiveName;
        if (rightName) rightName.color = player ? inactiveName : activeName;

        if (_inactiveLine) _inactiveLine.text = "";
    }

    public void Show() { group.alpha = 1; group.blocksRaycasts = true; group.interactable = true; }
    public void Hide()  { StartCoroutine(FadeOut()); }
    public void HideImmediate(){ group.alpha = 0; group.blocksRaycasts = false; group.interactable = false; leftLine.text = rightLine.text = ""; }

    public bool IsTyping => _typing != null;

    public void PlayLine(string text)
    {
        if (_activeLine == null) return;
        StopTyping();
        _full = text ?? "";
        _typing = StartCoroutine(TypeRoutine());
    }

    public void CompleteTyping()
    {
        if (_typing == null) return;
        StopTyping();
        _activeLine.text = _full;
    }

    IEnumerator TypeRoutine()
    {
        _activeLine.text = "";
        if (charsPerSecond <= 0f) { _activeLine.text = _full; _typing = null; yield break; }

        float t = 0; int len = _full.Length;
        while (true)
        {
            t += Time.unscaledDeltaTime * charsPerSecond;
            int n = Mathf.Clamp(Mathf.FloorToInt(t), 0, len);
            _activeLine.text = _full.Substring(0, n);
            if (n >= len) break;
            yield return null;
        }
        _typing = null;
    }

    void StopTyping(){ if (_typing != null) { StopCoroutine(_typing); _typing = null; } }

    IEnumerator FadeOut()
    {
        float a = group.alpha;
        while (a > 0f) { a -= Time.unscaledDeltaTime * 4f; group.alpha = Mathf.Clamp01(a); yield return null; }
        group.blocksRaycasts = false; group.interactable = false;
        leftLine.text = rightLine.text = "";
    }
}
