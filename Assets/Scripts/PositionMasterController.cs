using UnityEngine;

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(BoxCollider) ) ]
public class PositionMasterController : Controller 
{
	void FixedUpdate()
	{
		float inputDelay = GameManager.GetConnection().GetNetworkDelay( (byte) elementID );

		Vector3 masterPosition = new Vector3( GameManager.GetConnection().GetRemoteValue( (byte) elementID, X, POSITION ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION ) );

		Vector3 masterVelocity = new Vector3( GameManager.GetConnection().GetRemoteValue( (byte) elementID, X, VELOCITY ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY ) );

		Vector3 masterAcceleration = new Vector3( GameManager.GetConnection().GetRemoteValue( (byte) elementID, X, ACCELERATION ),
												  0.0f, GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, ACCELERATION ) );

		masterPosition += masterVelocity * inputDelay + masterAcceleration * inputDelay * inputDelay / 2.0f;

		//Debug.Log( "element " + elementID.ToString() + " position: " + masterPosition.ToString() + " - velocity: " + masterVelocity.ToString() );

		if( masterPosition.magnitude > rangeLimits.magnitude )
		{
			body.MovePosition( masterPosition.normalized * rangeLimits.magnitude );
			body.velocity = Vector3.zero;
		}
		else
		{
			body.MovePosition( masterPosition );
			body.velocity = masterVelocity;
		}

		// Send locally controlled object position over network
		GameManager.GetConnection().SetLocalValue( (byte) elementID, X, POSITION, body.position.x );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, X, VELOCITY, body.velocity.x );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
}