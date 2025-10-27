// TalkData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Talk/TalkData")]
public class TalkData : ScriptableObject
{
    public string npcName;
    [TextArea(2,4)] public string[] lines;
}
