using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

public class SkinSelectionView : View<SkinSelectionView>
{
    public SkinPreviewAtlas     m_Atlas;
    public SkinCell             m_CellPrefab;
    public GridLayoutGroup      m_Grid;
    public List<SkinData>       m_DisplayedSkins; // In display order, which is not the save order
    public float                m_DragThresholdInches = 0.08f; // Finger travel under which a touch on the list is a tap, not a scroll

    [Header("Selected skin")]
    public GameObject           m_Stage; // 3D objects: not faded by the canvas group, so toggled by hand
    public Transform            m_HeroParent;
    public float                m_HeroSize = 420f;
    public float                m_HeroPunchScale = 0.2f;
    public float                m_HeroPunchDuration = 0.3f;

    private IStatsService       m_StatsService;

    private List<SkinData>      m_Skins = new List<SkinData>();
    private List<SkinCell>      m_Cells = new List<SkinCell>();
    private GameObject          m_Hero;
    private int                 m_SelectedIndex = -1;
    private bool                m_IsBuilt;
    private bool                m_IsAtlasVisible;
    private int                 m_DefaultDragThreshold;

    [Inject]
    public void Construct(IStatsService statsService)
    {
        m_StatsService = statsService;
    }

    protected override void Awake()
    {
        base.Awake();

        m_Stage.SetActive(false);
    }

    public void Open()
    {
        if (GameService.m_IsSkinSelectionEnabled == false || m_Visible)
            return;

        if (m_IsBuilt == false)
            Build();

        FitCellSize();
        ShowAtlas();

        m_Stage.SetActive(true);
        Select(GetFavoriteIndex());
        SetDragThreshold(true);

        MainMenuView.Instance.Transition(false);
        Transition(true);
    }

    public void Close()
    {
        if (m_Visible == false)
            return;

        m_HeroParent.DOKill(true);
        m_Stage.SetActive(false);
        SetDragThreshold(false);

        Transition(false);
        MainMenuView.Instance.Transition(true);
    }

    protected override void Update()
    {
        base.Update();

        // The atlas is kept alive until the fade out is over, then costs nothing
        if (m_IsAtlasVisible && m_Visible == false && m_Group.alpha <= 0f)
            HideAtlas();
    }

    // The scene threshold is a few pixels whatever the screen density: on a phone nearly every tap on a cell
    // would start a scroll and be lost. It is only changed while the list is shown.
    private void SetDragThreshold(bool _ForList)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        if (_ForList)
        {
            m_DefaultDragThreshold = eventSystem.pixelDragThreshold;
            eventSystem.pixelDragThreshold = Mathf.Max(m_DefaultDragThreshold, Mathf.RoundToInt(Screen.dpi * m_DragThresholdInches));
        }
        else
        {
            eventSystem.pixelDragThreshold = m_DefaultDragThreshold;
        }
    }

    #region List

    private void Build()
    {
        // Only the skins the game currently knows can be selected
        for (int i = 0; i < m_DisplayedSkins.Count; ++i)
        {
            if (GameService.m_Skins.Contains(m_DisplayedSkins[i]))
                m_Skins.Add(m_DisplayedSkins[i]);
        }

        m_Atlas.Build(m_Skins);

        m_Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        m_Grid.constraintCount = m_Atlas.m_Columns;

        for (int i = 0; i < m_Skins.Count; ++i)
        {
            SkinCell cell = Instantiate(m_CellPrefab, m_Grid.transform);
            cell.Init(OnCellClicked);
            m_Cells.Add(cell);
        }

        m_IsBuilt = true;
    }

    private void ShowAtlas()
    {
        // Canvas units to screen pixels, so the atlas matches the device resolution
        float scaleFactor = m_Grid.GetComponentInParent<Canvas>().scaleFactor;
        m_Atlas.Show(Mathf.CeilToInt(m_Grid.cellSize.x * scaleFactor));
        m_IsAtlasVisible = true;

        // The texture is recreated each time the atlas is shown
        for (int i = 0; i < m_Cells.Count; ++i)
            m_Cells[i].SetPreview(m_Atlas.texture, m_Atlas.GetUVRect(i));
    }

    private void HideAtlas()
    {
        m_Atlas.Hide();
        m_IsAtlasVisible = false;
    }

    // The canvas width depends on the device aspect ratio, so the cells are sized to always fill the row
    private void FitCellSize()
    {
        int columns = m_Atlas.m_Columns;
        RectTransform gridRect = (RectTransform)m_Grid.transform;
        float width = gridRect.rect.width - m_Grid.padding.horizontal - m_Grid.spacing.x * (columns - 1);

        m_Grid.cellSize = Vector2.one * (width / columns);
    }

    #endregion

    #region Selection

    private void OnCellClicked(SkinCell _Cell)
    {
        int index = m_Cells.IndexOf(_Cell);
        if (index == m_SelectedIndex)
            return;

        SaveFavorite(m_Skins[index]);
        Select(index);
        PlaySelectFeedback(_Cell);
    }

    private void PlaySelectFeedback(SkinCell _Cell)
    {
        _Cell.Punch();
        m_HeroParent.DOPunchScale(Vector3.one * m_HeroPunchScale, m_HeroPunchDuration, 0, 0);

        MobileHapticManager.Instance.Vibrate(MobileHapticManager.E_FeedBackType.SelectionChange);
    }

    private void Select(int _Index)
    {
        if (_Index < 0 || _Index >= m_Skins.Count)
            return;

        m_SelectedIndex = _Index;

        for (int i = 0; i < m_Cells.Count; ++i)
            m_Cells[i].SetSelected(i == _Index);

        RefreshHero(m_Skins[_Index]);
    }

    private void RefreshHero(SkinData _Skin)
    {
        // The new brush is fitted from its bounds, which a running punch would distort
        m_HeroParent.DOKill(true);

        if (m_Hero != null)
            Destroy(m_Hero);

        // The size is set in canvas units
        float worldSize = m_HeroSize * m_HeroParent.lossyScale.y;
        m_Hero = SkinPreviewBuilder.Create(_Skin, m_HeroParent, m_HeroParent.position, m_Atlas.m_PreviewEuler, worldSize);
    }

    // The favorite is saved as an index in the game skins, the list only changes the order they are shown in
    private void SaveFavorite(SkinData _Skin)
    {
        int skinID = GameService.m_Skins.IndexOf(_Skin);

        GameService.m_PlayerSkinID = skinID;
        m_StatsService.FavoriteSkin = skinID;
        GameService.SetColor(GameService.ComputeCurrentPlayerColor(true, 0));
    }

    private int GetFavoriteIndex()
    {
        int favoriteSkin = Mathf.Min(m_StatsService.FavoriteSkin, GameService.m_Skins.Count - 1);
        return Mathf.Max(0, m_Skins.IndexOf(GameService.m_Skins[favoriteSkin]));
    }

    #endregion
}
