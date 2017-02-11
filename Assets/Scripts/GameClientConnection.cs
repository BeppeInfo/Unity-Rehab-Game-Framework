using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;

public struct ConnectionInfo
{
	public int socketID, connectionID, channel;
	public int sentPackets, receivedPackets, lostPackets;
	public int rtt;
}

public class GameClientConnection : GameConnection
{
	public const string SERVER_HOST_ID = "Game Server Host";

	private int connectionID;
	private int clientID = -1;

	public override void Connect()
    {
		HostTopology networkTopology = new HostTopology( connectionConfig, 1 );
		socketID = NetworkTransport.AddHost( networkTopology );

		string gameServerHost = PlayerPrefs.GetString( SERVER_HOST_ID, Configuration.DEFAULT_IP_HOST );
		Debug.Log( "Connectingo to host " + gameServerHost + " on port " + GAME_SERVER_PORT.ToString() );
		connectionID = NetworkTransport.Connect( socketID, gameServerHost, GAME_SERVER_PORT, 0, out connectionError );
		Debug.Log( string.Format( "Added host {0} and connection {1} with channels {2} and {3}", socketID, connectionID, eventChannel, dataChannel )  );
    }

	protected override void SendUpdateMessage()
	{
		//Debug.Log( string.Format( "Sending message from host {0} to connection {1} and client {2}", socketID, connectionID, dataChannel ) );
		NetworkTransport.Send( socketID, connectionID, dataChannel, outputBuffer, PACKET_SIZE, out connectionError );
	}

	protected override bool ReceiveUpdateMessage()
	{
		int remoteConnectionID, channel, receivedSize;
		if( NetworkTransport.ReceiveFromHost( socketID, out remoteConnectionID, out channel, inputBuffer, PACKET_SIZE, out receivedSize, out connectionError ) == NetworkEventType.DataEvent )
		{
			if( connectionError == (byte) NetworkError.Ok ) 
			{
				networkDelay = NetworkTransport.GetCurrentRTT( socketID, connectionID, out connectionError ) / 2000.0f;

				if( channel == eventChannel ) clientID = (int) inputBuffer[ 0 ];
				else if( channel == dataChannel ) return true;
			}
		}

		return false;
	}

	public int GetID()
	{
		return clientID;
	}

	public ConnectionInfo GetCurrentInfo()
	{
		ConnectionInfo currentConnectionInfo = new ConnectionInfo();
		currentConnectionInfo.socketID = socketID;
		currentConnectionInfo.connectionID = connectionID;
		currentConnectionInfo.channel = dataChannel;
		currentConnectionInfo.sentPackets = NetworkTransport.GetCurrentOutgoingMessageAmount();
		currentConnectionInfo.receivedPackets = NetworkTransport.GetCurrentIncomingMessageAmount();
		currentConnectionInfo.lostPackets = NetworkTransport.GetIncomingPacketLossCount( socketID, connectionID, out connectionError );
		currentConnectionInfo.rtt = NetworkTransport.GetCurrentRTT( socketID, connectionID, out connectionError );

		return currentConnectionInfo;
	}

	void OnApplicationQuit()
	{
		NetworkTransport.Disconnect( socketID, connectionID, out connectionError );
	}
}

