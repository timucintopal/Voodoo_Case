using UnityEngine;

public class GameModeService : IGameModeService
{
    public GameModeData m_CurrentMode { get; private set; }

    public void SetMode(GameModeData _Mode)
    {
        m_CurrentMode = _Mode;
    }

    public void ClearMode()
    {
        m_CurrentMode = null;
    }

    public int GetLevel(GameModeData _Mode)
    {
        return (PlayerPrefs.GetInt(_Mode.m_SaveKey + Constants.c_ModeLevelSave, 1));
    }

    public BoosterLevelData GetCurrentLevelData()
    {
        if (m_CurrentMode == null)
            return null;

        return m_CurrentMode.GetLevelData(GetLevel(m_CurrentMode));
    }

    public void OnLevelFinished(int _Rank)
    {
        if (m_CurrentMode == null || _Rank != 0)
            return;

        PlayerPrefs.SetInt(m_CurrentMode.m_SaveKey + Constants.c_ModeLevelSave, GetLevel(m_CurrentMode) + 1);
    }
}
