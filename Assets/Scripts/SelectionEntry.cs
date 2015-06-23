using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//[RequireComponent( typeof(Text) )]
public class SelectionEntry : MonoBehaviour
{
	public Text displayText = null; 
	protected string value = "";

	public string valueType = "";

	protected virtual void Start()
	{
		//displayText = GetComponentInChildren<Text>();

		if( displayText )
		{
			if( value == "" ) displayText.text = PlayerPrefs.GetString( valueType, "<empty>" );
		}
	}

	public void SetDisplayText( string textString )
	{
		if( displayText ) displayText.text = textString;
	}

	public void SetValue()
	{
		PlayerPrefs.SetString( valueType, value );
		Debug.Log( valueType + ": " + PlayerPrefs.GetString( valueType ) );
	}

	public void SetStringValue( string valueString )
	{
		value = valueString;
		SetValue();
	}

	public void ClearEntry()
	{
		PlayerPrefs.DeleteKey( valueType );
		Debug.Log( "Deleted value: " + valueType );
	}
}

