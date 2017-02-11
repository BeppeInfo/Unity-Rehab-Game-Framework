using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;

public class DoublePongClient : GameClient 
{
	public PositionSlaveController ball;
	private Vector3 lastBallPosition;

	public Controller[] verticalBats = new Controller[ 2 ];
	public Controller[] horizontalBats = new Controller[ 2 ];
	private PositionPlayerController[] playerBats = new PositionPlayerController[ 2 ];

	private int targetMask;

	private float error = 0.0f;

	private int clientID = -1;

	void Awake()
	{
		targetMask = LayerMask.GetMask( "Target" );

		playerBats[ 0 ] = verticalBats[ 0 ].GetComponent<PositionPlayerController>();
		playerBats[ 1 ] = verticalBats[ 1 ].GetComponent<PositionPlayerController>();

		sliderHandle = setpointSlider.handleRect.GetComponent<Image>();

		lastBallPosition = ball.transform.position;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();

		Vector3 impactPoint = ball.FindImpactPoint( targetMask );

		playerBats[ 1 ].ControlPosition( impactPoint, out error );
		float setpoint = playerBats[ 0 ].ControlPosition( impactPoint, out error );

		setpointSlider.value = setpoint;

		if( error >= 2 * PositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.red;
		else if( error >= PositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.yellow;
		else sliderHandle.color = Color.green;
	}

	IEnumerator RegisterValues()
	{
		// Set log file names
		StreamWriter verticalLog = new StreamWriter( "./vertical" + clientID.ToString() + ".log", false );
		StreamWriter horizontalLog = new StreamWriter( "./horizontal" + clientID.ToString() + ".log", false );
		StreamWriter ballLog = new StreamWriter( "./ball" + clientID.ToString() + ".log", false );
		StreamWriter networkLog = new StreamWriter( "./network" + clientID.ToString() + ".log", false );

		while( Application.isPlaying )
		{
			ConnectionInfo currentConnectionInfo = connection.GetCurrentInfo();

			infoText.text =  string.Format( "Client: {0} Sent: {1} Received: {2}\nLost Packets: {3} RTT: {4,3}ms", clientID,
											currentConnectionInfo.sentPackets, currentConnectionInfo.receivedPackets, currentConnectionInfo.lostPackets, currentConnectionInfo.rtt );

			if( ball.transform.position != lastBallPosition )
			{
				double gameTime = DateTime.Now.TimeOfDay.TotalSeconds;
				verticalLog.WriteLine( string.Format( "{0}\t{1}", gameTime, verticalBats[ 0 ].transform.position.z ) );
				horizontalLog.WriteLine( string.Format( "{0}\t{1}", gameTime, horizontalBats[ 0 ].transform.position.x ) );
				ballLog.WriteLine( string.Format( "{0}\t{1}\t{2}", gameTime, ball.transform.position.x, ball.transform.position.z ) );
				networkLog.WriteLine( string.Format( "{0}\t{1}", gameTime, currentConnectionInfo.rtt / 2.0f ) );
			}

			yield return new WaitForFixedUpdate();
		}

		verticalLog.Flush();
		horizontalLog.Flush();
		ballLog.Flush();
		networkLog.Flush();
	}

	IEnumerator HandleConnection()
	{
		while( clientID == -1 && Application.isPlaying )
		{
			clientID = connection.GetID();
			yield return new WaitForSeconds( 0.1f );
		}

		if( clientID == 0 ) 
		{
			verticalBats[ 0 ].GetComponent<PositionPlayerController>().enabled = true;
			verticalBats[ 1 ].GetComponent<PositionPlayerController>().enabled = true;
			horizontalBats[ 0 ].GetComponent<PositionSlaveController>().enabled = true;
			horizontalBats[ 1 ].GetComponent<PositionSlaveController>().enabled = true;
			playerBats[ 0 ] = verticalBats[ 0 ].GetComponent<PositionPlayerController>();
			playerBats[ 1 ] = verticalBats[ 1 ].GetComponent<PositionPlayerController>();
		} 
		else if( clientID == 1 ) 
		{
			horizontalBats[ 0 ].GetComponent<PositionPlayerController>().enabled = true;
			horizontalBats[ 1 ].GetComponent<PositionPlayerController>().enabled = true;
			verticalBats[ 0 ].GetComponent<PositionSlaveController>().enabled = true;
			verticalBats[ 1 ].GetComponent<PositionSlaveController>().enabled = true;
			playerBats[ 0 ] = horizontalBats[ 0 ].GetComponent<PositionPlayerController>();
			playerBats[ 1 ] = horizontalBats[ 1 ].GetComponent<PositionPlayerController>();
			gameCamera.transform.RotateAround( transform.position, Vector3.up, 90f );
		}

		ball.enabled = true;
		StartCoroutine( RegisterValues() );
	}

	public void StartPlay()
	{
		if( clientID == -1 )
		{
			connection.Connect();
			StartCoroutine( HandleConnection() );
		}
	}

	public void StopPlay()
	{
		ball.enabled = false;
		verticalBats[ 0 ].enabled = verticalBats[ 1 ].enabled = false;
		horizontalBats[ 0 ].enabled = horizontalBats[ 1 ].enabled = false;
	}
}
