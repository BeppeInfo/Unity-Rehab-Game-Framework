using UnityEngine;

public class ForceSlaveController : Controller
{
	protected float waveImpedance = 10.0f;

	void FixedUpdate()
	{
		float inputPosition = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 2 );
		float inputVelocity = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 3 );

		body.MovePosition( inputPosition * Vector3.forward );
		body.velocity = inputVelocity * Vector3.forward;
	}
}