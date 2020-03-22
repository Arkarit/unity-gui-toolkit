using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiVideoServer : MonoBehaviour
	{
		//This must be the-same with SEND_COUNT on the client
		private const int SEND_RECEIVE_COUNT = 15;
		private const int PORT = 8010;

		public RawImage m_image;
		public bool m_enableLogging = false;

		private WebCamTexture m_webCam;
		private Texture2D m_currentTexture;
		private TcpListener m_listener;
		private bool m_stop = false;
		private List<TcpClient> m_clients = new List<TcpClient>();

		private void Start()
		{
			Application.runInBackground = true;

			//Start WebCam coroutine
			StartCoroutine(initAndWaitForWebCamTexture());
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

		IEnumerator initAndWaitForWebCamTexture()
		{
			// Open the Camera on the desired device, in my case IPAD pro
			m_webCam = new WebCamTexture();
			// Get all devices , front and back camera
			m_webCam.deviceName = WebCamTexture.devices[WebCamTexture.devices.Length - 1].name;

			// request the lowest width and heigh possible
			m_webCam.requestedHeight = 10;
			m_webCam.requestedWidth = 10;

			m_image.texture = m_webCam;

			m_webCam.Play();

			m_currentTexture = new Texture2D(m_webCam.width, m_webCam.height);

			// Connect to the server
			m_listener = new TcpListener(IPAddress.Any, PORT);

			m_listener.Start();

			while (m_webCam.width < 100)
			{
				yield return null;
			}

			//Start sending coroutine
			StartCoroutine(senderCOR());
		}

		WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

		IEnumerator senderCOR()
		{
			bool isConnected = false;
			TcpClient client = null;
			NetworkStream stream = null;

			// Wait for client to connect in another Thread 
			Loom.RunAsync(() =>
			{
				while (!m_stop)
				{
					// Wait for client connection
					client = m_listener.AcceptTcpClient();
					// We are connected
					m_clients.Add(client);

					isConnected = true;
					stream = client.GetStream();
				}
			});

			//Wait until client has connected
			while (!isConnected)
			{
				yield return null;
			}

			LOG("Connected to client!");

			bool readyToGetFrame = true;

			byte[] frameBytesLength = new byte[SEND_RECEIVE_COUNT];

			while (!m_stop)
			{
				//Wait for End of frame
				yield return endOfFrame;

				m_currentTexture.SetPixels(m_webCam.GetPixels());
				byte[] pngBytes = m_currentTexture.EncodeToPNG();
				//Fill total byte length to send. Result is stored in frameBytesLength
				byteLengthToFrameByteArray(pngBytes.Length, frameBytesLength);

				//Set readyToGetFrame false
				readyToGetFrame = false;

				Loom.RunAsync(() =>
				{
					//Send total byte count first
					stream.Write(frameBytesLength, 0, frameBytesLength.Length);
						LOG("Sent Image byte Length: " + frameBytesLength.Length);

					//Send the image bytes
					stream.Write(pngBytes, 0, pngBytes.Length);
						LOG("Sending Image byte array data : " + pngBytes.Length);

					//Sent. Set readyToGetFrame true
					readyToGetFrame = true;
				});

				//Wait until we are ready to get new frame(Until we are done sending data)
				while (!readyToGetFrame)
				{
					LOG("Waiting To get new frame");
					yield return null;
				}
			}
		}


		void LOG( string messsage )
		{
			if (m_enableLogging)
				Debug.Log(messsage);
		}

		private void Update()
		{
			m_image.texture = m_webCam;
		}

		// stop everything
		private void OnApplicationQuit()
		{
			if (m_webCam != null && m_webCam.isPlaying)
			{
				m_webCam.Stop();
				m_stop = true;
			}

			if (m_listener != null)
			{
				m_listener.Stop();
			}

			foreach (TcpClient c in m_clients)
				c.Close();
		}
	}
}