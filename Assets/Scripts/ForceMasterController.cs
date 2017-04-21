using UnityEngine;

public class ForceMasterController : Controller
{
	protected float waveImpedance = 10.0f;

	private float outputForce = 0.0f;
	private float outputForceIntegral = 0.0f;

	private float proportionalGain = 0.0f, derivativeGain = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	void FixedUpdate()
	{
		//float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		//float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - outputForce ) / waveImpedance;
		//float inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral - outputForceIntegral ) / waveImpedance;

		float inputVelocity = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY );
		float inputPosition = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION );

		float relativePosition = body.position.z - initialPosition.z;
		float interactionForce = proportionalGain * ( inputPosition - relativePosition ) - derivativeGain * body.velocity.z;
		body.AddForce( Vector3.forward * interactionForce, ForceMode.Force );
		//body.AddForce( transform.forward * Input.GetAxis( "Vertical" ) );

		outputForce = interactionForce;

		outputForceIntegral += outputForce * Time.fixedDeltaTime;

		//float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForce;
		//float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForceIntegral;

		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, FORCE, outputForce );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}

	public void SetInteractionForce( float value ) { outputForce = value; }
	public float GetOutputForce() {	return outputForce / rangeLimits.z / transform.forward.z; }

	public float GetSetpoint() { return GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION ); }
	public float GetPosition() { return body.position.z - initialPosition.z; }

	public void SetProportionalGain( float value ){ proportionalGain = value; }
	public void SetDerivativeGain( float value ){ derivativeGain = value; }
}