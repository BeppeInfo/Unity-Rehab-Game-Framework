using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class BoxClashServer : GameServer 
{
	public ForceMasterController[] boxes = new ForceMasterController[ 2 ];

	public SpringJoint boxesSpringJoint;

	public override void Start()
	{
		base.Start();

		connection.Connect();

		boxesSpringJoint.spring = 0.0f;
		boxesSpringJoint.damper = 0.0f;

		//StartCoroutine( WaitClients() );

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}

	void Update()
	{
		infoText.text = string.Format( "Position 1:{0:+#0.0000;-#0.0000; #0.0000} - Force 1:{1:+#0.0000;-#0.0000; #0.0000}" +
			                           "\nPosition 2:{2:+#0.0000;-#0.0000; #0.0000}, Force 2:{3:+#0.0000;-#0.0000; #0.0000}", 
			                           boxes[ 0 ].GetPosition(), boxes[ 0 ].GetInputForce(), boxes[ 1 ].GetPosition(), boxes[ 1 ].GetInputForce() );
	}

	IEnumerator WaitClients()
	{
		while( connection.GetClientsNumber() < 2 ) 
			yield return new WaitForFixedUpdate();

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}
}