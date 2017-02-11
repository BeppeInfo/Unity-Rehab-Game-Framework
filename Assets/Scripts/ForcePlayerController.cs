using UnityEngine;

public class ForcePlayerController : Controller
{
	private const float DRAG_DAMPING = 0.1f;

	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, feedbackForce = 0.0f;

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 0 );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 1 );

		playerForce = controlAxis.GetNormalizedValue( AxisVariable.FORCE ) * rangeLimits.z * transform.forward.z;
		//Debug.Log( "Input force: " + playerForce.ToString() );
		feedbackForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		//feedbackForce = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 0 );

		body.AddForce( ( playerForce + feedbackForce ) * Vector3.forward - body.velocity * DRAG_DAMPING, ForceMode.Force );
		//controlAxis.SetNormalizedValue( AxisVariable.FORCE, feedbackForce );
		controlAxis.SetNormalizedValue( AxisVariable.POSITION, body.position.z / rangeLimits.z / transform.forward.z );

		float outputWaveVariable = -inputWaveVariable + Mathf.Sqrt( 2.0f * waveImpedance ) * body.velocity.z;
		float outputWaveIntegral = -inputWaveIntegral + Mathf.Sqrt( 2.0f * waveImpedance ) * body.position.z;

		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 0, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 1, outputWaveIntegral );
		//GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 0, body.velocity.z );
		//GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 1, body.position.z );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetInputForce() { return playerForce; }
	public float GetInteractionForce() { return feedbackForce; }
}

