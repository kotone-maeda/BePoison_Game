using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] float mouseXSensitivity = 0.1f;   // 好みで
    // [Optional] スムージングしたい場合
    [SerializeField] float mouseXSmooth = 12f;
    float mouseXVel;   // SmoothDamp 用ワーク
    // [SerializeField] float pitchSpeed = 50f; // 垂直回転速度（deg/sec）
    // [SerializeField] float minPitch = -30f;   // 下限（下向き）
    // [SerializeField] float maxPitch = 65f;    // 上限（上向き）
    // [SerializeField] bool invertY = true;     // マウスを上に動かすと上を見る: true
    [SerializeField] float yawSpeed = 50f; // 水平回転速度（deg/sec）
    // float currentPitch = 0f;                // 現在のピッチ角（deg）
    private Vector3 playerPos;
    private float speed = 100f;
    private float mouseInputX;
    // private float mouseInputY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerPos = player.transform.position;

        // 現在のカメラ-プレイヤー方向から初期ピッチを算出
        Vector3 toCam = (transform.position - player.transform.position).normalized;
        // currentPitch = Mathf.Asin(Mathf.Clamp(toCam.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += player.transform.position - playerPos;
        playerPos = player.transform.position;

        var m = Mouse.current;
        if (m == null) return;

        // マウスの X だけ使う（Yは無視）
        float mouseX = m.delta.ReadValue().x;

        // --- Yaw（水平）だけ回す ---
        float yaw = mouseX * Time.deltaTime * yawSpeed;
        transform.RotateAround(playerPos, Vector3.up, yaw);

        // 視線はプレイヤーに向ける（これ自体はピッチのアニメではなく「向き」だけ）
        transform.LookAt(playerPos, Vector3.up);
    }

}