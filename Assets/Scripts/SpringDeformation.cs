using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringDeformation : MonoBehaviour 
{
	const float INTERACTION_STIFFNESS = 5.0f;

	public ForceMasterController[] endPoints = new ForceMasterController[ 2 ];

	private float baseSpringLength = 1.0f;
	private Vector3 baseScale = Vector3.one;

	void Start() 
	{
		baseSpringLength = endPoints[ 0 ].transform.position.z - endPoints[ 1 ].transform.position.z;
		baseScale = transform.localScale;
	}
	
	void Update() 
	{
		transform.position = ( endPoints[ 0 ].transform.position + endPoints[ 1 ].transform.position ) / 2.0f;

		float springLength = endPoints[ 0 ].transform.position.z - endPoints[ 1 ].transform.position.z;
		transform.localScale = Vector3.Scale( baseScale, new Vector3( 1.0f, 1.0f, springLength / baseSpringLength ) );

		float interactionForce = -INTERACTION_STIFFNESS * ( springLength - baseSpringLength );
		if( baseSpringLength * springLength < 0.0f ) interactionForce *= 10.0f;
		endPoints[ 0 ].SetInteractionForce( interactionForce );
		endPoints[ 1 ].SetInteractionForce( -interactionForce );
	}
}
