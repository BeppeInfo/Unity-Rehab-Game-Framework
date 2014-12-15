using UnityEngine;
using System.Collections;

public class SceneManager : MonoBehaviour 
{
	public void ChangeScene( string fileName )
	{
		Application.LoadLevel( fileName );
	}

	public void QuitGame()
	{
		Application.Quit();

		// Editor
		//ConnectionManager.InfoClient.Disconnect();
		//ConnectionManager.GameClient.Disconnect();
		//ConnectionManager.AxisClient.Disconnect();
		//UnityEditor.EditorApplication.isPlaying = false;
	}

	void OnApplicationQuit() 
	{
		ConnectionManager.InfoClient.Disconnect();
		ConnectionManager.GameClient.Disconnect();
		ConnectionManager.AxisClient.Disconnect();
		PlayerPrefs.Save();
	}
}
