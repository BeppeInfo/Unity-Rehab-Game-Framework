using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	const float TUNNING_PARAMETER = 1.0f; // lambda = 1.0;
	const float INPUT_DAMPING = 1.0f; // R

	private InputAxis controlAxis = null;

	private float inputVelocity = 0.0f, inputPosition = 0.0f;
	private float outputForce = 0.0f, outputForceIntegral = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm with impedance matching between dynamical system and b (wave impedance) parameter
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		float waveImpedance = INPUT_DAMPING + TUNNING_PARAMETER * body.mass; // b = R + lamda * m

		// Receive u_s (delayed u_m) and U_s (delayed U_m)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Read scaled player force (F_s) value
		float playerForce = transform.forward.z * controlAxis.GetValue( AxisVariable.FORCE ) * body.mass / controlAxis.GetValue( AxisVariable.INERTIA );
		// Convert from player input force to wave transformation output force (simple copy for now)
		outputForce = playerForce + TUNNING_PARAMETER * body.velocity.z;
        // Integrate force for moment (p_s) calculation
        //outputForceIntegral += outputForce * Time.fixedDeltaTime;

        // x_dot_s = (sqrt(2*b) * u_s - F_s) / b
		inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - outputForce ) / waveImpedance;
		// should be inputVelocity = inputVelocity / inertia_scaling_factor
		// x_s = (sqrt(2*b) * U_s - p_s) / b
		//inputPosition = ( Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveIntegral - outputForceIntegral ) / Controller.WaveImpedance;

		// Begin updating local movement if client started receiving messages
		if( inputWaveVariable != 0.0f ) 
		{
			// Convert from wave transformation input velocity to player output velocity (simple copy for now)
			float playerVelocity = inputVelocity;
			// Apply velocity to player's body as a 3D vector
			body.velocity = Vector3.forward * playerVelocity;
			// Add drift correction proportional to position tracking error
			//body.velocity = Vector3.forward * ( DRIFT_CORRECTION_GAIN * ( inputPosition - body.position.z ) );
		}

        // Set robot position and velocity setpoints (relative to initial axis/body position and normalized by scenario dimensions)
		//float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		//controlAxis.SetScaledValue( AxisVariable.POSITION, Mathf.Clamp( relativeSetpoint, -1.0f, 1.0f ) );
		controlAxis.SetValue( AxisVariable.VELOCITY, inputVelocity * controlAxis.GetValue( AxisVariable.INERTIA ) / body.mass );

		// Set impedance matching control K (lamda^2 * M) and B,D (lamda * M) parameters
		controlAxis.SetValue( AxisVariable.STIFFNESS, TUNNING_PARAMETER * TUNNING_PARAMETER * controlAxis.GetValue( AxisVariable.INERTIA ) );
		controlAxis.SetValue( AxisVariable.DAMPING, TUNNING_PARAMETER * controlAxis.GetValue( AxisVariable.INERTIA ) );

        // v_s = sqrt(2 * b) * x_dot_s - u_s
		float outputWaveVariable = Mathf.Sqrt( 2.0f * waveImpedance ) * inputVelocity - inputWaveVariable;
		// V_s = sqrt(2 * b) * x_s - V_s
		//float outputWaveIntegral = Mathf.Sqrt( 2.0f * waveImpedance ) * body.position.z - inputWaveIntegral;

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

	public float GetOutputForce() { return outputForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetInputPosition() { return inputPosition; }
	public float GetInputVelocity() { return inputVelocity; }
	public float GetVelocity() { return body.velocity.z; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}