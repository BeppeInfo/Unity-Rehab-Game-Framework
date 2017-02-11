using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public abstract class InputAxisClient
{
	public const int BUFFER_SIZE = 512;

	protected Socket workSocket = null;

	private string currentHost = "localhost";
	private int currentRemotePort = 0;

	public InputAxisClient( Socket newSocket = null )
	{
		workSocket = newSocket;
	}

	private void connectCallback( IAsyncResult ar ) 
	{		
		try 
		{
			Socket handle = (Socket) ar.AsyncState;
			
			handle.EndConnect( ar );

			Debug.Log( "Bound to: " + workSocket.LocalEndPoint.ToString() );
			Debug.Log( "Connected to: " + workSocket.RemoteEndPoint.ToString() );
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}
	}

	private void writeCallback( IAsyncResult ar ) 
	{
		try 
		{
			Socket handle = (Socket) ar.AsyncState;
			
			/*int bytesSent =*/ handle.EndSend( ar );
			
			//Debug.Log( "Sent " + bytesSent.ToString() + " bytes to: " + handle.RemoteEndPoint.ToString() );
		}
		catch( Exception e ) 
		{		
			Debug.Log( e.ToString() );
		}
	}

	public virtual void Connect( string host, int remotePort ) 
	{
		if( ! workSocket.Connected || host != currentHost || remotePort != currentRemotePort ) 
		{
			Debug.Log( "Trying to connect to host " + host + " and port " + remotePort.ToString() );
			try 
			{
				//if( workSocket.IsBound ) 
				//{
				//	Debug.Log( "Disconnect previously connected socket" );
				//	workSocket.Disconnect( true );
				//	workSocket.Close();
				//}
					
				Debug.Log( "Connecting to: host: " + host + " - port: " + remotePort.ToString() );

				if( ! workSocket.IsBound ) 
				{
					workSocket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
					workSocket.ExclusiveAddressUse = false;
				}
				workSocket.ReceiveBufferSize = BUFFER_SIZE;
				workSocket.SendBufferSize = BUFFER_SIZE;

				IPAddress ipRemoteHost = Dns.GetHostEntry( host ).AddressList[ 0 ];
				Debug.Log( ipRemoteHost.ToString() );
				IPEndPoint remoteIpAddress = new IPEndPoint( ipRemoteHost, remotePort );
				IAsyncResult connectionResult = workSocket.BeginConnect( (EndPoint) remoteIpAddress, connectCallback, workSocket );

				if( ! connectionResult.AsyncWaitHandle.WaitOne( 5000, true ) )
				{
					workSocket.Close();
					throw new ApplicationException( "Failed to connect server." );
				}

				currentHost = host;
				currentRemotePort = remotePort;
			} 
			catch( Exception e ) 
			{
				Disconnect();
				Debug.Log( e.ToString() );
			}
		}
	}

	public abstract bool ReceiveData( byte[] inputBuffer );

	public void SendData( byte[] outputBuffer ) 
	{
		if( workSocket.Connected ) 
		{
			try 
			{	
				Debug.Log( "Sending " + outputBuffer.Length.ToString() + " bytes to " + workSocket.RemoteEndPoint.ToString() );
				workSocket.BeginSend( outputBuffer, 0, outputBuffer.Length, SocketFlags.None, new AsyncCallback( writeCallback ), workSocket );
			} 
			catch( Exception e ) 
			{
				Debug.Log( e.ToString() );
			}	
		}
	}

	public virtual void Disconnect()
	{
		try
		{
			workSocket.Disconnect( true );
			workSocket.Close();
		}
		catch( Exception e )
		{
			Debug.Log( e.ToString() );
		}

		currentHost = "localhost";
		currentRemotePort = 0;

		Debug.Log( "Encerrando conexao" );
	}

	~InputAxisClient() 
	{
		Disconnect();
	}
}