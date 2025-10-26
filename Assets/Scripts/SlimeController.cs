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
    [SerializeField] string areaName = "ForestArea";
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float attackRange = 1.8f;
    [SerializeField] float patrolRadius = 8f;
    [SerializeField] float patrolInterval = 2.0f;
    [SerializeField] float attackCooldown = 1.0f;

    [Header("Death")]
    [SerializeField] float destroyDelayOnDieAnim = 0f; // ※今は使わない（Edibleが消す）

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private int damage;

    private float nextPatrolTime;
    private float nextAttackTime;
    private int areaMask;

    private bool isDead = false;
    public bool IsDead => isDead;        // ← Edible から参照される

    enum State { Patrol, Chase, Attack }
    State state = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent)
        {
            agent.speed = speed;
            agent.stoppingDistance = attackRange;
            agent.autoBraking = true;
        }

        int idx = NavMesh.GetAreaFromName(areaName);
        areaMask = (idx >= 0) ? (1 << idx) : NavMesh.AllAreas;
        if (idx < 0)
            Debug.LogWarning($"Area '{areaName}' が見つかりません。徘徊は全エリアで行われます。", this);

        currentHP = enemyStatusSO.enemyStatusList[0].HP;

        // NavMesh 用は Rigidbody なし/または isKinematic 推奨
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (isDead)
        {
            SafeStopAgent();
            animator.SetBool("Walk", false);
            return;
        }

        // 状態遷移
        float dist = Vector3.Distance(target.position, transform.position);
        if (dist <= attackRange)          state = State.Attack;
        else if (dist <= detectionRadius) state = State.Chase;
        else                              state = State.Patrol;

        switch (state)
        {
            case State.Patrol:
                PatrolTick();
                break;

            case State.Chase:
                if (CanUseAgent())
                {
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                }
                break;

            case State.Attack:
                SafeStopAgent();
                Vector3 to = target.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 12f * Time.deltaTime);

                if (Time.time >= nextAttackTime)
                {
                    animator.SetTrigger("Attack");
                    nextAttackTime = Time.time + attackCooldown;
                }
                break;
        }

        // 歩行アニメ（実際に動いている間だけ）
        bool walking = CanUseAgent() && !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool("Walk", walking);

        if (currentHP <= 0 && !isDead)
        {
            Die();
        }
    }

    void PatrolTick()
    {
        if (!CanUseAgent()) return;

        agent.isStopped = false;

        bool reached = (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f);
        if (reached || Time.time >= nextPatrolTime || agent.velocity.sqrMagnitude < 0.01f)
        {
            PickNewPatrolPoint();
        }
    }

    void PickNewPatrolPoint()
    {
        if (!CanUseAgent()) return;

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
        nextPatrolTime = Time.time + patrolInterval;
    }

    // 武器ヒットは「生存中のみ」受け付ける
    void OnTriggerEnter(Collider col)
    {
        if (isDead) return;

        if (col.gameObject.CompareTag("Weapon"))
        {
            animator.SetTrigger("GetHit");
            damage = (int)(playerStatusSO.ATTACK / 2 - enemyStatusSO.enemyStatusList[0].Defence / 4);
            if (damage > 0) ApplyDamage(damage);
        }
    }

    // 外からも呼べるダメージ口
    public void ApplyDamage(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHP -= amount;
        if (currentHP <= 0) Die();
    }

    // ★ ここで死亡処理を一元化（食べる/Destroyは Edible が実施）
    void Die()
    {
        isDead = true;
        Debug.Log($"{name} set IsDead = {isDead}");

        SafeStopAgent();
        if (agent) agent.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // 重要：Collider は残す（UI/Interact のため）
        // 例）var col = GetComponent<Collider>(); if (col) col.enabled = true;

        animator.SetTrigger("Die");
        animator.SetBool("Walk", false);

        // ここで Destroy はしない（食べた時に Edible が destroyTarget を消す）
        // if (destroyDelayOnDieAnim > 0f) Destroy(gameObject, destroyDelayOnDieAnim);
    }

    bool CanUseAgent()
    {
        return agent != null && agent.enabled && gameObject.activeInHierarchy && agent.isOnNavMesh;
    }

    void SafeStopAgent()
    {
        if (CanUseAgent())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    // 必要ならアニメイベントで呼ぶ
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
