using UnityEngine;
using System;
using System.Collections;

public class ForceMasterController : Controller
{
	protected float waveImpedance = 10.0f;

	private float outputForce = 0.0f;
	private float outputForceIntegral = 0.0f;

	void Start()
	{
		initialPosition = body.position;
	}

	void FixedUpdate()
	{
		float inputWaveVariable = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 0 );
		float inputWaveIntegral = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 1 );

		float inputVelocity = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveVariable - outputForce ) / waveImpedance;
		float inputPosition = ( Mathf.Sqrt( 2.0f * waveImpedance ) * inputWaveIntegral - outputForceIntegral ) / waveImpedance;

		//float inputVelocity = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 0 );
		//float inputPosition = GameManager.GetConnection().GetRemoteValue( elementID, (int) GameAxis.Z, 1 );

		//body.MovePosition( inputPosition * Vector3.forward );
		body.velocity = inputVelocity * Vector3.forward;

		outputForceIntegral += outputForce * Time.fixedDeltaTime;

		float outputWaveVariable = inputWaveVariable - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForce;
		float outputWaveIntegral = inputWaveIntegral - Mathf.Sqrt( 2.0f / waveImpedance ) * outputForceIntegral;

		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 0, outputWaveVariable );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 1, outputWaveIntegral );
		//GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 0, outputForce );
		//GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 1, outputForceIntegral );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 2, body.position.z );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, 3, body.velocity.z );
	}

	public void SetInteractionForce( float value )
	{
		outputForce = value;
	}
}

