using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class AxisData 
{
	public float position = 0.0f, velocity = 0.0f, acceleration = 0.0f;
	public float maxPosition = 0.0f, maxVelocity = 0.0f, maxAcceleration = 0.0f;
	public float minPosition = 0.0f, minVelocity = 0.0f, minAcceleration = 0.0f;
	public float range = 1.0f;
	public float error = 0.0f;
	public float motionTime = Time.realtimeSinceStartup;

	public float actualPosition = 0.0f, actualVelocity = 0.0f;
	public float[] setpoints;

	public AxisData( float initialPosition )
	{
		position = initialPosition;
	}
}

public class InputManager : MonoBehaviour
{
	public static string axisServerHost;
	private static int axisServerPort;
	private static int axisClientPort;

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
		axisServerPort = PlayerPrefs.GetInt( ConnectionManager.SERVER_PORT_ID, ConnectionManager.DEFAULT_SERVER_PORT );
		axisClientPort = PlayerPrefs.GetInt( ConnectionManager.AXIS_CLIENT_PORT_ID, ConnectionManager.DEFAULT_AXIS_CLIENT_PORT );
			
		ConnectionManager.AxisClient.Connect( axisServerHost, axisServerPort, axisClientPort );				

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
				mouseAxisValues[ axisName ].motionTime = elapsedTime;
			}

			foreach( string axisName in keyboardAxisValues.Keys )
			{
				keyboardAxisValues[ axisName ].velocity = Input.GetAxis( axisName );
				keyboardAxisValues[ axisName ].position += keyboardAxisValues[ axisName ].velocity * elapsedTime;
				keyboardAxisValues[ axisName ].motionTime = elapsedTime;
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
						float position = float.Parse( axisData[ 1 ] );
						float velocity = float.Parse( axisData[ 2 ] );
						float acceleration = float.Parse( axisData[ 3 ] );

						remoteAxisValues[ axisID ].error = Mathf.Abs( ( position - remoteAxisValues[ axisID ].position ) / remoteAxisValues[ axisID ].range );

						float delay = ( Time.realtimeSinceStartup - remoteAxisValues[ axisID ].motionTime ) / 2;
						remoteAxisValues[ axisID ].motionTime = Time.realtimeSinceStartup;
						remoteAxisValues[ axisID ].position = position + velocity * delay + acceleration * delay * delay / 2;

						remoteAxisValues[ axisID ].acceleration = acceleration;
						remoteAxisValues[ axisID ].velocity = velocity;
					}
					catch( Exception e )
					{
						Debug.Log( e.ToString() );
					}

					Debug.Log( "InputManager: Receiving input from axis: " + axisMessage );

					StringBuilder axisMessageBuilder = new StringBuilder( axisID );
					
					foreach( float setpoint in remoteAxisValues[ axisID ].setpoints )
					{
						axisMessageBuilder.Append( ' ' );
						axisMessageBuilder.Append( setpoint );
					}
					
					Debug.Log( "InputManager: Sending input feedback: " + axisMessageBuilder.ToString() );
					ConnectionManager.AxisClient.SendString( axisMessageBuilder.ToString() );
				}
			}

			yield return null;
		}
	}

	public static float GetAxisSpeed( string axisID )
	{
		if( mouseAxisValues.ContainsKey( axisID ) )
			return mouseAxisValues[ axisID ].velocity;
		else if( keyboardAxisValues.ContainsKey( axisID ) )
			return keyboardAxisValues[ axisID ].velocity;
		else if( remoteAxisValues.ContainsKey( axisID ) )
		{
			float deltaTime = Time.realtimeSinceStartup - remoteAxisValues[ axisID ].motionTime;
			float deltaVelocity = remoteAxisValues[ axisID ].actualVelocity;
			float targetPosition = remoteAxisValues[ axisID ].position + remoteAxisValues[ axisID ].velocity * deltaTime 
				                   + remoteAxisValues[ axisID ].acceleration * deltaTime * deltaTime / 2;

			targetPosition = Mathf.Clamp( targetPosition, remoteAxisValues[ axisID ].minPosition, remoteAxisValues[ axisID ].maxPosition );

			/*if( remoteAxisValues[ axisID ].acceleration != 0.0 )
			{
				if( remoteAxisValues[ axisID ].velocity / remoteAxisValues[ axisID ].acceleration )
					remoteAxisValues[ axisID ].velocity += remoteAxisValues[ axisID ].acceleration * deltaTime / 2
			}*/


			return remoteAxisValues[ axisID ].velocity;
		}
		else
			return 0.0;
	}

	public static void SetAxisFeedback( string axisID, float[] setpoints )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			if( setpoints[ 0 ] > remoteAxisValues[ axisID ].maxPosition ) remoteAxisValues[ axisID ].maxPosition = setpoints[ 0 ];
			else if( setpoints[ 0 ] < remoteAxisValues[ axisID ].minPosition ) remoteAxisValues[ axisID ].minPosition = setpoints[ 0 ];
			remoteAxisValues[ axisID ].range = remoteAxisValues[ axisID ].maxPosition - remoteAxisValues[ axisID ].minPosition;
			remoteAxisValues[ axisID ].actualPosition = setpoints[ 0 ];
			remoteAxisValues[ axisID ].setpoints = setpoints;
			Debug.Log( "New setpoints for " + axisID + " " + setpoints.ToString() );
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.AxisClient.Disconnect();
	}
}

