using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(Collider) ) ]
public abstract class Controller : MonoBehaviour 
{
    public int elementID;

	protected const byte X = 0, Y = 1, Z = 2;
	protected const int POSITION = 0, VELOCITY = 1, ACCELERATION = 2;
	protected const int FORCE = 2, MOMENTUM = 3;
	protected const int WAVE = 2, WAVE_INTEGRAL = 3;

	private static float waveImpedance = 5.0f;
	public static float WaveImpedance { get { return waveImpedance; } set { waveImpedance = Mathf.Clamp( value, 1.0f, 20.0f ); } }

	public Collider boundaries;
    protected Vector3 rangeLimits = new Vector3( 7.5f, 0.0f, 7.5f );

	protected Vector3 size = Vector3.one;
	/*protected*/public Vector3 initialPosition = Vector3.zero;

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