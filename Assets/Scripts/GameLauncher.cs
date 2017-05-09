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

	void Start() 
	{
		gameSelector = GetComponent<Dropdown>();

		string labelText = gameSelector.captionText.text;

		if( gameSelector.options.Count > 1 )
		{
			hasGamesAvailable = true;

			RectTransform optionsArea = gameSelector.template;
			float itemHeight = gameSelector.template.GetComponent<ScrollRect>().content.rect.height;
			optionsArea.sizeDelta = new Vector2( optionsArea.sizeDelta.x, gameSelector.options.Count * itemHeight );
			optionsArea.anchoredPosition = new Vector2( optionsArea.anchoredPosition.x, optionsArea.sizeDelta.y );
		}

		gameSelector.captionText.text = labelText;
	}

	public void LaunchSelectedGame( Int32 gameIndex )
	{
		if( hasGamesAvailable )
			SceneManager.LoadScene( gameSelector.options[ gameIndex ].text );
	}
}
