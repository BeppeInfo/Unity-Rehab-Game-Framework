using UnityEngine;

public class ForceSlaveController : Controller
{
	void FixedUpdate()
	{
		float inputPosition = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION );
		float inputVelocity = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY );

		body.MovePosition( inputPosition * Vector3.forward );
		body.velocity = inputVelocity * Vector3.forward;
	}
}