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
		public float minReach = 0.0f, maxReach = 0.0f;

		private float absolutePosition = 0.0f, oldPosition = 0.0f, absoluteSpeed = 0.0f;

		public float motionTime;
		
		public float Position
		{
			get 
			{
				if( active ) absolutePosition = InputManager.GetAxisValues( variableName ).position;
				if( Mathf.Approximately( maxReach - minReach, 0.0f ) )
					return 0.0f;
				else
					return 2 * ( ( absolutePosition - minReach ) / ( maxReach - minReach ) ) - 1;
			}
		}
		
		public float Speed
		{
			get 
			{
				if( active ) //absoluteSpeed = InputManager.GetAxisValues( variableName ).velocity;
				{
					absoluteSpeed = ( InputManager.GetAxisValues( variableName ).position - oldPosition ) /
															InputManager.GetAxisValues( variableName ).motionTime;
					oldPosition = InputManager.GetAxisValues( variableName ).position;
				}

				if( Mathf.Approximately( maxReach - minReach, 0.0f ) )
					return float.NaN;
				else
					return 2 * absoluteSpeed / ( maxReach - minReach );
			}
		}

		public void Feedback( float actualPosition, float actualSpeed )
		{
			absolutePosition = ( actualPosition + 1 ) * ( maxReach - minReach ) / 2 + minReach;
			/*if( maxReach > minReach )
				absolutePosition = Mathf.Clamp( absolutePosition, minReach, maxReach );
			else
				absolutePosition = Mathf.Clamp( absolutePosition, maxReach, minReach );*/

			absoluteSpeed = actualSpeed * ( maxReach - minReach ) / 2;

			/*Debug.Log( string.Format( "AxisMotion {0} Feedback: {1} -> {2} : {3} -> {4}", variableName,
			                         actualPosition, absolutePosition, actualSpeed, absoluteSpeed ) );*/

			if( active ) InputManager.SetAxisFeedback( variableName, absolutePosition, absoluteSpeed );
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
			motionAxes[ i ].maxReach = PlayerPrefs.GetFloat( playerName + axisIds[ i ] + "Max", 0.0f );
			motionAxes[ i ].minReach = PlayerPrefs.GetFloat( playerName + axisIds[ i ] + "Min", 0.0f );
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
		{
			normalizedPosition[ i ] = motionAxes[ i ].Position;
			normalizedSpeed[ i ] = motionAxes[ i ].Speed;
		}

		for( int i = 0; i < rotationAxes.Length; i++ )
		{
			normalizedRotation[ i ] = motionAxes[ i + translationAxes.Length ].Position;
			normalizedAngularSpeed[ i ] = motionAxes[ i + translationAxes.Length ].Speed;
		}
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

