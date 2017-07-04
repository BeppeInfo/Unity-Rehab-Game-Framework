using UnityEngine;

public class ForceMasterController : Controller
{
	float inputPosition = 0.0f, outputForce = 0.0f;

	const float TUNNING_PARAMETER = 1.0f; // lambda = 1.0;
	const float INPUT_DAMPING = 1.0f; // R

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm with impedance matching between dynamical system and b (wave impedance) parameter
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		float outputDamping = TUNNING_PARAMETER * body.mass; // D = lamda * m
		float proportionalGain = TUNNING_PARAMETER * TUNNING_PARAMETER * body.mass; // K = lamda^2 * m
		float derivativeGain = TUNNING_PARAMETER * body.mass; // B = lamda * m

		float waveImpedance = INPUT_DAMPING + TUNNING_PARAMETER * body.mass; // b = R + lamda * m

		// Receive v_m (delayed v_s) and V_m (delayed V_s)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Lock local body if no messages are being received
		if( inputWaveVariable == 0.0 ) body.constraints |= RigidbodyConstraints.FreezePositionZ;
		else body.constraints &= (~RigidbodyConstraints.FreezePositionZ);

		// x_dot_m = (sqrt(2*b) * v_m + F_m) / b
		float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable + outputForce ) / waveImpedance;
		// should be inputVelocity = inputVelocity / inertia_scaling_factor
		// x_m = (sqrt(2*b) * V_m + p_m) / b
		//inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral - outputForceIntegral ) / waveImpedance;

		inputPosition += inputVelocity * Time.fixedDeltaTime;
		float positionError = -inputPosition + body.position.z;
		float velocityError = -inputVelocity + body.velocity.z;
		outputForce = proportionalGain * positionError + derivativeGain * velocityError;
		outputForce -= INPUT_DAMPING * inputVelocity;

		float controlForce = outputForce + outputDamping * body.velocity.z;
		body.AddForce( - Vector3.forward * controlForce, ForceMode.Force );

		// u_m = sqrt(2 * b) * x_dot_m - v_m
		float outputWaveVariable = Mathf.Sqrt( 2.0f * waveImpedance ) * inputVelocity - inputWaveVariable;
		// U_m = sqrt(2 * b) * x_m - V_m
		//float outputWaveIntegral = Mathf.Sqrt( 2.0f * waveImpedance ) * inputPosition - inputWaveIntegral;

		// Send u_m and U_m
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		// Send position and velocity values directly
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
		
	public float GetOutputForce() { return outputForce; }

	public float GetPosition() { return body.position.z; }
}