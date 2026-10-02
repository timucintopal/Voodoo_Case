using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// One entry of the skin list: shows its own part of the preview atlas.
public class SkinCell : MonoBehaviour
{
    public RawImage     m_Preview;
    public Button       m_Button;
    public GameObject   m_SelectedFrame;
    public float        m_PunchScale = 0.1f;
    public float        m_PunchDuration = 0.25f;

    private Action<SkinCell> m_OnClick;

    void Awake()
    {
        m_Button.onClick.AddListener(OnClick);
        SetSelected(false);
    }

    public void Init(Action<SkinCell> _OnClick)
    {
        m_OnClick = _OnClick;
    }

    public void SetPreview(Texture _Texture, Rect _UVRect)
    {
        m_Preview.texture = _Texture;
        m_Preview.uvRect = _UVRect;
    }

    public void SetSelected(bool _Selected)
    {
        m_SelectedFrame.SetActive(_Selected);
    }

    public void Punch()
    {
        transform.DOKill(true);
        transform.DOPunchScale(Vector3.one * m_PunchScale, m_PunchDuration, 0, 0);
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnClick()
    {
        if (m_OnClick != null)
            m_OnClick.Invoke(this);
    }
}
