using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	const float TUNNING_PARAMETER = 0.5f; // lambda;
	const float INPUT_DAMPING = 0.1f; // R

	private InputAxis controlAxis = null;

	private float inputVelocity = 0.0f, inputPosition = 0.0f;
	private float outputForce = 0.0f, outputForceIntegral = 0.0f, controlForce = 0.0f;

	float lastVelocityError = 0.0f;

	public Transform reference;

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm with impedance matching between dynamical system and b (wave impedance) parameter
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		float outputDamping = TUNNING_PARAMETER * body.mass; // D = lamda * m
		float controlStiffness = TUNNING_PARAMETER * TUNNING_PARAMETER * body.mass; // K = lamda^2 * m
		float controlDamping = TUNNING_PARAMETER * body.mass; // B = lamda * m

		float waveImpedance = INPUT_DAMPING + TUNNING_PARAMETER * body.mass; // b = R + lamda * m

		float presentGain = controlDamping + controlStiffness * Time.fixedDeltaTime / 2;
		float pastGain = -controlDamping + controlStiffness * Time.fixedDeltaTime / 2;

		float scalingFactor = 1.0f;//body.mass / controlAxis.GetValue( AxisVariable.INERTIA );

		// Receive u_s (delayed u_m) and U_s (delayed U_m)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Read scaled player force (F_s) value
		float playerForce = transform.forward.z * controlAxis.GetValue( AxisVariable.FORCE ) * scalingFactor;
		// Convert from player input force to wave transformation output force (simple copy for now)

        // x_dot_s = (sqrt(2*b) * u_s - F_s) / b
		inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - outputForce ) / waveImpedance;
		// should be inputVelocity = inputVelocity / inertia_scaling_factor
		// x_s = (sqrt(2*b) * U_s - p_s) / b
		//inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral + outputForceIntegral ) / waveImpedance;

		if( reference != null ) reference.position += Vector3.forward * inputVelocity * Time.fixedDeltaTime;

		//float positionError = inputPosition - ( body.position.z - initialPosition.z );
		float velocityError = inputVelocity - body.velocity.z;
		controlForce += ( presentGain * velocityError + pastGain * lastVelocityError );
		//controlForce = /*controlStiffness * positionError +*/ controlDamping * velocityError;
		lastVelocityError = velocityError;
		body.AddForce( Vector3.forward * ( playerForce + controlForce - outputDamping * body.velocity.z ), ForceMode.Force );

		outputForce = controlForce + INPUT_DAMPING * inputVelocity;

		// Integrate force for moment (p_s) calculation
		//outputForceIntegral += outputForce * Time.fixedDeltaTime;

        // Set robot position and velocity setpoints (relative to initial axis/body position and normalized by scenario dimensions)
		//float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		//controlAxis.SetScaledValue( AxisVariable.POSITION, Mathf.Clamp( relativeSetpoint, -1.0f, 1.0f ) );
		inputPosition += inputVelocity * Time.fixedDeltaTime;
		controlAxis.SetValue( AxisVariable.POSITION, ( ( inputPosition - initialPosition.z ) / transform.forward.z ) / scalingFactor );
		controlAxis.SetValue( AxisVariable.VELOCITY, ( inputVelocity / transform.forward.z ) / scalingFactor );

		// Set impedance matching control K (lamda^2 * M) and B,D (lamda * M) parameters
		controlAxis.SetValue( AxisVariable.STIFFNESS, TUNNING_PARAMETER * TUNNING_PARAMETER * controlAxis.GetValue( AxisVariable.INERTIA ) );
		controlAxis.SetValue( AxisVariable.DAMPING, TUNNING_PARAMETER * controlAxis.GetValue( AxisVariable.INERTIA ) );

		// v_s = ( b * x_dot_s - F_s ) / sqrt(2 * b)
		float outputWaveVariable = ( waveImpedance * inputVelocity - outputForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		// V_s = ( b * x_s - p_s ) / sqrt(2 * b)
		//float outputWaveIntegral = ( waveImpedance * inputPosition - outputForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

        // Send v_s and V_s
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public void OnDisable()
	{
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, 0.0f );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, 0.0f );
	}

	public float GetInputForce() { return controlForce; }
	public float GetOutputForce() { return outputForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetInputPosition() { return inputPosition; }
	public float GetInputVelocity() { return inputVelocity; }
	public float GetVelocity() { return body.velocity.z; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}