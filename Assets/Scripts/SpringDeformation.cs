using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringDeformation : MonoBehaviour 
{
	public SpringJoint springJoint;

	public ForceMasterController topController, bottomController;

	private float baseSpringLength = 1.0f;
	private Vector3 baseScale = Vector3.one;

	void Start() 
	{
		baseSpringLength = topController.transform.position.z - bottomController.transform.position.z;
		baseScale = transform.localScale;

		/*if( GameManager.isMaster ) StartCoroutine( SpringUpdate() ); 
		else*/ if( ! GameManager.isMaster ) springJoint.spring = springJoint.damper = 0.0f;
	}
	
	void Update() 
	{
		transform.position = ( topController.transform.position + bottomController.transform.position ) / 2.0f;

		float springLength = topController.transform.position.z - bottomController.transform.position.z;
		springLength += ( springLength - baseSpringLength );
		transform.localScale = Vector3.Scale( baseScale, new Vector3( 1.0f, 1.0f, springLength / baseSpringLength ) );
	}

	IEnumerator SpringUpdate()
	{
		while( Application.isPlaying )
		{
			topController.SetInteractionForce( springJoint.currentForce.z );
			bottomController.SetInteractionForce( -springJoint.currentForce.z );

			yield return new WaitForFixedUpdate();
		}
	}
}
