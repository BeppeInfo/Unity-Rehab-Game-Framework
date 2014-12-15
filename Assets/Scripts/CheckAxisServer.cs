using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent( typeof(Toggle) )]
public class CheckAxisServer : MonoBehaviour 
{
	Toggle axisRemoteServerToggle;

	// Use this for initialization
	void Start () 
	{
		axisRemoteServerToggle = GetComponent<Toggle>();

		if( PlayerPrefs.GetString( ConnectionManager.AXIS_SERVER_HOST_ID ) == ConnectionManager.LOCAL_SERVER_HOST )
			axisRemoteServerToggle.isOn = false;
		else
			axisRemoteServerToggle.isOn = true;
	}
}
