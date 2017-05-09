using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, playerForceIntegral = 0.0f;

	void Start()
	{
		//body.isKinematic = true;
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

		if( inputWaveVariable != 0.0f ) body.velocity = Vector3.forward * ( inputVelocity /*+ inputPosition - body.position.z*/ );

		float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		controlAxis.SetNormalizedValue( AxisVariable.POSITION, relativeSetpoint );

		float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / waveImpedance ) * playerForce;
		float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / waveImpedance ) * playerForceIntegral;

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetPlayerForce() { return playerForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetVelocity() { return body.velocity.z; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}