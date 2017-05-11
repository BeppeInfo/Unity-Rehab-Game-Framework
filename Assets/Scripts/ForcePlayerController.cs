using UnityEngine;
using UnityEngine.UI;

public class ForcePlayerController : Controller
{
	private const float DRIFT_CORRECTION_GAIN = 0.1f;

	private InputAxis controlAxis = null;

	private float inputVelocity = 0.0f, inputPosition = 0.0f;
	private float outputForce = 0.0f, outputForceIntegral = 0.0f;

	void Start()
	{
		//body.isKinematic = true;
		initialPosition = body.position;
	}

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		outputForce = controlAxis.GetScaledValue( AxisVariable.FORCE ) * rangeLimits.z * transform.forward.z;
		outputForceIntegral += outputForce * Time.fixedDeltaTime;

		inputVelocity = ( Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveVariable - outputForce ) / Controller.WaveImpedance;
		inputPosition = ( Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveIntegral - outputForceIntegral ) / Controller.WaveImpedance;

		if( inputWaveVariable != 0.0f ) body.velocity = Vector3.forward * ( inputVelocity + DRIFT_CORRECTION_GAIN * ( inputPosition - body.position.z ) );

		float relativeSetpoint = ( body.position.z - initialPosition.z ) / rangeLimits.z / transform.forward.z;
		controlAxis.SetScaledValue( AxisVariable.POSITION, relativeSetpoint );

		float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / Controller.WaveImpedance ) * outputForce;
		float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / Controller.WaveImpedance ) * outputForceIntegral;

		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}

	public void OnEnable()
	{
		controlAxis = Configuration.GetSelectedAxis();
	}

	public float GetOutputForce() { return outputForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetInputPosition() { return inputPosition; }
	public float GetInputVelocity() { return inputVelocity; }
	public float GetVelocity() { return body.velocity.z; }

	public void SetHelperStiffness( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.STIFFNESS, value ); }
	public void SetHelperDamping( float value ){ if( controlAxis != null ) controlAxis.SetValue( AxisVariable.DAMPING, value ); }
}