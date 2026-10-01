using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ServerPasswordOnce.Config;
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
	/// Takes a banned player off the guest list.
	///
	/// A banned player is refused before the password is even looked at, so while the ban stands this
	/// changes nothing. It matters afterwards: without it, lifting a ban would let that player back in
	/// without the password, which is not what an admin means by unbanning somebody.
	/// </summary>
	[HarmonyPatch(typeof(ZNet), "InternalBan")]
	internal static class BanPatch
	{
		private static void Postfix(string user)
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer() || string.IsNullOrEmpty(user))
			{
				return;
			}

			// The same resolution the admin command uses: an id from the guest list, or the name of a
			// connected player. The ban has not disconnected them yet at this point.
			List<string> userIds = AdminCommand.Resolve(user);
			if (userIds.Count == 0)
			{
				Core.Log.LogInfo($"'{user}' was banned. No guest list entry matches, so there is nothing to remove.");
				return;
			}

			foreach (string userId in userIds)
			{
				string name = GuestRegistry.Describe(userId);
				if (GuestRegistry.Forget(userId))
				{
					Core.Log.LogInfo($"{name} was banned and taken off the guest list.");
				}
			}
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
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ZNet), "RPC_ServerHandshake");
			yield return AccessTools.Method(typeof(ZNet), "RPC_PeerInfo");
		}

		private static void Prefix(ZRpc rpc, out string __state, MethodBase __originalMethod)
		{
			__state = null;

			if (!GuestRegistry.Armed || ZNet.instance == null || !ZNet.instance.IsServer())
			{
				return;
			}

			if (!GuestIdentity.TryRead(rpc?.GetSocket(), out string userId, out _) || !GuestRegistry.Knows(userId))
			{
				return;
			}

			__state = ZNet.m_serverPassword;
			ZNet.m_serverPassword = string.Empty;

			// A password window that never opens leaves no trace anywhere. Without this line a working mod
			// and a mod that never ran look exactly the same in the log. An admin who finds it too chatty
			// turns it off; the first time a guest is added is logged either way.
			if (__originalMethod.Name == "RPC_ServerHandshake" && ServerPasswordOnceConfig.LogJoins.Value)
			{
				Core.Log.LogInfo($"{GuestRegistry.Describe(userId)} gave this password before and joins without the password window.");
			}
		}

		private static void Postfix(ZRpc rpc, string __state, MethodBase __originalMethod)
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

			if (GuestIdentity.TryRead(rpc.GetSocket(), out string userId, out string platformId))
			{
				GuestRegistry.Remember(userId, platformId);
			}
		}
	}
}
