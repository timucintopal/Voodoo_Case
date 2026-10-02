using System.Collections.Generic;
using UnityEngine;

// Renders every skin preview with a single camera into one RenderTexture,
// so UI cells can show (and mask) the rotating 3D brushes as regular RawImages.
public class SkinPreviewAtlas : MonoBehaviour
{
    private const int       c_DepthBits = 16;

    public Camera           m_Camera;
    public Transform        m_PreviewsParent;
    public int              m_Columns = 3;
    public float            m_CellWorldSize = 4f;
    [Range(0.1f, 1f)]
    public float            m_FillPercent = 0.95f;
    public Vector3          m_PreviewEuler = new Vector3(0f, 0f, -20f);

    [Header("Quality")]
    public int              m_MaxCellPixelSize = 512;
    public int              m_AntiAliasing = 2;

    [Header("Low end devices")]
    public int              m_LowEndMemorySize = 3000; // In MB, devices below are considered low end
    [Range(0.1f, 1f)]
    public float            m_LowEndResolutionScale = 0.6f;
    public int              m_LowEndFrameInterval = 2; // The atlas is rendered once every N frames

    public RenderTexture    texture { get { return m_Texture; } }

    private RenderTexture   m_Texture;
    private int             m_Rows;
    private int             m_CellPixelSize;
    private int             m_FrameInterval = 1;
    private bool            m_IsVisible;
    private List<GameObject> m_Previews = new List<GameObject>();

    void Awake()
    {
        m_Camera.enabled = false;
        m_PreviewsParent.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        ReleaseTexture();
    }

    void LateUpdate()
    {
        // The camera is disabled on low end devices, and rendered by hand at a lower rate
        if (m_IsVisible && m_FrameInterval > 1 && Time.frameCount % m_FrameInterval == 0)
            m_Camera.Render();
    }

    public void Build(List<SkinData> _Skins)
    {
        bool wasVisible = m_IsVisible;
        Hide();
        Clear();

        m_Rows = Mathf.CeilToInt(_Skins.Count / (float)m_Columns);
        m_Camera.orthographicSize = m_Rows * m_CellWorldSize * 0.5f;

        // Renderer bounds are only reliable on active objects
        m_PreviewsParent.gameObject.SetActive(true);

        for (int i = 0; i < _Skins.Count; ++i)
            m_Previews.Add(CreatePreview(_Skins[i], i));

        m_PreviewsParent.gameObject.SetActive(false);

        if (wasVisible)
            Show(m_CellPixelSize);
    }

    // _CellPixelSize is the size a cell is displayed at on screen: rendering more would be wasted
    public void Show(int _CellPixelSize)
    {
        bool isLowEnd = SystemInfo.systemMemorySize < m_LowEndMemorySize;

        m_CellPixelSize = Mathf.Min(_CellPixelSize, m_MaxCellPixelSize);
        m_FrameInterval = isLowEnd ? Mathf.Max(1, m_LowEndFrameInterval) : 1;

        ReleaseTexture();

        int pixelSize = Mathf.CeilToInt(m_CellPixelSize * (isLowEnd ? m_LowEndResolutionScale : 1f));
        m_Texture = new RenderTexture(m_Columns * pixelSize, m_Rows * pixelSize, c_DepthBits, RenderTextureFormat.ARGB32);
        m_Texture.name = "SkinPreviewAtlas";
        m_Texture.antiAliasing = isLowEnd ? 1 : Mathf.Max(1, m_AntiAliasing);

        m_Camera.targetTexture = m_Texture;
        m_Camera.allowMSAA = isLowEnd == false;
        m_Camera.enabled = m_FrameInterval == 1;
        m_PreviewsParent.gameObject.SetActive(true);
        m_IsVisible = true;

        // Avoids an empty texture until the first manual render
        if (m_Camera.enabled == false)
            m_Camera.Render();
    }

    public void Hide()
    {
        m_IsVisible = false;
        m_Camera.enabled = false;
        m_PreviewsParent.gameObject.SetActive(false);
        ReleaseTexture();
    }

    public Rect GetUVRect(int _Index)
    {
        int column = _Index % m_Columns;
        int row = _Index / m_Columns;
        float width = 1f / m_Columns;
        float height = 1f / m_Rows;

        // UVs start from the bottom, cells from the top
        return new Rect(column * width, 1f - (row + 1) * height, width, height);
    }

    private GameObject CreatePreview(SkinData _Skin, int _Index)
    {
        return SkinPreviewBuilder.Create(_Skin, m_PreviewsParent, GetCellCenter(_Index), m_PreviewEuler, m_CellWorldSize * m_FillPercent);
    }

    private Vector3 GetCellCenter(int _Index)
    {
        int column = _Index % m_Columns;
        int row = _Index / m_Columns;
        float x = (column - (m_Columns - 1) * 0.5f) * m_CellWorldSize;
        float y = ((m_Rows - 1) * 0.5f - row) * m_CellWorldSize;

        return m_PreviewsParent.TransformPoint(new Vector3(x, y, 0f));
    }

    private void Clear()
    {
        for (int i = 0; i < m_Previews.Count; ++i)
        {
            if (m_Previews[i] != null)
                Destroy(m_Previews[i]);
        }

        m_Previews.Clear();
    }

    private void ReleaseTexture()
    {
        if (m_Texture == null)
            return;

        m_Camera.targetTexture = null;
        m_Texture.Release();
        Destroy(m_Texture);
        m_Texture = null;
    }
}
