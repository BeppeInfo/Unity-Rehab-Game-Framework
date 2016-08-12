using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	public const string GAME_SERVER_ID = "Game Server";
	//public const string GAME_NAME_ID = "Game Name";

	public const string NETWORK_IDENTIFIER_ID = "Network ID";

	public Collider mapBoundingBox;
	public static Vector3 mapScale;
	public static Vector3 inverseMapScale;

	private static string networkID = "";

	public List<string> localPlayers = new List<string>();
	public List<string> remotePlayers = new List<string>();

	protected class ObjectData
	{
		public NetworkPlayer handle = null;
		public Vector3 initialPosition, initialRotation;
		public Vector3 globalPosition, globalRotation;
		public Vector3 mapPosition, mapRotation;
	}

	protected Dictionary<string, ObjectData> activeObjects = new Dictionary<string, ObjectData>();

	// Use this for initialization
	protected virtual void Start()
	{
		Cursor.visible = false;

		mapScale = mapBoundingBox.bounds.size;
		inverseMapScale = new Vector3( 1 / mapScale.x, 1 / mapScale.y, 1 / mapScale.z );

		foreach( NetworkPlayer player in FindObjectsOfType<NetworkPlayer>() )
		{
			string playerName = player.gameObject.name;
			Debug.Log( "Found NetworkPlayer: " + playerName );
			localPlayers.Add( playerName );
			activeObjects[ playerName ] = new ObjectData();
			activeObjects[ playerName ].handle = player;
			activeObjects[ playerName ].initialPosition = player.GetComponent<Rigidbody>().position;
			activeObjects[ playerName ].initialRotation = player.GetComponent<Rigidbody>().rotation.eulerAngles;

			Debug.Log( "Position: " + player.transform.position.ToString() );
		}

		if( PlayerPrefs.HasKey( GAME_SERVER_ID ) )
		{
			networkID = PlayerPrefs.GetString( NETWORK_IDENTIFIER_ID, "" );

			string gameInfo = PlayerPrefs.GetString( GAME_SERVER_ID );

			string gameHost = PlayerPrefs.GetString( ConnectionManager.GAME_SERVER_HOST_ID, ConnectionManager.LOCAL_SERVER_HOST ).Trim();
			int gameServerPort = 0;
			if( int.TryParse( gameInfo.Substring( gameInfo.LastIndexOf( ':' ) + 1 ), out gameServerPort ) )
			{
				ConnectionManager.GameClient.Connect( gameHost, gameServerPort );				
				StartCoroutine( UpdateServer() );
				StartCoroutine( UpdateRemote() );
			}
		}
	}

	protected virtual void Update()
	{
		if( Input.GetKey( KeyCode.Escape ) )
			Application.LoadLevel( "Play Game" );
	}
	
	protected virtual void FixedUpdate()
	{
		/*Vector3 playerLocalPosition, playerLocalRotation;
		Vector3 playerCurrentSpeed, playerNewSpeed, playerDeltaSpeed;
		Vector3 playerCurrentAngularSpeed, playerNewAngularSpeed, playerDeltaAngularSpeed;

		foreach( string playerName in localPlayers )
		{
			ObjectData playerData = activeObjects[ playerName ];
			NetworkPlayer player = playerData.handle;

			playerCurrentSpeed = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().velocity );
			playerNewSpeed = Vector3.Scale( player.normalizedSpeed, mapScale );
			playerDeltaSpeed = new Vector3( 
			                     ( float.IsNaN( playerNewSpeed.x ) ) ? 0.0f : playerNewSpeed.x - playerCurrentSpeed.x,
			                     ( float.IsNaN( playerNewSpeed.y ) ) ? 0.0f : playerNewSpeed.y - playerCurrentSpeed.y,
			                     ( float.IsNaN( playerNewSpeed.z ) ) ? 0.0f : playerNewSpeed.z - playerCurrentSpeed.z );

			playerCurrentAngularSpeed = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().angularVelocity );
			playerNewAngularSpeed = player.normalizedAngularSpeed * 180.0f;
			playerDeltaAngularSpeed = new Vector3( 
			                            ( float.IsNaN( playerNewAngularSpeed.x ) ) ? 0.0f : playerNewAngularSpeed.x - playerCurrentAngularSpeed.x,
			                            ( float.IsNaN( playerNewAngularSpeed.y ) ) ? 0.0f : playerNewAngularSpeed.y - playerCurrentAngularSpeed.y,
			                            ( float.IsNaN( playerNewAngularSpeed.z ) ) ? 0.0f : playerNewAngularSpeed.z - playerCurrentAngularSpeed.z );

			player.GetComponent<Rigidbody>().AddRelativeForce( playerDeltaSpeed, ForceMode.VelocityChange );
			player.GetComponent<Rigidbody>().AddRelativeTorque( playerDeltaAngularSpeed, ForceMode.VelocityChange );

			playerData.globalPosition = player.GetComponent<Rigidbody>().position;
			playerData.globalRotation = player.GetComponent<Rigidbody>().rotation.eulerAngles;

			playerLocalPosition = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().position - playerData.initialPosition );
			playerLocalRotation = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().rotation.eulerAngles - playerData.initialRotation );

			playerCurrentSpeed = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().velocity );
			playerCurrentAngularSpeed = player.transform.InverseTransformDirection( player.GetComponent<Rigidbody>().angularVelocity );

			Debug.Log( "GameManager: " + playerName + " speed: " + player.normalizedPosition + 
			          " -> " + playerLocalPosition.ToString() + 
			          " -> " + Vector3.Scale( playerLocalPosition, inverseMapScale ).ToString() );

			player.FeedBack( Vector3.Scale( playerLocalPosition, inverseMapScale ), playerLocalRotation / 180.0f,
			                Vector3.Scale( playerCurrentSpeed, inverseMapScale ), playerCurrentAngularSpeed / 180.0f );
		}*/
	}

	IEnumerator UpdateServer()
	{
		while( Application.isPlaying )
		{
			string localMessage = "Game Position";
			string positionData;
			foreach( string playerName in localPlayers )
			{
				ObjectData playerData = activeObjects[ playerName ];

				if( networkID == "" )
					ConnectionManager.GameClient.SendString( "Game Request NetworkID" );
				else if( ( playerData.mapRotation - playerData.globalRotation ).magnitude > 10.0f ||
					( playerData.mapPosition - Vector3.Scale( playerData.globalPosition, inverseMapScale ) ).magnitude > 0.1f )
				{
					playerData.mapPosition = Vector3.Scale( playerData.globalPosition, inverseMapScale );
					playerData.mapRotation = playerData.globalRotation;

					string playerNetworkName = string.Format( "{0}[{1}]", playerName, networkID );
					positionData = string.Format( ":{0}:{1}:{2}:{3}:{4}:{5}:{6}", playerNetworkName,
					                             playerData.mapPosition.x, playerData.mapPosition.y, playerData.mapPosition.z, 
					                             playerData.mapRotation.x, playerData.mapRotation.y, playerData.mapRotation.z );

					if( ( localMessage + positionData ).Length > NetworkClient.BUFFER_SIZE )
					{
						ConnectionManager.GameClient.SendString( localMessage );
						localMessage = "Game Position";
					}

					localMessage += positionData;
				}
			}

			if( localMessage != "Game Position" )
				ConnectionManager.GameClient.SendString( localMessage );

			yield return new WaitForSeconds( 0.1f );
		}
	}

	IEnumerator UpdateRemote()
	{
		Vector3 playerPosition;
		Vector3 playerRotation;

		string[] remoteData;
		while( Application.isPlaying )
		{
			if( networkID == "" )
			{
				remoteData = ConnectionManager.GameClient.QueryData( "Game NetworkID" );
				int networkNumber = 0;
				if( remoteData.Length >= 1 ) 
				{
					int.TryParse( remoteData[ 0 ], out networkNumber );
					networkID = networkNumber.ToString();
				}
			}

			Debug.Log( "UpdateRemote: Network ID: " + networkID.ToString() );

			remoteData = ConnectionManager.GameClient.QueryData( "Game Position" );
			if( remoteData.Length >= 7 )
			{
				NetworkPlayer player;
				string playerName = remoteData[ 0 ];

				playerPosition = new Vector3( float.Parse( remoteData[ 1 ] ) * mapScale.x, 
				                             float.Parse( remoteData[ 2 ] ) * mapScale.y, float.Parse( remoteData[ 3 ] ) * mapScale.z );

				playerRotation = new Vector3( float.Parse( remoteData[ 4 ] ), 
											float.Parse( remoteData[ 5 ] ), float.Parse( remoteData[ 6 ] ) );

				if( remotePlayers.Contains( playerName ) )
				{
					player = activeObjects[ playerName ].handle;

					//player.rigidbody.MovePosition( playerPosition - player.transform.position );
					player.transform.Translate( playerPosition - player.transform.position );
					//player.rigidbody.MoveRotation( Quaternion.Euler( playerRotation - player.transform.rotation.eulerAngles ) );
					player.transform.Rotate( playerRotation - player.transform.rotation.eulerAngles );
				}
				else
				{
					foreach( string modelName in localPlayers )
					{
						if( playerName.Contains( modelName ) )
						{
							player = (NetworkPlayer) Instantiate( activeObjects[ modelName ].handle, playerPosition, Quaternion.Euler( playerRotation ) );
							player.GetComponent<NetworkPlayer>().enabled = false;
							remotePlayers.Add( playerName );
							activeObjects[ playerName ] = new ObjectData();
							activeObjects[ playerName ].handle = player;
							activeObjects[ playerName ].initialPosition = player.transform.position;
							activeObjects[ playerName ].initialRotation = player.transform.rotation.eulerAngles;
						}
					}
				}
			}

			yield return null;
		}
	}

	protected virtual void OnDestroy()
	{
		Cursor.visible = true;
		//ConnectionManager.GameClient.Disconnect();
	}
}

