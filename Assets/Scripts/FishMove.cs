using UnityEngine;
using System.Collections;

[RequireComponent( typeof(Rigidbody) )]
public class FishMove : MonoBehaviour
{
	private float smoothInterval;

	private bool isJumping = false;

	private Vector3 initialPosition;
	private Vector3 initialRotation;

	private Vector3 speed = Vector3.zero;

	private Vector3 targetPosition  = Vector3.zero;
	public Vector3 movementRange = new Vector3( 1.0f, 0.0f, 0.0f );

	public Transform fish;

	// Use this for initialization
	void Start()
	{
		Random.seed = (int) Time.realtimeSinceStartup;

		initialPosition = transform.position;
		initialRotation = fish.rotation.eulerAngles;
		targetPosition = initialPosition + new Vector3( Random.Range( -movementRange.x, movementRange.x ), 0.0f, 0.0f );
		smoothInterval = 3.0f * Mathf.Abs( initialPosition.x - targetPosition.x ) / movementRange.x;
	}
	
	// Update is called once per frame
	void Update()
	{
		if( !isJumping ) 
		{
			//Debug.Log( transform.position.ToString() + targetPosition .ToString() );
			transform.position = Vector3.SmoothDamp( transform.position, targetPosition, ref speed, smoothInterval );

			if( Mathf.Abs( transform.position.x - targetPosition.x ) / movementRange.x < 0.1f )
			{
				targetPosition = new Vector3( initialPosition.x + Random.Range( -movementRange.x, movementRange.x ), 
												initialPosition.y, transform.position.z );
				smoothInterval = 3.0f * Mathf.Abs( initialPosition.x - targetPosition.x ) / movementRange.x;
			}

			if( Random.Range( 0, 500 ) == 0 )
			{
				GetComponent<Rigidbody>().isKinematic = false;
				GetComponent<Rigidbody>().useGravity = true;
				GetComponent<Rigidbody>().AddForce( new Vector3( speed.x, 10.0f, 0.0f ), ForceMode.VelocityChange );
				isJumping = true;
			} 
		}
		else 
		{
			if( transform.position.y <= initialPosition.y - movementRange.y / 2.0f ) 
			{
				isJumping = false;
				GetComponent<Rigidbody>().useGravity = false;
				GetComponent<Rigidbody>().velocity = Vector3.zero;
				GetComponent<Rigidbody>().isKinematic = true;
				transform.position = new Vector3( transform.position.x, initialPosition.y, transform.position.z );
				fish.rotation = Quaternion.Euler( initialRotation );
			}
		}

		fish.position = transform.position;
		//fish.rotation = Quaternion.Euler( initialRotation.x, -speed.x * 10 + initialRotation.y, -rigidbody.velocity.y * 10.0f + initialRotation.z );
		fish.localRotation = Quaternion.Euler( -GetComponent<Rigidbody>().velocity.y * 8.0f, Mathf.Clamp( -speed.x * 8.0f + initialRotation.y, 90.0f, 270.0f ), 0.0f );
	}
}

