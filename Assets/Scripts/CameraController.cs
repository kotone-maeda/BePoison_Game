using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] float mouseXSensitivity = 0.1f;   // 好みで
    // [Optional] スムージングしたい場合
    [SerializeField] float mouseXSmooth = 12f;
    float mouseXVel;   // SmoothDamp 用ワーク
    [SerializeField] float pitchSpeed = 50f; // 垂直回転速度（deg/sec）
    [SerializeField] float minPitch = -30f;   // 下限（下向き）
    [SerializeField] float maxPitch = 65f;    // 上限（上向き）
    [SerializeField] bool invertY = true;     // マウスを上に動かすと上を見る: true
    [SerializeField] float yawSpeed = 50f; // 水平回転速度（deg/sec）
    float currentPitch = 0f;                // 現在のピッチ角（deg）
    private Vector3 playerPos;
    private float speed = 100f;
    private float mouseInputX;
    private float mouseInputY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerPos = player.transform.position;

        // 現在のカメラ-プレイヤー方向から初期ピッチを算出
        Vector3 toCam = (transform.position - player.transform.position).normalized;
        currentPitch = Mathf.Asin(Mathf.Clamp(toCam.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += player.transform.position - playerPos;
        playerPos = player.transform.position;

        var m = Mouse.current;
        if (m == null) return;

        Vector2 d = m.delta.ReadValue(); // d.x = Mouse X, d.y = Mouse Y

        // --- Yaw（水平）---
        float yaw = d.x * Time.deltaTime * yawSpeed;
        transform.RotateAround(playerPos, Vector3.up, yaw);

        // --- Pitch（垂直）---
        float sign = invertY ? -1f : 1f;
        float pitchDelta = d.y * Time.deltaTime * pitchSpeed * sign;

        float prev = currentPitch;
        currentPitch = Mathf.Clamp(prev + pitchDelta, minPitch, maxPitch);
        float apply = currentPitch - prev;

        // カメラの“右方向”を軸に上下回転（プレイヤー中心）
        transform.RotateAround(playerPos, transform.right, apply);

        // いつもプレイヤーを見るなら（好みでON）
        transform.LookAt(playerPos, Vector3.up);
    }

}