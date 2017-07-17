using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceArrowScaler : MonoBehaviour 
{
	private ForceController forceController;

	void Start() 
	{
		if( GameManager.isMaster ) forceController = GetComponentInParent<ForceMasterController>();
		else forceController = GetComponentInParent<ForcePlayerController>();
	}

	void Update()
	{
		transform.localScale = Vector3.one * forceController.GetRemoteForce();
	}
}
