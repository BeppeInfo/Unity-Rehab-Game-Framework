using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;

public class BoxClashClient : GameClient 
{
	public Controller[] boxes = new Controller[ 2 ];

	private ForcePlayerController player = null;

	public Text waveImpedanceText, filterStrengthText;

	private int clientID = -1;

	private IEnumerator logCoroutine;

	void Awake()
	{
		player = boxes[ 0 ].GetComponent<ForcePlayerController>();

		sliderHandle = setpointSlider.handleRect.GetComponent<Image>();
	}

	public override void Start()
	{
		base.Start();

		logCoroutine = RegisterValues();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();

		//setpointSlider.value = setpoint;

		//if( error >= 2 * PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.red;
		//else if( error >= PositionPositionPlayerController.ERROR_THRESHOLD ) sliderHandle.color = Color.yellow;
		//else sliderHandle.color = Color.green;

		localPlayerText.text = string.Format( "Force:{0:+#0.00000;-#0.00000; #0.00000}N", player.GetRemoteForce() );
		remotePlayerText.text = string.Format( "Position:{0:+#0.0000;-#0.0000; #0.0000} ({1:+#0.0000;-#0.0000; #0.0000})\nVelocity:{2:+#0.0000;-#0.0000; #0.0000}", 
			                                   player.GetRelativePosition(), player.GetAbsolutePosition(), player.GetVelocity() );

		waveImpedanceText.text = player.GetWaveImpedance().ToString( "0.000" );
		filterStrengthText.text = player.GetFilteringStrength().ToString( "0.000" );
	}

	IEnumerator RegisterValues()
	{
		// Set log file names
		StreamWriter boxLog = new StreamWriter( "./box_client" + clientID.ToString() + ".log", false );
		StreamWriter networkLog = new StreamWriter( "./network_client" + clientID.ToString() + ".log", false );

		while( Application.isPlaying )
		{
			ConnectionInfo currentConnectionInfo = connection.GetCurrentInfo();

			infoText.text =  string.Format( "Client: {0} Sent: {1} Received: {2} Lost Packets: {3} RTT: {4,3}ms", clientID,
				currentConnectionInfo.sentPackets, currentConnectionInfo.receivedPackets, currentConnectionInfo.lostPackets, currentConnectionInfo.rtt );

			double gameTime = DateTime.Now.TimeOfDay.TotalSeconds;
			boxLog.WriteLine( string.Format( "{0}\t{1}\t{2}\t{3}", gameTime, player.GetAbsolutePosition(), player.GetVelocity(), player.GetRemoteForce() ) );                            
			networkLog.WriteLine( string.Format( "{0}\t{1}\t{2}\t{3}", gameTime, currentConnectionInfo.rtt / 2.0f, player.GetWaveImpedance(), player.GetFilteringStrength() ) );

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

		StartCoroutine( logCoroutine );
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

		StopCoroutine( logCoroutine );
	}
}