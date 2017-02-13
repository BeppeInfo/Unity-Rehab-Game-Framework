using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[ RequireComponent( typeof(Dropdown) ) ]
public class GameLauncher : MonoBehaviour 
{
	private Dropdown gameSelector = null;

	private bool hasGamesAvailable = false;

	public List<string> gameTitles = new List<string>();

	void Start() 
	{
		gameSelector = GetComponent<Dropdown>();

		string labelText = gameSelector.captionText.text;

		if( gameTitles.Count > 0 )
		{
			hasGamesAvailable = true;

			RectTransform optionsArea = gameSelector.template;
			float itemHeight = gameSelector.template.GetComponent<ScrollRect>().content.rect.height;
			optionsArea.sizeDelta = new Vector2( optionsArea.sizeDelta.x, gameTitles.Count * itemHeight );
			optionsArea.anchoredPosition = new Vector2( optionsArea.anchoredPosition.x, optionsArea.sizeDelta.y );

			gameSelector.ClearOptions();
			gameSelector.AddOptions( gameTitles );

			Debug.Log( "Content height: " + gameSelector.template.GetChild( 0 ).GetComponentInChildren<RectTransform>().rect.height );
		}

		gameSelector.captionText.text = labelText;
	}

	public void LaunchSelectedGame( Int32 gameIndex )
	{
		if( hasGamesAvailable )
			SceneManager.LoadScene( gameSelector.options[ gameIndex ].text );
	}
}
