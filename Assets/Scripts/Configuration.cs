using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using SimpleJSON;

[ RequireComponent( typeof(InputAxisManager) ) ]
public class Configuration : MonoBehaviour
{
	public const string DEFAULT_IP_HOST = "127.0.0.1";

	public InputField axisServerEntry, gameServerEntry;

	public Toggle setpointToggle;
	public Slider calibrationSlider;
	public Text valueDisplay;

	private AxisVariable calibratedVariable = AxisVariable.POSITION;

	private static InputAxis controlAxis = null;
	private InputAxisManager axisManager = null;

	public Dropdown axisSelector;

	private InputAxisInfoClient infoStateClient;

	// Use this for initialization
	void Start()
	{
		axisManager = GetComponent<InputAxisManager>();
		axisManager.ResetDefaultAxes();

		axisSelector.ClearOptions();
		axisSelector.AddOptions( InputAxisManager.DEFAULT_AXIS_NAMES );

        SetSelectedAxis( 0 );

		axisServerEntry.text = PlayerPrefs.GetString( RemoteInputAxis.AXIS_SERVER_HOST_ID, Configuration.DEFAULT_IP_HOST );
		gameServerEntry.text = PlayerPrefs.GetString( GameClientConnection.SERVER_HOST_ID, Configuration.DEFAULT_IP_HOST );

		infoStateClient = new InputAxisInfoClient();
	}
	
	// Update is called once per frame
	void FixedUpdate()
	{
		float currentAbsoluteValue = 0.0f;

		if( controlAxis != null ) currentAbsoluteValue = controlAxis.GetValue( calibratedVariable );

		if( ! calibrationSlider.interactable ) calibrationSlider.value = currentAbsoluteValue;
		valueDisplay.text = currentAbsoluteValue.ToString( "+#0.000;-#0.000; #0.000" );
	}

	public void SetAxisServer( string serverHost )
	{
		Debug.Log( "Setting axis server host as " + serverHost );
		PlayerPrefs.SetString( RemoteInputAxis.AXIS_SERVER_HOST_ID, serverHost );
	}

	public void SetGameServer( string serverHost )
	{
		Debug.Log( "Setting game server host as " + serverHost );
		PlayerPrefs.SetString( GameClientConnection.SERVER_HOST_ID, serverHost );
	}

    public void SetSelectedAxis( Int32 typeIndex )
    {
		controlAxis = axisManager.GetAxis( axisSelector.captionText.text );
	}

	public static InputAxis GetSelectedAxis()
	{
		return controlAxis;
	}

	public void RefreshAxesInfo()
	{
		byte[] infoBuffer = new byte[ InputAxisClient.BUFFER_SIZE ];

		infoStateClient.Connect( PlayerPrefs.GetString( RemoteInputAxis.AXIS_SERVER_HOST_ID, Configuration.DEFAULT_IP_HOST ), 50000 );

		infoStateClient.SendData( infoBuffer );
		if( infoStateClient.ReceiveData( infoBuffer ) )
		{
			axisManager.ResetDefaultAxes();

			axisSelector.ClearOptions();
			axisSelector.AddOptions( InputAxisManager.DEFAULT_AXIS_NAMES );				

			string infoString = Encoding.ASCII.GetString( infoBuffer ).Trim();
			Debug.Log( "Received info string: " + infoString );
			try
			{
				var remoteInfo = JSON.Parse( infoString );
				Debug.Log( "Received info: " + remoteInfo.ToString() );

				List<string> remoteAxisNames = new List<string>();
				var remoteAxesList = remoteInfo[ "axes" ].AsArray;
				for( int remoteAxisIndex = 0; remoteAxisIndex < remoteAxesList.Count; remoteAxisIndex++ )
				{
					string remoteAxisName = remoteAxesList[ remoteAxisIndex ].Value;
					axisManager.AddRemoteAxis( remoteAxisName, remoteAxisIndex.ToString() );
					remoteAxisNames.Add( remoteAxisName );
				}
				axisSelector.AddOptions( remoteAxisNames );
			}
			catch( Exception e )
			{
				Debug.Log( e.ToString() );
			}
		}

	}

	public void SetAxisMax()
	{
		Debug.Log( "Set axis Max" );
		if( controlAxis != null ) 
		{
			controlAxis.SetMaxValue( calibratedVariable, calibrationSlider.value );
			SetSliderLimits();
		}
	}

	public void SetAxisMin()
	{
		Debug.Log( "Set axis Min" );
		if( controlAxis != null ) 
		{
			controlAxis.SetMinValue( calibratedVariable, calibrationSlider.value );
			SetSliderLimits();
		}
	}

	private void SetSliderLimits()
	{
		float maxValue = controlAxis.GetMaxValue( calibratedVariable );
		float minValue = controlAxis.GetMinValue( calibratedVariable );
		if( maxValue > minValue )
		{
			calibrationSlider.maxValue = maxValue;
			calibrationSlider.minValue = minValue;
		}
		else
		{
			calibrationSlider.maxValue = minValue;
			calibrationSlider.minValue = maxValue;
		}
	}

	public void SetCalibratedVariable( Int32 variableIndex )
	{
		calibratedVariable = (AxisVariable) variableIndex;
		if( controlAxis != null ) SetSliderLimits();
		setpointToggle.isOn = false;
	}

	private IEnumerator WaitForOffset()
	{
		yield return new WaitForSecondsRealtime( 1.0f );

		Debug.Log( "Offset end" );
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) infoStateClient.SendData( new byte[] { 1, 0, 4 } );
		controlAxis.SetOffset();
	}

	public void GetAxisOffset()
	{
		Debug.Log( "Offset begin" );
		if( controlAxis != null )
		{
			if( controlAxis.GetType() == typeof(RemoteInputAxis) ) infoStateClient.SendData( new byte[] { 1, 0, 5 } );

			StartCoroutine( WaitForOffset() );
		}
	}

	public void ResetAxisValues()
	{
		if( controlAxis != null ) controlAxis.Reset();
	}

	public void SetAxisControl( bool enabled )
	{
		calibrationSlider.interactable = enabled;
	}

	public void SetSetpoint( float setpoint )
	{
		//Debug.Log( "Setting setpoint: " + setpoint.ToString() );
		if( calibrationSlider.interactable ) controlAxis.SetValue( calibratedVariable, setpoint );
	}

	public void EndConfiguration()
    {
		infoStateClient.Disconnect();
		GameManager.isMaster = false;
    }
}

