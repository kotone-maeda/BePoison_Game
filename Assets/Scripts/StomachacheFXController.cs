using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StomachacheFXController : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public Volume globalVolume;

    [Header("Curves")]
    public AnimationCurve vignetteCurve = AnimationCurve.Linear(0,0,1,1);   // 30→70
    public AnimationCurve blurCurve     = AnimationCurve.EaseInOut(0,0,1,1); // 70→100

    [Header("Max Values")]
    [Range(0,1)] public float maxVignette = 0.55f;
    [Range(0,1)] public float maxCA       = 0.18f;
    [Range(-100,0)] public float minSaturation = -20f;
    public Color vignetteColor = new(0.29f, 0f, 0.40f, 1f);

    // DoF(ガウシアン)の目安
    public float blurStartBase = 6f;
    public float blurEndBase   = 18f;
    public float blurStartMin  = 1.5f;
    public float blurEndMin    = 4f;
    public float blurMaxRadiusMax = 2f;

    Vignette _vig;
    ColorAdjustments _color;
    ChromaticAberration _ca;
    DepthOfField _dof;

    void Awake()
    {
        if (!globalVolume || !globalVolume.profile)
        {
            Debug.LogError("[StomachacheFX] Global Volume or Profile missing.");
            enabled = false; return;
        }

        // 必要なオーバーライド取得
        globalVolume.profile.TryGet(out _vig);
        globalVolume.profile.TryGet(out _color);
        globalVolume.profile.TryGet(out _ca);
        globalVolume.profile.TryGet(out _dof);

        if (_vig == null || _color == null || _ca == null || _dof == null)
        {
            Debug.LogError("[StomachacheFX] Add Vignette/ColorAdjustments/ChromaticAberration/DepthOfField to the Profile.");
            enabled = false; return;
        }

        // ★ Inspectorのチェックをコードで強制ON（overrideState）
        _vig.intensity.overrideState = true;
        _vig.color.overrideState     = true;
        _vig.smoothness.overrideState= true;
        _vig.rounded.overrideState   = true;

        _color.saturation.overrideState = true;

        _ca.intensity.overrideState = true;

        _dof.mode.overrideState = true;
        _dof.gaussianStart.overrideState = true;
        _dof.gaussianEnd.overrideState   = true;
        _dof.gaussianMaxRadius.overrideState = true;
        _dof.highQualitySampling.overrideState = true;

        // 初期状態を「何もしない」にリセット
        ResetToNeutral();
    }

    void ResetToNeutral()
    {
        if (_vig)
        {
            _vig.active = false;
            _vig.intensity.value   = 0f;
            _vig.smoothness.value  = 0.9f;
            _vig.rounded.value     = true;
            _vig.color.value       = vignetteColor;
        }
        if (_color)  _color.saturation.value = 0f;
        if (_ca)     _ca.intensity.value     = 0f;

        if (_dof)
        {
            _dof.mode.value = DepthOfFieldMode.Gaussian;
            _dof.highQualitySampling.value = true;
            _dof.active = false; // ★ ぼけはデフォルトOFF
            _dof.gaussianStart.value = blurStartBase;
            _dof.gaussianEnd.value   = blurEndBase;
            _dof.gaussianMaxRadius.value = 0f;
        }
    }

    void LateUpdate()
    {
        if (!player || player.maxStomachache <= 0) return;

        float pct = Mathf.Clamp01((float)player.stomachache / player.maxStomachache);

        // ===== 0～30%：何もなし =====
        if (pct < 0.30f)
        {
            // どちらもOFF/中立に
            if (_vig) { _vig.active = false; _color.saturation.value = 0f; _ca.intensity.value = 0f; }
            if (_dof) { _dof.active = false; _dof.gaussianMaxRadius.value = 0f; }
            return;
        }

        // ===== 30～70%：紫黒化のみ =====
        float tVig = Mathf.InverseLerp(0.30f, 0.70f, pct); // 0..1
        tVig = Mathf.Clamp01(vignetteCurve.Evaluate(tVig));

        if (_vig)
        {
            _vig.active = true;
            _vig.intensity.value = Mathf.Lerp(0f, maxVignette, tVig);
            _vig.color.value     = vignetteColor;
        }
        if (_color) _color.saturation.value = Mathf.Lerp(0f, minSaturation, tVig);
        if (_ca)    _ca.intensity.value     = Mathf.Lerp(0f, maxCA,       tVig * 0.8f);

        // ===== 70～100%：紫黒 + ぼけ =====
        float tBlur = Mathf.InverseLerp(0.70f, 1.00f, pct);
        tBlur = Mathf.Clamp01(blurCurve.Evaluate(tBlur));

        if (tBlur <= 0f)
        {
            // 70未満：DoFは確実にOFF
            if (_dof) { _dof.active = false; _dof.gaussianMaxRadius.value = 0f; }
            return;
        }

        if (_dof)
        {
            _dof.active = true; // ★ 70%以上でON
            _dof.mode.value = DepthOfFieldMode.Gaussian;
            _dof.gaussianStart.value     = Mathf.Lerp(blurStartBase, blurStartMin, tBlur);
            _dof.gaussianEnd.value       = Mathf.Lerp(blurEndBase,   blurEndMin,   tBlur);
            _dof.gaussianMaxRadius.value = Mathf.Lerp(0.2f, blurMaxRadiusMax, tBlur);
        }
    }
}
