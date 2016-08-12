using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent( typeof(Rigidbody) )]
public class NetworkPlayer : MonoBehaviour
{
	private class AxisMotion
	{
		public bool active = false;

		public string variableName = "";

		public float GetPosition()
		{
			return InputManager.GetAxisNormalizedPosition( variableName );
		}

		public float GetSpeed()
		{
			return InputManager.GetAxisNormalizedSpeed( variableName );
		}

		public void SetFeedback( float actualPosition, float actualSpeed )
		{
			if( active ) InputManager.SetAxisFeedback( variableName, actualPosition, actualSpeed );
		}
	}

	private Rigidbody body;

	private const string[] translationAxes = { "X", "Y", "Z" };
	private const string[] rotationAxes = { "Roll", "Yaw", "Pitch" };
//	private Dictionary<string, AxisMotion> motionAxes = new Dictionary<string, AxisMotion>()
//															{
//																{ "X", new AxisMotion() },
//																{ "Y", new AxisMotion() },
//																{ "Z", new AxisMotion() },
//																{ "Roll", new AxisMotion() },
//																{ "Yaw", new AxisMotion() },
//																{ "Pitch", new AxisMotion() }
//															};
	private List<AxisMotion> motionAxes = new List<AxisMotion>();

	private Vector3 /*newPosition = Vector3.zero,*/ newSpeed = Vector3.zero;
	public Vector3 currentPosition = Vector3.zero, currentSpeed = Vector3.zero;
	private Vector3 positionScale = Vector3.one, positionOffset = Vector3.zero;

	private Vector3 /*newRotation = Vector3.zero,*/ newAngularSpeed = Vector3.zero;
	public Vector3 currentRotation = Vector3.zero, currentAngularSpeed = Vector3.zero;
	private Vector3 rotationScale = Vector3.one, rotationOffset = Vector3.zero;
	
	int collisionsNumber = 0;

	void Start()
	{
		string playerName = gameObject.name;

		body = GetComponent<Rigidbody>();

		SetScale( Vector3.one, Vector3.one );

		List<string> axisIds = new List<string>( translationAxes );
		axisIds.AddRange( rotationAxes );
		for( int i = 0; i < axisIds.Count; i++ ) 
		{
			motionAxes.Add( new AxisMotion() );
			//motionAxes[ i ].maxReach = PlayerPrefs.GetFloat( playerName + axisIds[ i ] + "Max", 0.0f );
			//motionAxes[ i ].minReach = PlayerPrefs.GetFloat( playerName + axisIds[ i ] + "Min", 0.0f );
			if( PlayerPrefs.HasKey( playerName + axisIds[ i ] + "Var" ) )
			{
				motionAxes[ i ].variableName = PlayerPrefs.GetString( playerName + axisIds[ i ] + "Var" );
				motionAxes[ i ].active = true;
			}
			Debug.Log( "NetworkPlayer: Setting control axis: " + motionAxes[ i ].variableName + " -> " + playerName + axisIds[ i ] );
		}
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		currentPosition = transform.InverseTransformDirection( body.position ) - positionOffset;
		currentSpeed = transform.InverseTransformDirection( body.velocity );

		for( int i = 0; i < translationAxes.Length; i++ )
		{
			motionAxes[ i ].SetFeedback( currentPosition[ i ] / positionScale[ i ], currentSpeed[ i ] / positionScale[ i ] );
			newSpeed[ i ] = motionAxes[ i ].GetSpeed() * positionScale[ i ];
		}

		body.AddRelativeForce( newSpeed - currentSpeed, ForceMode.VelocityChange );

		currentRotation = transform.InverseTransformDirection( body.rotation.eulerAngles ) - rotationOffset;
		currentAngularSpeed = transform.InverseTransformDirection( body.angularVelocity );

		for( int i = 0; i < rotationAxes.Length; i++ )
		{
			motionAxes[ i + translationAxes.Length ].SetFeedback( currentRotation[ i ] / rotationScale[ i ], currentAngularSpeed[ i ] / rotationScale[ i ] );
 			newAngularSpeed[ i ] = motionAxes[ i + translationAxes.Length ].GetSpeed() * rotationScale[ i ];
		}

		body.AddRelativeTorque( newAngularSpeed - currentAngularSpeed, ForceMode.VelocityChange );
	}

	public void SetScale( Vector3 positionScale, Vector3 rotationScale )
	{
		this.positionScale = positionScale;
		this.rotationScale = rotationScale;

		Vector3 positionInput = Vector3.zero;
		for( int i = 0; i < translationAxes.Length; i++ )
			positionInput[ i ] = motionAxes[ i ].GetPosition() * positionScale[ i ];

		positionOffset = transform.InverseTransformDirection( body.position ) - positionInput;

		Vector3 rotationInput = Vector3.zero;
		for( int i = 0; i < rotationAxes.Length; i++ )
			rotationInput[ i ] = motionAxes[ i + translationAxes.Length ].GetPosition() * rotationScale[ i ];
		
		rotationOffset = transform.InverseTransformDirection( body.rotation.eulerAngles ) - rotationInput;
	}

	public void FeedBack( Vector3 actualPosition, Vector3 actualRotation, Vector3 actualSpeed, Vector3 actualAngularSpeed )
	{
		for( int i = 0; i < translationAxes.Length; i++ )
			motionAxes[ i ].SetFeedback( actualPosition[ i ], actualSpeed[ i ] );

		for( int i = 0; i < rotationAxes.Length; i++ )
			motionAxes[ i + translationAxes.Length ].SetFeedback( actualRotation[ i ], actualAngularSpeed[ i ] );

		Debug.Log( "NetworkPlayer: Feedback for player " + gameObject.name );
	}

	void OnCollisionEnter( Collision collisionInfo )
	{
		Debug.Log( "NetworkPlayer: Collision Enter" );
		collisionsNumber++;
	}
	
	void OnCollisionExit( Collision collisionInfo )
	{
		Debug.Log( "NetworkPlayer: Collision Exit" );
		collisionsNumber--;
	}
}

