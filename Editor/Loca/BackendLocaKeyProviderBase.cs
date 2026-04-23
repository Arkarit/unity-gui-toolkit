using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Abstract base class for Editor-only ScriptableObjects that fetch localization keys from a
	/// backend HTTP endpoint and expose them to the Loca processor as an <see cref="ILocaKeyProvider"/>.
	///
	/// Usage:
	/// 1. Create a concrete subclass that overrides <see cref="ParseKeys"/> to extract keys from the
	///    JSON response of your endpoint.
	/// 2. Create a ScriptableObject asset from your subclass (Assets > Create > …) somewhere under
	///    <c>Assets/</c> (e.g. <c>Assets/Editor/Loca/</c>).
	/// 3. Fill in the <c>Api Base Url</c>, <c>Endpoint</c> and (optionally) <c>Group</c> fields.
	/// 4. If the endpoint requires authentication, set <c>Auth Token Editor Prefs Key</c> to the
	///    <see cref="EditorPrefs"/> key that holds a Bearer token.  The token itself is stored only
	///    in EditorPrefs — never in source control.
	/// 5. Run <em>Gui Toolkit &gt; Localization &gt; Process Loca Keys</em>.  The provider's keys are
	///    picked up automatically and written to the POT file.
	///
	/// Call <see cref="FetchAndRefresh"/> (via the context menu or in code) to invalidate the cache
	/// and re-fetch from the endpoint.
	/// </summary>
	public abstract class BackendLocaKeyProviderBase : ScriptableObject, ILocaKeyProvider
	{
		[Tooltip("Base URL of the backend API, e.g. https://preprod-api.example.com")]
		[SerializeField] private string m_apiBaseUrl;

		[Tooltip("Endpoint path, e.g. /api/v1/catalog/getAll")]
		[SerializeField] private string m_endpoint;

		[Tooltip("Loca group for all keys from this provider, e.g. Catalog. May be empty.")]
		[SerializeField] private string m_group;

		[Tooltip("EditorPrefs key that holds the Bearer token for authenticated endpoints. " +
		         "Leave empty if no auth is required. The token itself is NEVER stored in source control.")]
		[SerializeField] private string m_authTokenEditorPrefsKey;

		private List<string> m_cachedKeys;

		// ── ILocaKeyProvider ──────────────────────────────────────────────────────────────────────

		bool ILocaKeyProvider.UsesLocaKey => FetchIfNeeded().Count > 0;
		bool ILocaKeyProvider.UsesMultipleLocaKeys => true;
		string ILocaKeyProvider.LocaKey => string.Empty;
		List<string> ILocaKeyProvider.LocaKeys => FetchIfNeeded();
		string ILocaKeyProvider.Group => m_group;

		// ── Abstract / virtual ────────────────────────────────────────────────────────────────────

		/// <summary>
		/// Parse the raw JSON response body and return the localization keys it contains.
		/// </summary>
		protected abstract IEnumerable<string> ParseKeys( string responseJson );

		/// <summary>
		/// Override to customize the outgoing HTTP request (e.g. add extra headers).
		/// The default implementation sets the <c>Authorization: Bearer &lt;token&gt;</c> header
		/// when <c>Auth Token Editor Prefs Key</c> is configured and the token is non-empty.
		/// </summary>
		protected virtual void ConfigureRequest( HttpRequestMessage request )
		{
			if (string.IsNullOrEmpty(m_authTokenEditorPrefsKey))
				return;

			string token = EditorPrefs.GetString(m_authTokenEditorPrefsKey, string.Empty);
			if (!string.IsNullOrEmpty(token))
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		// ── Public API ────────────────────────────────────────────────────────────────────────────

		/// <summary>Invalidates the key cache and immediately fetches fresh data from the endpoint.</summary>
		[ContextMenu("Fetch / Refresh Keys")]
		public void FetchAndRefresh()
		{
			m_cachedKeys = null;
			FetchIfNeeded();
		}

		// ── Internal ──────────────────────────────────────────────────────────────────────────────

		private List<string> FetchIfNeeded()
		{
			if (m_cachedKeys != null)
				return m_cachedKeys;

			try
			{
				string url = m_apiBaseUrl.TrimEnd('/') + "/" + m_endpoint.TrimStart('/');
				using var client = new HttpClient();
				using var request = new HttpRequestMessage(HttpMethod.Get, url);
				ConfigureRequest(request);

				var response = client.SendAsync(request).Result;
				response.EnsureSuccessStatusCode();
				string json = response.Content.ReadAsStringAsync().Result;
				m_cachedKeys = new List<string>(ParseKeys(json));
				UiLog.Log($"[{GetType().Name}] Fetched {m_cachedKeys.Count} keys from {url}");
			}
			catch (Exception ex)
			{
				UiLog.LogError($"[{GetType().Name}] Failed to fetch keys from {m_apiBaseUrl}{m_endpoint}: {ex.Message}");
				m_cachedKeys = new List<string>();
			}

			return m_cachedKeys;
		}
	}
}
