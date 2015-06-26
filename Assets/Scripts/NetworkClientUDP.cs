using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class NetworkClientUDP : NetworkClient {

	private List<string> messageQueue = new List<string>( 5 );

	private Thread updateThread = null;
	private bool isReceiving = false;

	private object searchLock = new object();

	public NetworkClientUDP() 
	{
		try 
		{
			client = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}
	}

	public override void Connect( string host, int remotePort, int localPort = 0 ) 
	{	
		//client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );

		base.Connect( host, remotePort, localPort );

		if( !isReceiving )
		{
			updateThread = new Thread( new ThreadStart( updateCallback ) );
			updateThread.Start();
		}
	}

	private void updateCallback() 
	{	
		isReceiving = true;

		Debug.Log( "NetworkClientUDP: Starting to receive messages" );

		try 
		{
			while( isReceiving )
			{
				if( client.Available > 0 )
				{
					Debug.Log( "NetworkClientUDP: Messages available" );

					//try
					//{
						/*int bytesRead =*/ client.Receive( inputBuffer );

						//Debug.Log( "Received " + bytesRead.ToString() + " bytes from : " + Encoding.ASCII.GetString( inputBuffer ) );
					//}
					//catch( SocketException e )
					//{
					//	Debug.Log( e.ToString() );
					//}

					try
					{
						if( messageQueue.Count >= messageQueue.Capacity )
							messageQueue.RemoveAt( 0 );
					}
					catch( IndexOutOfRangeException e )
					{
						Debug.Log( e.ToString() );
					}

					lock( searchLock )
					{
						messageQueue.Add( Encoding.ASCII.GetString( inputBuffer ) );
					}
				} 
			}
		}
		catch( ObjectDisposedException e ) 
		{
			Debug.Log( e.ToString() );
		}
		
		Disconnect();
			
		Debug.Log( "NetworkClientUDP: Finishing update thread" );
	}

	public override string ReceiveString() 
	{	
		if( messageQueue.Count > 0 )
		{
			try
			{
				lock( searchLock )
				{
					string remoteMessage = messageQueue[ messageQueue.Count - 1 ];
					messageQueue.RemoveAt( messageQueue.Count - 1 );
					return remoteMessage;
				}
			}
			catch( Exception e ) 
			{
				Debug.Log( e.ToString() );
			}
		}

		return "";
	}

	public override string[] QueryData( string key )
	{
		lock( searchLock )
		{
			int matchIndex = messageQueue.FindLastIndex( item => item.StartsWith( key + ':' ) );
			if( matchIndex >= 0 )
			{
				string remoteMessage = messageQueue[ matchIndex ];
				Debug.Log( "Query found " + key + " in " + remoteMessage );
				Debug.Log( "Query data: " + remoteMessage.Substring( key.Length + 1 ).Trim().Split(':').ToString() );
				messageQueue.RemoveAt( matchIndex );
				return remoteMessage.Substring( key.Length + 1 ).Trim().Split(':');
			}
		}

		return "".Split();
	}

	public override void Disconnect()
	{
		isReceiving = false;
		messageQueue.Clear();
		//base.Disconnect();

		//if( updateThread != null )
		//	updateThread.Join();

		Debug.Log( "Encerrando conexao UDP" );
	}

	~NetworkClientUDP() 
	{
		Disconnect();
	}
}
