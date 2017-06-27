using UnityEngine;

public class ForceMasterController : Controller
{
	float inputForce = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	// Wave variables control algorithm based on 2004 paper by Niemeyer and Slotline
	void FixedUpdate()
	{
		// Receive v_m (delayed v_s) and V_m (delayed V_s)
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Lock local body if no messages are being received
		if( inputWaveVariable == 0.0 ) body.constraints |= RigidbodyConstraints.FreezePositionZ;
		else body.constraints &= (~RigidbodyConstraints.FreezePositionZ);

		// F_m = b * x_dot_m - sqrt(2 * b) * v_m
		inputForce = Controller.WaveImpedance * body.velocity.z - Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveVariable;
		// p_m = b * x_m - sqrt(2 * b) * V_m
		//float inputForceIntegral = Controller.WaveImpedance * body.position.z - Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * inputWaveIntegral;

		//Debug.Log( string.Format( "force: {0} - force integral: {1}", inputForce, inputForceIntegral ) );

		body.AddForce( Vector3.forward * inputForce, ForceMode.Force );

		// u_m = sqrt(2 * b) * x_dot_m - v_m
		float outputWaveVariable = Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * body.velocity.z - inputWaveVariable;
		// U_m = sqrt(2 * b) * x_m - V_m
		//float outputWaveIntegral = Mathf.Sqrt( 2.0f * Controller.WaveImpedance ) * body.position.z - inputWaveIntegral;

		// Send u_m and U_m
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );

		// Send position and velocity values directly
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, VELOCITY, body.velocity.z );
	}
		
	public float GetInputForce() { return inputForce; }

	public float GetPosition() { return body.position.z; }
}