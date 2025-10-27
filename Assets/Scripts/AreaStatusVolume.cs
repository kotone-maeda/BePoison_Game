using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaStatusZoneSimpleFixedTick : MonoBehaviour
{
    [Header("Zone Parameters")]
    [Tooltip("このゾーンの“毒の強さ”。プレイヤーの毒耐性がこれ以上なら腹痛ダメージ無効。")]
    public int zonePoison = 50;

    [System.Serializable]
    public class StomachacheEntry
    {
        [Tooltip("1ティックあたり腹痛増加量（合計されます）")]
        [Min(0f)] public float stomachachePerTick = 5f;
    }

    [Tooltip("腹痛寄与のリスト（全部合算）")]
    public List<StomachacheEntry> stomachacheContributions = new() {
        new StomachacheEntry(){ stomachachePerTick = 5f }
    };

    [Header("Tick")]
    [Min(0.05f)] public float tickInterval = 1.0f;
    public bool active = true;

    [Header("Debug")]
    public bool debugLog = false;

    private readonly HashSet<PlayerController> _inside = new();
    private readonly Dictionary<PlayerController, float> _fracAccum = new();
    private Coroutine _tickLoop;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 必須
    }

    void OnEnable()
    {
        // 念のためコルーチン停止→再始動
        StopTick();
        TryStartTick();
    }

    void OnDisable()
    {
        StopTick();
        _inside.Clear();
        _fracAccum.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController>(); // ← 親を辿る
        if (pc != null)
        {
            _inside.Add(pc);
            if (!_fracAccum.ContainsKey(pc)) _fracAccum[pc] = 0f;
            if (debugLog) Debug.Log($"[Zone] Enter: {pc.name}, poisonRes={pc.poisonRes}, zonePoison={zonePoison}", this);
            TryStartTick();
        }
    }

    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            _inside.Remove(pc);
            _fracAccum.Remove(pc);
            if (debugLog) Debug.Log($"[Zone] Exit : {pc.name}", this);
            if (_inside.Count == 0) StopTick();
        }
    }

    void TryStartTick()
    {
        if (!active || _inside.Count == 0) return;
        if (_tickLoop == null) _tickLoop = StartCoroutine(TickLoop());
    }

    void StopTick()
    {
        if (_tickLoop != null) { StopCoroutine(_tickLoop); _tickLoop = null; }
    }

    IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (active && _inside.Count > 0)
        {
            float perTickSum = 0f;
            for (int i = 0; i < stomachacheContributions.Count; i++)
                perTickSum += Mathf.Max(0f, stomachacheContributions[i].stomachachePerTick);

            if (perTickSum > 0f)
            {
                PlayerController toRemove = null;
                foreach (var pc in _inside)
                {
                    if (pc == null) { toRemove = pc; continue; }

                    bool immune = pc.poisonRes >= zonePoison; // 境界は >= で無効
                    if (debugLog) Debug.Log($"[Zone] Tick -> {pc.name}: PR={pc.poisonRes}, ZP={zonePoison}, immune={immune}", this);
                    if (immune) continue;

                    float acc = _fracAccum.TryGetValue(pc, out var f) ? f : 0f;
                    acc += perTickSum;
                    int whole = (int)acc;    // 切り捨てで整数化
                    acc -= whole;
                    _fracAccum[pc] = acc;

                    if (whole > 0)
                    {
                        if (debugLog) Debug.Log($"[Zone] AddStomachache {whole} to {pc.name}", this);
                        pc.AddStomachache(whole);
                    }
                }
                if (toRemove != null) _inside.Remove(toRemove);
            }

            yield return wait;
        }
        _tickLoop = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.25f);
        var box = GetComponent<BoxCollider>();
        if (box && box.isTrigger) Gizmos.DrawCube(box.center, box.size);
    }
#endif
}
