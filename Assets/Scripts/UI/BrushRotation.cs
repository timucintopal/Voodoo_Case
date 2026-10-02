using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushRotation : MonoBehaviour
{
	public float		m_Speed = 90f;

	// Cache
	private Transform m_Transform;

	void Awake()
	{
		// Cache
		m_Transform = transform;
	}

	void Update ()
	{
		m_Transform.RotateAround(m_Transform.position, m_Transform.up, Time.deltaTime * m_Speed);
	}
}
