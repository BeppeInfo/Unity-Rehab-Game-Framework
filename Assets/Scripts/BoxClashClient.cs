using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;

public class BoxClashClient : GameClient 
{
	public Controller[] boxes = new Controller[ 2 ];

	public SpringJoint boxesSpringJoint;

	private ForcePlayerController player = null;

	private int clientID = -1;

	void Awake()
	{
		player = boxes[ 0 ].GetComponent<ForcePlayerController>();

		sliderHandle = setpointSlider.handleRect.GetComponent<Image>();
	}

	public override void Start()
	{
		base.Start();

		boxesSpringJoint.spring = 0.0f;
		boxesSpringJoint.damper = 0.0f;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();

		//setpointSlider.value = setpoint;

		//if( error >= 2 * PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.red;
		//else if( error >= PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.yellow;
		//else sliderHandle.color = Color.green;

		localPlayerText.text = string.Format( "Force:{0:+#0.0000;-#0.0000; #0.0000}N\n({1:+#0.0000;-#0.0000; #0.0000}N)", player.GetRemoteForce(), player.GetPlayerForce() );
		remotePlayerText.text = string.Format( "Position:{0:+#0.0000;-#0.0000; #0.0000} ({1:+#0.0000;-#0.0000; #0.0000})\nVelocity:{2:+#0.0000;-#0.0000; #0.0000}", 
			                                   player.GetRelativePosition(), player.GetAbsolutePosition(), player.GetVelocity() );
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
			boxLog.WriteLine( string.Format( "{0}\t{1}\t{2}\t{3}\t{4}", gameTime, player.GetPlayerForce(), player.GetRemoteForce(), player.GetAbsolutePosition(), player.GetVelocity() ) );
				                                                                            
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
			boxes[ 1 ].GetComponent<PositionSlaveController>().enabled = true;
			player = boxes[ 0 ].GetComponent<ForcePlayerController>();
		} 
		else if( clientID == 1 ) 
		{
			boxes[ 0 ].GetComponent<PositionSlaveController>().enabled = true;
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