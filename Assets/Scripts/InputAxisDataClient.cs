using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class InputAxisDataClient : InputAxisClient 
{
	private byte[] lastMessage = new byte[ BUFFER_SIZE ];
    private bool hasNewMessage = false;

	private Thread updateThread = null;
	private volatile bool isReceiving = false;

	private volatile bool isAwaitingConnection = true;

	private object searchLock = new object();

	public InputAxisDataClient() 
	{
		try 
		{
			workSocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}
	}

	public InputAxisDataClient( Socket clientSocket ) : base( clientSocket ) {	}

	public override void Connect( string host, int remotePort ) 
	{	
		//client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );

		base.Connect( host, remotePort );

		if( !isReceiving )
		{
			updateThread = new Thread( new ThreadStart( UpdateCallback ) );
			updateThread.Start();
		}
	}

	private void UpdateCallback() 
	{	
		isReceiving = true;

		byte[] messageBuffer = new byte[ BUFFER_SIZE ];

		Debug.Log( "InputAxisDataClient: Starting to receive messages" );

		try 
		{
			while( isReceiving )
			{
				Debug.Log( "Awaiting connection: " + isAwaitingConnection.ToString() );
				if( isAwaitingConnection ) 
				{
					Debug.Log( "Sending " + messageBuffer.Length.ToString() + " bytes to " + workSocket.RemoteEndPoint.ToString() );
					workSocket.Send( messageBuffer, 0, messageBuffer.Length, SocketFlags.None );
					Thread.Sleep( 100 );
				}

				if( workSocket.Available > 0 )
				{
					//Debug.Log( "InputAxisDataClient: Messages available" );
					isAwaitingConnection = false;

					lock( searchLock )
					{
						try
						{
							int bytesRead = workSocket.Receive( messageBuffer );

							Debug.Log( "Received " + bytesRead.ToString() + " bytes from : " + workSocket.RemoteEndPoint.ToString() );

							if( bytesRead >= BUFFER_SIZE )
							{
								Buffer.BlockCopy( messageBuffer, 0, lastMessage, 0, BUFFER_SIZE );
                            	hasNewMessage = true;
							}
						}
						catch( SocketException e )
						{
							Debug.Log( e.ToString() );
						}
					}
				} 
			}
		}
		catch( ObjectDisposedException e ) 
		{
			Debug.Log( e.ToString() );
		}
		
		Disconnect();
			
		Debug.Log( "InputAxisDataClient: Finishing update thread" );
	}

	public override bool ReceiveData( byte[] inputBuffer ) 
	{	
		try
		{
			lock( searchLock )
			{
                bool isNewMessage = hasNewMessage;

                Buffer.BlockCopy( lastMessage, 0, inputBuffer, 0, Math.Min( inputBuffer.Length, BUFFER_SIZE ) );

                hasNewMessage = false;

                return isNewMessage;
			}
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}

		return false;
	}

	public override void Disconnect()
	{
		isReceiving = false;
        if( updateThread != null )
        {
            if( updateThread.IsAlive )
            {
                if( !updateThread.Join( 500 ) ) updateThread.Abort();
            }
        }

		base.Disconnect();

		Debug.Log( "Encerrando conexao UDP" );
	}

	~InputAxisDataClient() 
	{
		Disconnect();
	}
}
