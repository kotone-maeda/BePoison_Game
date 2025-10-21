using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractBox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerController player;
    [SerializeField] Transform facingRef;          // 前方参照（プレイヤー本体など）
    [SerializeField] bool useCameraXZ = true;      // 3人称ならカメラのXZを前方に

    [Header("Zone")]
    [Tooltip("このトリガー内にいる対象だけUI表示＆選択（未指定なら自動取得）")]
    [SerializeField] Collider triggerZone;

    [Header("UI Prompt")]
    [SerializeField] CanvasGroup promptGroup;
    [SerializeField] TextMeshProUGUI promptText;

    [Header("Selection")]
    [Tooltip("視線(LINE OF SIGHT)のレイが当たってほしいレイヤー（壁・地形のみ推奨）")]
    [SerializeField] LayerMask losMask = 0;
    [Tooltip("通常の前方角度しきい値")]
    [SerializeField] float maxPickAngle = 100f;
    [Tooltip("一度選ばれた対象に対して許容する追加角度")]
    [SerializeField] float stickyExtraAngle = 25f;
    [Tooltip("至近距離なら角度・LOSを免除する距離")]
    [SerializeField] float nearGraceDist = 1.2f;
    [Tooltip("選択済みを保持する距離（これ以内ならLOSも緩く）")]
    [SerializeField] float stickyKeepDistance = 2.5f;

    [Header("Audio")]
    [SerializeField] AudioSource sfxSource;

    [Header("Debug")]
    [SerializeField] bool debugLog = false;

    readonly HashSet<IInteractable> candidates = new HashSet<IInteractable>();
    IInteractable current;

    void Awake()
    {
        if (!triggerZone) triggerZone = GetComponent<Collider>();
    }

    void OnEnable()
    {
        current = null;
        HidePrompt();
    }

    void Update()
    {
        UpdateTargetAndPrompt();

        var k = Keyboard.current;
        if (k != null && k.eKey.wasPressedThisFrame && current != null)
        {
            TryInteract();
        }
    }

    // ====== メインロジック ======
    void UpdateTargetAndPrompt()
    {
        // 破棄済み候補を掃除
        candidates.RemoveWhere(x =>
        {
            var mb = x as MonoBehaviour;
            return (mb == null) || (mb && mb.Equals(null));
        });

        // ★ ゾーン外の候補を毎フレーム除外（Exit取りこぼし対策）
        if (triggerZone)
        {
            candidates.RemoveWhere(x =>
            {
                var mb = x as MonoBehaviour;
                return mb == null || !IsInsideZone(mb.transform);
            });
        }

        // ★ current は「候補に残っている & ゾーン内」じゃなければ手放す
        if (current != null)
        {
            var cmb = current as MonoBehaviour;
            if (cmb == null || cmb.Equals(null) || !candidates.Contains(current) || (triggerZone && !IsInsideZone(cmb.transform)))
            {
                current = null;
                HidePrompt();
            }
        }

        // --- 選択済み（current）を“保持”できるならそれを使う ---
        if (current != null)
        {
            Transform ctr;
            var cpos = GetPosSafe(current, out ctr);
            if (ctr != null &&
                current.CanInteract(player) &&
                IsInFront(cpos, sticky: true) &&
                HasLineOfSight(cpos, ctr, sticky: true))
            {
                // まだ保持できる
                ShowPrompt(current.GetPrompt(player));
                if (debugLog) Debug.Log($"[InteractDbg] Keep: {ctr.name}");
                return;
            }
            current = null; // 手放して選び直し
        }

        if (debugLog)
        {
            foreach (var x in candidates)
            {
                Transform tr;
                var pos = GetPosSafe(x, out tr);
                if (tr == null) continue;
                bool inZone = !triggerZone || IsInsideZone(tr);
                bool can = inZone && x.CanInteract(player);
                bool front = inZone && IsInFront(pos, sticky: false);
                bool los = inZone && HasLineOfSight(pos, tr, sticky: false);
                Debug.Log($"[InteractDbg] {tr.name} inZone:{inZone} can:{can} front:{front} los:{los}", tr);
            }
        }

        // --- 新規選定：優先度 → 距離 ---
        IInteractable best = null;
        float bestDist2 = float.MaxValue;

        foreach (var it in candidates)
        {
            Transform tr;
            var pos = GetPosSafe(it, out tr);
            if (tr == null) continue;

            if (triggerZone && !IsInsideZone(tr)) continue;
            if (!it.CanInteract(player)) continue;
            if (!IsInFront(pos, sticky: false)) continue;
            if (!HasLineOfSight(pos, tr, sticky: false)) continue;

            float d2 = (pos - transform.position).sqrMagnitude;
            if (best == null ||
                it.Priority > best.Priority ||
                (it.Priority == best.Priority && d2 < bestDist2))
            {
                best = it; bestDist2 = d2;
            }
        }

        current = best;
        if (current != null) ShowPrompt(current.GetPrompt(player));
        else HidePrompt();
    }

    void TryInteract()
    {
        if (current == null) return;

        var targetMb = current as MonoBehaviour;

        // 実行（中で Destroy される可能性あり）
        current.Interact(player);

        // SFX（あれば）
        if (current != null && current.TryGetSfx(out var clip, out var vol) && clip && sfxSource)
            sfxSource.PlayOneShot(clip, vol);

        // 破棄 or ゾーン外なら即クリーンアップ
        bool gone = (targetMb == null || targetMb.Equals(null));
        if (!gone && triggerZone && targetMb != null) gone = !IsInsideZone(targetMb.transform);

        if (gone)
        {
            candidates.RemoveWhere(x =>
            {
                var mb = x as MonoBehaviour;
                return (mb == null) || (mb && mb.Equals(null)) || (triggerZone && mb && !IsInsideZone(mb.transform));
            });
            current = null;
            HidePrompt();
            return;
        }

        UpdateTargetAndPrompt();
    }

    // ====== ヘルパ ======
    Vector3 GetPosSafe(IInteractable it, out Transform tr)
    {
        tr = null;
        var mb = it as MonoBehaviour;
        if (!mb) return default;
        if (mb == null) return default; // Destroy 済み
        tr = mb.transform;
        return tr ? tr.position : default;
    }

    bool IsInFront(Vector3 worldPos, bool sticky)
    {
        // 原点：プレイヤー（facingRef）か InteractBox
        Vector3 origin = facingRef ? facingRef.position : transform.position;

        // 近距離なら無条件OK
        float keepDist = Mathf.Max(nearGraceDist, 1.0f);
        float sqrDist = (worldPos - origin).sqrMagnitude;
        if (sqrDist <= keepDist * keepDist) return true;

        // 前方ベクトル：カメラのXZ or facingRef.forwardのXZ
        Vector3 fwd;
        if (useCameraXZ && Camera.main != null)
            fwd = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        else
            fwd = (facingRef ? facingRef.forward : transform.forward);

        if (fwd.sqrMagnitude < 1e-6f) fwd = transform.forward;

        // XZ 平面で比較（縦方向の影響を除去）
        Vector3 to = worldPos - origin;
        to.y = 0f; fwd.y = 0f;

        float mag = to.magnitude;
        if (mag < 0.05f) return true; // ほぼ重なり
        to /= mag;

        float limit = sticky ? (maxPickAngle + stickyExtraAngle) : maxPickAngle;
        float dot = Mathf.Clamp(Vector3.Dot(fwd, to), -1f, 1f);
        float ang = Mathf.Acos(dot) * Mathf.Rad2Deg;
        return ang <= limit;
    }

    bool HasLineOfSight(Vector3 worldPos, Transform target, bool sticky)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = worldPos - origin;
        float dist = dir.magnitude;

        // 至近/スティッキー保持距離はゆるめ
        if (dist <= nearGraceDist) return true;
        if (sticky && dist <= stickyKeepDistance) return true;

        dir /= Mathf.Max(dist, 0.0001f);
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, losMask, QueryTriggerInteraction.Ignore))
        {
            if (target != null && (hit.transform == target || hit.transform.IsChildOf(target)))
                return true; // 目標自身に当たっただけならOK
            return false; // 壁・地形で遮られた
        }
        return true; // 何にも当たらない＝見えてる
    }

    // 互換用（昔の呼び出しが残っていても落ちないように）
    bool HasLineOfSight(Vector3 worldPos) => HasLineOfSight(worldPos, null, false);

    bool IsInsideZone(Transform tr)
    {
        if (!triggerZone || tr == null) return true; // 参照無ければスキップ
        return triggerZone.bounds.Contains(tr.position);
    }

    void ShowPrompt(string text)
    {
        if (!promptGroup) return;
        promptGroup.alpha = 1f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
        if (promptText) promptText.text = text;
    }

    void HidePrompt()
    {
        if (!promptGroup) return;
        promptGroup.alpha = 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
    }

    // ====== Trigger 検知 ======
    void OnTriggerEnter(Collider other)
    {
        foreach (var it in other.GetComponents<IInteractable>())
            if (it != null) candidates.Add(it);

        foreach (var it in other.GetComponentsInParent<IInteractable>())
            if (it != null) candidates.Add(it);

        if (debugLog) Debug.Log($"[InteractDbg] Enter: {other.name}");
    }

    void OnTriggerExit(Collider other)
    {
        foreach (var it in other.GetComponents<IInteractable>())
            candidates.Remove(it);
        foreach (var it in other.GetComponentsInParent<IInteractable>())
            candidates.Remove(it);

        // current が出て行ったら即消す
        if (current != null)
        {
            var mb = current as MonoBehaviour;
            if (mb == null || mb.Equals(null) || (triggerZone && !IsInsideZone(mb.transform)))
            {
                current = null;
                HidePrompt();
            }
        }

        if (debugLog) Debug.Log($"[InteractDbg] Exit: {other.name}");
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<BoxCollider>();
        if (!col) return;
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
    }
}
