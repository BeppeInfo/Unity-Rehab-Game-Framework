using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class NetworkClientTCP : NetworkClient {
	
	public NetworkClientTCP() 
	{	
		try 
		{
			client = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			client.ReceiveTimeout = 1000;
		}
		catch( Exception e ) 
		{
			Debug.Log( e.ToString() );
		}
		
	}

	public override string ReceiveString() 
	{	
		if( IsConnected() ) 
		{
			Debug.Log( "Receive" );
			try 
			{
				Array.Clear( inputBuffer, 0, inputBuffer.Length );
				if( client.Available > 0 )
				{
				    client.Receive( inputBuffer );

				    Debug.Log( "Received string: " + Encoding.ASCII.GetString( inputBuffer ) );

					return Encoding.ASCII.GetString( inputBuffer );
				}
			} 
			catch( Exception e ) 
			{
				Debug.Log( e.ToString () );
			}
		} 

		return "";
	}

	public override string[] QueryData( string key )
	{
		if( IsConnected() ) 
		{
			try 
			{
				Array.Clear( inputBuffer, 0, inputBuffer.Length );
				client.Receive( inputBuffer, SocketFlags.Peek );

				if( Encoding.ASCII.GetString( inputBuffer ).StartsWith( key + ':' ) )
				{
					client.Receive( inputBuffer );
					return Encoding.ASCII.GetString( inputBuffer ).Substring( key.Length + 1 ).Split(':');
				}
			} 
			catch( Exception e ) 
			{
				Debug.Log( e.ToString () );
			}
		} 

		return "".Split();
	}

	public bool IsConnected()
	{
		try
		{
			if( client.Connected )
				return !( ( client.Poll( 10, SelectMode.SelectRead ) ) && ( client.Available == 0 ) );
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
				client.Shutdown( SocketShutdown.Both );
			} 
			catch( Exception e )
			{
				Debug.Log( e.ToString() );
			}

			base.Disconnect();
		}
		Debug.Log( "Encerrando conexao TCP" );
	}

	~NetworkClientTCP() 
	{
		Disconnect();
	}
}