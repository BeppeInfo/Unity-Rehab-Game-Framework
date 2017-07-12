using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, remoteForce = 0.0f, remoteForceIntegral = 0.0f;

	private float waveImpedance = 1.0f, inputWaveVariable = 0.0f, inputWaveIntegral = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm with wave filtering
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		// Get total delay between received wave values
		float networkDelay = GameManager.GetConnection().GetNetworkDelay( (byte) elementID );
		float waveDelay = networkDelay + Time.fixedDeltaTime;

		float scalingFactor = 1.0f;//body.mass / controlAxis.GetValue( AxisVariable.INERTIA );
		// Read scaled player force (F_s) value
		playerForce = transform.forward.z * controlAxis.GetValue( AxisVariable.FORCE ) * scalingFactor;
		// Extract remote force from wave variable: F_s = sqrt( 2 * b ) * u_s - b * xdot_s
		remoteForce = Mathf.Sqrt( 2 * waveImpedance ) * inputWaveVariable - waveImpedance * body.velocity.z;
		//remoteForceIntegral = Mathf.Sqrt( 2 * waveImpedance ) * inputWaveIntegral - waveImpedance * body.position.z;

		// Apply resulting force to rigid body
		body.AddForce( Vector3.forward * ( playerForce + remoteForce ), ForceMode.Force );

		// Receive delayed u_m and U_m
		float delayedWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float delayedWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Wave filtering:
		// Calculate wave derivative: udot_s = ( u_m_old - u_s ) / delay
		float inputWaveDerivative = ( delayedWaveVariable - inputWaveVariable ) / waveDelay;
		// Calculate next wave value: u_s = u_s + udot_s * dt
		inputWaveVariable += inputWaveDerivative * Time.fixedDeltaTime;

		// v_s = ( b * x_dot_s - F_s ) / sqrt(2 * b)
		float outputWaveVariable = ( waveImpedance * body.velocity.z - remoteForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		// V_s = ( b * x_s - p_s ) / sqrt(2 * b)
		//float outputWaveIntegral = ( waveImpedance * body.position.z - remoteForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

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

	public float GetInputForce() { return ( playerForce + remoteForce ); }
	public float GetPlayerForce() { return playerForce; }
	public float GetRemoteForce() { return remoteForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetVelocity() { return body.velocity.z; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}