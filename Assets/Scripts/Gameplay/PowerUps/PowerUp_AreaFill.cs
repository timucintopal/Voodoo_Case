using UnityEngine;
using Zenject;

public sealed class PowerUp_AreaFill : PowerUp
{
	public float 			m_Radius = 20.0f;
	public float			m_FillDuration = 0.5f;
	public ParticleSystem 	m_FillEffect;
	public float 			m_FillEffectDuration = 2.0f;
	private ITerrainService	m_TerrainService;

    [Inject]
    public void ChildConstruct(ITerrainService terrainService)
    {
	    m_TerrainService = terrainService;
    }

	public override void OnPlayerTouched (Player _Player)
	{
		UnregisterMap();
        m_Model.enabled = false;
        m_ParticleSystem.Play(true);
		m_IdleParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        m_Shadow.SetActive(false);

		Vector3 position = m_TerrainService.GetRandomPosition(m_Radius);

		if (m_FillEffect != null)
		{
			ParticleSystem effect = Instantiate(m_FillEffect, position + Vector3.up * m_Transform.position.y, Quaternion.identity);
			effect.SetColor(_Player.m_Color);
			effect.Play(true);
			Destroy(effect.gameObject, m_FillEffectDuration);
		}

		if (_Player is HumanPlayer)
		{
			MessageView.Instance.QueueMessage("Area filled!");
			ScreenShaker.Instance.Shake(0.3f, 0.2f);
		}

		StartCoroutine(m_TerrainService.FillCoroutine(_Player, position, m_Radius, m_FillDuration, SelfDestroy));
	}

    private void SelfDestroy()
    {
        Destroy(gameObject);
    }
}
