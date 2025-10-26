using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="EnemyStatus", menuName="Game/Enemy Status")]
public class EnemyStatusSO : ScriptableObject
{
    public List<EnemyStatus> enemyStatusList = new List<EnemyStatus>();

    [System.Serializable]
    public class EnemyStatus
    {
        [SerializeField] string name;
        [SerializeField] int hP;
        [SerializeField] int attack;
        [SerializeField] int defence;

        public int HP { get => hP; }
        public int Attack { get => attack; }
        public int Defence { get => defence; }

    }
    
}
