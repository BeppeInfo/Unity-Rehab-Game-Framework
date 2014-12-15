using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RemoteInfo : MonoBehaviour
{
	public string infoHostId;

	public string infoType;
	public string valueType;

	public void RefreshServerInfo()
	{
		Debug.Log( "Atualizando Informações" );
		ConnectionManager.InfoClient.Connect( PlayerPrefs.GetString( infoHostId, ConnectionManager.LOCAL_SERVER_HOST ), 
												PlayerPrefs.GetInt( ConnectionManager.SERVER_PORT_ID, ConnectionManager.DEFAULT_SERVER_PORT ) );
		ConnectionManager.InfoClient.SendString( infoType + " Info" );

		SelectionList.stringLists[ valueType ] = new List<string>();

		StartCoroutine( ReceiveInfo() );
	}

	IEnumerator ReceiveInfo()
	{
		bool receiving = false;
		int failedMessages = 0;

		while( Application.isPlaying ) 
		{
			string info = ConnectionManager.InfoClient.ReceiveString().Trim();

			if( info == "" ) 
			{
				failedMessages++;
				if( failedMessages > 1000 ) 
				{
					Debug.Log( "Failed to Receive Server Info" );
					break;
				}
			}
			else if( info.Contains( infoType + " Info Begin" ) ) 
				receiving = true;
			else 
			{
				if( info.Contains( infoType + " Info End" ) ) 
					break;

				if( receiving ) 
				{
					Debug.Log( "Recebendo Informaçoes" );
					SelectionList.stringLists[ valueType ].Add( info );
				}
			}

			yield return null;
		}
	}

	void OnDestroy()
	{
		//ConnectionManager.InfoClient.Disconnect();
	}
}

