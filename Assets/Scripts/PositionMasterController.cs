using UnityEngine;

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(BoxCollider) ) ]
public class PositionMasterController : Controller 
{
	const int POSITION = 0, VELOCITY = 1, ACCELERATION = 2;

	void FixedUpdate()
	{
		float inputDelay = GameManager.GetConnection().GetNetworkDelay( elementID );

		Vector3 masterPosition = new Vector3( GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.X, POSITION ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, POSITION ) );

		Vector3 masterVelocity = new Vector3( GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.X, VELOCITY ),
											  0.0f, GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, VELOCITY ) );

		Vector3 masterAcceleration = new Vector3( GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.X, ACCELERATION ),
												  0.0f, GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, ACCELERATION ) );

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
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.X, POSITION, body.position.x );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.X, VELOCITY, body.velocity.x );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, VELOCITY, body.velocity.z );
	}
}