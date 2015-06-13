using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent( typeof(Rigidbody) )]
public class LocalPlayer : MonoBehaviour
{
	private class AxisMotion
	{
		public bool active = false;

		public string variableName = "";
		
		public float Speed
		{
			get 
			{
				return InputManager.GetAxisSpeed( variableName );
			}
		}

		public void Feedback( float actualPosition, float actualSpeed )
		{
			if( active ) InputManager.SetAxisFeedback( variableName, actualPosition, actualSpeed );
		}
	}

	private string[] translationAxes = { "X", "Y", "Z" };
	private string[] rotationAxes = { "Roll", "Yaw", "Pitch" };
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

	public Vector3 normalizedPosition = Vector3.zero;
	public Vector3 normalizedRotation = Vector3.zero;

	public Vector3 normalizedSpeed = Vector3.zero;
	public Vector3 normalizedAngularSpeed = Vector3.zero;

	int collisionsNumber = 0;

	void Start()
	{
		string playerName = gameObject.name;
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
			Debug.Log( "LocalPlayer: Setting control axis: " + motionAxes[ i ].variableName + " -> " + playerName + axisIds[ i ] );
		}
	}

	// Update is called once per frame
	void Update()
	{
		for( int i = 0; i < translationAxes.Length; i++ )
			normalizedSpeed[ i ] = motionAxes[ i ].Speed;

		for( int i = 0; i < rotationAxes.Length; i++ )
			normalizedAngularSpeed[ i ] = motionAxes[ i + translationAxes.Length ].Speed;
	}

	void OnCollisionEnter( Collision collisionInfo )
	{
		Debug.Log( "LocalPlayer: Collision Enter" );
		collisionsNumber++;
	}

	void OnCollisionExit( Collision collisionInfo )
	{
		Debug.Log( "LocalPlayer: Collision Exit" );
		collisionsNumber--;
	}

	public void FeedBack( Vector3 actualPosition, Vector3 actualRotation, Vector3 actualSpeed, Vector3 actualAngularSpeed )
	{
		for( int i = 0; i < translationAxes.Length; i++ )
			motionAxes[ i ].Feedback( actualPosition[ i ], actualSpeed[ i ] );

		for( int i = 0; i < rotationAxes.Length; i++ )
			motionAxes[ i + translationAxes.Length ].Feedback( actualRotation[ i ], actualAngularSpeed[ i ] );

		Debug.Log( "LocalPlayer: Feedback for player " + gameObject.name );
	}
}

