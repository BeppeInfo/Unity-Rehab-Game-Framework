using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceArrowScaler : MonoBehaviour 
{
	private ForcePlayerController player;
	private ForceMasterController master;

	void Start() 
	{
		player = GetComponentInParent<ForcePlayerController>();
		master = GetComponentInParent<ForceMasterController>();
	}

	void Update()
	{
		//float scale = player.GetInputForce() + master.GetOutputForce();

		//transform.localScale = Vector3.one * scale;

		if( GameManager.isMaster ) transform.localScale = Vector3.one * master.GetInputForce();
		else transform.localScale = Vector3.one * player.GetPlayerForce();
	}
}
