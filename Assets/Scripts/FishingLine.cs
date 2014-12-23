using UnityEngine;
using System.Collections;

//[RequireComponent( typeof( LineRenderer ) )]
public class FishingLine : MonoBehaviour
{
	public Transform target;
	public Transform pivot;

	private LineRenderer fishingLine;

	// Use this for initialization
	void Start()
	{
		fishingLine = GetComponentInChildren<LineRenderer>();
		fishingLine.useWorldSpace = true;
	}
	
	// Update is called once per frame
	void Update()
	{
		fishingLine.SetPosition( 0, fishingLine.transform.position );
		fishingLine.SetPosition( 1, target.position );
	}

	void LateUpdate()
	{
		transform.RotateAround( transform.position, transform.up, pivot.eulerAngles.y - transform.eulerAngles.y );
	}
}

