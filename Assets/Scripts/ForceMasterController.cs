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

		inputForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		float inputForceIntegral = waveImpedance * body.position.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral;

		Debug.Log( string.Format( "force: {0} - force integral: {1}", inputForce, inputForceIntegral ) );

		body.AddForce( Vector3.forward * inputForce, ForceMode.Force );

		float outputWaveVariable = Mathf.Sqrt( 2.0f * waveImpedance ) * body.velocity.z - inputWaveVariable;
		float outputWaveIntegral = Mathf.Sqrt( 2.0f * waveImpedance ) * body.position.z - inputWaveIntegral;

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
		
	public float GetInputForce() { return inputForce; }

	public float GetPosition() { return body.position.z; }
}