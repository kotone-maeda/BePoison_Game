using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerStatusSO playerStatusSO;

    [Header("Move")]
    [SerializeField] float moveForce = 10f;     // 基本移動力
    [SerializeField] float slowMultiplier = 0.5f; // 腹痛しきい値超えたときの減速率（1/2）

    [Header("Jump")]
    [SerializeField] float jumpSpeed = 7.5f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] LayerMask groundMask = ~0;

    bool jumpRequested;
    bool isGrounded;

    [Header("Attack")]
    [SerializeField] float attackCooldown = 0.35f;
    float nextAttackTime = 0f;

    Rigidbody rb;
    Animator animator;

    // 入力バッファ
    float inputHorizontal;
    float inputVertical;
    Vector3 cameraForward;
    Vector3 moveForward;

    [Header("Stomachache / Poison")]
    public int maxStomachache; // 腹痛の最大（SO から）
    public int stomachache;    // 現在の腹痛（0→増えるほどツラい）

    public int maxPoisonRes;   // 毒耐性の最大（SO から）
    public int poisonRes;      // 現在の毒耐性

    [Tooltip("この割合(0-1)を超えた腹痛で移動半減")]
    [Range(0f, 1f)] public float slowThresholdPercent = 0.6f;

    public event Action OnStatsChanged;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // ---- Max系を SO から反映 ----
        maxStomachache = playerStatusSO ? playerStatusSO.MaxStomachache : 100;
        maxPoisonRes   = playerStatusSO ? playerStatusSO.MaxPoisonRes   : 100;

        // 現在値の初期化（腹痛は通常0開始、毒耐性はSOの初期値）
        stomachache = Mathf.Clamp(stomachache, 0, maxStomachache);
        if (stomachache == 0) stomachache = 0;

        poisonRes = playerStatusSO
            ? Mathf.Clamp(playerStatusSO.BasePoisonRes, 0, maxPoisonRes)
            : Mathf.Clamp(poisonRes, 0, maxPoisonRes);

        OnStatsChanged?.Invoke();
    }

    void Update()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k == null || m == null) return;

        // 右クリック攻撃
        if (m.rightButton.wasPressedThisFrame && Time.time >= nextAttackTime)
        {
            animator.SetTrigger("Attack");
            nextAttackTime = Time.time + attackCooldown;
        }

        if (k.spaceKey.wasPressedThisFrame) jumpRequested = true;

        float h = 0f, v = 0f;
        if (k.aKey.isPressed) h -= 1f;
        if (k.dKey.isPressed) h += 1f;
        if (k.sKey.isPressed) v -= 1f;
        if (k.wKey.isPressed) v += 1f;

        // ゲームパッド併用
        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > Mathf.Abs(h)) h = stick.x;
            if (Mathf.Abs(stick.y) > Mathf.Abs(v)) v = stick.y;
        }

        // 斜め正規化
        if (h != 0f && v != 0f)
        {
            Vector2 n = new Vector2(h, v).normalized;
            h = n.x; v = n.y;
        }

        inputHorizontal = h;
        inputVertical   = v;

        bool run = (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f);
        animator.SetBool("Run", run);
    }

    void FixedUpdate()
    {
        // 接地チェック
        if (groundCheck)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        else
            isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, groundMask, QueryTriggerInteraction.Ignore);

        // ジャンプ
        if (jumpRequested && isGrounded)
        {
            var v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);
            animator.SetTrigger("Jump");
        }
        jumpRequested = false;

        // カメラ基準移動
        cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        moveForward   = cameraForward * inputVertical + Camera.main.transform.right * inputHorizontal;

        // 腹痛で減速判定
        bool isSlowed = (float)stomachache >= maxStomachache * slowThresholdPercent;
        float speedMul = isSlowed ? slowMultiplier : 1f;

        rb.linearVelocity = moveForward * (moveForce * speedMul) + new Vector3(0, rb.linearVelocity.y, 0);

        if (moveForward != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveForward);

        // 死亡判定（腹痛が最大に達したら）
        if (stomachache >= maxStomachache)
        {
            // TODO: 死亡処理（アニメ・リスポーンなど）
            // Die();
        }
    }

    // ===== 腹痛・毒の操作 =====

    /// <summary>腹痛を加算（0以上）。最大でクランプし、変更通知。</summary>
    public void AddStomachache(int amount)
    {
        if (amount <= 0) return;
        int prev = stomachache;
        stomachache = Mathf.Min(maxStomachache, stomachache + amount);
        if (stomachache != prev) OnStatsChanged?.Invoke();
    }

    /// <summary>腹痛を軽減（0以上）。0までクランプし、変更通知。</summary>
    public void RelieveStomachache(int amount)
    {
        if (amount <= 0) return;
        int prev = stomachache;
        stomachache = Mathf.Max(0, stomachache - amount);
        if (stomachache != prev) OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 毒入りを食べた時:
    /// 1) 毒耐性は常に poisonAmount 増える（上限あり）
    /// 2) 毒量 > 現在耐性 のとき、超過ぶんだけ腹痛に加算
    /// </summary>
    public void EatPoison(int poisonAmount)
    {
        if (poisonAmount <= 0) { OnStatsChanged?.Invoke(); return; }

        int overflow = Mathf.Max(0, poisonAmount - poisonRes);
        poisonRes = Mathf.Min(maxPoisonRes, poisonRes + poisonAmount);

        if (overflow > 0) AddStomachache(overflow);
        else OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// ひとつの食べ物効果をまとめて適用するためのユーティリティ。
    /// stomachacheIncrease → EatPoison(poison) → Relieve(heal) の順に適用。
    /// </summary>
    public void ConsumeFood(int stomachacheIncrease, int poison, int heal)
    {
        if (stomachacheIncrease > 0) AddStomachache(stomachacheIncrease);
        if (poison > 0) EatPoison(poison);
        if (heal > 0) RelieveStomachache(heal);
    }

    // 既存呼び出し互換（ダメージ=腹痛加算 / 回復=腹痛軽減）
    public void ApplyDamage(int dmg) => AddStomachache(Mathf.Max(0, dmg));
    public void Heal(int amount)     => RelieveStomachache(Mathf.Max(0, amount));
}
