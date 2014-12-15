using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//[RequireComponent( typeof(Text) )]
public class SelectionEntry : MonoBehaviour
{
	protected Text valueDisplay = null; 

	public string valueType = "";

	protected virtual void Start()
	{
		valueDisplay = GetComponentInChildren<Text>();

		if( valueDisplay )
		{
			if( valueDisplay.text == "" )
				valueDisplay.text = PlayerPrefs.GetString( valueType, "<vazio>" );
		}
	}

	public void SetStringValue()
	{
		PlayerPrefs.SetString( valueType, valueDisplay.text );
		Debug.Log( valueType + ": " + PlayerPrefs.GetString( valueType ) );
	}

	public void ClearEntry()
	{
		PlayerPrefs.DeleteKey( valueType );
		Debug.Log( "Deleted value: " + valueType );
	}
}

