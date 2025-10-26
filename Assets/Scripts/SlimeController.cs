using UnityEngine;
using UnityEngine.AI;

public class SlimeController : MonoBehaviour
{
    public enum SlimeKind { Chaser = 0, Runner = 1 } // 0=向かってくる, 1=逃げる

    [Header("Refs")]
    [SerializeField] Transform target;
    [SerializeField] EnemyStatusSO enemyStatusSO;   // index 0: Chaser, 1: Runner
    [SerializeField] PlayerStatusSO playerStatusSO;

    [Header("Kind")]
    [SerializeField] SlimeKind kind = SlimeKind.Chaser;
    [Tooltip("サイズで自動判定したい場合はON（Xスケールが threshold 以上なら Runner とする等）")]
    [SerializeField] bool autoKindByScale = false;
    [SerializeField] float runnerScaleThreshold = 1.25f; // 例: 大きいのを Runner 扱いなど

    [Header("Nav/Anim")]
    [SerializeField] float speed = 2.5f;
    [SerializeField] string areaName = "ForestArea";
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float attackRange = 1.8f;      // Chaser 用
    [SerializeField] float patrolRadius = 8f;
    [SerializeField] float patrolInterval = 2.0f;
    [SerializeField] float attackCooldown = 1.0f;

    [Header("Runner (逃げるタイプ)")]
    [SerializeField] float keepAwayRange = 6f;      // これ以内に入られたら離れる
    [SerializeField] float fleePointJitter = 2.0f;  // 逃げ先のランダム幅

    [Header("Death")]
    [SerializeField] float destroyDelayOnDieAnim = 0f; // ※ 今は使わない（Edibleが消す）

    private NavMeshAgent agent;
    private Animator animator;

    // 現在値
    private int currentHP;
    private int damage;
    private EnemyStatusSO.EnemyStatus stat; // 種別に応じた参照

    private float nextPatrolTime;
    private float nextAttackTime;
    private int areaMask;

    private bool isDead = false;
    public bool IsDead => isDead;

    enum State { Patrol, Chase, Attack, Flee }
    State state = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 種別を決定
        if (autoKindByScale)
            kind = (transform.localScale.x >= runnerScaleThreshold) ? SlimeKind.Runner : SlimeKind.Chaser;

        // ステータスをインデックスから取得
        int idx = (int)kind;
        if (enemyStatusSO != null && enemyStatusSO.enemyStatusList != null && enemyStatusSO.enemyStatusList.Count > idx)
            stat = enemyStatusSO.enemyStatusList[idx];

        // HP 初期化
        currentHP = (stat != null) ? stat.HP : 30;

        if (agent)
        {
            agent.speed = speed;
            agent.stoppingDistance = (kind == SlimeKind.Chaser) ? attackRange : 0f;
            agent.autoBraking = true;
        }

        int a = NavMesh.GetAreaFromName(areaName);
        areaMask = (a >= 0) ? (1 << a) : NavMesh.AllAreas;
        if (a < 0) Debug.LogWarning($"Area '{areaName}' が見つかりません。徘徊は全エリアで行われます。", this);

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true; // NavMeshAgentと干渉しないように

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

        float dist = Vector3.Distance(target.position, transform.position);

        // 状態遷移
        if (kind == SlimeKind.Chaser)
        {
            if (dist <= attackRange)          state = State.Attack;
            else if (dist <= detectionRadius) state = State.Chase;
            else                              state = State.Patrol;
        }
        else // Runner
        {
            if (dist <= keepAwayRange)        state = State.Flee;   // 一定距離まで来られたら逃走
            else if (dist <= detectionRadius) state = State.Flee;   // 見つけたら基本逃げる
            else                              state = State.Patrol;
        }

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
                // 近距離で向きを合わせて攻撃（Chaserのみ）
                SafeStopAgent();
                FaceTargetFlat(target.position);
                if (Time.time >= nextAttackTime)
                {
                    animator.SetTrigger("Attack");
                    nextAttackTime = Time.time + attackCooldown;
                    // ダメージ判定はアニメーションイベント側でプレイヤーへ
                }
                break;

            case State.Flee:
                FleeTick();
                break;
        }

        // 歩行アニメ（エージェントが実際に動いている間だけ）
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

    void FleeTick()
    {
        if (!CanUseAgent()) return;

        // プレイヤーと反対方向へ逃げ先を設定
        Vector3 from = transform.position;
        Vector3 away = (from - target.position).normalized;
        if (away.sqrMagnitude < 0.001f) away = transform.forward;

        Vector3 candidate = from + away * (keepAwayRange + 1.5f);
        // ちょっとランダムに散らす
        candidate += new Vector3(Random.Range(-fleePointJitter, fleePointJitter), 0f, Random.Range(-fleePointJitter, fleePointJitter));

        if (NavMesh.SamplePosition(candidate, out var hit, 2f, areaMask))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        // 逃げるだけなので攻撃はしない
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

    void FaceTargetFlat(Vector3 worldPos)
    {
        Vector3 to = worldPos - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
        {
            var rot = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 12f * Time.deltaTime);
        }
    }

    // ヒットは生存中のみ
    void OnTriggerEnter(Collider col)
    {
        if (isDead) return;

        if (col.gameObject.CompareTag("Weapon"))
        {
            animator.SetTrigger("GetHit");
            int def = (stat != null) ? stat.Defence : 0;
            int atk = (playerStatusSO != null) ? playerStatusSO.ATTACK : 10;
            damage = (int)(atk / 2f - def / 4f);
            if (damage > 0) ApplyDamage(damage);
        }
    }

    public void ApplyDamage(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHP -= amount;
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        SafeStopAgent();
        if (agent) agent.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        animator.SetTrigger("Die");
        animator.SetBool("Walk", false);
        // Destroy はしない（食べる側 Edible が消す）
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
