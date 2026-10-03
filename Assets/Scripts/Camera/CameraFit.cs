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
    /// Authored framing aspect (16:9 default). The camera keeps the design
    /// WIDTH fully visible at any screen aspect and adapts the vertical
    /// (fit-width): orthoSize = baseOrthoSize * designAspect / aspect.
    /// </summary>
    [Tooltip("Aspecto de diseño esperado, p.ej. 16/9. La cámara mantiene el ancho visible y adapta el alto.")]
    [SerializeField] private float designAspect = 16f / 9f;

    /// <summary>
    /// Orthographic size at the design aspect (the scene is authored with
    /// ortho 5 → 10 world units tall at 16:9).
    /// </summary>
    [Tooltip("Ortho size en el aspect de diseño (escena autorada con ortho 5).")]
    [SerializeField] private float baseOrthoSize = 5f;

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

        float target = baseOrthoSize * designAspect / aspect;
        if (!Mathf.Approximately(_cam.orthographicSize, target))
            _cam.orthographicSize = target;
    }
}