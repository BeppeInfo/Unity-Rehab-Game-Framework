using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class BoxClashServer : GameServer 
{
	public ForceMasterController[] boxes = new ForceMasterController[ 2 ];

	public Slider stiffnessSlider, dampingSlider;

	public override void Start()
	{
		base.Start();

		connection.Connect();

		//StartCoroutine( WaitClients() );
		stiffnessSlider.value = 5.0f;
		dampingSlider.value = 0.1f;

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}

	void Update()
	{
		infoText.text = string.Format( "Setpoint 1: {0:F3} Position 1: {1:F3}\n Setpoint 2: {2:F3} Position 2 {3:F3}", boxes[ 0 ].GetSetpoint(), boxes[ 0 ].GetPosition(),
			                                                                                                           boxes[ 1 ].GetSetpoint(), boxes[ 1 ].GetPosition() );
	}

	IEnumerator WaitClients()
	{
		while( connection.GetClientsNumber() < 2 ) 
			yield return new WaitForFixedUpdate();

		stiffnessSlider.value = 5.0f;
		dampingSlider.value = 0.1f;

		foreach( ForceMasterController box in boxes )
			box.enabled = true;
	}
}