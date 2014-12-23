using UnityEngine;
using System.Collections;

public class FollowTransform : MonoBehaviour
{
	private Vector3 initialPosition;
	public Transform target = null;
	public Vector3 factor = Vector3.zero;

	// Use this for initialization
	void Start()
	{
		Vector3 distance = target.position - transform.position;
		initialPosition = transform.position + Vector3.Project( distance, transform.forward );
	}
	
	// Update is called once per frame
	void Update()
	{
		transform.LookAt( Vector3.Scale( initialPosition, Vector3.one - factor ) + Vector3.Scale( target.position, factor ) );
		/*transform.LookAt( new Vector3( initialPosition.x + ( target.position.x - initialPosition.x ) * factor.x,
										initialPosition.y + ( target.position.y - initialPosition.y ) * factor.y,
										initialPosition.z + ( target.position.z - initialPosition.z ) * factor.z ) );*/
	}
}

