using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerStatusSO playerStatusSO;
    [Header("Move")]
    public float moveForce = 10f; // WASDで加える力の大きさ
    public TextMeshProUGUI HPText;
    [SerializeField] GameObject statusWindow;
    public int currentHP;
    [SerializeField] GameObject itemBoxManager;
    [Header("Attack")]
    [SerializeField] float attackCooldown = 0.35f; // 連打抑制したいとき
    float nextAttackTime = 0f;
    private Rigidbody rb;
    private Animator animator;

    // Updateで作った入力結果を物理用にバッファ
    // private Vector3 heldDir = Vector3.zero;
    private float inputHorizontal;
    private float inputVertical;
    private Vector3 cameraForward;
    private Vector3 moveForward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        currentHP = playerStatusSO.HP;
        HPText.text = "HP: " + currentHP.ToString();
    }

    // ---- 入力・アニメはフレーム更新で処理 ----
    void Update()
    {
        HPText.text = "HP: " + currentHP.ToString();
        var k = Keyboard.current;
        var m = Mouse.current;
        if (k == null || m == null) return;

        // ---- 攻撃：右クリックの「押された瞬間」
        if (m.rightButton.wasPressedThisFrame && Time.time >= nextAttackTime)
        {
            animator.SetTrigger("Attack");      // ← Trigger を使うのが楽
            nextAttackTime = Time.time + attackCooldown;
        }

        // if (k.tabKey.wasPressedThisFrame)
        // {
        //     bool isActive = !statusWindow.activeSelf;
        //     statusWindow.SetActive(isActive);

        //     if (isActive)
        //     {
        //         // UIを開いたとき：ゲームを一時停止
        //         Time.timeScale = 0f;
        //         itemBoxManager.GetComponent<ItemBoxManager>().ItemOpen();
        //     }
        //     else
        //     {
        //         // UIを閉じたとき：再開
        //         Time.timeScale = 1f;
        //     }
        // }

        float h = 0f, v = 0f;
        if (k.aKey.isPressed) h -= 1f;
        if (k.dKey.isPressed) h += 1f;
        if (k.sKey.isPressed) v -= 1f;
        if (k.wKey.isPressed) v += 1f;

        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            // キー入力より有意に動いていればスティック値を優先
            if (Mathf.Abs(stick.x) > Mathf.Abs(h)) h = stick.x;
            if (Mathf.Abs(stick.y) > Mathf.Abs(v)) v = stick.y;
        }

        // 斜め入力で速くなりすぎないように（W+Dなど）：両方押しのときだけ正規化
        if (h != 0f && v != 0f)
        {
            Vector2 n = new Vector2(h, v).normalized;
            h = n.x; v = n.y;
        }

        inputHorizontal = h;
        inputVertical   = v;

        // --- アニメーション（押下状態から算出） ---
        // 元コードの意図を踏襲：W/SでRun、AでRunLeft、DでRunRight
        bool run = k.wKey.isPressed || k.sKey.isPressed;
        bool runLeft = k.aKey.isPressed;
        bool runRight = k.dKey.isPressed;

        animator.SetBool("Run", run);
        animator.SetBool("RunLeft", runLeft);
        animator.SetBool("RunRight", runRight);
    }

    // ---- 物理は固定タイミングで処理 ----
    void FixedUpdate()
    {
        cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        moveForward = cameraForward * inputVertical + Camera.main.transform.right * inputHorizontal;
        rb.linearVelocity = moveForward * moveForce + new Vector3(0, rb.linearVelocity.y, 0);

        if(moveForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveForward);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        currentHP -= 10;
    }

    void OnTriggerEnter(Collider col)
    {
        // if (col.gameObject.CompareTag("Item"))
        // {
        //     itemBoxManager.GetComponent<ItemBoxManager>().getItem = col.gameObject.GetComponent<ItemManager>().itemNo;
        //     itemBoxManager.GetComponent<ItemBoxManager>().ItemGet();
        //     Destroy(col.gameObject);
        // }
    }
}
