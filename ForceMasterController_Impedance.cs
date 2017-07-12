using UnityEngine;

public class ForceMasterController : Controller
{
	const float TUNNING_PARAMETER = 0.5f; // lambda;
	const float INPUT_DAMPING = 0.1f; // R

	float inputPosition = 0.0f, outputForce = 0.0f, outputForceIntegral = 0.0f, controlForce = 0.0f;

	float lastVelocityError = 0.0f;

	public Transform reference;

	void Start()
	{
		initialPosition = body.position;
		body.constraints |= RigidbodyConstraints.FreezePositionZ;
	}

	// Wave variables control algorithm with impedance matching between dynamical system and b (wave impedance) parameter
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		float outputDamping = TUNNING_PARAMETER * body.mass; // D = lamda * m
		float controlStiffness = TUNNING_PARAMETER * TUNNING_PARAMETER * body.mass; // K = lamda^2 * m
		float controlDamping = TUNNING_PARAMETER * body.mass; // B = lamda * m

		float waveImpedance = INPUT_DAMPING + TUNNING_PARAMETER * body.mass; // b = R + lamda * m

		// Dicrete controller gains ( s = 2(z-1)/(T*(z+1)) )
		float presentGain = controlDamping + controlStiffness * Time.fixedDeltaTime / 2;
		float pastGain = -controlDamping + controlStiffness * Time.fixedDeltaTime / 2;

		// Receive v_m (delayed v_s) and V_m (delayed V_s)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Lock local body if no messages are being received
		//if( inputWaveVariable == 0.0 ) body.constraints |= RigidbodyConstraints.FreezePositionZ;
		//else body.constraints &= (~RigidbodyConstraints.FreezePositionZ);
		if( inputWaveVariable != 0.0f ) body.constraints &= (~RigidbodyConstraints.FreezePositionZ);

		// x_dot_m = (sqrt(2*b) * v_m + F_m) / b
		float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable + outputForce ) / waveImpedance;
		// should be inputVelocity = inputVelocity / inertia_scaling_factor
		// x_m = (sqrt(2*b) * V_m + p_m) / b
		//inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral + outputForceIntegral ) / waveImpedance;

		if( reference != null ) reference.position += Vector3.forward * inputVelocity * Time.fixedDeltaTime;

		//float positionError = inputPosition - ( body.position.z - initialPosition.z );
		float velocityError = -inputVelocity + body.velocity.z;
		controlForce += ( presentGain * velocityError + pastGain * lastVelocityError );
		//controlForce = /*controlStiffness * positionError +*/ controlDamping * velocityError;
		Debug.Log( string.Format( "Velocity error: {0} - {1} = {2}, Control: {3}", inputVelocity, body.velocity.z, velocityError, controlForce ) );
		lastVelocityError = velocityError;
		body.AddForce( -Vector3.forward * ( controlForce + outputDamping * body.velocity.z ), ForceMode.Force );

		outputForce = controlForce - INPUT_DAMPING * inputVelocity;

		// Integrate force for moment (p_s) calculation
		//outputForceIntegral += outputForce * Time.fixedDeltaTime;

		// u_m = ( b * x_dot_m + F_m ) / sqrt(2 * b)
		float outputWaveVariable = ( waveImpedance * inputVelocity + outputForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		// U_m = ( b * x_m + p_m ) / sqrt(2 * b)
		//float outputWaveIntegral = ( waveImpedance * inputPosition - outputForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

		// Send u_m and U_m
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		// Send position and velocity values directly
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
		
	public float GetOutputForce() { return outputForce; }

	public float GetInputForce() { return controlForce; }

	public float GetPosition() { return body.position.z; }
}