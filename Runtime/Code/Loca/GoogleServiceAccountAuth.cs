#if UNITY_EDITOR
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GuiToolkit
{
	internal static class GoogleServiceAccountAuth
	{
		private const string SESSION_TOKEN_KEY = "GoogleAuth_Token";
		private const string SESSION_EXPIRY_KEY = "GoogleAuth_Expiry";

		public static string GetAccessToken(string _serviceAccountJsonPath)
		{
			string cachedToken = SessionState.GetString(SESSION_TOKEN_KEY, null);
			string cachedExpiry = SessionState.GetString(SESSION_EXPIRY_KEY, null);

			if (!string.IsNullOrEmpty(cachedToken) && !string.IsNullOrEmpty(cachedExpiry))
			{
				if (long.TryParse(cachedExpiry, out long expiryUnix))
				{
					long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
					if (nowUnix < expiryUnix - 60)
						return cachedToken;
				}
			}

			try
			{
				(string clientEmail, string privateKey) = ParseServiceAccountJson(_serviceAccountJsonPath);
				using (RSACryptoServiceProvider rsa = ImportPkcs8PrivateKey(privateKey))
				{
					string jwt = BuildJwt(clientEmail, rsa);
					string token = FetchAccessToken(jwt);
					if (token == null)
						return null;

					long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3300;
					SessionState.SetString(SESSION_TOKEN_KEY, token);
					SessionState.SetString(SESSION_EXPIRY_KEY, expiry.ToString());
					return token;
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"GoogleServiceAccountAuth: {ex.Message}");
				return null;
			}
		}

		private static (string clientEmail, string privateKey) ParseServiceAccountJson(string _jsonPath)
		{
			string json = System.IO.File.ReadAllText(_jsonPath);
			string clientEmail = ExtractJsonStringField(json, "client_email");
			string privateKey = ExtractJsonStringField(json, "private_key");
			return (clientEmail, privateKey);
		}

		// Extracts a JSON string field value, handling escape sequences including \n
		private static string ExtractJsonStringField(string _json, string _fieldName)
		{
			string searchFor = $"\"{_fieldName}\"";
			int idx = _json.IndexOf(searchFor, StringComparison.Ordinal);
			if (idx < 0)
				throw new InvalidOperationException($"Field '{_fieldName}' not found in JSON");

			idx += searchFor.Length;
			while (idx < _json.Length && (_json[idx] == ' ' || _json[idx] == '\t' || _json[idx] == ':' || _json[idx] == '\r' || _json[idx] == '\n'))
				idx++;

			if (idx >= _json.Length || _json[idx] != '"')
				throw new InvalidOperationException($"Expected string value for field '{_fieldName}'");

			idx++; // skip opening quote
			var sb = new StringBuilder();
			while (idx < _json.Length && _json[idx] != '"')
			{
				if (_json[idx] == '\\' && idx + 1 < _json.Length)
				{
					char next = _json[idx + 1];
					switch (next)
					{
						case '"':  sb.Append('"');  idx += 2; break;
						case '\\': sb.Append('\\'); idx += 2; break;
						case 'n':  sb.Append('\n'); idx += 2; break;
						case 'r':  sb.Append('\r'); idx += 2; break;
						case 't':  sb.Append('\t'); idx += 2; break;
						default:   sb.Append(_json[idx]); idx++; break;
					}
				}
				else
				{
					sb.Append(_json[idx]);
					idx++;
				}
			}
			return sb.ToString();
		}

		private static RSACryptoServiceProvider ImportPkcs8PrivateKey(string _pem)
		{
			string base64 = _pem
				.Replace("-----BEGIN PRIVATE KEY-----", "")
				.Replace("-----END PRIVATE KEY-----", "")
				.Replace("\n", "").Replace("\r", "").Replace(" ", "");
			byte[] der = Convert.FromBase64String(base64);

			int offset = 0;
			ReadDerTag(der, ref offset, 0x30); // outer SEQUENCE
			ReadDerTag(der, ref offset, 0x02); // INTEGER 0 (version)
			ReadDerTag(der, ref offset, 0x30); // SEQUENCE { OID rsaEncryption, NULL }
			ReadDerTag(der, ref offset, 0x04); // OCTET STRING (inner PKCS#1 key)
			ReadDerTag(der, ref offset, 0x30); // inner SEQUENCE
			ReadDerTag(der, ref offset, 0x02); // INTEGER 0 (version)

			var p = new RSAParameters
			{
				Modulus  = ReadDerInteger(der, ref offset),
				Exponent = ReadDerInteger(der, ref offset),
				D        = ReadDerInteger(der, ref offset),
				P        = ReadDerInteger(der, ref offset),
				Q        = ReadDerInteger(der, ref offset),
				DP       = ReadDerInteger(der, ref offset),
				DQ       = ReadDerInteger(der, ref offset),
				InverseQ = ReadDerInteger(der, ref offset),
			};

			var rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(p);
			return rsa;
		}

		private static string BuildJwt(string _clientEmail, RSACryptoServiceProvider _rsa)
		{
			string header = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
			long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			string payloadJson =
				$"{{\"iss\":\"{_clientEmail}\"," +
				$"\"scope\":\"https://www.googleapis.com/auth/spreadsheets.readonly https://www.googleapis.com/auth/drive.readonly\"," +
				$"\"aud\":\"https://oauth2.googleapis.com/token\"," +
				$"\"iat\":{now},\"exp\":{now + 3600}}}";
			string payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
			string headerPayload = $"{header}.{payload}";
			byte[] sig = _rsa.SignData(Encoding.UTF8.GetBytes(headerPayload), new SHA256CryptoServiceProvider());
			return $"{headerPayload}.{Base64UrlEncode(sig)}";
		}

		private static string FetchAccessToken(string _jwt)
		{
			string body = $"grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer&assertion={_jwt}";
			byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

			using (UnityWebRequest www = new UnityWebRequest("https://oauth2.googleapis.com/token", "POST"))
			{
				www.uploadHandler = new UploadHandlerRaw(bodyBytes);
				www.downloadHandler = new DownloadHandlerBuffer();
				www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

				var op = www.SendWebRequest();
				var sw = Stopwatch.StartNew();
				const int timeoutMs = 30000;

				while (!op.isDone)
				{
					if (sw.ElapsedMilliseconds > timeoutMs)
					{
						www.Abort();
						Debug.LogError("GoogleServiceAccountAuth: Token request timed out");
						return null;
					}
					EditorApplication.QueuePlayerLoopUpdate();
					Thread.Sleep(10);
				}

				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"GoogleServiceAccountAuth: Token request failed: {www.error}\n{www.downloadHandler.text}");
					return null;
				}

				return ExtractJsonStringField(www.downloadHandler.text, "access_token");
			}
		}

		private static string Base64UrlEncode(byte[] _data)
		{
			return Convert.ToBase64String(_data)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');
		}

		// Advances past the DER tag and length header.
		// For INTEGER (0x02): also skips the value bytes (used for version fields).
		// For SEQUENCE (0x30) and OCTET STRING (0x04): leaves offset at start of content.
		private static void ReadDerTag(byte[] _data, ref int _offset, byte _expectedTag)
		{
			if (_offset >= _data.Length)
				throw new InvalidOperationException($"Unexpected end of DER data at offset {_offset} (expected tag 0x{_expectedTag:X2})");
			if (_data[_offset] != _expectedTag)
				throw new InvalidOperationException(
					$"Expected DER tag 0x{_expectedTag:X2} at offset {_offset}, got 0x{_data[_offset]:X2}");
			_offset++;
			int length = ReadDerLength(_data, ref _offset);
			if (_expectedTag == 0x02)
			{
				if (_offset + length > _data.Length)
					throw new InvalidOperationException("DER INTEGER length exceeds data");
				_offset += length; // skip INTEGER value (version = 0)
			}
			// SEQUENCE and OCTET STRING: offset is now at content start
		}

		private static byte[] ReadDerInteger(byte[] _data, ref int _offset)
		{
			if (_offset >= _data.Length)
				throw new InvalidOperationException($"Unexpected end of DER data at offset {_offset} (expected INTEGER)");
			if (_data[_offset] != 0x02)
				throw new InvalidOperationException(
					$"Expected INTEGER tag (0x02) at offset {_offset}, got 0x{_data[_offset]:X2}");
			_offset++;
			int length = ReadDerLength(_data, ref _offset);
			if (_offset + length > _data.Length)
				throw new InvalidOperationException("DER INTEGER length exceeds data");
			byte[] value = new byte[length];
			Array.Copy(_data, _offset, value, 0, length);
			_offset += length;
			// Strip leading 0x00 padding byte required by DER for positive integers
			if (value.Length > 1 && value[0] == 0x00)
			{
				byte[] trimmed = new byte[value.Length - 1];
				Array.Copy(value, 1, trimmed, 0, trimmed.Length);
				return trimmed;
			}
			return value;
		}

		private static int ReadDerLength(byte[] _data, ref int _offset)
		{
			int b = _data[_offset++];
			if (b < 0x80)
				return b;
			int numBytes = b & 0x7F;
			int length = 0;
			for (int i = 0; i < numBytes; i++)
				length = (length << 8) | _data[_offset++];
			return length;
		}
	}
}
#endif
