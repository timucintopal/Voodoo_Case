using UnityEngine;

// Shared by the preview atlas and the selected skin podium: spawns a light brush model and makes it fit a given spot.
public static class SkinPreviewBuilder
{
    public static GameObject Create(SkinData _Skin, Transform _Parent, Vector3 _Center, Vector3 _Euler, float _Size)
    {
        GameObject prefab = _Skin.Brush.m_PreviewPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("No preview prefab on brush " + _Skin.Brush.name);
            return null;
        }

        GameObject preview = Object.Instantiate(prefab, _Parent);
        preview.transform.localRotation = Quaternion.Euler(_Euler);
        SetLayerRecursively(preview.transform, _Parent.gameObject.layer);
        Fit(preview.transform, _Center, _Size);

        BrushMenu brushMenu = preview.GetComponent<BrushMenu>();
        if (brushMenu != null)
            brushMenu.SetNewColor(_Skin.Color.m_Colors[0]);

        return preview;
    }

    // Scales and centers the target from its renderers, so any model fits whatever its pivot or size.
    // Renderer bounds are only reliable on active objects.
    public static void Fit(Transform _Target, Vector3 _Center, float _Size)
    {
        Bounds bounds;
        if (TryGetBounds(_Target, out bounds) == false)
            return;

        // The brush spins around its vertical axis: its height stays the same, its width sweeps the XZ diagonal
        Vector3 extents = bounds.extents;
        float halfWidth = new Vector2(extents.x, extents.z).magnitude;
        float halfSize = Mathf.Max(extents.y, halfWidth);
        if (halfSize <= Mathf.Epsilon)
            return;

        _Target.localScale *= (_Size * 0.5f) / halfSize;

        TryGetBounds(_Target, out bounds);
        _Target.position += _Center - bounds.center;
    }

    private static bool TryGetBounds(Transform _Target, out Bounds _Bounds)
    {
        Renderer[] renderers = _Target.GetComponentsInChildren<Renderer>();
        _Bounds = new Bounds();

        if (renderers.Length == 0)
            return false;

        _Bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
            _Bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private static void SetLayerRecursively(Transform _Transform, int _Layer)
    {
        _Transform.gameObject.layer = _Layer;

        for (int i = 0; i < _Transform.childCount; ++i)
            SetLayerRecursively(_Transform.GetChild(i), _Layer);
    }
}
