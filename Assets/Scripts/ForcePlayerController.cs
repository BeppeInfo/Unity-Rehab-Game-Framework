using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, playerForceIntegral = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		playerForce = controlAxis.GetValue( AxisVariable.FORCE ) * transform.forward.z;
		playerForceIntegral += playerForce * Time.fixedDeltaTime;

		float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - playerForce ) / waveImpedance;
		float inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral - playerForceIntegral ) / waveImpedance;

		if( inputPosition != 0.0f )	body.velocity = Vector3.forward * ( inputVelocity + inputPosition - body.position.z );

		float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		controlAxis.SetNormalizedValue( AxisVariable.POSITION, relativeSetpoint );

		float outputWaveVariable = ( waveImpedance * inputVelocity + playerForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		float outputWaveIntegral = ( waveImpedance * inputPosition + playerForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetPlayerForce() { return playerForce; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}