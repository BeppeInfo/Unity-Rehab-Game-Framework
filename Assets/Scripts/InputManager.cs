using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InputData 
{ 
	public float position = 0.0f, velocity = 0.0f;
	public float positionFeedback = 0.0f, velocityFeedback = 0.0f;
	public float motionTime = Time.realtimeSinceStartup;
}

public class InputManager : MonoBehaviour
{
	public static string axisServerHost;
	private static int axisServerPort;
	private static int axisClientPort;

	private static List<string> mouseAxes = new List<string>() { "Mouse X", "Mouse Y" };
	private static List<string> keyboardAxes = new List<string>() { "Horizontal", "Vertical" };

	private static List<string> remoteAxes = new List<string>();

	private static Dictionary<string, InputData> axisValues = new Dictionary<string, InputData>();

	void Start()
	{
		foreach( string axisName in mouseAxes )
			axisValues[ axisName ] = new InputData();
		foreach( string axisName in keyboardAxes )
			axisValues[ axisName ] = new InputData();

		axisValues[ "Mouse X" ].position = Input.mousePosition.x;
		axisValues[ "Mouse Y" ].position = Input.mousePosition.y;

		axisServerHost = PlayerPrefs.GetString( ConnectionManager.AXIS_SERVER_HOST_ID, ConnectionManager.LOCAL_SERVER_HOST );
		axisServerPort = PlayerPrefs.GetInt( ConnectionManager.SERVER_PORT_ID, ConnectionManager.DEFAULT_SERVER_PORT );
		axisClientPort = PlayerPrefs.GetInt( ConnectionManager.AXIS_CLIENT_PORT_ID, ConnectionManager.DEFAULT_AXIS_CLIENT_PORT );
			
		ConnectionManager.AxisClient.Connect( /*axisServerHost*/ConnectionManager.LOCAL_SERVER_HOST, axisServerPort, axisClientPort );				

		ConnectionManager.AxisClient.SendString( "Axis Connect" );

		StartCoroutine( UpdateAxisValues() );
	}

	public void UseRemoteServer( bool enabled )
	{
		if( enabled )
		{
			axisServerHost = PlayerPrefs.GetString( ConnectionManager.MASTER_SERVER_HOST_ID, ConnectionManager.LOCAL_SERVER_HOST );
			Debug.Log( "Input Manager: enabling remote axes" );
		} 
		else
		{
			axisServerHost = ConnectionManager.LOCAL_SERVER_HOST;
			Debug.Log( "Input Manager: disabling remote axes" );
		}

		PlayerPrefs.SetString( ConnectionManager.AXIS_SERVER_HOST_ID, axisServerHost );
		ConnectionManager.AxisClient.Connect( axisServerHost, axisServerPort );
	}

	private static IEnumerator UpdateAxisValues()
	{
		while( Application.isPlaying )
		{
			float elapsedTime = Time.deltaTime;

			string[] axisData = ConnectionManager.AxisClient.QueryData( "Axis Data" );
			if( axisData.Length >= 3 )
			{
				for( int axisDataOffset = 0; axisDataOffset < axisData.Length - 2; axisDataOffset += 3 )
				{
					string axisName = axisData[ axisDataOffset ];
					if( !mouseAxes.Contains( axisName ) && !keyboardAxes.Contains( axisName ) )
					{
						if( !axisValues.ContainsKey( axisName ) )
						{
							remoteAxes.Add( axisName );
							axisValues[ axisName ] = new InputData();
						}
						float.TryParse( axisData[ axisDataOffset + 1 ], out axisValues[ axisName ].position );
						float.TryParse( axisData[ axisDataOffset + 2 ], out axisValues[ axisName ].velocity );
						axisValues[ axisName ].motionTime = Time.realtimeSinceStartup - axisValues[ axisName ].motionTime;

						string axisMessage = string.Format( "{0}:{1}:{2}", axisName, axisValues[ axisName ].position, axisValues[ axisName ].velocity );
						Debug.Log( "InputManager: Receiving input from axis: " + axisMessage );

						float positionFeedback = axisValues[ axisName ].positionFeedback + axisValues[ axisName ].velocityFeedback * elapsedTime;
						Debug.Log( "InputManager: Sending Feedback: " + positionFeedback.ToString() );
						ConnectionManager.AxisClient.SendString( string.Format( "Axis Feedback:{0}:{1}:{2}", axisName, 
						                                                       (int) axisValues[ axisName ].positionFeedback, 
						                                                       (int) axisValues[ axisName ].velocityFeedback ) );
					}
				}
			}

			foreach( string axisName in mouseAxes )
			{
				axisValues[ axisName ].velocity = Input.GetAxis( axisName ) / elapsedTime;
				axisValues[ axisName ].position += axisValues[ axisName ].velocity * elapsedTime;
				axisValues[ axisName ].motionTime = elapsedTime;
			}
			foreach( string axisName in keyboardAxes )
			{
				axisValues[ axisName ].velocity = Input.GetAxis( axisName );
				axisValues[ axisName ].position += axisValues[ axisName ].velocity * elapsedTime;
				axisValues[ axisName ].motionTime = elapsedTime;
			}

			yield return null;
		}
	}

	public static InputData GetAxisValues( string axisName )
	{
		if( axisValues.ContainsKey( axisName ) )
			return axisValues[ axisName ];
		else
			return new InputData();
	}

	public static void SetAxisFeedback( string axisName, float positionFeedback, float velocityFeedback )
	{
		if( axisValues.ContainsKey( axisName ) )
		{
			axisValues[ axisName ].positionFeedback = positionFeedback;
			axisValues[ axisName ].velocityFeedback = velocityFeedback;
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.AxisClient.Disconnect();
	}
}

