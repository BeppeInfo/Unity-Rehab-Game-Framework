using UnityEngine;

public class ForcePlayerController : Controller
{
	private const float DRAG_DAMPING = 0.1f;

	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, feedbackForce = 0.0f;

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		playerForce = controlAxis.GetNormalizedValue( AxisVariable.FORCE ) * rangeLimits.z * transform.forward.z;
		//Debug.Log( "Input force: " + playerForce.ToString() );
		feedbackForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		//feedbackForce = GameManager.GetConnection().GetRemoteValue( elementID, GameAxis.Z, FORCE );

		body.AddForce( ( playerForce + feedbackForce ) * Vector3.forward - body.velocity * DRAG_DAMPING, ForceMode.Force );
		//controlAxis.SetNormalizedValue( AxisVariable.FORCE, feedbackForce );
		controlAxis.SetNormalizedValue( AxisVariable.POSITION, body.position.z / rangeLimits.z / transform.forward.z );

		float outputWaveVariable = -inputWaveVariable + Mathf.Sqrt( 2.0f * waveImpedance ) * body.velocity.z;
		float outputWaveIntegral = -inputWaveIntegral + Mathf.Sqrt( 2.0f * waveImpedance ) * body.position.z;

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetInputForce() { return playerForce; }
	public float GetInteractionForce() { return feedbackForce; }
}

