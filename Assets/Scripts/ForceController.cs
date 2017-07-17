using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Wave variables control algorithm with wave filtering
// Please refer to section 7 of 2004 paper by Niemeyer and Slotline for more details
public abstract class ForceController : Controller 
{
	protected const int IMPEDANCE = 0, FILTER = 1;
	protected const int WAVE = 2, WAVE_INTEGRAL = 3;

	protected float waveImpedance = 1.0f, filterStrength = 1.0f;

	protected float remoteForce = 0.0f, remoteForceIntegral = 0.0f;

	protected float inputWaveVariable = 0.0f, inputWaveIntegral = 0.0f;
	protected float lastInputWaveVariable = 0.0f, lastDelayedWaveVariable = 0.0f;

	protected void ProcessInputWave() 
	{
		// Receive delayed u_in (u_in_old) and U_in (U_in_old)
		float delayedWaveVariable = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE );
		//float delayedWaveIntegral = GameManager.GetConnection().GetRemoteValue( (byte) elementID, Z, WAVE_INTEGRAL );

		// Filter delayed input wave to ensure stability: u_in / u_in_old = 1 / ( s + 1 ) => discrete form (s = 2/T * (z-1)/(z+1))
		float deltaTime = Time.fixedDeltaTime / filterStrength;
		inputWaveVariable = ( ( 2 - deltaTime ) * lastInputWaveVariable + deltaTime * ( delayedWaveVariable + lastDelayedWaveVariable ) ) / ( 2 + deltaTime );
		lastInputWaveVariable = inputWaveVariable;
		lastDelayedWaveVariable = delayedWaveVariable;

		// Extract remote force from wave variable: F_in = sqrt( 2 * b ) * u_in - b * xdot_out
		remoteForce = Mathf.Sqrt( 2 * waveImpedance ) * inputWaveVariable - waveImpedance * body.velocity.z;
		// Extract remote moment from wave integral: p_in = sqrt( 2 * b ) * U_in - b * x_out
		//remoteForceIntegral = Mathf.Sqrt( 2 * waveImpedance ) * inputWaveIntegral - waveImpedance * body.position.z;
	}

	protected void ProcessOutputWave() 
	{
		// Encode output wave variable (velocity data): u_out = ( b * xdot_out - F_in ) / sqrt( 2 * b )
		float outputWaveVariable = ( waveImpedance * body.velocity.z - remoteForce ) / Mathf.Sqrt( 2.0f * waveImpedance );
		// Encode output wave integral (position data): U_out = ( b * x_out - p_in ) / sqrt( 2 * b )
		//float outputWaveIntegral = ( waveImpedance * body.position.z - remoteForceIntegral ) / Mathf.Sqrt( 2.0f * waveImpedance );

		// Send u_out and U_out
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, outputWaveVariable );
		//GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, outputWaveIntegral );
	}


	public void OnDisable()
	{
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE, 0.0f );
		GameManager.GetConnection().SetLocalValue( (byte) elementID, Z, WAVE_INTEGRAL, 0.0f );
	}


	// Utilitary methods
	public float GetRemoteForce() { return remoteForce; }
	public float GetRelativePosition() { return body.position.z - initialPosition.z; }
	public float GetAbsolutePosition() { return body.position.z; }
	public float GetVelocity() { return body.velocity.z; }
}
