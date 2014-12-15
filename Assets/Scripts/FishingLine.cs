using UnityEngine;
using System.Collections;

//[RequireComponent( typeof( LineRenderer ) )]
public class FishingLine : MonoBehaviour
{
	public Transform target;
	public Transform pivot;

	private LineRenderer fishingLine;

	private Vector3 rotation = Vector3.zero;

	// Use this for initialization
	void Start()
	{
		fishingLine = GetComponentInChildren<LineRenderer>();
		fishingLine.useWorldSpace = true;
	}
	
	// Update is called once per frame
	void Update()
	{
		transform.RotateAround( pivot.position, Vector3.up, pivot.rotation.eulerAngles.y - rotation.y );
		rotation = pivot.rotation.eulerAngles;
		//transform.LookAt( target, transform.up );

		fishingLine.SetPosition( 0, fishingLine.transform.position );
		fishingLine.SetPosition( 1, target.position );
	}
}

