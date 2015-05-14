using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class AxisData 
{
	public float position = 0.0f, velocity = 0.0f;
	public float motionTime = Time.realtimeSinceStartup;

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
					float.TryParse( axisData[ 1 ], out remoteAxisValues[ axisID ].position );
					float.TryParse( axisData[ 2 ], out remoteAxisValues[ axisID ].velocity );
					remoteAxisValues[ axisID ].motionTime = Time.realtimeSinceStartup - remoteAxisValues[ axisID ].motionTime;

					Debug.Log( "InputManager: Receiving input from axis: " + axisMessage );
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
			return remoteAxisValues[ axisID ].velocity;
		else
			return 0.0;
	}

	public static void SetAxisFeedback( string axisID, float[] setpoints )
	{
		if( remoteAxisValues.ContainsKey( axisID ) )
		{
			StringBuilder axisMessageBuilder = new StringBuilder( axisID );

			foreach( float setpoint in setpoints )
			{
				axisMessageBuilder.Append( ' ' );
				axisMessageBuilder.Append( setpoint );
			}

			Debug.Log( "InputManager: Sending Feedback: " + axisMessageBuilder.ToString() );
			ConnectionManager.AxisClient.SendString( axisMessageBuilder.ToString() );
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.AxisClient.Disconnect();
	}
}

