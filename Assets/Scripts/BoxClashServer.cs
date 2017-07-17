using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;

public class BoxClashServer : GameServer 
{
	public ForceMasterController[] boxes = new ForceMasterController[ 2 ];

	public override void Start()
	{
		base.Start();

		connection.Connect();

		foreach( ForceMasterController box in boxes )
			box.enabled = true;

		StartCoroutine( RegisterValues() );
	}

	void Update()
	{
		infoText.text = string.Format( "Position 1:{0:+#0.0000;-#0.0000; #0.0000}, Force 1:{1:+#0.0000;-#0.0000; #0.0000}" +
			                           "\nPosition 2:{2:+#0.0000;-#0.0000; #0.0000}, Force 2:{3:+#0.0000;-#0.0000; #0.0000}", 
									   boxes[ 0 ].GetAbsolutePosition(), boxes[ 0 ].GetRemoteForce(), boxes[ 1 ].GetAbsolutePosition(), boxes[ 1 ].GetRemoteForce() );
	}

	IEnumerator RegisterValues()
	{
		// Set log file names
		StreamWriter boxLog = new StreamWriter( "./box_server.log", false );

		while( connection.GetClientsNumber() < 1 ) 
			yield return new WaitForFixedUpdate();

		while( Application.isPlaying )
		{
			double gameTime = DateTime.Now.TimeOfDay.TotalSeconds;
			boxLog.WriteLine( string.Format( "{0}\t{1}\t{2}\t{3}", gameTime, boxes[ 0 ].GetAbsolutePosition(), boxes[ 0 ].GetRemoteForce(), boxes[ 1 ].GetAbsolutePosition(), boxes[ 1 ].GetRemoteForce() ) );                            

			yield return new WaitForFixedUpdate();
		}

		boxLog.Flush();
	}
}