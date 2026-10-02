using UnityEngine;
using TMPro;
using Zenject;

public class GameModeButton : MonoBehaviour
{
	public GameModeData 	m_Mode;
	public TMP_Text 		m_LevelText;

	private IGameModeService 	m_GameModeService;

	[Inject]
	public void Construct(IGameModeService gameModeService)
	{
		m_GameModeService = gameModeService;
	}

	void Awake()
	{
		gameObject.SetActive(m_Mode != null && m_Mode.m_IsEnabled);
	}

	void Start()
	{
		if (m_LevelText != null)
			m_LevelText.text = "LEVEL " + m_GameModeService.GetLevel(m_Mode);
	}

	public void OnClick()
	{
		MainMenuView.Instance.OnPlayModeButton(m_Mode);
	}
}
