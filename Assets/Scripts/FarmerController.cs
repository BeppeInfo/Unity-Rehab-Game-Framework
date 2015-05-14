using UnityEngine;
using System.Collections;

[RequireComponent( typeof(Animation) )]
[RequireComponent( typeof(Rigidbody) )]
public class FarmerController : MonoBehaviour {

	// Use this for initialization
	void Start() 
	{
	}
	
	// Update is called once per frame
	void Update() 
	{
		float speed = -Vector3.Dot( GetComponent<Rigidbody>().velocity, transform.forward );

		if( speed > 1.0f )
			GetComponent<Animation>().CrossFade( "WalkRight" );
		else if( speed < -1.0f )
			GetComponent<Animation>().CrossFade( "WalkLeft" );
		else
			GetComponent<Animation>().CrossFade( "Idle" );
	}
}
