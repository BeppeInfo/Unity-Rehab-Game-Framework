using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent( typeof(ScrollRect) )]
[RequireComponent( typeof(Mask) )]
public class SelectionBox : SelectionList 
{	
	public Button entry;
	private List<Button> entryList = new List<Button>();
	private IEnumerable<string> valuesList;
	private int entriesNumber = 0;

	public UnityEvent selectionEvent;

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		stringLists[ valueType ].Clear();

		StartCoroutine( RefreshInfo() );
	}

	IEnumerator RefreshInfo()
	{
		while( Application.isPlaying )
		{
			valuesList = defaultValues.Concat( stringLists[ valueType ] );
			if( entriesNumber != valuesList.Count() )
			{
				RectTransform listBox = GetComponent<ScrollRect>().content;
			
				foreach( Button entry in entryList )
					GameObject.Destroy( entry.gameObject );
				entryList.Clear();
			
				float buttonHeight = GetComponent<Mask>().rectTransform.rect.width / 8;
				if( valuesList.Count() * buttonHeight > listBox.rect.height )
					listBox.sizeDelta = new Vector2( 0.0f, ( valuesList.Count() + 1 ) * buttonHeight - listBox.rect.height );
				
				float listLength = 0.0f;
				foreach( string stringValue in valuesList )
				{
					Button newEntry = (Button) Instantiate( entry );
					newEntry.image.rectTransform.SetParent( listBox.transform );

					newEntry.image.rectTransform.sizeDelta = new Vector2( listBox.rect.width - 20, buttonHeight );

					listLength += buttonHeight;

					newEntry.GetComponent<SelectionEntry>().valueType = valueType;
					newEntry.GetComponent<SelectionEntry>().SetDisplayText( stringValue.Split( ':' )[ 1 ] );
					newEntry.GetComponent<SelectionEntry>().SetStringValue( stringValue.Split( ':' )[ 0 ] );

					newEntry.onClick.AddListener( delegate { selectionEvent.Invoke(); } );
				
					entryList.Add( newEntry );

					float entryPosition = listBox.rect.height * 0.5f - listLength + newEntry.image.rectTransform.rect.height * 0.5f;
					newEntry.image.rectTransform.anchoredPosition = new Vector2( 0.5f, entryPosition );
				}

				entriesNumber = entryList.Count;
			}

			yield return new WaitForSeconds( 2.0f );
		}
	}
}
