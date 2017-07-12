using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public enum AxisVariable { POSITION, VELOCITY, FORCE, ACCELERATION, INERTIA, STIFFNESS, DAMPING };

public class InputAxis
{
	protected string id;
	public string ID { get { return id; } }

	protected float scale = 1.0f;

	protected class InputAxisValue
	{
		public float current = 0.0f, setpoint = 0.0f;
		public float min = /*Mathf.NegativeInfinity*/0.0f, max = /*Mathf.Infinity*/0.0f;
		public float range = 1.0f;/*Mathf.Infinity;*/
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
		scale = 1.0f;

		foreach( InputAxisValue value in inputValues )
		{
			value.min = 0.0f;//Mathf.NegativeInfinity;
			value.max = 0.0f;//Mathf.Infinity;
			value.offset = 0.0f;
		}
	}

	public virtual void Update( float updateTime ) {}

	public float GetValue( AxisVariable variable ) { return inputValues[ (int) variable ].current - inputValues[ (int) variable ].offset; }
	public void SetValue( AxisVariable variable, float value ) 
	{ 
		value += inputValues[ (int) variable ].offset;
		//value = Mathf.Clamp( value, inputValues[ (int) variable ].min, inputValues[ (int) variable ].max );
		inputValues[ (int) variable ].setpoint = value;
	}

	public float GetScaledValue( AxisVariable variable ) 
	{
		float absoluteInputValue = Mathf.Clamp( GetValue( variable ), inputValues[ (int) variable ].min, inputValues[ (int) variable ].max );
		return absoluteInputValue * scale / inputValues[ (int) variable ].range; 
	} 
	public void SetScaledValue( AxisVariable variable, float scaledValue ) { SetValue( variable, scaledValue * inputValues[ (int) variable ].range / scale ); }

	public float GetAxisScale() { return scale; } 
	public void SetAxisScale( float newScale )
	{
		if( Mathf.Approximately( newScale, 0.0f ) ) scale = 1.0f;
		else scale = newScale;
	}

	public float GetMinValue( AxisVariable variable ) { return inputValues[ (int) variable ].min; }
	public float GetMaxValue( AxisVariable variable ) { return inputValues[ (int) variable ].max; }

	public void AdjustRange() 
	{
		foreach( InputAxisValue value in inputValues )
		{
			value.min = Mathf.Min( value.min, value.current );
			value.max = Mathf.Max( value.max, value.current );
			value.range = value.max - value.min;
			if( Mathf.Approximately( value.range, 0.0f ) ) value.range = 1.0f;
		}
	}
		
	public void AdjustOffset() 
	{ 
		foreach( InputAxisValue value in inputValues )
			value.offset = value.current; 
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

	public const byte COMMAND_DISABLE = 1, COMMAND_ENABLE = 2, COMMAND_RESET = 3, COMMAND_OPERATE = 4, COMMAND_OFFSET = 5, COMMAND_CALIBRATE = 6, COMMAND_PREPROCESS = 7, COMMAND_SET_USER = 8;

	private readonly int AXIS_DATA_LENGTH = sizeof(byte) + /*Enum.GetValues(typeof(AxisVariable)).Length*/7 * sizeof(float);

	private static List<AxisConnection> axisConnections = new List<AxisConnection>();

	private byte index;
	public byte Index { get { return index; } }

	private AxisConnection connection;

	private float[] previousSetpoints = new float[ Enum.GetValues(typeof(AxisVariable)).Length ];

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
			float oldSetpoint = previousSetpoints[ valueIndex ];
			float setpointDelta = inputValues[ valueIndex ].setpoint - oldSetpoint;
			if( Mathf.Abs( setpointDelta / inputValues[ valueIndex ].range ) > 0.01f ) hasOutputChanged = true;
			//if( valueIndex == 0 ) Debug.Log( string.Format( "checking {0}: {1} - ({2}) = {3} ({4})", index, inputValues[ valueIndex ].setpoint, oldSetpoint, setpointDelta, hasOutputChanged ) );
		}

		if( hasOutputChanged ) 
		{
			connection.outputBuffer[ outputIDPosition ] = index;
			for( int valueIndex = 0; valueIndex < inputValues.Length; valueIndex++ )
			{
				int outputValuePosition = outputDataPosition + valueIndex * sizeof(float);
				Buffer.BlockCopy( BitConverter.GetBytes( inputValues[ valueIndex ].setpoint ), 0, connection.outputBuffer, outputValuePosition, sizeof(float) );
				previousSetpoints[ valueIndex ] = inputValues[ valueIndex ].setpoint;
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

