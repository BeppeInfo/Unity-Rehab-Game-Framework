using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class InputAxisInfoClient : InputAxisClient
{	
	public InputAxisInfoClient() 
	{	
		try 
		{
			workSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			workSocket.ReceiveTimeout = 5000;
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}
		
	}

	public InputAxisInfoClient( Socket clientSocket ) : base( clientSocket ) {	}

	public override bool ReceiveData( byte[] inputBuffer ) 
	{	
		if( IsConnected() ) 
		{
			Debug.Log( "Receive" );
			try 
			{
                int bytesRead = 0;
                while( bytesRead < inputBuffer.Length )
                    bytesRead += workSocket.Receive( inputBuffer );

				Debug.Log( "Received " + bytesRead.ToString() + "bytes from " + workSocket.RemoteEndPoint );

				return true;
			} 
			catch( Exception e ) 
			{
				Debug.Log( e.ToString () );
			}
		} 

		return false;
	}

	public bool IsConnected()
	{
		try
		{
			if( workSocket.Connected )
				return !( ( workSocket.Poll( 10, SelectMode.SelectRead ) ) && ( workSocket.Available == 0 ) );
		}
		catch( ObjectDisposedException e )
		{
			Debug.Log( e.ToString() );
		}
		
		return false;
	}

	public override void Disconnect()
	{
		if( IsConnected() )
		{
			try
			{
				workSocket.Shutdown( SocketShutdown.Both );
			} 
			catch( Exception e )
			{
				Debug.Log( e.ToString() );
			}

			base.Disconnect();
		}
		Debug.Log( "Encerrando conexao TCP" );
	}

	~InputAxisInfoClient() 
	{
		Disconnect();
	}
}