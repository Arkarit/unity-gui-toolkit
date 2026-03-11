#if UNITY_EDITOR
using NUnit.Framework;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="GoogleServiceAccountAuth"/>.
	/// <para>
	/// All private authentication logic (RSA key import, JWT building, token exchange)
	/// requires real service-account credentials and network access, so only the public
	/// surface that can be exercised without credentials is tested here.
	/// </para>
	/// </summary>
	public class TestGoogleServiceAccountAuth
	{
		[Test]
		public void GetAccessToken_Null_ReturnsNull()
		{
			// A null path must return null gracefully — no exception, no LogError.
			string token = GoogleServiceAccountAuth.GetAccessToken(null);
			Assert.IsNull(token,
				"GetAccessToken(null) must return null without throwing");
		}

		[Test]
		public void GetAccessToken_Empty_ReturnsNull()
		{
			// An empty path must return null gracefully — no exception, no LogError.
			string token = GoogleServiceAccountAuth.GetAccessToken(string.Empty);
			Assert.IsNull(token,
				"GetAccessToken(\"\") must return null without throwing");
		}
	}
}
#endif
