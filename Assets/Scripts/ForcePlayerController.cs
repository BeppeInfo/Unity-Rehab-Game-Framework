using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float lastPosition = 0.0f;
	private float playerForce = 0.0f, feedbackForce = 0.0f;

	private float absoluteSetpoint = 0.0f;

	private float feedbackForceScale = 0.0f;

	float playerPosition = 0.0f;

	void Start()
	{
		initialPosition = body.position;
		absoluteSetpoint = initialPosition.z;
	}

	void FixedUpdate()
	{
		//float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		playerPosition = Mathf.Clamp( controlAxis.GetNormalizedValue( AxisVariable.POSITION ), -1.0f, 1.0f );
		float outputPosition = playerPosition * rangeLimits.z * transform.forward.z;
		float outputVelocity = ( outputPosition - lastPosition ) / Time.fixedDeltaTime;
		lastPosition = outputPosition;

		playerForce = Mathf.Clamp( controlAxis.GetNormalizedValue( AxisVariable.FORCE ), -1.0f, 1.0f );

		//feedbackForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		feedbackForce = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, FORCE );
		feedbackForce = feedbackForce / rangeLimits.z / transform.forward.z;
		controlAxis.SetValue( AxisVariable.FORCE, -feedbackForce * feedbackForceScale );

		float inputPosition = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, POSITION );
		float inputVelocity = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, VELOCITY );

		if( inputPosition != 0.0f )
			body.velocity = Vector3.forward * ( inputVelocity + inputPosition - body.position.z );

		float relativeSetpoint = ( absoluteSetpoint - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		controlAxis.SetNormalizedValue( AxisVariable.POSITION, relativeSetpoint );

		//float outputWaveVariable = -inputWaveVariable + Mathf.Sqrt( 2.0f * waveImpedance ) * outputVelocity;
		//float outputWaveIntegral = -inputWaveIntegral + Mathf.Sqrt( 2.0f * waveImpedance ) * outputPosition;

		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, outputPosition );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, outputVelocity );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetInputForce() { return /*playerForce*/playerPosition; }
	public float GetInteractionForce() { return feedbackForce; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }

	public void SetFeedbackForceScale( float value ){ feedbackForceScale = value; }
}