using UnityEngine;

public class ForceMasterController : Controller
{
	const float INTERACTION_STIFFNESS = 10.0f;

	private Transform localAttachment = null;
	public Transform distantAttachment;

	protected float waveImpedance = 10.0f;

	private float baseSpringLength = 0.0f;

	private float outputForce = 0.0f;
	private float outputForceIntegral = 0.0f;

	void Start()
	{
		initialPosition = body.position;
		localAttachment = GetComponentInChildren<Transform>();
		baseSpringLength = localAttachment.position.z - distantAttachment.position.z;
	}

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - outputForce ) / waveImpedance;
		float inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral - outputForceIntegral ) / waveImpedance;

		//float inputVelocity = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION );
		//float inputPosition = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY );

		//body.MovePosition( inputPosition * Vector3.forward );
		body.velocity = inputVelocity * Vector3.forward;

		float springLength = localAttachment.position.z - distantAttachment.position.z;
		outputForce = -INTERACTION_STIFFNESS * ( springLength - baseSpringLength );
		if( baseSpringLength * springLength < 0.0f ) outputForce *= 20.0f;
		outputForceIntegral += outputForce * Time.fixedDeltaTime;

		float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForce;
		float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForceIntegral;

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, FORCE, outputForce );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
}