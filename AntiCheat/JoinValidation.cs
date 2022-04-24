using AdminLogger.Utils;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VRage.GameServices;
using VRage.Network;
using Torch.Server.Managers;
using System.Reflection;
using Steamworks;

namespace AdminLogger.AntiCheat
{
    public class JoinValidation
    {
        private static readonly Logger Log = LogManager.GetLogger("AdminLogger");
        public static readonly Type MySteamServerDiscovery = Type.GetType("VRage.Steam.MySteamGameServer, Vrage.Steam");


        public JoinValidation()
        {
            MethodInfo p =  MySteamServerDiscovery.GetMethod("NotifyValidateAuthTicketResponse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);



            Patcher.ctx.GetPattern(p).Prefixes.Add(GetPatchMethod(nameof(NotifyValidateAuthTicketResponse)));
            //Patcher.PrePatch<MultiplayerManagerDedicated>("ValidateAuthTicketResponse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, nameof(GameServer_ValidateAuthTicketResponse));
        }

        private static MethodInfo GetPatchMethod(string v)
        {
            return typeof(JoinValidation).GetMethod(v, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }


        private static void GameServer_ValidateAuthTicketResponse(ulong steamID, JoinResult response, ulong steamOwner, string serviceName)
        {
            var state = new MyP2PSessionState();
            MyGameService.Peer2Peer.GetSessionState(steamID, ref state);
            var ip = new IPAddress(BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray());


            IPAddress i = IPAddress.Parse("0.0.0.0");

            Log.Warn(i.GetAddressBytes());
            Log.Warn(Environment.StackTrace);

        }

		private static bool NotifyValidateAuthTicketResponse(ValidateAuthTicketResponse_t response)
		{


            var state = new MyP2PSessionState();
            MyGameService.Peer2Peer.GetSessionState(response.m_OwnerSteamID.m_SteamID, ref state);
            var ip = new IPAddress(BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray());
            IPAddress i = IPAddress.Parse("0.0.0.0");

            bool isValid = response.m_OwnerSteamID.IsValid();
            Log.Info($"ResponseID: {response.m_SteamID} - ResponseOwnerID: {response.m_OwnerSteamID} - ValidOwner: {isValid} IP:{ip.ToString()}");

            if (isValid == false || ip.ToString() == "0.0.0.0")
                return false;


            //Log.Warn(Environment.StackTrace);
            return true;
        }


	}
}
