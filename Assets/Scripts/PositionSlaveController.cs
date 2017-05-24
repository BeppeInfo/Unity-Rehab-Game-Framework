using UnityEngine;

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(Collider) ) ]
public class PositionSlaveController : Controller 
{
	void FixedUpdate()
	{
		Vector3 masterPosition = new Vector3( GameManager.GetConnection().GetRemoteValue( (byte) elementID, X, POSITION ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION ) );

		Vector3 masterVelocity = new Vector3( GameManager.GetConnection().GetRemoteValue( (byte) elementID, X, VELOCITY ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY ) );

		Vector3 trackingError = masterPosition - body.position;

		//Debug.Log( "master pos: " + masterPosition.ToString() + " - vel: " + masterVelocity.ToString() + " - err: " + trackingError.ToString() );

		if( trackingError.magnitude > 0.6f * rangeLimits.magnitude ) body.MovePosition( masterPosition );
		else masterVelocity += trackingError;

		if( masterPosition != Vector3.zero ) body.velocity = masterVelocity;

		body.angularVelocity = Quaternion.AngleAxis( 90.0f, Vector3.up ) * body.velocity / size.y / 2.0f;
	}

	public void OnEnable()
	{
		body.isKinematic = false;
		body.position = initialPosition;
		body.velocity = Vector3.zero;
	}

	public void OnDisable()
	{
		body.position = initialPosition;
		body.velocity = Vector3.zero;
	}

	public Vector3 FindImpactPoint( int layerMask )
	{
		RaycastHit hit;

		if( Physics.Raycast( body.position, body.velocity.normalized, out hit, 60.0f, layerMask ) ) return hit.point;

		return body.position + body.velocity.normalized * 60.0f;
	}
}