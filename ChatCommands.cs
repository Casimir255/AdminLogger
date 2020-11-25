using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Network;

namespace AdminLogger
{

    [Torch.Commands.Category("admin")]
    public class ChatCommands : CommandModule
    {
        [Command("listadminsettings", "Lists the current enabled admin settings for the given user")]
        [Permission(MyPromoteLevel.Admin)]

        public void GetRemoteAdminSettings(string NameOrId)
        {
            AdminSettingsEnum PlayerSettings = new AdminSettingsEnum();
            ulong Result = 0;

            if (!UInt64.TryParse(NameOrId, out Result))
            {
                MyIdentity ID = MySession.Static.Players.GetAllIdentities().FirstOrDefault(x => x.DisplayName.Equals(NameOrId));
                Result = MySession.Static.Players.TryGetSteamId(ID.IdentityId);
            }


            if(Result == 0)
            {
                Context.Respond("Invalid Input: " + NameOrId);
                return;
            }


            if (!MySession.Static.RemoteAdminSettings.ContainsKey(Result))
            {
                Context.Respond("There are no registered admin settings applied for this player");
                return;
            }

            StringBuilder Response = new StringBuilder();
            Response.AppendLine("Current Admin Settings for User: " + NameOrId);
            PlayerSettings = MySession.Static.RemoteAdminSettings[Result];


            bool CreativeTools = MySession.Static.CreativeToolsEnabled(Result);
            Response.AppendLine("CreativeTools: " + CreativeTools);


            bool IgnorePCU = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.IgnorePcu)){
                IgnorePCU = true;
            }
            Response.AppendLine("IgnorePCU: " + IgnorePCU);

            bool IgnoreSafeZones = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.IgnoreSafeZones))
            {
                IgnoreSafeZones = true;
            }
            Response.AppendLine("IgnoreSafeZones: " + IgnoreSafeZones);

            bool Invulnerable = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.Invulnerable))
            {
                Invulnerable = true;
            }
            Response.AppendLine("Invulnerable: " + Invulnerable);

            bool KeepOriginalOwnershipOnPaste = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.KeepOriginalOwnershipOnPaste))
            {
                KeepOriginalOwnershipOnPaste = true;
            }
            Response.AppendLine("KeepOriginalOwnershipOnPaste: " + KeepOriginalOwnershipOnPaste);

            bool ShowPlayers = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.ShowPlayers))
            {
                ShowPlayers = true;
            }
            Response.AppendLine("ShowPlayers: " + ShowPlayers);

            bool Untargetable = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.Untargetable))
            {
                Untargetable = true;
            }
            Response.AppendLine("Untargetable: " + Untargetable);

            bool UseTerminals = false;
            if (PlayerSettings.HasFlag(AdminSettingsEnum.UseTerminals))
            {
                UseTerminals = true;
            }
            Response.AppendLine("UseTerminals: " + UseTerminals);

            Context.Respond(Response.ToString());

        }

            
        [Command("clearadminsettings", "Lists the current enabled admin settings for the given user")]
        [Permission(MyPromoteLevel.Admin)]
        public void ClearAdminSettingsForUser(string NameOrId)
        {
            ulong Result = 0;
            if (!UInt64.TryParse(NameOrId, out Result))
            {
                MyIdentity ID = MySession.Static.Players.GetAllIdentities().FirstOrDefault(x => x.DisplayName.Equals(NameOrId));
                Result = MySession.Static.Players.TryGetSteamId(ID.IdentityId);
            }

            if (Result == 0)
            {
                Context.Respond("Invalid Input: " + NameOrId);
                return;
            }


            if (!MySession.Static.RemoteAdminSettings.ContainsKey(Result))
            {
                Context.Respond("There are no registered admin settings applied for this player");
                return;
            }

            AdminSettingsEnum PlayerSettings = new AdminSettingsEnum();

            MySession.Static.RemoteAdminSettings[Result] = PlayerSettings;

            MySession.Static.EnableCreativeTools(Result, false);
            MethodInfo P = typeof(MyGuiScreenAdminMenu).GetMethod("AdminSettingsChangedClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Events.RaiseStaticEvent<AdminSettingsEnum, ulong>(P, PlayerSettings, Result, new EndpointId(Result));

            Context.Respond("Successfully reset admin settings!");
        }
    }
}
