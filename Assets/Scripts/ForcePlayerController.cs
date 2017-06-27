using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	private const float DRIFT_CORRECTION_GAIN = 0.0f; // Increase for position error correction

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
		// Receive u_s (delayed u_m) and U_s (delayed U_m)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Read scaled player force (F_s) value
		float playerForce = transform.forward.z * controlAxis.GetValue( AxisVariable.FORCE ) / controlAxis.GetValue( AxisVariable.INERTIA );
		// Convert from player input force to wave transformation output force (simple copy for now)
		outputForce = playerForce;
        // Integrate force for moment (p_s) calculation
        //outputForceIntegral += outputForce * Time.fixedDeltaTime;

        // x_dot_s = (sqrt(2*b) * u_s - F_s) / b
		inputVelocity = ( Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveVariable - outputForce ) / Controller.WaveImpedance;
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

        // Set robot position setpoint (relative to initial axis/body position and normalized by scenario dimensions)
		float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		controlAxis.SetScaledValue( AxisVariable.POSITION, Mathf.Clamp( relativeSetpoint, -1.0f, 1.0f ) );

        // v_s = u_s - sqrt(2/b) * F_s
		float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / Controller.WaveImpedance ) * outputForce;
		// V_s = U_s - sqrt(2/b) * p_s
		//float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / Controller.WaveImpedance ) * outputForceIntegral;

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