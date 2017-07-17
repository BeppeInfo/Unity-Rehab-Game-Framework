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

	public Slider calibrationSlider;
	public Text valueDisplay;

	private AxisVariable calibratedVariable = AxisVariable.POSITION;

	private enum CalibrationState { PASSIVE, OFFSET, RANGE, SAMPLING, SETPOINT };
	private CalibrationState calibrationState = CalibrationState.PASSIVE;

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
	void Update()
	{
		float currentAbsoluteValue = 0.0f;

		if( controlAxis != null ) 
		{
			currentAbsoluteValue = controlAxis.GetValue( calibratedVariable );
			if( calibrationState == CalibrationState.RANGE ) controlAxis.AdjustRange();
		}

		if( ! calibrationSlider.interactable ) calibrationSlider.value = currentAbsoluteValue;
		valueDisplay.text = currentAbsoluteValue.ToString( "+#0.00000;-#0.00000; #0.00000" );
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
		//if( controlAxis.GetType() == typeof(RemoteInputAxis) ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_OPERATE } );
		controlAxis = axisManager.GetAxis( axisSelector.captionText.text );
		if( controlAxis.GetType() == typeof(RemoteInputAxis) ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_CALIBRATE } );
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

			//if( infoBuffer[ 0 ] != 0x00 ) return;
			Debug.Log( "Received info key: " + infoBuffer[ 0 ] );

			byte[] infoContent = new ArraySegment<byte>( infoBuffer, 1, infoBuffer.Length - 1 ).Array;
			string infoString = Encoding.ASCII.GetString( infoContent ).Trim();
			Debug.Log( "Received info string: " + infoString );
			try
			{
				var remoteInfo = JSON.Parse( infoString );
				Debug.Log( "Received info: " + remoteInfo.ToString() );

				string robotID = remoteInfo[ "id" ].Value;

				List<string> remoteAxisNames = new List<string>();
				var remoteAxesList = remoteInfo[ "axes" ].AsArray;
				for( int remoteAxisIndex = 0; remoteAxisIndex < remoteAxesList.Count; remoteAxisIndex++ )
				{
					string remoteAxisName = robotID + "-" + remoteAxesList[ remoteAxisIndex ].Value;
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

	private void AdjustSlider()
	{
		calibrationSlider.minValue = controlAxis.GetValueMin( calibratedVariable );
		calibrationSlider.maxValue = controlAxis.GetValueMax( calibratedVariable );
		if( controlAxis.GetScale() > 0.0f ) calibrationSlider.direction = Slider.Direction.LeftToRight;
		else calibrationSlider.direction = Slider.Direction.RightToLeft;
	}

	public void SetCalibratedVariable( Int32 variableIndex )
	{
		calibratedVariable = (AxisVariable) variableIndex;
		if( controlAxis != null ) AdjustSlider();
	}

	public void InvertCalibration()
	{
		if( controlAxis != null )
		{
			controlAxis.SetScale( -1.0f * controlAxis.GetScale() );
			AdjustSlider();
		}
	}

	public void SetCalibrationState( Int32 stateIndex )
	{
		CalibrationState newCalibrationState = (CalibrationState) stateIndex;

		if( controlAxis != null )
		{
			if( controlAxis.GetType() == typeof(RemoteInputAxis) )
			{
				if( newCalibrationState == CalibrationState.PASSIVE ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_CALIBRATE } );
				else if( newCalibrationState == CalibrationState.OFFSET ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_OFFSET } );
				else if( newCalibrationState == CalibrationState.RANGE ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_CALIBRATE } );
				else if( newCalibrationState == CalibrationState.SAMPLING ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_PREPROCESS } );
				else if( newCalibrationState == CalibrationState.SETPOINT ) infoStateClient.SendData( new byte[] { (byte) RemoteInputAxis.COMMAND_OPERATE } );
			}

			if( calibrationState == CalibrationState.OFFSET ) controlAxis.AdjustOffset();
			else if( calibrationState == CalibrationState.RANGE ) AdjustSlider();
			else if( calibrationState == CalibrationState.SETPOINT ) calibrationSlider.interactable = false;

			if( newCalibrationState == CalibrationState.SETPOINT ) calibrationSlider.interactable = true;
		}

		calibrationState = newCalibrationState;
	}

	public void ResetAxisValues()
	{
		if( controlAxis != null ) controlAxis.Reset();
		AdjustSlider();
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

