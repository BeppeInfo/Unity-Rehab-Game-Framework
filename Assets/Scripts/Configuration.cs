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
		ResetAxisValues();

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
		//if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
		//	infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_OPERATE } );
		controlAxis = axisManager.GetAxis( axisSelector.captionText.text );
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
			infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_CALIBRATE } );
	}

	public static InputAxis GetSelectedAxis()
	{
		//if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
		//	infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_OPERATE } );
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
			float direction = ( controlAxis.GetValue( calibratedVariable ) < controlAxis.GetMinValue( calibratedVariable ) ) ? -1.0f : 1.0f;
			controlAxis.SetMaxValue( calibratedVariable, controlAxis.GetValue( calibratedVariable ) );
			controlAxis.SetAxisScale( 2.0f * direction );
			AdjustSlider();
		}
	}

	public void SetAxisMin()
	{
		Debug.Log( "Set axis Min" );
		if( controlAxis != null ) 
		{
			float direction = ( controlAxis.GetValue( calibratedVariable ) > controlAxis.GetMaxValue( calibratedVariable ) ) ? -1.0f : 1.0f;
			controlAxis.SetMinValue( calibratedVariable, controlAxis.GetValue( calibratedVariable ) );
			controlAxis.SetAxisScale( 2.0f * direction );
			AdjustSlider();
		}
	}

	private void AdjustSlider()
	{
		calibrationSlider.minValue = controlAxis.GetMinValue( calibratedVariable );
		calibrationSlider.maxValue = controlAxis.GetMaxValue( calibratedVariable );
		if( controlAxis.GetAxisScale() > 0.0f ) calibrationSlider.direction = Slider.Direction.LeftToRight;
		else calibrationSlider.direction = Slider.Direction.RightToLeft;
	}

	public void SetCalibratedVariable( Int32 variableIndex )
	{
		calibratedVariable = (AxisVariable) variableIndex;
		if( controlAxis != null ) AdjustSlider();
		setpointToggle.isOn = false;
	}

	private IEnumerator WaitForOffset()
	{
		yield return new WaitForSecondsRealtime( 1.0f );

		Debug.Log( "Offset end" );
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
			infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_CALIBRATE } );
		controlAxis.AdjustOffset();
	}

	public void GetAxisOffset()
	{
		Debug.Log( "Offset begin" );
		if( controlAxis != null )
		{
			if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
				infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_OFFSET } );
			StartCoroutine( WaitForOffset() );
		}
	}

	public void ResetAxisValues()
	{
		if( controlAxis != null ) controlAxis.Reset();
		AdjustSlider();
	}

	public void SetAxisControl( bool enabled )
	{
		calibrationSlider.interactable = enabled;
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
			infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, enabled ? RemoteInputAxis.COMMAND_OPERATE : RemoteInputAxis.COMMAND_CALIBRATE } );
	}

	public void SetSetpoint( float setpoint )
	{
		if( calibrationSlider.interactable ) 
		{
			Debug.Log( "Setting setpoint: " + setpoint.ToString() );
			controlAxis.SetValue( calibratedVariable, setpoint );
		}
	}

	public void EndConfiguration()
    {
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) 
			infoStateClient.SendData( new byte[] { 1, ((RemoteInputAxis) controlAxis).Index, RemoteInputAxis.COMMAND_OPERATE } );
		infoStateClient.Disconnect();
		GameManager.isMaster = false;
    }
}

