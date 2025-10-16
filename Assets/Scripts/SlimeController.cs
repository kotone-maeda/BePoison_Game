using UnityEngine;
using UnityEngine.AI;

public class SlimeController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform target;
    [SerializeField] EnemyStatusSO enemyStatusSO;
    [SerializeField] PlayerStatusSO playerStatusSO;

    [Header("Nav/Anim")]
    [SerializeField] float speed = 2.5f;
    [SerializeField] string areaName = "ForestArea"; // NavMeshModifierVolume の Area 名
    [SerializeField] float detectionRadius = 10f;   // 追跡開始距離
    [SerializeField] float attackRange = 1.8f;      // 周囲(攻撃停止距離)
    [SerializeField] float patrolRadius = 8f;       // 徘徊ターゲットの抽選半径
    [SerializeField] float patrolInterval = 2.0f;   // 次の点を選ぶ最短間隔
    [SerializeField] float attackCooldown = 1.0f;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private int damage;

    private float nextPatrolTime;
    private float nextAttackTime;
    private int areaMask;

    enum State { Patrol, Chase, Attack }
    State state = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.speed = speed;
        agent.stoppingDistance = attackRange;
        agent.autoBraking = true;

        // Area マスク（指定名が無ければ AllAreas）
        int idx = NavMesh.GetAreaFromName(areaName);
        areaMask = (idx >= 0) ? (1 << idx) : NavMesh.AllAreas;
        if (idx < 0)
            Debug.LogWarning($"Area '{areaName}' が見つかりません。徘徊は全エリアで行われます。", this);

        currentHP = enemyStatusSO.enemyStatusList[0].HP;
        PickNewPatrolPoint(); // 最初の目的地
    }

    void Update()
    {
        // 状態遷移
        float dist = Vector3.Distance(target.position, transform.position);
        if (dist <= attackRange)         state = State.Attack;
        else if (dist <= detectionRadius) state = State.Chase;
        else                              state = State.Patrol;

        switch (state)
        {
            case State.Patrol:
                PatrolTick();
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(target.position);
                break;

            case State.Attack:
                // 周囲で停止し向きを合わせる
                agent.isStopped = true;
                Vector3 to = (target.position - transform.position);
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 12f * Time.deltaTime);

                if (Time.time >= nextAttackTime)
                {
                    animator.SetTrigger("Attack"); // 攻撃アニメをセットしている前提
                    nextAttackTime = Time.time + attackCooldown;
                    // ダメージ判定はアニメーションイベント側でプレイヤーに与えるのが◎
                }
                break;
        }

        // 歩行アニメ（実際に動いている間だけ）
        bool walking = !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool("Walk", walking);

        // HP処理
        if (currentHP <= 0) Destroy(gameObject);
    }

    void PatrolTick()
    {
        agent.isStopped = false;

        // 目的地に着いた/止まったら次を抽選
        bool reached = (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f);
        if (reached || Time.time >= nextPatrolTime || agent.velocity.sqrMagnitude < 0.01f)
        {
            PickNewPatrolPoint();
        }
    }

    void PickNewPatrolPoint()
    {
        // いまの位置を中心に、指定 Area の三角形上でランダムサンプリング
        Vector3 center = transform.position;
        for (int i = 0; i < 20; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 p = center + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, areaMask))
            {
                agent.SetDestination(hit.position);
                nextPatrolTime = Time.time + patrolInterval;
                return;
            }
        }
        // 20回失敗したら保険で現状維持
        nextPatrolTime = Time.time + patrolInterval;
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Weapon"))
        {
            damage = (int)(playerStatusSO.ATTACK / 2 - enemyStatusSO.enemyStatusList[0].Defence / 4);
            if (damage > 0) currentHP -= damage;
        }
    }
}