using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractBox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerController player;
    [Header("UI Prompt")]
    [SerializeField] CanvasGroup promptGroup;
    [SerializeField] TextMeshProUGUI promptText;

    [Header("Selection")]
    [SerializeField] LayerMask losMask = ~0;  // 視線チェック用
    [SerializeField] float maxPickAngle = 150f;

    [Header("Audio")]
    [SerializeField] AudioSource sfxSource;

    [SerializeField] bool debugLog = true; // ← 追加
    Transform GetTr(IInteractable it) => ((MonoBehaviour)it).transform;

    readonly HashSet<IInteractable> candidates = new HashSet<IInteractable>();
    IInteractable current;

    void OnEnable() { HidePrompt(); }
    void Update()
    {
        UpdateTargetAndPrompt();

        var k = Keyboard.current;
        if (k != null && k.eKey.wasPressedThisFrame && current != null)
        {
            current.Interact(player);
            if (current.TryGetSfx(out var clip, out var vol) && sfxSource && clip)
                sfxSource.PlayOneShot(clip, vol);
            // 成功後に対象が消える場合は自然に候補から外れる
            HidePrompt();
        }
    }

    void UpdateTargetAndPrompt()
    {
        if (candidates.Count == 0)
        {
            current = null; HidePrompt(); return;
        }

        // ★各候補の可否をログ表示（どこで落ちるか可視化）
        if (debugLog) {
            foreach (var x in candidates) {
                var tr = GetTr(x);
                bool can   = x.CanInteract(player);
                bool front = IsInFront(tr.position);
                bool los   = HasLineOfSight(tr.position, tr);
                Debug.Log($"[InteractDbg] {tr.name} can:{can} front:{front} los:{los}", tr);
            }
        }

        // 生きてる・視野内・視線OK・CanInteract=true を優先度＆距離で選択
        current = candidates
            .Where(x => x != null && x.CanInteract(player))
            .Where(x => IsInFront(GetPos(x)))
            .Where(x => HasLineOfSight(GetPos(x)))
            .OrderByDescending(x => x.Priority) // 先に優先度
            .ThenBy(x => (GetPos(x) - transform.position).sqrMagnitude)
            .FirstOrDefault();

        if (current != null)
        {
            ShowPrompt(current.GetPrompt(player));
        }
        else HidePrompt();
    }

    Vector3 GetPos(IInteractable it)
    {
        return (it as MonoBehaviour).transform.position;
    }

    bool IsInFront(Vector3 worldPos)
    {
        Vector3 to = (worldPos - transform.position).normalized;
        float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.forward, to), -1f, 1f)) * Mathf.Rad2Deg;
        return ang <= maxPickAngle;
    }

    bool HasLineOfSight(Vector3 worldPos, Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = worldPos - origin;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir /= dist;

        // 壁/地形だけにRay（Enemy/Player/Triggersはマスクから外す）
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, losMask, QueryTriggerInteraction.Ignore))
        {
            // もし当たっても、相手自身/子ならOK扱い
            if (target != null && (hit.transform == target || hit.transform.IsChildOf(target)))
                return true;
            return false; // 壁で遮られた
        }
        return true; // 何にも当たらない＝見えてる
    }

    // （互換用のオーバーロードを置いておくと他所の呼び出しも死なない）
    bool HasLineOfSight(Vector3 worldPos) => HasLineOfSight(worldPos, null);
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

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("colはしたよ");
        foreach (var it in other.GetComponents<IInteractable>())
            candidates.Add(it);
        foreach (var it in other.GetComponentsInParent<IInteractable>())
            candidates.Add(it);
    }

    void OnTriggerExit(Collider other)
    {
        foreach (var it in other.GetComponents<IInteractable>())
            candidates.Remove(it);
        foreach (var it in other.GetComponentsInParent<IInteractable>())
            candidates.Remove(it);
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
