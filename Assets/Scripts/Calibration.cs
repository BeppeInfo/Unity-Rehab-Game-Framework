using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Calibration : MonoBehaviour
{
	public Slider calibrationSlider;
	public Text axisValueDisplay;

	private float currentAbsoluteValue = 0.0f;

	private string motionAxisID = "";
	private string controlAxis = "";

	private const float DEFAULT_AXIS_MIN = -1000.0f;
	private const float DEFAULT_AXIS_MAX = 1000.0f;

	private const string PLAYER_NAME_ID = "Player Name";

	private const string MOTION_AXIS_ID = "Motion Axis";
	private const string MOTION_AXIS_MAX_SUFFIX = "Max";
	private const string MOTION_AXIS_MIN_SUFFIX = "Min";
	private const string MOTION_AXIS_VARIABLE_SUFFIX = "Var";

	private const string CONTROL_AXIS_ID = "Control Axis";

	// Use this for initialization
	void Start()
	{
		SetAxis();
	}
	
	// Update is called once per frame
	void Update()
	{
		InputManager.CalibrateAxisSpeed( controlAxis );
		currentAbsoluteValue = InputManager.GetAxisAbsolutePosition( controlAxis );
		Debug.Log( "Calibration: " + controlAxis + " position: " + currentAbsoluteValue );

		calibrationSlider.value = currentAbsoluteValue;

		axisValueDisplay.text = currentAbsoluteValue.ToString( "#0.###" );
	}

	public void SetControl()
	{
		InputManager.CalibrateAxisPosition( controlAxis, calibrationSlider.minValue, calibrationSlider.maxValue );
		controlAxis = PlayerPrefs.GetString( CONTROL_AXIS_ID );
		Debug.Log( "Calibration: Setting control axis: " + controlAxis );
		PlayerPrefs.SetString( motionAxisID + MOTION_AXIS_VARIABLE_SUFFIX, controlAxis );
	}

	public void SetAxis()
	{
		motionAxisID = PlayerPrefs.GetString( PLAYER_NAME_ID ) + PlayerPrefs.GetString( MOTION_AXIS_ID );

		AdjustSensitivity();

		if( PlayerPrefs.HasKey( motionAxisID + MOTION_AXIS_VARIABLE_SUFFIX ) )
			PlayerPrefs.SetString( CONTROL_AXIS_ID, PlayerPrefs.GetString( motionAxisID + MOTION_AXIS_VARIABLE_SUFFIX ) );
		else
			PlayerPrefs.DeleteKey( CONTROL_AXIS_ID );
	}

	public void SetMinimum()
	{
		PlayerPrefs.SetFloat( motionAxisID + MOTION_AXIS_MIN_SUFFIX, currentAbsoluteValue );
		calibrationSlider.minValue = currentAbsoluteValue;
		AdjustSensitivity();
	}

	public void SetMaximum()
	{
		PlayerPrefs.SetFloat( motionAxisID + MOTION_AXIS_MAX_SUFFIX, currentAbsoluteValue );
		calibrationSlider.maxValue = currentAbsoluteValue;
		AdjustSensitivity();
	}

	private void AdjustSensitivity()
	{
		if( PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MAX_SUFFIX, DEFAULT_AXIS_MAX ) 
			- PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MIN_SUFFIX, DEFAULT_AXIS_MIN ) > 0 )
		{
			calibrationSlider.minValue = PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MIN_SUFFIX, DEFAULT_AXIS_MIN );
			calibrationSlider.maxValue = PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MAX_SUFFIX, DEFAULT_AXIS_MAX );
		}
		else
		{
			calibrationSlider.minValue = PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MAX_SUFFIX, DEFAULT_AXIS_MAX );
			calibrationSlider.maxValue = PlayerPrefs.GetFloat( motionAxisID + MOTION_AXIS_MIN_SUFFIX, DEFAULT_AXIS_MIN );
		}
	}

	public void Reset()
	{
		PlayerPrefs.DeleteKey( motionAxisID + MOTION_AXIS_MAX_SUFFIX );
		PlayerPrefs.DeleteKey( motionAxisID + MOTION_AXIS_MIN_SUFFIX );
		PlayerPrefs.DeleteKey( motionAxisID + MOTION_AXIS_VARIABLE_SUFFIX );
	}

	void OnDestroy()
	{
		PlayerPrefs.DeleteKey( CONTROL_AXIS_ID );
		PlayerPrefs.Save();
	}
}

