using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class InputManager : MonoBehaviour
{
	private const int DEFAULT_AXIS_SERVER_PORT = 50001;

	public static string axisServerHost;
	//private static int axisServerPort;

	private static float serverDispatchTime = 0.0f, clientReceiveTime = 0.0f;

	private static StreamWriter inputLog = new StreamWriter( "c:\\Users\\Adriano\\Documents\\input.txt", false );
	private static StreamWriter trajectoryLog = new StreamWriter( "c:\\Users\\Adriano\\Documents\\trajectory.txt", false );

	//Kalman Filter
	private static float Q = 0.000001f;
	private static float R = 0.01f;
	private static float P = 1f, X = 0f, K;

	private static float filterUpdate( float measurement )
	{
		K = (P + Q) / (P + Q + R);
		P = R * (P + Q) / (R + P + Q);

		X = X + (measurement - X) * K;

		return X;
	}

	private class AxisData 
	{
		public float position = 0.0f, velocity = 0.0f, acceleration = 0.0f;
		public float maxPosition = 0.0f, maxVelocity = 0.0f, maxAcceleration = 0.0f;
		public float minPosition = 0.0f, minVelocity = 0.0f, minAcceleration = 0.0f;
		public float range = 1.0f;
		public float errorTolerance = 0.0f;
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
		//axisServerPort = PlayerPrefs.GetInt( ConnectionManager.AXIS_SERVER_DATA_PORT_ID, DEFAULT_AXIS_SERVER_PORT );
			
		ConnectionManager.AxisClient.Connect( /*axisServerHost*/"169.254.110.158", /*axisServerPort*/DEFAULT_AXIS_SERVER_PORT );

		StartCoroutine( UpdateAxisValues() );
	}

	public static void AddRemoteAxis( string axisID, float initialPosition )
	{
		if( !remoteAxisValues.ContainsKey( axisID ) || !mouseAxisValues.ContainsKey( axisID ) || !keyboardAxisValues.ContainsKey( axisID ) )
		{
			remoteAxisValues[ axisID ] = new AxisData( initialPosition );
			SetAxisSetpoint( axisID, 0.0f, 0.0f, 1.0f );
		}
	}

	public static void RemoveRemoteAxis( string axisID )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
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

			string[] messageData = ConnectionManager.AxisClient.ReceiveString().Split( '|' );
			if( messageData.Length >= 2 )
			{
				string[] timeData = messageData[ 0 ].Split( ' ' );

				// Network latency calculation based on the NTP protocol
				float clientDispatchTime = float.Parse( timeData[ 0 ] );
				float serverReceiveTime = float.Parse( timeData[ 1 ] );
				serverDispatchTime = float.Parse( timeData[ 2 ] );
				clientReceiveTime = Time.realtimeSinceStartup;
				
				float delay = ( ( clientReceiveTime - clientDispatchTime ) - ( serverReceiveTime - serverDispatchTime ) ) / 2.0f;
				if( delay < 0.0f ) delay = 0.0f;
				
				//Debug.Log( string.Format( "latency: ( ({0} - {1}) - ({2} - {3}) ) / 2 = {4}", clientReceiveTime, clientDispatchTime, serverReceiveTime, serverDispatchTime, delay ) );
				
				string[] axesMessages = messageData[ 1 ].Split( ':' );
				foreach( string axisMessage in axesMessages )
				{
					string[] axisData = axisMessage.Trim().Split( ' ' );

					string axisID = axisData[ 0 ];
					if( remoteAxisValues.ContainsKey( axisID ) )
					{
						AxisData remoteAxis = remoteAxisValues[ axisID ];

						try
						{
							float receivedPosition = float.Parse( axisData[ 1 ] );
							float receivedVelocity = float.Parse( axisData[ 2 ] );

							/*float receivedTorque = float.Parse( axisData[ 3 ] );
							float receivedStiffness = float.Parse( axisData[ 4 ] );
							float receivedDamping = float.Parse( axisData[ 5 ] );*/

							remoteAxis.errorTolerance = 1.0f + Mathf.Abs( ( receivedPosition - remoteAxis.position ) / remoteAxis.range );

							float updateTime = clientReceiveTime - remoteAxis.motionTime;
							remoteAxis.motionTime = clientReceiveTime;

							remoteAxis.position = receivedPosition + receivedVelocity * ( delay + updateTime );

							remoteAxis.velocity = ( remoteAxis.position - remoteAxis.actualPosition ) / updateTime;

							//remoteAxis.velocity = Mathf.Clamp( remoteAxis.velocity, remoteAxis.minVelocity * targetTolerance, remoteAxis.maxVelocity * targetTolerance );
							
							/*float targetAcceleration = ( remoteAxis.velocity - remoteAxis.actualVelocity ) / delay;
							targetAcceleration = Mathf.Clamp( targetAcceleration, remoteAxis.minAcceleration * targetTolerance, remoteAxis.maxAcceleration * targetTolerance );

							remoteAxis.velocity = remoteAxis.actualVelocity + targetAcceleration * delay;*/

							SetAxisSetpoint( axisID, 0.0f, 0.0f, 1.0f );

							Debug.Log( string.Format( "New position: {0} + {1} * ({2} + {3}) = {4}", receivedPosition, receivedVelocity, delay, updateTime, remoteAxis.position ) );

							inputLog.WriteLine( "{0} {1} {2} {3} {4} {5} {6} {7}", remoteAxis.motionTime, delay, updateTime, 
							                                                       receivedPosition, receivedVelocity, remoteAxis.position, remoteAxis.velocity, remoteAxis.actualPosition );
						}
						catch( Exception e )
						{
							Debug.Log( e.ToString() );
						}

						//Debug.Log( "InputManager: Receiving input from axis: " + axisMessage );
					}
				}
			}

			yield return new WaitForFixedUpdate();
		}
	}
	
	public static float GetAbsoluteAxisSpeed( string axisID )
	{
		if( mouseAxisValues.ContainsKey( axisID ) ) return mouseAxisValues[ axisID ].velocity;
		else if( keyboardAxisValues.ContainsKey( axisID ) )	return keyboardAxisValues[ axisID ].velocity;
		else if( remoteAxisValues.ContainsKey( axisID ) )
		{
			AxisData remoteAxis = remoteAxisValues[ axisID ];

			float deltaTime = Time.realtimeSinceStartup - remoteAxis.motionTime;

			if( remoteAxis.position > remoteAxis.maxPosition * remoteAxis.errorTolerance || remoteAxis.position < remoteAxis.minPosition * remoteAxis.errorTolerance )
				remoteAxis.velocity -= 0.1f * remoteAxis.velocity * deltaTime;

			//remoteAxis.actualPosition += remoteAxis.velocity * deltaTime;

			try
			{
				trajectoryLog.WriteLine( "{0} {1} {2}", Time.realtimeSinceStartup, remoteAxis.actualPosition, remoteAxis.velocity );
			}
			catch( Exception e )
			{
				Debug.Log( e.ToString() );
			}

			return remoteAxis.velocity;
		}
		else
			return 0.0f;
	}

	public static float GetNormalizedAxisSpeed( string axisID )
	{
		float absoluteSpeed = GetAbsoluteAxisSpeed( axisID );

		if( mouseAxisValues.ContainsKey( axisID ) )
			return ( 2 * absoluteSpeed / mouseAxisValues[ axisID ].range );
		else if( keyboardAxisValues.ContainsKey( axisID ) )
			return ( 2 * absoluteSpeed / keyboardAxisValues[ axisID ].range );
		else if( remoteAxisValues.ContainsKey( axisID ) )
			return ( 2 * absoluteSpeed / remoteAxisValues[ axisID ].range );

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

	public static void SetAxisAbsoluteFeedback( string axisID, float actualPosition, float actualVelocity )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			remoteAxisValues[ axisID ].actualVelocity = actualVelocity;
			
			remoteAxisValues[ axisID ].actualPosition = actualPosition;
			
			//Debug.Log( "Feedback values updated for " + axisID + ": " + remoteAxisValues[ axisID ].actualPosition + " " + remoteAxisValues[ axisID ].actualVelocity );
		}
	}

	public static void SetAxisSetpoint( string axisID, float setpointPosition, float setpointVelocity, float setpointReachTime )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			string axisMessage = string.Format( "{0} {1} {2}|{3} {4} {5} {6}", serverDispatchTime, clientReceiveTime, Time.realtimeSinceStartup, 
			                                                                   axisID, setpointPosition, setpointVelocity, setpointReachTime );

			//Debug.Log( "InputManager: Sending input setpoint: " + axisMessage );
			ConnectionManager.AxisClient.SendString( axisMessage );
		}
	}

	void OnDestroy()
	{
		inputLog.Close();
		trajectoryLog.Close();
		//ConnectionManager.AxisClient.Disconnect();
	}
}

