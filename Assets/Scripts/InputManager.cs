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

	private class InputAxis 
	{
		public string ID = "";
		public float position = 0.0f, velocity = 0.0f, acceleration = 0.0f;
		public float maxPosition = 0.0f, maxVelocity = 0.0f, maxAcceleration = 0.0f;
		public float minPosition = 0.0f, minVelocity = 0.0f, minAcceleration = 0.0f;
		public float range = 1.0f;
		public float errorTolerance = 0.0f;
		public float motionTime = Time.realtimeSinceStartup;
		
		public float feedbackPosition = 0.0f;
		
		public InputAxis( string axisID = "", float initialPosition = 0.0f )
		{
			ID = axisID;
			position = initialPosition;
		}

		public virtual void Update( float updateTime )
		{
			position += velocity * updateTime;
		}
	}

	private class MouseInputAxis : InputAxis
	{
		public MouseInputAxis( string axisID = "Mouse X", float initialPosition = 0.0f ) : base( axisID, initialPosition ) { }

		public override void Update( float updateTime )
		{
			velocity = Input.GetAxis( ID ) / updateTime;
			base.Update( updateTime );
		}
	}

	private class KeyboardInputAxis : InputAxis
	{
		public KeyboardInputAxis( string axisID = "Horizontal", float initialPosition = 0.0f ) : base( axisID, initialPosition ) { }

		public override void Update( float updateTime )
		{
			velocity = Input.GetAxis( ID );
			base.Update( updateTime );
		}
	}

	private static List<InputAxis> inputAxesValues = new List<InputAxis>();

	void Start()
	{
		inputAxesValues.Add( new MouseInputAxis( "Mouse X", Input.mousePosition.x ) );
		inputAxesValues.Add( new MouseInputAxis( "Mouse Y", Input.mousePosition.y ) );
		inputAxesValues.Add( new KeyboardInputAxis( "Horizontal" ) );
		inputAxesValues.Add( new KeyboardInputAxis( "Vertical" ) );

		axisServerHost = PlayerPrefs.GetString( ConnectionManager.AXIS_SERVER_HOST_ID, ConnectionManager.LOCAL_SERVER_HOST );
		//axisServerPort = PlayerPrefs.GetInt( ConnectionManager.AXIS_SERVER_DATA_PORT_ID, DEFAULT_AXIS_SERVER_PORT );
			
		ConnectionManager.AxisClient.Connect( /*axisServerHost*/"169.254.110.158", /*axisServerPort*/DEFAULT_AXIS_SERVER_PORT );

		StartCoroutine( UpdateAxisValues() );
	}

	public static void AddRemoteAxis( string axisID, float initialPosition )
	{
		if( !inputAxesValues.Exists( axis => axis.ID == axisID ) )
		{
			inputAxesValues.Add( new InputAxis( axisID, initialPosition ) );
			SetAxisSetpoint( axisID, 0.0f, 0.0f, 1.0f );
		}
	}

	public static void RemoveRemoteAxis( string axisID )
	{
		inputAxesValues.Remove( inputAxesValues.Find( axis => axis.ID == axisID ) );
	}

	private static IEnumerator UpdateAxisValues()
	{
		while( Application.isPlaying )
		{
			foreach( InputAxis inputAxis in inputAxesValues )
				inputAxis.Update( Time.fixedDeltaTime );

			string[] messageData = ConnectionManager.AxisClient.ReceiveString().Split( '|' );
			if( messageData.Length >= 2 )
			{
				string[] timeData = messageData[ 0 ].Split( ' ' );

				// Network latency calculation based on the NTP protocol
				float clientDispatchTime = float.Parse( timeData[ 0 ] );
				float serverReceiveTime = float.Parse( timeData[ 1 ] );
				serverDispatchTime = float.Parse( timeData[ 2 ] );
				clientReceiveTime = Time.realtimeSinceStartup;
				
				float delay = ( ( clientReceiveTime - clientDispatchTime ) - ( serverReceiveTime - serverDispatchTime ) ) / 2.0f + 0.1f;
				if( delay < 0.0f ) delay = 0.0f;
				
				Debug.Log( string.Format( "latency: ( ({0} - {1}) - ({2} - {3}) ) / 2 = {4}", clientReceiveTime, clientDispatchTime, serverReceiveTime, serverDispatchTime, delay ) );
				
				string[] axesMessages = messageData[ 1 ].Split( ':' );
				foreach( string axisMessage in axesMessages )
				{
					string[] axisData = axisMessage.Trim().Split( ' ' );

					string axisID = axisData[ 0 ];

					if( inputAxesValues.Exists( axis => axis.ID == axisID ) )
					{
						InputAxis remoteAxis = inputAxesValues.Find( axis => axis.ID == axisID );

						try
						{
							float receivedPosition = float.Parse( axisData[ 1 ] );
							float receivedVelocity = float.Parse( axisData[ 2 ] );
							float receivedAcceleration = float.Parse( axisData[ 3 ] );

							/*float receivedTorque = float.Parse( axisData[ 4 ] );
							float receivedStiffness = float.Parse( axisData[ 5 ] );
							float receivedDamping = float.Parse( axisData[ 6 ] );*/

							remoteAxis.errorTolerance = 1.0f + Mathf.Abs( ( receivedPosition - remoteAxis.position ) / remoteAxis.range );

							float updateTime = serverDispatchTime - remoteAxis.motionTime;
							remoteAxis.motionTime = serverDispatchTime;

							if( updateTime > 0.0f ) 
							{
								remoteAxis.position = receivedPosition + receivedVelocity * delay + receivedAcceleration * delay * delay / 2.0f;

								float predictedPosition = remoteAxis.position + ( receivedVelocity + receivedAcceleration * delay ) * updateTime;

								remoteAxis.velocity = ( predictedPosition - remoteAxis.feedbackPosition ) / updateTime;

								if( float.IsNaN( remoteAxis.velocity ) || float.IsInfinity( remoteAxis.velocity ) ) remoteAxis.velocity = 0.0f;

								//remoteAxis.velocity = Mathf.Clamp( remoteAxis.velocity, remoteAxis.minVelocity * targetTolerance, remoteAxis.maxVelocity * targetTolerance );
								
								/*float targetAcceleration = ( remoteAxis.velocity - remoteAxis.actualVelocity ) / delay;
								targetAcceleration = Mathf.Clamp( targetAcceleration, remoteAxis.minAcceleration * targetTolerance, remoteAxis.maxAcceleration * targetTolerance );

								remoteAxis.velocity = remoteAxis.actualVelocity + targetAcceleration * delay;*/

								SetAxisSetpoint( axisID, 0.0f, 0.0f, 1.0f );

								//Debug.Log( string.Format( "New position: {0} + {1} * ({2} + {3}) = {4}", receivedPosition, receivedVelocity, delay, updateTime, remoteAxis.position ) );

								inputLog.WriteLine( "{0} {1} {2} {3} {4} {5} {6} {7}", remoteAxis.motionTime, delay, updateTime, 
																	                   receivedPosition, receivedVelocity,
																	                   remoteAxis.position, remoteAxis.velocity, remoteAxis.feedbackPosition );
							}
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
	
	public static float GetAxisAbsoluteSpeed( string axisID )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) )
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );

			if( inputAxis.ID == "0" )
			{
				try
				{
					trajectoryLog.WriteLine( "{0} {1} {2}", Time.realtimeSinceStartup, inputAxis.feedbackPosition, inputAxis.velocity );
				}
				catch( Exception e )
				{
					Debug.Log( e.ToString() );
				}
			}

			return inputAxis.velocity;
		}
			
		return 0.0f;
	}

	public static float GetAxisNormalizedSpeed( string axisID )
	{
		float absoluteSpeed = GetAxisAbsoluteSpeed( axisID );

		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );

			return ( 2 * absoluteSpeed / inputAxis.range );
		}

		return 0.0f;
	}

	public static float GetAxisAbsolutePosition( string axisID )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			return inputAxis.position;
		}

		return 0.0f;
	}

	public static float GetAxisNormalizedPosition( string axisID )
	{
		float absolutePosition = GetAxisAbsolutePosition( axisID );

		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			return ( 2 * ( absolutePosition - inputAxis.minPosition ) / inputAxis.range - 1.0f );
		}
		
		return 0.0f;
	}

	public static void CalibrateAxisSpeed( string axisID )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			if( inputAxis.velocity > inputAxis.maxVelocity ) inputAxis.maxVelocity = inputAxis.velocity;
			else if( inputAxis.velocity < inputAxis.minVelocity ) inputAxis.minVelocity = inputAxis.velocity;

			if( inputAxis.acceleration > inputAxis.maxAcceleration ) inputAxis.maxAcceleration = inputAxis.acceleration;
			else if( inputAxis.acceleration < inputAxis.minAcceleration ) inputAxis.minAcceleration = inputAxis.acceleration;
		}
	}

	public static void CalibrateAxisPosition( string axisID, float minPosition, float maxPosition )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			inputAxis.maxPosition = maxPosition;
			inputAxis.minPosition = minPosition;
			inputAxis.range = ( maxPosition - minPosition != 0.0f ) ? maxPosition - minPosition : 1.0f;
		}
	}

	public static void SetAxisFeedback( string axisID, float actualPosition, float actualVelocity )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			inputAxis.feedbackPosition = ( ( actualPosition + 1.0f ) * inputAxis.range / 2.0f ) + inputAxis.minPosition;
		
			Debug.Log( "Feedback values updated for " + axisID + ": " + inputAxis.feedbackPosition );
		}
	}

	public static void SetAxisAbsoluteFeedback( string axisID, float actualPosition, float actualVelocity )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
		{
			InputAxis inputAxis = inputAxesValues.Find( axis => axis.ID == axisID );
			
			inputAxis.feedbackPosition = actualPosition;
			
			//Debug.Log( "Feedback values updated for " + axisID + ": " + remoteAxisValues[ axisID ].actualPosition + " " + remoteAxisValues[ axisID ].actualVelocity );
		}
	}

	public static void SetAxisSetpoint( string axisID, float setpointPosition, float setpointVelocity, float setpointReachTime )
	{
		if( inputAxesValues.Exists( axis => axis.ID == axisID ) ) 
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

