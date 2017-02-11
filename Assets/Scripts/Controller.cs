using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameAxis : byte { X, Y, Z };

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(Collider) ) ]
public abstract class Controller : MonoBehaviour 
{
    public int elementID;

	public Collider boundaries;
    protected Vector3 rangeLimits = new Vector3( 7.5f, 0.0f, 7.5f );

	protected Vector3 size = Vector3.one;
	protected Vector3 initialPosition = Vector3.zero;

	protected Rigidbody body;

	void Awake()
	{
		body = GetComponent<Rigidbody>();

		body.velocity = Vector3.zero;

		size = GetComponent<Collider>().bounds.size;
		//rangeLimits = boundaries.bounds.extents - Vector3.one * GetComponent<Collider>().bounds.extents.magnitude;
		Vector3 bodyExtents = transform.rotation * GetComponent<Collider>().bounds.extents;
		rangeLimits = new Vector3( boundaries.bounds.extents.x - Mathf.Abs( bodyExtents.x ), 
			                       boundaries.bounds.extents.y - Mathf.Abs( bodyExtents.y ), 
			                       boundaries.bounds.extents.z - Mathf.Abs( bodyExtents.z ) );
		
		initialPosition = transform.position;
	}
}