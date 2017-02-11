using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

[ RequireComponent( typeof(Rigidbody) ) ]
[ RequireComponent( typeof(Collider) ) ]
public class BallController : Controller 
{
	const int POSITION = 0, VELOCITY = 1;

	public float speed;	// Ball speed

	void Start()
	{
		body.angularVelocity = new Vector3( 0.0f, 1.0f, 0.0f );
	}

	void FixedUpdate()
	{
		body.velocity *= speed / body.velocity.magnitude;

		UpdateMasterValues( body.position, body.velocity );
	}

    private void UpdateMasterValues( Vector3 newPosition, Vector3 newVelocity )
    {
		body.MovePosition( newPosition );
		body.velocity = newVelocity;

		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.X, POSITION, body.position.x );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.X, VELOCITY, body.velocity.x );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, POSITION, body.position.z );
		GameManager.GetConnection().SetLocalValue( elementID, (int) GameAxis.Z, VELOCITY, body.velocity.z );
    }

    void OnTriggerExit( Collider collider )
	{
		if( !enabled ) return; 

		if( collider.tag == "Boundary" ) UpdateMasterValues( initialPosition, GenerateStartVector() * speed );
	}

	void OnTriggerEnter( Collider collider )
    {
		if( !enabled ) return; 

		Debug.Log( "Colliding with " + collider.tag + " on layer " + collider.gameObject.layer );

		if( collider.tag == "Vertical" ) UpdateMasterValues( body.position, new Vector3( -body.velocity.x, 0.0f, body.velocity.z ) );
		else if( collider.tag == "Horizontal" ) UpdateMasterValues( body.position, new Vector3( body.velocity.x, 0.0f, -body.velocity.z ) );
		else if( collider.tag == "Tower" ) UpdateMasterValues( body.position, new Vector3( -body.velocity.x, 0.0f, -body.velocity.z ) );
    }

	Vector3 GenerateStartVector()
	{
		float rand = Random.Range( 0.0f, Mathf.PI * 2.0f );
		return new Vector3( Mathf.Cos( rand ), 0.0f, Mathf.Sin( rand ) ); 
	}

	void OnEnable()
	{
		body.position = initialPosition;
		body.velocity = GenerateStartVector() * speed;
	}

	void OnDisable()
	{
		UpdateMasterValues( initialPosition, Vector3.zero );
	}

}
