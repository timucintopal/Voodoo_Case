public interface IGameModeService
{
    GameModeData m_CurrentMode { get; }
    void SetMode(GameModeData _Mode);
    void ClearMode();
    int GetLevel(GameModeData _Mode);
    BoosterLevelData GetCurrentLevelData();
    void OnLevelFinished(int _Rank);
}
