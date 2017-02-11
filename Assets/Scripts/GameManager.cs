using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class GameState : MonoBehaviour 
{
	public Text infoText;

	public abstract GameConnection GetConnection();

	public virtual void FixedUpdate()
	{
		GetConnection().UpdateData( Time.fixedDeltaTime );
	}
}

public abstract class GameServer : GameState 
{
	protected GameServerConnection connection = null;

	public virtual void Start()
	{
		connection = new GameServerConnection();
		Debug.Log( "Created connection " + connection.ToString() );
	}

	public override GameConnection GetConnection() { return connection; }
}

public abstract class GameClient : GameState
{
	public Camera gameCamera;

	public Text localPlayerText, remotePlayerText;

	public Slider setpointSlider;
	protected Image sliderHandle;

	protected GameClientConnection connection = null;

	public virtual void Start()
	{
		connection = new GameClientConnection();
		Debug.Log( "Created connection " + connection.ToString() );
	}

	public override GameConnection GetConnection() { return connection; }
}


public class GameManager : MonoBehaviour 
{
	public static bool isMaster = true;

	public GameClient client;
	public GameServer server;

	private static GameState game = null;

	void Start() 
	{
		if( isMaster ) game = server;
		else game = client;

		game.enabled = true;
	}

	/*void FixedUpdate()
	{
		game.GetConnection().UpdateData( Time.fixedDeltaTime );
	}*/

	/*IEnumerator UpdateNetworkData()
	{
		float networkDelay = 0.0f;

		while( Application.isPlaying )
		{
			networkDelay = connection.UpdateData( networkDelay );
			yield return new WaitForSecondsRealtime( networkDelay );
		}
	}*/

	public static GameConnection GetConnection()
	{
		return game.GetConnection();
	}

	void OnApplicationQuit()
	{
		GameConnection.Shutdown();
	}
}
