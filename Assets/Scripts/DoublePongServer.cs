using UnityEngine;
using System.Collections;
using System.IO;

public class DoublePongServer : GameServer 
{
	public BallController ball;
	public PositionMasterController[] bats = new PositionMasterController[ 4 ];

	public override void Start()
	{
		base.Start();

		connection.Connect();

		foreach( PositionMasterController bat in bats )
			bat.enabled = true;

		StartCoroutine( WaitClients() );
	}

	IEnumerator WaitClients()
	{
		while( connection.GetClientsNumber() < 2 ) 
			yield return new WaitForFixedUpdate();

		ball.enabled = true;
	}

//	IEnumerator RegisterValues()
//	{
//		float initialLogTime = 0.0f;
//
//		// Set log file names
//		StreamWriter verticalLog = new StreamWriter( "./vertical_server.log", false );
//		StreamWriter horizontalLog = new StreamWriter( "./horizontal_server.log", false );
//		StreamWriter ballLog = new StreamWriter( "./ball_server.log", false );
//		StreamWriter networkLog = new StreamWriter( "./network_server.log", false );
//
//		while( Application.isPlaying )
//		{
//			ConnectionInfo currentConnectionInfo = gameClient.GetConnectionInfo();
//
//			lazyScoreText.text =  string.Format( "Client: {0} Socket: {1} Connection: {2} Channel: {3}\nSend: {4,2}KB/s Receive: {5,2}KB/s RTT: {6,3}ms Lost Packets: {7}", 
//				clientID, currentConnectionInfo.socketID, currentConnectionInfo.connectionID, currentConnectionInfo.channel,
//				currentConnectionInfo.sendRate, currentConnectionInfo.receiveRate, currentConnectionInfo.rtt, currentConnectionInfo.lostPackets );
//
//			if( ball.transform.position != lastBallPosition )
//			{
//				if( initialLogTime == 0.0f ) initialLogTime = Time.realtimeSinceStartup;
//
//				float sampleTime = Time.realtimeSinceStartup - initialLogTime;
//
//				verticalLog.WriteLine( string.Format( "{0}\t{1}", sampleTime, verticalBats[ 0 ].transform.position.z ) );
//				horizontalLog.WriteLine( string.Format( "{0}\t{1}", sampleTime, horizontalBats[ 0 ].transform.position.x ) );
//				ballLog.WriteLine( string.Format( "{0}\t{1}\t{2}", sampleTime, ball.transform.position.x, ball.transform.position.z ) );
//				networkLog.WriteLine( string.Format( "{0}\t{1}", sampleTime, currentConnectionInfo.rtt ) );
//			}
//
//			yield return new WaitForFixedUpdate();
//		}
//
//		verticalLog.Flush();
//		horizontalLog.Flush();
//		ballLog.Flush();
//		networkLog.Flush();
//	}

	public void ResetBall()
	{
		ball.enabled = false;
		ball.enabled = true;
	}
}
