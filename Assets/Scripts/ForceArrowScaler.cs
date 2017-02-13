using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceArrowScaler : MonoBehaviour 
{
	private ForcePlayerController player;

	void Start() 
	{
		player = GetComponentInParent<ForcePlayerController>();
	}

	void Update()
	{
		float scale = player.GetInputForce();

		transform.localScale = Vector3.one * scale;
	}
}
