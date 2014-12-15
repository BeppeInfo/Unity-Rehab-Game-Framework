using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameplayManager : GameManager
{
	public Slider fishPositionSlider, rodPositionSlider, distanceSlider;

	public FishMove fishController;
	public LocalPlayer rodController;

	float fishDistance;
	Image distanceBar;

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		fishController.movementRange = mapScale;
		distanceBar = distanceSlider.fillRect.GetComponentInChildren<Image>();
	}
	
	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		if( fishController.transform.position.z <= mapBoundingBox.bounds.min.z )
			Application.LoadLevel( "Play Game" );
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		fishPositionSlider.value = fishController.transform.position.x / mapScale.x;
		rodPositionSlider.value = rodController.normalizedPosition.x;

		fishDistance = fishController.transform.position.z - mapBoundingBox.bounds.min.z;

		distanceSlider.value = 2 * ( fishDistance / mapScale.z ) - 1.0f;

		if( distanceSlider.value < -0.3f )
			distanceBar.color = Color.green;
		else if( distanceSlider.value > 0.5f )
			distanceBar.color = Color.red;
		else
			distanceBar.color = Color.yellow;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}
}

