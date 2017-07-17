using UnityEngine;

public class ForceMasterController : ForceController
{
	void Start()
	{
		initialPosition = body.position;
		body.constraints |= RigidbodyConstraints.FreezePositionZ;
	}

	// Wave variables control algorithm with wave filtering
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		// Extract remote force from received wave variable: -F_m = b * xdot_m - sqrt( 2 * b ) * v_m
		// Extract remote moment from received wave integral: -p_m = b * x_m - sqrt( 2 * b ) * V_m
		ProcessInputWave();

		// Apply resulting force F_m to rigid body
		body.AddForce( Vector3.forward * remoteForce, ForceMode.Force );

		// Lock local body if no messages are being received
		//if( inputWaveVariable == 0.0 ) body.constraints |= RigidbodyConstraints.FreezePositionZ;
		//else body.constraints &= (~RigidbodyConstraints.FreezePositionZ);
		if( inputWaveVariable != 0.0f ) body.constraints &= (~RigidbodyConstraints.FreezePositionZ);

		// Encode and send output wave variable (velocity data): u_m = ( b * xdot_m + (-F_m) ) / sqrt( 2 * b )
		// Encode and send output wave integral (position data): U_m = ( b * x_m + (-p_m) ) / sqrt( 2 * b )
		ProcessOutputWave();

		// Send position and velocity values directly
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );

		float newWaveImpedance = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, IMPEDANCE );
		if( newWaveImpedance > 0.0f ) waveImpedance = newWaveImpedance;
		float newFilterStrength = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, FILTER );
		if( newFilterStrength > 0.0f ) filterStrength = newFilterStrength;
	}
}