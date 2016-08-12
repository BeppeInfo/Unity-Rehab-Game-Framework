using UnityEngine;
//using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//[RequireComponent( typeof(Text) )]
public class SelectionList : SelectionEntry
{
	public static Dictionary<string, List<string>> stringLists = new Dictionary<string, List<string>>();

	private int currentValueID = 0;
	public string[] defaultValues;

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		stringLists[ valueType ] = new List<string>( defaultValues );

		UpdateValue();
	}
	
	private void UpdateValue()
	{
		if( stringLists[ valueType ].Count > 0 )
			SetStringValue( stringLists[ valueType ][ currentValueID ] );
		/*{
			PlayerPrefs.SetString( valueType, stringLists[ valueType ][ currentValueID ] );
			Debug.Log( valueType + ": " + PlayerPrefs.GetString( valueType ) );
		}*/
		else
			SetStringValue( "" );
		//	PlayerPrefs.SetString( valueType, "" );

		//value = PlayerPrefs.GetString( valueType );
		if( displayText ) displayText.text = value;
	}
	
	public void PreviousValue()
	{
		currentValueID = ( currentValueID <= 0 ) ? stringLists[ valueType ].Count - 1 : currentValueID - 1;
		UpdateValue();
	}
	
	public void NextValue()
	{
		currentValueID = ( currentValueID >= stringLists[ valueType ].Count - 1 ) ? 0 : currentValueID + 1;
		UpdateValue();
	}
	
	/*public void NewValue()
	{
		if( !availableValues.Contains( valueDisplay.text ) )
			availableValues.Add( valueDisplay.text );

		currentValueID = availableValues.Length - 1;
	}*/
}

