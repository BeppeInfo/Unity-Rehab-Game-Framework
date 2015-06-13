using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ConnectionManager
{
	public const string GAME_SERVER_HOST_ID = "Game Server Host";
	public const string GAME_SERVER_INFO_PORT_ID = "Game Info Port";
	public const string GAME_SERVER_DATA_PORT_ID = "Game Data Port";

	public const string AXIS_SERVER_HOST_ID = "Axis Server Host";
	public const string AXIS_SERVER_INFO_PORT_ID = "Axis Info Port";
	public const string AXIS_SERVER_DATA_PORT_ID = "Axis Data Port";

	public const string LOCAL_SERVER_HOST = "localhost";
	public const int DEFAULT_SERVER_PORT = 50000;

	private static NetworkClientTCP infoClient = null;
	public static NetworkClientTCP InfoClient
	{
		get 
		{
			if( infoClient == null )
				infoClient = new NetworkClientTCP();

			return infoClient;
		}
	}

	private static NetworkClientUDP gameClient = null;
	public static NetworkClientUDP GameClient
	{
		get 
		{
			if( gameClient == null )
				gameClient = new NetworkClientUDP();

			return gameClient;
		}
	}

	private static NetworkClientUDP axisClient = null;
	public static NetworkClientUDP AxisClient
	{
		get 
		{
			if( axisClient == null )
				axisClient = new NetworkClientUDP();
			
			return axisClient;
		}
	}
}

