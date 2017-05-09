using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class BoxClashServer : GameServer 
{
	public ForceMasterController[] boxes = new ForceMasterController[ 2 ];

	public override void Start()
	{
		base.Start();

		connection.Connect();

		//StartCoroutine( WaitClients() );

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}

	void Update()
	{
		infoText.text = string.Format( "Position 1: {1:F3}\nPosition 2 {3:F3}", boxes[ 0 ].GetPosition(), boxes[ 1 ].GetPosition() );
	}

	IEnumerator WaitClients()
	{
		while( connection.GetClientsNumber() < 2 ) 
			yield return new WaitForFixedUpdate();

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}
}