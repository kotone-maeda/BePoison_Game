using UnityEngine;
using UnityEngine.AI;

public class SlimeController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] EnemyStatusSO enemyStatusSO;
    [SerializeField] PlayerStatusSO playerStatusSO;
    private NavMeshAgent agent;
    private Animator animator;
    public float speed;
    private float distance;
    private int currentHP;
    private int damage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = speed;
        currentHP = enemyStatusSO.enemyStatusList[0].HP;
    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector3.Distance(target.position, this.transform.position);
        if (distance < 10)
        {
            agent.destination = target.position;
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }

        if (currentHP <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Weapon"))
        {
            // ダメージ計算を単純にattack-defenceでやると、defenceが大きいときにdefenceが大きいのとそうでないので差ができすぎてしまう。
            // それを解決するために少し複雑にする。ドラクエ型計算式という。バフとかでも使える。
            damage = (int)(playerStatusSO.ATTACK / 2 - enemyStatusSO.enemyStatusList[0].Defence / 4);
            if (damage > 0)
            {
                currentHP -= damage;
            }
            Debug.Log("current hp: "+ currentHP);
        }
    }
}
