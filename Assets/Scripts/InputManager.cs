using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class InputManager : MonoBehaviour
{
	public static string axisServerHost;
	private static int axisServerPort;

	private static float serverDispatchTime = 0.0f, clientReceiveTime = 0.0f;

	private class AxisData 
	{
		public float position = 0.0f, velocity = 0.0f, acceleration = 0.0f;
		public float maxPosition = 0.0f, maxVelocity = 0.0f, maxAcceleration = 0.0f;
		public float minPosition = 0.0f, minVelocity = 0.0f, minAcceleration = 0.0f;
		public float range = 1.0f;
		public float error = 0.0f;
		public float motionTime = Time.realtimeSinceStartup;
		
		public float actualPosition = 0.0f, actualVelocity = 0.0f;
		public List<float> setpoints = new List<float>();
		
		public AxisData( float initialPosition )
		{
			position = initialPosition;
		}
	}

	private static Dictionary<string, AxisData> mouseAxisValues = new Dictionary<string, AxisData>();
	private static Dictionary<string, AxisData> keyboardAxisValues = new Dictionary<string, AxisData>();
	private static Dictionary<string, AxisData> remoteAxisValues = new Dictionary<string, AxisData>();

	void Start()
	{
		mouseAxisValues[ "Mouse X" ] = new AxisData( Input.mousePosition.x );
		mouseAxisValues[ "Mouse Y" ] = new AxisData( Input.mousePosition.y );
		keyboardAxisValues[ "Horizontal" ] = new AxisData( 0.0f );
		keyboardAxisValues[ "Vertical" ] = new AxisData( 0.0f );

		axisServerHost = PlayerPrefs.GetString( ConnectionManager.AXIS_SERVER_HOST_ID, ConnectionManager.LOCAL_SERVER_HOST );
		axisServerPort = PlayerPrefs.GetInt( ConnectionManager.AXIS_SERVER_DATA_PORT_ID, ConnectionManager.DEFAULT_SERVER_PORT );
			
		ConnectionManager.AxisClient.Connect( axisServerHost, axisServerPort );				

		StartCoroutine( UpdateAxisValues() );
	}

	public void AddRemoteAxis( string axisID, float initialPosition )
	{
		remoteAxisValues[ axisID ] = new AxisData( initialPosition );
	}

	public void RemoveRemoteAxis( string axisID )
	{
		remoteAxisValues.Remove( axisID );
	}

	private static IEnumerator UpdateAxisValues()
	{
		while( Application.isPlaying )
		{
			float elapsedTime = Time.deltaTime;

			foreach( string axisName in mouseAxisValues.Keys )
			{
				mouseAxisValues[ axisName ].velocity = Input.GetAxis( axisName ) / elapsedTime;
				mouseAxisValues[ axisName ].position += mouseAxisValues[ axisName ].velocity * elapsedTime;
				mouseAxisValues[ axisName ].motionTime = Time.realtimeSinceStartup;
			}

			foreach( string axisName in keyboardAxisValues.Keys )
			{
				keyboardAxisValues[ axisName ].velocity = Input.GetAxis( axisName );
				keyboardAxisValues[ axisName ].position += keyboardAxisValues[ axisName ].velocity * elapsedTime;
				keyboardAxisValues[ axisName ].motionTime = Time.realtimeSinceStartup;
			}

			string[] axesMessages = ConnectionManager.AxisClient.ReceiveString().Split( ':' );
			foreach( string axisMessage in axesMessages )
			{
				string[] axisData = axisMessage.Trim().Split( ' ' );

				string axisID = axisData[ 0 ];
				if( remoteAxisValues.ContainsKey( axisID ) )
				{
					try
					{
						// Network latency calculation based on the NTP protocol
						float clientDispatchTime = float.Parse( axisData[ 1 ] );
						float serverReceiveTime = float.Parse( axisData[ 2 ] );
						serverDispatchTime = float.Parse( axisData[ 3 ] );
						clientReceiveTime = Time.realtimeSinceStartup;

						float delay = ( ( clientReceiveTime - clientDispatchTime ) - ( serverReceiveTime - serverDispatchTime ) ) / 2.0f;

						float receivedPosition = float.Parse( axisData[ 4 ] );
						float receivedVelocity = float.Parse( axisData[ 5 ] );
						float receivedAcceleration = float.Parse( axisData[ 6 ] );

						remoteAxisValues[ axisID ].error = Mathf.Abs( ( receivedPosition - remoteAxisValues[ axisID ].position ) / remoteAxisValues[ axisID ].range );

						remoteAxisValues[ axisID ].motionTime = Time.realtimeSinceStartup;
						remoteAxisValues[ axisID ].position = receivedPosition + receivedVelocity * delay + receivedAcceleration * delay * delay / 2.0f;

						remoteAxisValues[ axisID ].acceleration = receivedAcceleration;
						remoteAxisValues[ axisID ].velocity = receivedVelocity;
					}
					catch( Exception e )
					{
						Debug.Log( e.ToString() );
					}

					Debug.Log( "InputManager: Receiving input from axis: " + axisMessage );
				}
			}

			yield return null;
		}
	}

	public static float GetAxisSpeed( string axisID )
	{
		if( mouseAxisValues.ContainsKey( axisID ) )
			return ( 2 * mouseAxisValues[ axisID ].velocity / remoteAxisValues[ axisID ].range );
		else if( keyboardAxisValues.ContainsKey( axisID ) )
			return ( 2 * keyboardAxisValues[ axisID ].velocity / remoteAxisValues[ axisID ].range );
		else if( remoteAxisValues.ContainsKey( axisID ) )
		{
			AxisData remoteAxis = remoteAxisValues[ axisID ];

			float deltaTime = Time.realtimeSinceStartup - remoteAxis.motionTime;

			float targetTolerance = 1 + remoteAxis.error;

			float targetPosition = remoteAxis.position + remoteAxis.velocity * deltaTime + remoteAxis.acceleration * deltaTime * deltaTime / 2.0f;
			targetPosition = Mathf.Clamp( targetPosition, remoteAxis.minPosition * targetTolerance, remoteAxis.maxPosition * targetTolerance );

			float targetVelocity = ( targetPosition - remoteAxis.actualPosition ) / deltaTime;
			targetVelocity = Mathf.Clamp( targetVelocity, remoteAxis.minVelocity * targetTolerance, remoteAxis.maxVelocity * targetTolerance );

			float targetAcceleration = ( targetVelocity - remoteAxis.actualVelocity ) / deltaTime;
			targetAcceleration = Mathf.Clamp( targetAcceleration, remoteAxis.minAcceleration * targetTolerance, remoteAxis.maxAcceleration * targetTolerance );

			targetVelocity = remoteAxisValues[ axisID ].actualVelocity + targetAcceleration * deltaTime;

			return ( 2 * targetVelocity / remoteAxisValues[ axisID ].range );
		}
		else
			return 0.0f;
	}

	public static float GetAxisAbsolutePosition( string axisID )
	{
		if( mouseAxisValues.ContainsKey( axisID ) )
			return mouseAxisValues[ axisID ].position;
		else if( keyboardAxisValues.ContainsKey( axisID ) )
			return keyboardAxisValues[ axisID ].position;
		else if( remoteAxisValues.ContainsKey( axisID ) )
			return remoteAxisValues[ axisID ].position;

		return 0.0f;
	}

	public static void CalibrateAxisSpeed( string axisID )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			AxisData remoteAxis = remoteAxisValues[ axisID ];

			if( remoteAxis.velocity > remoteAxis.maxVelocity ) remoteAxis.maxVelocity = remoteAxis.velocity;
			else if( remoteAxis.velocity < remoteAxis.minVelocity ) remoteAxis.minVelocity = remoteAxis.velocity;

			if( remoteAxis.acceleration > remoteAxis.maxAcceleration ) remoteAxis.maxAcceleration = remoteAxis.acceleration;
			else if( remoteAxis.acceleration < remoteAxis.minAcceleration ) remoteAxis.minAcceleration = remoteAxis.acceleration;
		}
	}

	public static void CalibrateAxisPosition( string axisID, float minPosition, float maxPosition )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			AxisData remoteAxis = remoteAxisValues[ axisID ];

			remoteAxis.maxPosition = maxPosition;
			remoteAxis.minPosition = minPosition;
			remoteAxis.range = ( maxPosition - minPosition != 0.0f ) ? maxPosition - minPosition : 1.0f;
		}
	}

	public static void SetAxisFeedback( string axisID, float actualPosition, float actualVelocity )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			remoteAxisValues[ axisID ].actualVelocity = actualVelocity * remoteAxisValues[ axisID ].range / 2.0f;

			remoteAxisValues[ axisID ].actualPosition = ( ( actualPosition + 1.0f ) * remoteAxisValues[ axisID ].range / 2.0f ) + remoteAxisValues[ axisID ].minPosition;

			Debug.Log( "Feedback values updated for " + axisID + ": " + remoteAxisValues[ axisID ].actualPosition + " " + remoteAxisValues[ axisID ].actualVelocity );
		}
	}

	public static void SetAxisSetpoint( string axisID, float setpointPosition, float setpointVelocity, float setpointReachTime )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			string axisMessage = string.Format( "{0} {1} {2} {3} {4} {5} {6}", axisID, serverDispatchTime, clientReceiveTime, 
			                                   Time.realtimeSinceStartup, setpointPosition, setpointVelocity, setpointReachTime );

			Debug.Log( "InputManager: Sending input setpoint: " + axisMessage );
			ConnectionManager.AxisClient.SendString( axisMessage );
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.AxisClient.Disconnect();
	}
}

