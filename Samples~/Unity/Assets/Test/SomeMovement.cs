using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeMovement : MonoBehaviour
{

	public float m_distanceScale = 100;
	public float m_count;
	
	private Vector3 m_originalPosition;

	private void Start()
	{
		m_originalPosition = transform.position;
	}

	// Update is called once per frame
	private void Update()
    {
        m_count += Time.deltaTime;

		var cSharpSucks = m_originalPosition;
		cSharpSucks.y += Mathf.Sin(m_count) * m_distanceScale;
		transform.position = cSharpSucks;

		var cSharpSucksHard = transform.eulerAngles;
		cSharpSucksHard.y = m_count * 100;
		transform.eulerAngles = cSharpSucksHard;
    }
}
