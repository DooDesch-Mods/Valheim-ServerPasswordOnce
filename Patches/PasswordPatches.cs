using HarmonyLib;
using ServerPasswordOnce.Runtime;

namespace ServerPasswordOnce.Patches
{
	/// <summary>
	/// Catches the password the server was started with.
	///
	/// The game hashes it in the same line it receives it, with a salt drawn fresh on every start, so the
	/// stored hash of the game cannot be compared across restarts. The plain value exists only here.
	/// </summary>
	[HarmonyPatch(typeof(ZNet), nameof(ZNet.SetServer))]
	internal static class SetServerPatch
	{
		private static void Postfix(bool server, string password)
		{
			if (!server)
			{
				return;
			}
			GuestRegistry.UsePassword(password);
		}
	}

	/// <summary>
	/// Lets a known guest past the password, in both places the game checks it.
	///
	/// Both checks read one field: the handshake sends "you need a password" when it is not empty, and the
	/// join compares what the client sent against it. Emptying that field for the length of a single call is
	/// therefore the whole trick, and it needs no copy of the logic of the game. The client that is told it
	/// needs no password sends an empty one, which then matches.
	///
	/// Remote procedure calls are handled one after another on the main thread, so no other connection sees
	/// the field while it is empty.
	/// </summary>
	[HarmonyPatch]
	internal static class KnownGuestPatch
	{
		private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ZNet), "RPC_ServerHandshake");
			yield return AccessTools.Method(typeof(ZNet), "RPC_PeerInfo");
		}

		private static void Prefix(ZRpc rpc, out string __state, System.Reflection.MethodBase __originalMethod)
		{
			__state = null;

			if (!GuestRegistry.Armed || ZNet.instance == null || !ZNet.instance.IsServer())
			{
				return;
			}

			string userId = HostNameOf(rpc);
			if (userId == null || !GuestRegistry.Knows(userId))
			{
				return;
			}

			__state = ZNet.m_serverPassword;
			ZNet.m_serverPassword = string.Empty;

			// A password window that never opens leaves no trace anywhere. Without this line a working mod
			// and a mod that never ran look exactly the same in the log.
			if (__originalMethod.Name == "RPC_ServerHandshake")
			{
				Core.Log.LogInfo($"{userId} gave this password before and joins without the password window.");
			}
		}

		private static void Postfix(ZRpc rpc, string __state, System.Reflection.MethodBase __originalMethod)
		{
			if (__state != null)
			{
				ZNet.m_serverPassword = __state;
			}

			if (__originalMethod.Name != "RPC_PeerInfo" || !GuestRegistry.Armed)
			{
				return;
			}

			// A peer that was refused never gets an identifier, so this is the point where a visit counts as
			// a successful one.
			ZNet net = ZNet.instance;
			if (net == null || !net.IsServer())
			{
				return;
			}

			ZNetPeer peer = net.GetPeer(rpc);
			if (peer == null || peer.m_uid == 0L)
			{
				return;
			}

			string userId = HostNameOf(rpc);
			if (userId != null)
			{
				GuestRegistry.Remember(userId);
			}
		}

		/// <summary>The identity behind a connection, the same one the ban and admin lists are keyed on.</summary>
		private static string HostNameOf(ZRpc rpc)
		{
			ISocket socket = rpc?.GetSocket();
			string host = socket?.GetHostName();
			if (string.IsNullOrEmpty(host))
			{
				Core.Log.LogWarning("A connection reported no host name, so it is asked for the password as usual.");
				return null;
			}
			return host;
		}
	}
}
