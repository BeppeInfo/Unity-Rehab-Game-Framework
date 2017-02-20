using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	private const float DRAG_DAMPING = 1.0f;

	protected float waveImpedance = 10.0f;

	private InputAxis controlAxis = null;

	private float playerForce = 0.0f, feedbackForce = 0.0f;

	public Slider stiffnessSlider, dampingSlider;

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		float inputPosition = Mathf.Clamp( controlAxis.GetNormalizedValue( AxisVariable.POSITION ), -1.0f, 1.0f );
		float relativePosition = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		float positionError = Mathf.Clamp( inputPosition - relativePosition, -1.0f, 1.0f );

		playerForce = positionError * rangeLimits.z * transform.forward.z;
		//Debug.Log( "Input force: " + playerForce.ToString() );
		feedbackForce = waveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable;
		//feedbackForce = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, FORCE );

		body.AddForce( ( playerForce + feedbackForce ) * Vector3.forward - body.velocity * DRAG_DAMPING, ForceMode.Force );

		controlAxis.SetValue( AxisVariable.STIFFNESS, stiffnessSlider.value );
		controlAxis.SetValue( AxisVariable.DAMPING, dampingSlider.value );

		controlAxis.SetNormalizedValue( AxisVariable.POSITION, relativePosition );

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