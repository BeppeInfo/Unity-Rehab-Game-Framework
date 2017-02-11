using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;

public class BoxClashClient : GameClient 
{
	public Controller[] boxes = new Controller[ 2 ];

	private ForcePlayerController player = null;

	//private float error = 0.0f;

	private int clientID = -1;

	void Awake()
	{
		player = boxes[ 0 ].GetComponent<ForcePlayerController>();

		sliderHandle = setpointSlider.handleRect.GetComponent<Image>();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();

		//setpointSlider.value = setpoint;

		//if( error >= 2 * PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.red;
		//else if( error >= PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.yellow;
		//else sliderHandle.color = Color.green;

		Rigidbody playerBody = player.GetComponent<Rigidbody>();
		localPlayerText.text = string.Format( "Input:{0:F3}N\nInteract:{1:F3}N", player.GetInputForce(), player.GetInteractionForce() );
		remotePlayerText.text = string.Format( "Position:{0:F3}\nVelocity:{1:F3}", playerBody.position.z, playerBody.velocity.z );
	}

	IEnumerator RegisterValues()
	{
		// Set log file names
		StreamWriter boxLog = new StreamWriter( "./box" + clientID.ToString() + ".log", false );
		StreamWriter networkLog = new StreamWriter( "./network" + clientID.ToString() + ".log", false );

		while( Application.isPlaying )
		{
			ConnectionInfo currentConnectionInfo = connection.GetCurrentInfo();

			infoText.text =  string.Format( "Client: {0} Sent: {1} Received: {2} Lost Packets: {3} RTT: {4,3}ms", clientID,
				currentConnectionInfo.sentPackets, currentConnectionInfo.receivedPackets, currentConnectionInfo.lostPackets, currentConnectionInfo.rtt );

			double gameTime = DateTime.Now.TimeOfDay.TotalSeconds;
			Rigidbody playerBody = player.GetComponent<Rigidbody>();
			boxLog.WriteLine( string.Format( "{0}\t{1}\t{2}\t{3}\t{4}", gameTime, player.GetInputForce(), player.GetInteractionForce(), playerBody.position.z, playerBody.velocity.z ) );
			networkLog.WriteLine( string.Format( "{0}\t{1}", gameTime, currentConnectionInfo.rtt / 2.0f ) );

			yield return new WaitForFixedUpdate();
		}
			
		boxLog.Flush();
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
			boxes[ 0 ].GetComponent<ForcePlayerController>().enabled = true;
			boxes[ 1 ].GetComponent<ForceSlaveController>().enabled = true;
			player = boxes[ 0 ].GetComponent<ForcePlayerController>();
		} 
		else if( clientID == 1 ) 
		{
			boxes[ 0 ].GetComponent<ForceSlaveController>().enabled = true;
			boxes[ 1 ].GetComponent<ForcePlayerController>().enabled = true;
			player = boxes[ 1 ].GetComponent<ForcePlayerController>();
			gameCamera.transform.RotateAround( transform.position, Vector3.up, 180.0f );
		}

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
		boxes[ 0 ].enabled = boxes[ 1 ].enabled = false;
	}
}