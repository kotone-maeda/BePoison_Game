using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemyStatusSO : ScriptableObject
{
    public List<EnemyStatus> enemyStatusList = new List<EnemyStatus>();

    [System.Serializable]
    public class EnemyStatus
    {
        [SerializeField] string name;
        [SerializeField] int hP;
        [SerializeField] int poison;
        [SerializeField] int attack;
        [SerializeField] int defence;

        public int HP { get => hP; }
        public int Poison { get => poison; }
        public int Attack { get => attack; }
        public int Defence { get => defence; }

    }
    
}
