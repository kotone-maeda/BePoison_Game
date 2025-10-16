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

    [Header("Death")]
    [SerializeField] float destroyDelayOnDieAnim = 0f; // アニメイベント未設定の場合の保険（秒）0なら使わない

    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private int damage;

    private float nextPatrolTime;
    private float nextAttackTime;
    private int areaMask;

    private bool isDead = false;

    enum State { Patrol, Chase, Attack }
    State state = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.speed = speed;
        agent.stoppingDistance = attackRange;
        agent.autoBraking = true;

        int idx = NavMesh.GetAreaFromName(areaName);
        areaMask = (idx >= 0) ? (1 << idx) : NavMesh.AllAreas;
        if (idx < 0)
            Debug.LogWarning($"Area '{areaName}' が見つかりません。徘徊は全エリアで行われます。", this);

        currentHP = enemyStatusSO.enemyStatusList[0].HP;
        PickNewPatrolPoint();
    }

    void Update()
    {
        // ★ 死亡中は完全停止＆何もしない
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
        else                               state = State.Patrol;

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
                Vector3 to = (target.position - transform.position);
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 12f * Time.deltaTime);

                if (Time.time >= nextAttackTime)
                {
                    animator.SetTrigger("Attack");
                    nextAttackTime = Time.time + attackCooldown;
                    // ダメージはアニメイベントでプレイヤーへ
                }
                break;
        }

        // 歩行アニメ（実際に動いている間だけ）
        bool walking = CanUseAgent() && !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool("Walk", walking);

        // ★ HP処理：0以下になったら Die 発火（アニメ再生＆停止）
        if (currentHP <= 0 && !isDead)
        {
            Die(); // ここで停止＆アニメ遷移
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

    // ★ ここで死亡処理を一元化
    void Die()
    {
        isDead = true;

        // 動きを完全停止
        SafeStopAgent();          // isStopped/ResetPath（NavMesh上なら）
        if (agent != null) agent.enabled = false; // 以後のエラー防止

        // 物理で押されないようコライダーや剛体を調整（任意）
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // アニメへ遷移（Animatorに bool "Die" を用意しておく）
        animator.SetTrigger("Die");
        animator.SetBool("Walk", false);

        // アニメイベントで呼ぶなら不要。保険で遅延Destroyしたいときだけ使う
        if (destroyDelayOnDieAnim > 0f)
            Destroy(gameObject, destroyDelayOnDieAnim);
    }

    // ★ プレイヤーに“食べられた”ら即Destroy（例：PlayerEat というトリガーに当たったら）
    void OnTriggerEnter(Collider col)
    {
        if (isDead)
        {
            if (col.gameObject.CompareTag("PlayerEat"))
                Destroy(gameObject);
            return;
        }
        if (col.gameObject.CompareTag("Weapon"))
        {
            animator.SetTrigger("GetHit");
            damage = (int)(playerStatusSO.ATTACK / 2 - enemyStatusSO.enemyStatusList[0].Defence / 4);
            if (damage > 0) currentHP -= damage;
            return;
        }

        // プレイヤーの“食べる判定”用の当たり（タグ名はプロジェクトの実際に合わせて変更）
        if (col.gameObject.CompareTag("PlayerEat"))
        {
            // すぐ消す（演出が要るなら先に Die() を呼んでから短い遅延DestroyでもOK）
            Destroy(gameObject);
        }
    }

    // --- Utility ---

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

    // ▼ アニメーションイベント用：Dieアニメの最後で呼ぶと綺麗に消える
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
