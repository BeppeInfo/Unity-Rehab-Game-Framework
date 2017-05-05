using UnityEngine;

public class ForceMasterController : Controller
{
	protected float waveImpedance = 10.0f;

	float inputForce = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		inputForce = -waveImpedance * body.velocity.z + Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		float inputForceIntegral = -waveImpedance * body.position.z + Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral;

		body.AddForce( Vector3.forward * inputForce, ForceMode.Force );

		float outputWaveVariable = ( waveImpedance * body.velocity.z - inputForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		float outputWaveIntegral = ( waveImpedance * body.position.z - inputForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}
		
	public float GetInputForce() { return inputForce; }

	public float GetSetpoint() { return GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION ); }
	public float GetPosition() { return body.position.z - initialPosition.z; }
}