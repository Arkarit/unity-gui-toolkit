using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiVideoClient : ThreadedMonoBehaviour
	{
		public string IP = "192.168.1.165";
		public RawImage m_image;
		public bool m_enableLog = false;

		private const int PORT = 8010;
		TcpClient m_client;

		Texture2D m_texture;
		int m_width = 0;
		int m_height = 0;

		//This must be the-same with SEND_COUNT on the server
		const int SEND_RECEIVE_COUNT = 255;

		// Use this for initialization
		protected override void Start()
		{
			base.Start();
			Application.runInBackground = true;

			m_client = new TcpClient();
		}

		protected override void OnThreadStarting()
		{
			LOGWARNING("Connecting to server...");
			// if on desktop
			m_client.Connect(IPAddress.Loopback, PORT);

			// if using the IPAD
			//client.Connect(IPAddress.Parse(IP), port);
			LOGWARNING("Connected to server!");

			CallWorker(ReadImage);
		}

		private void ReadImage()
		{
			readProportions(ref m_width, ref m_height);

			//Read Image Count
			int imageSize = readImageByteSize(SEND_RECEIVE_COUNT);
				LOGWARNING("Received Image byte Length: " + imageSize);

			//Read Image Bytes and Display it
			readFrameByteArray(imageSize);

			CallWorker(ReadImage);
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
		private void readProportions( ref int _width, ref int _height )
		{
			bool disconnected = false;

			NetworkStream serverStream = m_client.GetStream();
			byte[] buffer = new byte[4];
			var total = 0;
			do
			{
				var read = serverStream.Read(buffer, total, 4 - total);
				//Debug.LogFormat("Client recieved {0} bytes", total);
				if (read == 0)
				{
					disconnected = true;
					break;
				}
				total += read;
			} while (total != 4);

			if (!disconnected)
			{
				_width = (int) buffer[0] * 256 + (int) buffer[1];
				_height = (int) buffer[2] * 256 + (int) buffer[3];
			}
		}

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

			Color[] imageColors = imageBytes.ToColors();

			bool readyToReadAgain = false;

			//Display Image
			if (!disconnected)
			{
				//Display Image on the main Thread
				CallMain(() =>
				{
					if (m_texture == null || m_texture.width != m_width || m_texture.height != m_height)
					{
						m_texture = new Texture2D(m_width, m_height);
					}

					displayReceivedImage(imageColors);
					readyToReadAgain = true;
				});
			}

			//Wait until old Image is displayed
			while (!readyToReadAgain)
			{
				System.Threading.Thread.Sleep(1);
			}
		}

private int bla;
		void displayReceivedImage( Color[] imageColors )
		{

			m_texture.SetPixels(imageColors);
			m_texture.Apply();
//m_texture.SaveAsPNG($"C:/temp/bla_{bla}.png");
//m_texture = LoadPNG($"C:/temp/bla_{bla++}.png");
			m_image.texture = m_texture;

		}

public static Texture2D LoadPNG(string filePath) {
     
Texture2D tex = null;
byte[] fileData;
     
if (File.Exists(filePath))     {
fileData = File.ReadAllBytes(filePath);
tex = new Texture2D(2, 2);
tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
}
return tex;
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

		protected override void OnApplicationQuit()
		{
			LOGWARNING("OnApplicationQuit");

			base.OnApplicationQuit();

			if (m_client != null)
			{
				m_client.Close();
			}

			StopThread(true, true);

		}
	}
}