// DialogueAsset.cs
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/Talk/DialogueAsset")]
public class DialogueAsset : ScriptableObject
{
    public string npcDisplayName = "村人A";   // デフォ名
    public string playerDisplayName = "主人公";

    [Serializable]
    public struct Line
    {
        public Speaker speaker;
        [TextArea(2, 4)] public string text;

        // 任意: 演出用オプション（必要になったら使う）
        public AudioClip voice;         // セリフ音声
        public Sprite portrait;         // 顔グラ
        public string emote;            // "Angry"/"Sad" 等
        public float autoAdvanceAfter;  // >0なら自動で次へ（秒）
    }

    public enum Speaker { Player, NPC }

    public Line[] lines;
}
