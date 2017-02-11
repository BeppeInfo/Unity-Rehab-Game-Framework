using System;
using System.IO;
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

		string[] gameFileNames = Directory.GetFiles( Application.dataPath + "/Games/", "*.unity" );

		if( gameFileNames.Length > 0 )
		{
			hasGamesAvailable = true;

			List<string> gameTitles = new List<string>();
			foreach( string gameFileName in gameFileNames )
				gameTitles.Add( Path.GetFileNameWithoutExtension( gameFileName ) );

			RectTransform optionsArea = gameSelector.template;
			float itemHeight = gameSelector.template.GetComponent<ScrollRect>().content.rect.height;
			optionsArea.sizeDelta = new Vector2( optionsArea.sizeDelta.x, gameFileNames.Length * itemHeight );
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
