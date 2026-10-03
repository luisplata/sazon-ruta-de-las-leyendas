using UnityEngine;

/// <summary>
/// Fit-width ortho camera (ported from WaifuCafe's V2 CameraFit): keeps the
/// authored 16:9 design WIDTH fully visible at any screen aspect; the vertical
/// (orthographic size) adapts instead of letterboxing/pillarboxing.
///
/// Fixes the "weird screen sides" bug: with CameraViewportFitter the camera
/// pillarboxed on wide screens and the empty sides glitched without an official
/// background. Fit-width fills the whole screen — no bars, no side artifacts.
/// Trade-off: on wider aspects the top/bottom world crops (characters are
/// centered at y≈0 so gameplay stays visible).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    /// <summary>
    /// Design width: visible world width at 16:9 with ortho size 5 (10 units
    /// tall → 10 * 16/9 ≈ 17.78). Tune if the scene's authored width differs.
    /// </summary>
    [SerializeField] private float designWidth = 17.78f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null || !_cam.orthographic) return;

        float aspect = _cam.aspect;
        if (aspect <= 0.01f) return;

        float target = (designWidth / 2f) / aspect;
        if (!Mathf.Approximately(_cam.orthographicSize, target))
            _cam.orthographicSize = target;
    }
}