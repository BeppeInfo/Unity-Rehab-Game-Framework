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

	private Dictionary<string, AxisMotion> motionAxes = new Dictionary<string, AxisMotion>()
															{
																{ "X", new AxisMotion() },
																{ "Y", new AxisMotion() },
																{ "Z", new AxisMotion() },
																{ "Roll", new AxisMotion() },
																{ "Yaw", new AxisMotion() },
																{ "Pitch", new AxisMotion() }
															};

	public Vector3 normalizedPosition = Vector3.zero;
	public Vector3 normalizedRotation = Vector3.zero;

	public Vector3 normalizedSpeed = Vector3.zero;
	public Vector3 normalizedAngularSpeed = Vector3.zero;

	int collisionsNumber = 0;

	void Start()
	{
		string playerName = gameObject.name;
		foreach( string axisName in motionAxes.Keys )
		{
			motionAxes[ axisName ].maxReach = PlayerPrefs.GetFloat( playerName + axisName + "Max", 0.0f );
			motionAxes[ axisName ].minReach = PlayerPrefs.GetFloat( playerName + axisName + "Min", 0.0f );
			if( PlayerPrefs.HasKey( playerName + axisName + "Var" ) )
			{
				motionAxes[ axisName ].variableName = PlayerPrefs.GetString( playerName + axisName + "Var" );
				motionAxes[ axisName ].active = true;
			}
			Debug.Log( "LocalPlayer: Setting control axis: " + motionAxes[ axisName ].variableName + " -> " + playerName + axisName );
		}
	}

	// Update is called once per frame
	void Update()
	{
		normalizedPosition = 
			new Vector3( motionAxes[ "X" ].Position, motionAxes[ "Y" ].Position, motionAxes[ "Z" ].Position );
		normalizedSpeed = 
			new Vector3( motionAxes[ "X" ].Speed, motionAxes[ "Y" ].Speed, motionAxes[ "Z" ].Speed );

		normalizedRotation = 
			new Vector3( motionAxes[ "Roll" ].Position, motionAxes[ "Yaw" ].Position, motionAxes[ "Pitch" ].Position );
		normalizedAngularSpeed = 
			new Vector3( motionAxes[ "Roll" ].Speed, motionAxes[ "Yaw" ].Speed, motionAxes[ "Pitch" ].Speed );
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
		motionAxes[ "X" ].Feedback( actualPosition.x, actualSpeed.x );
		motionAxes[ "Y" ].Feedback( actualPosition.y, actualSpeed.y );
		motionAxes[ "Z" ].Feedback( actualPosition.z, actualSpeed.z );
		motionAxes[ "Roll" ].Feedback( actualRotation.x, actualAngularSpeed.x );
		motionAxes[ "Yaw" ].Feedback( actualRotation.y, actualAngularSpeed.y );
		motionAxes[ "Pitch" ].Feedback( actualRotation.z, actualAngularSpeed.z );

		Debug.Log( "LocalPlayer: Feedback for player " + gameObject.name );
	}
}

