using System;

namespace ServerPasswordOnce.Runtime
{
	/// <summary>
	/// Who is behind a connection, in the form the guest list is keyed on.
	///
	/// On Steam the host name of the socket is the Steam id. It comes from the transport and the game
	/// verifies a session ticket for it.
	///
	/// On a crossplay server the host name is the platform id (`Steam_1234`, `Xbox_1234`). The client sends
	/// that value in a data message and the game accepts it without a check, so it cannot say who may skip
	/// the password. The end point of the socket holds the PlayFab player id of the remote player. That one
	/// comes from the PlayFab Party library: the relay server of PlayFab lets a user into the network only
	/// with a login token that belongs to that id. The guest list is keyed on it, and the platform id is
	/// kept beside it because the ban and admin lists of the game use the platform id.
	///
	/// Every value is read through `ISocket`, never from the socket class. Other mods put a socket of their
	/// own in place of the real one while the game handles `RPC_PeerInfo`. ServerSync, which many mods
	/// carry, does that with a class that derives from `ZPlayFabSocket` on every backend and passes only the
	/// interface calls on to the real socket. The fields and the class methods of such a socket are empty.
	/// </summary>
	internal static class GuestIdentity
	{
		private const string PlayFabEndPoint = "playfab/";

		/// <summary>
		/// Reads the key of a connection and, where it differs, the platform id. Returns false when the
		/// connection has no usable key; that connection is asked for the password as usual.
		/// </summary>
		internal static bool TryRead(ISocket socket, out string key, out string platformId)
		{
			key = null;
			platformId = string.Empty;

			if (socket == null)
			{
				Core.Log.LogWarning("A connection has no socket, so it is asked for the password as usual.");
				return false;
			}

			string hostName = socket.GetHostName();

			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				string endPoint = socket.GetEndPointString();
				if (endPoint == null || !endPoint.StartsWith(PlayFabEndPoint, StringComparison.Ordinal) || endPoint.Length == PlayFabEndPoint.Length)
				{
					Core.Log.LogWarning($"A crossplay connection reported no PlayFab player id (end point '{endPoint}'), so it is asked for the password as usual.");
					return false;
				}

				key = endPoint.Substring(PlayFabEndPoint.Length);
				platformId = hostName ?? string.Empty;
				return true;
			}

			key = hostName;
			if (string.IsNullOrEmpty(key))
			{
				Core.Log.LogWarning("A connection reported no host name, so it is asked for the password as usual.");
				return false;
			}
			return true;
		}

		/// <summary>The name of a guest for a log line: the id an admin knows first, then the key.</summary>
		internal static string Describe(string key, string platformId)
			=> string.IsNullOrEmpty(platformId) || platformId == key ? key : $"{platformId} (PlayFab id {key})";
	}
}
