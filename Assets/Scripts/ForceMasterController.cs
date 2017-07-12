using UnityEngine;

public class ForceMasterController : Controller
{
	private float remoteForce = 0.0f, remoteForceIntegral = 0.0f;

	private float waveImpedance = 1.0f, inputWaveVariable = 0.0f, inputWaveIntegral = 0.0f;

	public Transform reference;

	void Start()
	{
		initialPosition = body.position;
		body.constraints |= RigidbodyConstraints.FreezePositionZ;
	}

	// Wave variables control algorithm with wave filtering
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		// Get total delay between received wave values
		float networkDelay = GameManager.GetConnection().GetNetworkDelay( (byte) elementID );
		float waveDelay = networkDelay + Time.fixedDeltaTime;

		// Receive delayed v_s and delayed V_s
		float delayedWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float delayedWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Wave filtering:
		// Calculate wave derivative: vdot_m = ( v_s_old - v_m ) / delay
		float inputWaveDerivative = ( delayedWaveVariable - inputWaveVariable ) / waveDelay;
		// Calculate next wave value: v_m = v_m + vdot_m * dt
		inputWaveVariable += inputWaveDerivative * Time.fixedDeltaTime;

		// Extract remote force from wave variable: F_m = b * xdot_m - sqrt( 2 * b ) * v_m
		remoteForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2 * waveImpedance ) * inputWaveVariable;
		//remoteForceIntegral = waveImpedance * body.position.z - Mathf.Sqrt( 2 * waveImpedance ) * inputWaveIntegral;

		// Apply resulting force to rigid body
		body.AddForce( -Vector3.forward * remoteForce, ForceMode.Force );

		// Lock local body if no messages are being received
		//if( inputWaveVariable == 0.0 ) body.constraints |= RigidbodyConstraints.FreezePositionZ;
		//else body.constraints &= (~RigidbodyConstraints.FreezePositionZ);
		if( inputWaveVariable != 0.0f ) body.constraints &= (~RigidbodyConstraints.FreezePositionZ);

		// u_m = ( b * x_dot_m + F_m ) / sqrt(2 * b)
		float outputWaveVariable = ( waveImpedance * body.velocity.z + remoteForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		// U_m = ( b * x_m + p_m ) / sqrt(2 * b)
		//float outputWaveIntegral = ( waveImpedance * body.position.z - remoteForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

		// Send u_m and U_m
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		// Send position and velocity values directly
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
		
	public float GetInputForce() { return remoteForce; }

	public float GetPosition() { return body.position.z; }
}