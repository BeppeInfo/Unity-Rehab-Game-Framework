using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ConnectionManager
{
	public const string MASTER_SERVER_HOST_ID = "Master Server";
	public const string SERVER_PORT_ID = "Server Port";

	public const string AXIS_SERVER_HOST_ID = "Axis Server";
	public const string AXIS_CLIENT_PORT_ID = "Axis Port";

	public const string GAME_CLIENT_PORT_ID = "Game Port";

	public const string LOCAL_SERVER_HOST = "localhost";

	public const int DEFAULT_SERVER_PORT = 10000;
	public const int DEFAULT_AXIS_CLIENT_PORT = 11000;
	public const int DEFAULT_GAME_CLIENT_PORT = 5001;

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

