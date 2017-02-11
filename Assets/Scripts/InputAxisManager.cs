using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

public class InputAxisManager : MonoBehaviour
{
	//private static StreamWriter inputLog = new StreamWriter( "c:\\Users\\Adriano\\Documents\\input.txt", false );
	//private static StreamWriter trajectoryLog = new StreamWriter( "c:\\Users\\Adriano\\Documents\\trajectory.txt", false );

	public static readonly List<string> DEFAULT_AXIS_NAMES = KeyboardInputAxis.DEFAULT_AXIS_NAMES.Concat( MouseInputAxis.DEFAULT_AXIS_NAMES ).ToList();

	private static Dictionary<string, InputAxis> inputAxes = new Dictionary<string, InputAxis>();

	public void ResetDefaultAxes()
	{
		ClearAxes();

		foreach( string axisName in KeyboardInputAxis.DEFAULT_AXIS_NAMES )
		{
			InputAxis keyboardAxis = new KeyboardInputAxis();
			keyboardAxis.Init( axisName );
			inputAxes[ axisName ] = keyboardAxis;
		}

		foreach( string axisName in MouseInputAxis.DEFAULT_AXIS_NAMES )
		{
			InputAxis mouseAxis = new MouseInputAxis();
			mouseAxis.Init( axisName );
			inputAxes[ axisName ] = mouseAxis;
		}
	}

	public void AddRemoteAxis( string axisName, string axisID )
	{
		InputAxis newAxis = GetAxis( axisName );

		if( newAxis == null ) 
		{
			newAxis = new RemoteInputAxis();
			if( newAxis.Init( axisID ) ) inputAxes[ axisName ] = newAxis;
		}
	}

	public InputAxis GetAxis( string axisName )
	{
		if( inputAxes.ContainsKey( axisName ) ) return inputAxes[ axisName ];
		else return null;
	}

	public void ClearAxes()
	{
		foreach( InputAxis inputAxis in inputAxes.Values )
			inputAxis.End();

		inputAxes.Clear();
	}

	void FixedUpdate()
	{
		foreach( InputAxis inputAxis in inputAxes.Values )
			inputAxis.Update( Time.fixedDeltaTime );
	}

    void OnApplicationQuit()
	{
		ClearAxes();

		//inputLog.Close();
		//trajectoryLog.Close();
	}
}

