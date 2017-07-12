using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;


public abstract class GameConnection
{
	protected ConnectionConfig connectionConfig = new ConnectionConfig();

	protected const int GAME_SERVER_PORT = 50004;
	protected const int PACKET_SIZE = 512;
	private const int PACKET_HEADER_LENGTH = sizeof(int);

	public const int TYPE_VALUES_NUMBER = 4;
	private const int VALUE_HEADER_SIZE = 2;
	private const int VALUE_DATA_SIZE = TYPE_VALUES_NUMBER * sizeof(float);
	protected const int VALUE_BLOCK_SIZE = VALUE_HEADER_SIZE + VALUE_DATA_SIZE;
	private const int VALUE_OID_OFFSET = 0, VALUE_INDEX_OFFSET = 1;

	protected int socketID = -1;
	protected int eventChannel = -1, dataChannel = -1;
	protected byte connectionError = 0;

	protected float networkDelay = 0.0f;

	protected byte[] inputBuffer = new byte[ PACKET_SIZE ];
	protected byte[] outputBuffer = new byte[ PACKET_SIZE ];

	private Dictionary<KeyValuePair<byte,byte>,float[]> remoteValues = new Dictionary<KeyValuePair<byte,byte>,float[]>();
	private Dictionary<KeyValuePair<byte,byte>,float[]> localValues = new Dictionary<KeyValuePair<byte,byte>,float[]>();

	private List<KeyValuePair<byte,byte>> updatedLocalKeys = new List<KeyValuePair<byte,byte>>();

	private Dictionary<byte,float> inputDelays = new Dictionary<byte,float>();

	public GameConnection()
	{
		GlobalConfig networkConfig = new GlobalConfig();
		//networkConfig.MaxPacketSize = 2 * PACKET_SIZE;
		NetworkTransport.Init( networkConfig );

		//connectionConfig.PacketSize = 2 * PACKET_SIZE - 1;
		//connectionConfig.FragmentSize = PACKET_SIZE;

		eventChannel = connectionConfig.AddChannel( QosType.Reliable );
		dataChannel = connectionConfig.AddChannel( QosType.StateUpdate ); // QosType.Unreliable sending just most recent
	}

	public static void Shutdown()
	{ 
		NetworkTransport.Shutdown();
	}

	public abstract void Connect();

	public void SetLocalValue( byte objectID, byte valueType, int valueIndex, float value ) 
	{
		KeyValuePair<byte,byte> localKey = new KeyValuePair<byte,byte>( objectID, valueType );

		if( !localValues.ContainsKey( localKey ) ) localValues[ localKey ] = new float[ TYPE_VALUES_NUMBER ];

		bool isLocalKeyUpdated = false;
		if( Mathf.Approximately( localValues[ localKey ][ valueIndex ], 0.0f ) ) isLocalKeyUpdated = true;
		else if( Mathf.Abs( ( localValues[ localKey ][ valueIndex ] - value ) / localValues[ localKey ][ valueIndex ] ) > 0.01f ) isLocalKeyUpdated = true;
		
		if( isLocalKeyUpdated )
		{
			//Debug.Log( "updating value [" + localKey.ToString() + "," + valueIndex.ToString() + "]: " + localValues[ localKey ][ valueIndex ].ToString() + " -> " + value.ToString() );
			localValues[ localKey ][ valueIndex ] = value;
			updatedLocalKeys.Add( localKey );
		}
	}

	public float GetRemoteValue( byte objectID, byte valueType, int valueIndex )
	{
		KeyValuePair<byte,byte> remoteKey = new KeyValuePair<byte,byte>( objectID, valueType );

		float remoteValue = 0.0f;

		if( remoteValues.ContainsKey( remoteKey ) ) 
		{
			remoteValue = remoteValues[ remoteKey ][ valueIndex ];
			remoteValues[ remoteKey ][ valueIndex ] = 0.0f;
		}

		return remoteValue;
	}

	public void UpdateData( float updateTime )
	{
		int outputMessageLength = PACKET_HEADER_LENGTH;

		if( socketID == -1 ) return;

		foreach( KeyValuePair<byte,byte> localKey in updatedLocalKeys ) 
		{
			outputBuffer[ outputMessageLength + VALUE_OID_OFFSET ] = localKey.Key;
			outputBuffer[ outputMessageLength + VALUE_INDEX_OFFSET ] = localKey.Value;
			outputMessageLength += VALUE_HEADER_SIZE;

			for( int valueIndex = 0; valueIndex < TYPE_VALUES_NUMBER; valueIndex++ ) 
			{
				int dataOffset = outputMessageLength + valueIndex * sizeof(float);
				Buffer.BlockCopy( BitConverter.GetBytes( localValues[ localKey ][ valueIndex ] ), 0, outputBuffer, dataOffset, sizeof(float) );
			}

			outputMessageLength += VALUE_DATA_SIZE;
		}
			
		if( updatedLocalKeys.Count > 0 ) 
		{
			Buffer.BlockCopy( BitConverter.GetBytes( outputMessageLength ), 0, outputBuffer, 0, PACKET_HEADER_LENGTH );
			//Debug.Log( "sending " + updatedLocalKeys.Count.ToString() + " blocks (" + BitConverter.ToInt32( outputBuffer, 0 ).ToString() + " bytes)" );
			SendUpdateMessage();
		}

		updatedLocalKeys.Clear();

		if( ReceiveUpdateMessage() )
		{
			int inputMessageLength = Math.Min( BitConverter.ToInt32( inputBuffer, 0 ), PACKET_SIZE - VALUE_BLOCK_SIZE );
			Debug.Log( "receiving " + inputMessageLength.ToString() + " bytes" );
			for( int blockOffset = PACKET_HEADER_LENGTH; blockOffset < inputMessageLength; blockOffset += VALUE_BLOCK_SIZE )
			{
				byte objectID = inputBuffer[ blockOffset + VALUE_OID_OFFSET ];
				byte axisIndex = inputBuffer[ blockOffset + VALUE_INDEX_OFFSET ];
				KeyValuePair<byte,byte> remoteKey = new KeyValuePair<byte,byte>( objectID, axisIndex );
				Debug.Log( "Received values for key " + remoteKey.ToString() );
				if( !remoteValues.ContainsKey( remoteKey ) ) remoteValues[ remoteKey ] = new float[ TYPE_VALUES_NUMBER ];

				for( int valueIndex = 0; valueIndex < TYPE_VALUES_NUMBER; valueIndex++ ) 
				{
					int dataOffset = blockOffset + VALUE_HEADER_SIZE + valueIndex * sizeof(float);
					remoteValues[ remoteKey ][ valueIndex ] = BitConverter.ToSingle( inputBuffer, dataOffset );
				}

				inputDelays[ objectID ] = networkDelay;
			}
		}
	}

	protected abstract void SendUpdateMessage();

	protected abstract bool ReceiveUpdateMessage();

	public float GetNetworkDelay( byte objectID ) 
	{ 
		if( inputDelays.ContainsKey( objectID ) ) return inputDelays[ objectID ];

		return 0.0f; 
	}
}