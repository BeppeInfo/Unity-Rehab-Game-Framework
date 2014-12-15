using UnityEngine;
using System.Collections;

[RequireComponent( typeof(RemoteInfo) )]
public class CreateConnection : MonoBehaviour 
{
	private RemoteInfo gameInfo = null;

	// Use this for initialization
	void Start() 
	{
		gameInfo = gameObject.GetComponent<RemoteInfo>();
	}

	public void SetupServer( string serverNameId )
	{
		if( PlayerPrefs.HasKey( serverNameId ) )
		{
			string currentGameName = PlayerPrefs.GetString( serverNameId );

			ConnectionManager.InfoClient.Connect( PlayerPrefs.GetString( gameInfo.infoHostId, ConnectionManager.LOCAL_SERVER_HOST ), 
													PlayerPrefs.GetInt( ConnectionManager.SERVER_PORT_ID, ConnectionManager.DEFAULT_SERVER_PORT ) );
			ConnectionManager.InfoClient.SendString( gameInfo.infoType + " New:" + currentGameName );

			gameInfo.RefreshServerInfo();

			foreach( string gameServer in SelectionList.stringLists[ gameInfo.valueType ] )
			{
				string gameName = gameServer.Substring( 0, gameServer.LastIndexOf( ' ' ) ).Trim();

				if( currentGameName == gameName )
				{
					PlayerPrefs.SetString( gameInfo.valueType, gameServer );
					Debug.Log( "Found server " + currentGameName );
					Debug.Log( gameInfo.valueType + ": " + PlayerPrefs.GetString( gameInfo.valueType ) );
					break;
				}
			} 
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.InfoClient.Disconnect();
	}
}
