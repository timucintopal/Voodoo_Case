using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameMode", menuName = "Data/GameMode", order = 1)]
public class GameModeData : ScriptableObject
{
	public bool 	m_IsEnabled = true;
	public string 	m_DisplayName;
	public string 	m_SaveKey;
	public List<BoosterLevelData> 	m_Levels;
	public int 						m_LoopLevelCount = 3;

	public BoosterLevelData GetLevelData(int _Level)
	{
		if (m_Levels == null || m_Levels.Count == 0)
			return null;

		int index = Mathf.Max(0, _Level - 1);
		if (index < m_Levels.Count)
			return m_Levels[index];

		int loopCount = Mathf.Clamp(m_LoopLevelCount, 1, m_Levels.Count);
		int loopStart = m_Levels.Count - loopCount;
		return m_Levels[loopStart + (index - m_Levels.Count) % loopCount];
	}
}
