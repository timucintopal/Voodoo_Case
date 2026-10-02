using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushMenu : MonoBehaviour {

    private static readonly int s_ColorID = Shader.PropertyToID("_Color");

    public List<GameObject> m_BrushParts;

    private MaterialPropertyBlock m_PropertyBlock;

    public void SetNewColor(Color _Color)
    {
        if (m_PropertyBlock == null)
            m_PropertyBlock = new MaterialPropertyBlock();

        // A property block, on the first material as before, avoids creating a material instance per part
        for (int i = 0; i < m_BrushParts.Count; i++)
        {
            Renderer partRenderer = m_BrushParts[i].GetComponent<Renderer>();
            partRenderer.GetPropertyBlock(m_PropertyBlock, 0);
            m_PropertyBlock.SetColor(s_ColorID, _Color);
            partRenderer.SetPropertyBlock(m_PropertyBlock, 0);
        }
    }
}
