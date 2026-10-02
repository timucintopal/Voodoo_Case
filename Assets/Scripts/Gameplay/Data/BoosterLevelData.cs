using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoosterLevel", menuName = "Data/BoosterLevel", order = 1)]
public class BoosterLevelData : ScriptableObject
{
	[System.Serializable]
	public class BoosterEntry
	{
		public PowerUpData 	m_PowerUp;
		public int 			m_Weight = 1;
	}

	public List<BoosterEntry> 	m_Boosters;
	public float 				m_MinSpawnRate = 1f;
	public float 				m_MaxSpawnRate = 2.5f;

	public GameObject PickBooster()
	{
		int totalWeight = 0;
		for (int i = 0; i < m_Boosters.Count; ++i)
		{
			if (m_Boosters[i].m_PowerUp != null)
				totalWeight += Mathf.Max(0, m_Boosters[i].m_Weight);
		}

		if (totalWeight <= 0)
			return null;

		int roll = Random.Range(0, totalWeight);
		for (int i = 0; i < m_Boosters.Count; ++i)
		{
			if (m_Boosters[i].m_PowerUp == null)
				continue;

			roll -= Mathf.Max(0, m_Boosters[i].m_Weight);
			if (roll < 0)
				return m_Boosters[i].m_PowerUp.m_Prefab;
		}

		return null;
	}
}
