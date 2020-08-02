using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiVideoClient : MonoBehaviour
	{
		public string IP = "192.168.1.165";
		public RawImage m_image;
		public bool m_enableLog = false;

		private const int PORT = 8010;
		TcpClient m_client;

		Texture2D m_texture;

		private bool m_stop = false;

		//This must be the-same with SEND_COUNT on the server
		const int SEND_RECEIVE_COUNT = 15;

		// Use this for initialization
		void Start()
		{
			Application.runInBackground = true;

			m_texture = new Texture2D(0, 0);
			m_client = new TcpClient();

			//Connect to server from another Thread
			Loom.RunAsync(ThreadStart);
		}

		private void ThreadStart()
		{
			LOGWARNING("Connecting to server...");
			// if on desktop
			m_client.Connect(IPAddress.Loopback, PORT);

			// if using the IPAD
			//client.Connect(IPAddress.Parse(IP), port);
			LOGWARNING("Connected to server!");

			ThreadLoop();
		}

		private void ThreadLoop()
		{
			while (!m_stop)
			{
				//Read Image Count
				int imageSize = readImageByteSize(SEND_RECEIVE_COUNT);
					LOGWARNING("Received Image byte Length: " + imageSize);

				//Read Image Bytes and Display it
				readFrameByteArray(imageSize);
			}
		}

		//Converts the data size to byte array and put result to the fullBytes array
		void byteLengthToFrameByteArray( int byteLength, byte[] fullBytes )
		{
			//Clear old data
			Array.Clear(fullBytes, 0, fullBytes.Length);
			//Convert int to bytes
			byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
			//Copy result to fullBytes
			bytesToSendCount.CopyTo(fullBytes, 0);
		}

		//Converts the byte array to the data size and returns the result
		int frameByteArrayToByteLength( byte[] frameBytesLength )
		{
			int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
			return byteLength;
		}

		/////////////////////////////////////////////////////Read Image SIZE from Server///////////////////////////////////////////////////
		private int readImageByteSize( int size )
		{
			bool disconnected = false;

			NetworkStream serverStream = m_client.GetStream();
			byte[] imageBytesCount = new byte[size];
			var total = 0;
			do
			{
				var read = serverStream.Read(imageBytesCount, total, size - total);
				//Debug.LogFormat("Client recieved {0} bytes", total);
				if (read == 0)
				{
					disconnected = true;
					break;
				}
				total += read;
			} while (total != size);

			int byteLength;

			if (disconnected)
			{
				byteLength = -1;
			}
			else
			{
				byteLength = frameByteArrayToByteLength(imageBytesCount);
			}
			return byteLength;
		}

		/////////////////////////////////////////////////////Read Image Data Byte Array from Server///////////////////////////////////////////////////
		private void readFrameByteArray( int size )
		{
			bool disconnected = false;

			NetworkStream serverStream = m_client.GetStream();
			byte[] imageBytes = new byte[size];
			var total = 0;
			do
			{
				var read = serverStream.Read(imageBytes, total, size - total);
				//Debug.LogFormat("Client recieved {0} bytes", total);
				if (read == 0)
				{
					disconnected = true;
					break;
				}
				total += read;
			} while (total != size);

			bool readyToReadAgain = false;

			//Display Image
			if (!disconnected)
			{
				//Display Image on the main Thread
				Loom.QueueOnMainThread(() =>
				{
					displayReceivedImage(imageBytes);
					readyToReadAgain = true;
				});
			}

			//Wait until old Image is displayed
			while (!readyToReadAgain)
			{
				System.Threading.Thread.Sleep(1);
			}
		}


		void displayReceivedImage( byte[] receivedImageBytes )
		{
			m_texture.LoadImage(receivedImageBytes);
			m_image.texture = m_texture;
		}


		// Update is called once per frame
		void Update()
		{


		}


		void LOG( string messsage )
		{
			if (m_enableLog)
				Debug.Log(messsage);
		}

		void LOGWARNING( string messsage )
		{
			if (m_enableLog)
				Debug.LogWarning(messsage);
		}

		void OnApplicationQuit()
		{
			LOGWARNING("OnApplicationQuit");
			m_stop = true;

			if (m_client != null)
			{
				m_client.Close();
			}
		}
	}
}