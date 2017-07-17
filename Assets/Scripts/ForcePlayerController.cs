using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : ForceController
{
	private InputAxis controlAxis = null;

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm with wave filtering
	// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
	void FixedUpdate()
	{
		// Read scaled player position (x_s) and velocity (xdot_s)
		float inputScalingFactor = 1.0f;//controlAxis.GetScale() * rangeLimits.z;
		float playerForce = transform.forward.z * controlAxis.GetValue( AxisVariable.FORCE ) * inputScalingFactor;
		//body.position = transform.forward * controlAxis.GetScaledValue( AxisVariable.POSITION ) * rangeLimits.z + initialPosition.z;
		//body.velocity = transform.forward * controlAxis.GetValue( AxisVariable.VELOCITY ) * inputScalingFactor;

		// Extract remote force from wave variable: F_s = -b * xdot_s + sqrt( 2 * b ) * u_s
		// Extract remote moment from received wave integral: p_m = -b * x_s + sqrt( 2 * b ) * U_s
		ProcessInputWave();

		remoteForce += playerForce; // hack

		// Apply resulting force to user device
		body.AddForce( Vector3.forward * remoteForce, ForceMode.Force );
		//float feedbackScalingFactor = 1.0f;//0.005f;// controlAxis.GetValue( AxisVariable.INERTIA ) / body.mass;
		//controlAxis.SetValue( AxisVariable.FORCE, transform.forward.z * remoteForce * feedbackScalingFactor );

		// Encode and send output wave variable (velocity data): v_s = ( b * xdot_s - F_s ) / sqrt( 2 * b )
		// Encode and send output wave integral (position data): V_s = ( b * x_s - p_s ) / sqrt( 2 * b )
		ProcessOutputWave();


		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, IMPEDANCE, waveImpedance );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, FILTER, filterStrength );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public void SetWaveImpedance( float value ){ waveImpedance = value; }
	public void SetFilteringStrength( float value ){ filterStrength = value; }

	public float GetWaveImpedance(){ return waveImpedance; }
	public float GetFilteringStrength(){ return filterStrength; }
}