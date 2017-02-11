using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public enum AxisVariable { POSITION, VELOCITY, FORCE, ACCELERATION, STIFFNESS, DAMPING };

public class InputAxis
{
	protected string id;
	public string ID { get { return id; } }

	protected class InputAxisValue
	{
		public float current = 0.0f, setpoint = 0.0f;
		public float max = 1.0f, min = -1.0f, range = 2.0f;
		public float offset = 0.0f;
	}

	protected InputAxisValue[] inputValues = new InputAxisValue[ Enum.GetValues(typeof(AxisVariable)).Length ];

	public virtual bool Init( string axisID )
	{
		id = axisID;

		for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
			inputValues[ valueIndex ] = new InputAxisValue();

		return true;
	}

    public virtual void End() {}

	public void Reset()
	{
		foreach( InputAxisValue value in inputValues )
		{
			value.max = 1.0f;
			value.min = -1.0f;
			value.range = 2.0f;
			value.offset = 0.0f;
		}
	}

	public virtual void Update( float updateTime ) {}

	public float GetValue( AxisVariable variable ) { return inputValues[ (int) variable ].current - inputValues[ (int) variable ].offset; }
	public void SetValue( AxisVariable variable, float value ) 
	{ 
		inputValues[ (int) variable ].setpoint = value + inputValues[ (int) variable ].offset; 
	}

	public float GetNormalizedValue( AxisVariable variable ) 
	{
		InputAxisValue value = inputValues[ (int) variable ];
		return ( 2.0f * ( value.current - value.offset - value.min ) / value.range - 1.0f ); 
	}
	public void SetNormalizedValue( AxisVariable variable, float normalizedValue ) 
	{ 
		InputAxisValue value = inputValues[ (int) variable ];
		value.setpoint = ( ( normalizedValue + 1.0f ) * value.range / 2.0f ) + value.offset + value.min; 
		Debug.Log( "Returning setpoint: " + value.setpoint.ToString() );
	}

	public float GetMinValue( AxisVariable variable ) {	return inputValues[ (int) variable ].min; }
	public float GetMaxValue( AxisVariable variable ) {	return inputValues[ (int) variable ].max; }

	public void SetMinValue( AxisVariable variable, float value ) 
	{ 
		inputValues[ (int) variable ].min = value;
		Calibrate( variable );
	}
	public void SetMaxValue( AxisVariable variable, float value ) 
	{ 
		inputValues[ (int) variable ].max = value;
		Calibrate( variable );
	}

	private void Calibrate( AxisVariable variable )
	{
		InputAxisValue value = inputValues[ (int) variable ];
		value.range = value.max - value.min;
		if( Mathf.Approximately( value.range, 0.0f ) ) value.range = 1.0f;
	}
		
	public void SetOffset() 
	{ 
		for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
			inputValues[ valueIndex ].offset = inputValues[ valueIndex ].current; 
	}
}


public class MouseInputAxis : InputAxis
{
	public static readonly List<string> DEFAULT_AXIS_NAMES = new List<string> { "Mouse X", "Mouse Y" };

	public override bool Init( string axisID ) 
	{
		base.Init( axisID );

		if( DEFAULT_AXIS_NAMES.Contains( axisID ) ) return true;

		return false;
	}

	public override void Update( float updateTime )
	{
		inputValues[ (int) AxisVariable.VELOCITY ].current = Input.GetAxis( id ) / updateTime;
		inputValues[ (int) AxisVariable.POSITION ].current += inputValues[ (int) AxisVariable.VELOCITY ].current * updateTime;
		inputValues[ (int) AxisVariable.FORCE ].current = inputValues[ (int) AxisVariable.VELOCITY ].current;
	}
}

public class KeyboardInputAxis : InputAxis
{
	public static readonly List<string> DEFAULT_AXIS_NAMES = new List<string> { "Horizontal", "Vertical" };

	public override bool Init( string axisID ) 
	{
		base.Init( axisID );

		if( DEFAULT_AXIS_NAMES.Contains( axisID ) ) return true;

		return false;
	}

	public override void Update( float updateTime )
	{
		inputValues[ (int) AxisVariable.VELOCITY ].current = Input.GetAxis( id );
		inputValues[ (int) AxisVariable.POSITION ].current += inputValues[ (int) AxisVariable.VELOCITY ].current * updateTime;
		inputValues[ (int) AxisVariable.FORCE ].current = inputValues[ (int) AxisVariable.VELOCITY ].current;
	}
}



public class RemoteInputAxis : InputAxis
{
	private class AxisConnection
	{
		public string hostID;
		public InputAxisDataClient dataClient = null;
		public byte[] inputBuffer = new byte[ InputAxisClient.BUFFER_SIZE ];
		public byte[] outputBuffer = new byte[ InputAxisClient.BUFFER_SIZE ];
		public int totalAxesNumber = 0, updatedAxesCount = 0;
		public int changedOutputsCount = 0;

		public AxisConnection( string hostID )
		{
			this.hostID = hostID;
			dataClient = new InputAxisDataClient();
			dataClient.Connect( hostID, 50001 );
		}
	}

	public const string AXIS_SERVER_HOST_ID = "Axis Server Host";

	const int AXIS_DATA_LENGTH = sizeof(byte) + 6 * sizeof(float);

	private static List<AxisConnection> axisConnections = new List<AxisConnection>();

	private byte index;
	private AxisConnection connection;

	public override bool Init( string axisID )
	{
		Debug.Log( "Initializing remote input axis with ID " + axisID.ToString() );

		string axisHost = PlayerPrefs.GetString( AXIS_SERVER_HOST_ID, Configuration.DEFAULT_IP_HOST );

		base.Init( axisID );

		if( byte.TryParse( axisID, out index ) )
        {
			connection = axisConnections.Find( connection => connection.hostID == axisHost );
			if( connection == null ) 
			{
				connection = new AxisConnection( axisHost );
				axisConnections.Add( connection );
			}
			connection.totalAxesNumber++;

            return true;
        }

        return false;
	}

    public override void End()
    {
		connection.dataClient.Disconnect();
    }

	public override void Update( float updateTime )
	{
		/*bool newDataReceived =*/ connection.dataClient.ReceiveData( connection.inputBuffer );

		int axesNumber = (int) connection.inputBuffer[ 0 ];
		Debug.Log( "Received measures for " + axesNumber.ToString() + " axes" );
		for( int axisIndex = 0; axisIndex < axesNumber; axisIndex++ ) 
		{
			int inputIDPosition = 1 + axisIndex * AXIS_DATA_LENGTH;

			if( connection.inputBuffer[ inputIDPosition ] == index ) 
			{
				int inputDataPosition = inputIDPosition + sizeof(byte);

				for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
					inputValues[ valueIndex ].current = BitConverter.ToSingle( connection.inputBuffer, inputDataPosition + valueIndex * sizeof(float) );

				break;
			}
		}

		int outputIDPosition = 1 + connection.changedOutputsCount * AXIS_DATA_LENGTH;
		int outputDataPosition = outputIDPosition + sizeof(byte);

		bool hasOutputChanged = false;
		for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
		{
			int outputValuePosition = outputDataPosition + valueIndex * sizeof(float);
			if( Mathf.Abs( inputValues[ valueIndex ].setpoint - BitConverter.ToSingle( connection.outputBuffer, outputValuePosition ) ) > 0.1f ) 
				hasOutputChanged = true;
		}

		if( hasOutputChanged ) 
		{
			connection.outputBuffer[ outputIDPosition ] = index;
			for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
			{
				int outputValuePosition = outputDataPosition + valueIndex * sizeof(float);
				Buffer.BlockCopy( BitConverter.GetBytes( inputValues[ valueIndex ].setpoint ), 0, connection.outputBuffer, outputValuePosition, sizeof(float) );
			}
			connection.changedOutputsCount++;
		}

		connection.updatedAxesCount++;

		if( connection.updatedAxesCount >= connection.totalAxesNumber && connection.changedOutputsCount > 0 ) 
		{
			Debug.Log( "Sending setpoints for " + connection.changedOutputsCount.ToString() + " axes" );
			connection.outputBuffer[ 0 ] = (byte) connection.changedOutputsCount;
			connection.dataClient.SendData( connection.outputBuffer );
			connection.updatedAxesCount = 0;
			connection.changedOutputsCount = 0;
		}
	}
}

