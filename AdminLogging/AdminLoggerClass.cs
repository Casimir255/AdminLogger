using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using static Sandbox.Game.Entities.MyCubeGrid;

namespace AdminLogger.AdminLogging
{
    [HarmonyPatch]
    public static class AdminLoggerClass
    {
        private static readonly Logger Log = LogManager.GetLogger("AdminLogger");



        public static void ApplyPatch(Harmony ctx)
        {
            //Dumb ass types (I can use harmony but idc)
            Type type = typeof(MyGuiScreenDebugBase).Assembly.GetType("Sandbox.Game.Gui.MyGuiScreenDebugSpawnMenu");
            if (type == null)
            {
                throw new InvalidOperationException("Couldn't find MyGuiScreenDebugSpawnMenu");
            }

            var RequestSpawnCreativeCargoMethod = type.GetMethod("SpawnIntoContainer_Implementation", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, new Type[4] { typeof(long), typeof(SerializableDefinitionId), typeof(long), typeof(long) }, null);
            if (RequestSpawnCreativeCargoMethod == null)
            {
                throw new InvalidOperationException("Couldn't find EnableCreativeTools");
            }

            ctx.Patch(RequestSpawnCreativeCargoMethod, postfix: new HarmonyMethod(Method(nameof(SpawnIntoContainer))));

            var OnTeleport = typeof(MyMultiplayer).GetMethod("OnTeleport", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, new Type[2] { typeof(ulong), typeof(Vector3D) }, null);
            if (OnTeleport == null)
            {
                throw new InvalidOperationException("Couldn't find OnTeleport");
            }

            ctx.Patch(OnTeleport, postfix: new HarmonyMethod(Method(nameof(TeleportRequest))));
        }


        private static MethodInfo Method(string v)
        {
            return typeof(AdminLoggerClass).GetMethod(v, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyBuildComponentBase), "BeforeCreateBlock")]
        private static void PlacedBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob, bool buildAsAdmin)
        {
            if (buildAsAdmin == true)
            {
                string Builder = builder.DisplayName;
                string block = ob.SubtypeName;

                Log.Warn(string.Format("{0} placed {1} in creative mode!", Builder, block));
            }

        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyGuiScreenAdminMenu), "AdminSettingsChanged")]
        private static void AdminModeChanged(AdminSettingsEnum settings)
        {

            ulong steamId = MyEventContext.Current.Sender.Value;
            if (MySession.Static.OnlineMode != 0 && (((settings & AdminSettingsEnum.AdminOnly) > AdminSettingsEnum.None && !MySession.Static.IsUserAdmin(steamId)) || !MySession.Static.IsUserModerator(steamId)))
            {
                Log.Warn($"Player {steamId} tried to change their admins settings but they arent an admin or mod. Are they cheating?");
                return;
            }




            AdminSettingsEnum OldSettings = new AdminSettingsEnum();
            try
            {
                //Possible that they arent and admin originially
                OldSettings = MySession.Static.RemoteAdminSettings[steamId];
            }
            catch
            {

            }

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(steamId);


            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }



            string PlayerName = Player.DisplayName;

            if (OldSettings.HasFlag(AdminSettingsEnum.IgnorePcu) != settings.HasFlag(AdminSettingsEnum.IgnorePcu))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.IgnorePcu) == true)
                {
                    Log.Warn(string.Format("{0} disabled IgnorePCULimits", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled IgnorePCULimits", PlayerName));
                }
            }

            if (OldSettings.HasFlag(AdminSettingsEnum.IgnoreSafeZones) != settings.HasFlag(AdminSettingsEnum.IgnoreSafeZones))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.IgnoreSafeZones) == true)
                {
                    Log.Warn(string.Format("{0} disabled IgnoreSafeZones", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled IgnoreSafeZones", PlayerName));
                }
            }

            if (OldSettings.HasFlag(AdminSettingsEnum.Invulnerable) != settings.HasFlag(AdminSettingsEnum.Invulnerable))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.Invulnerable) == true)
                {
                    Log.Warn(string.Format("{0} disabled Invulnerable", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled Invulnerable", PlayerName));
                }
            }

            if (OldSettings.HasFlag(AdminSettingsEnum.KeepOriginalOwnershipOnPaste) != settings.HasFlag(AdminSettingsEnum.KeepOriginalOwnershipOnPaste))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.KeepOriginalOwnershipOnPaste) == true)
                {
                    Log.Warn(string.Format("{0} disabled KeepOriginalOwnershipOnPaste", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled KeepOriginalOwnershipOnPaste", PlayerName));
                }
            }

            if (OldSettings.HasFlag(AdminSettingsEnum.ShowPlayers) != settings.HasFlag(AdminSettingsEnum.ShowPlayers))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.ShowPlayers) == true)
                {
                    Log.Warn(string.Format("{0} disabled ShowAllPlayers", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled ShowAllPlayers", PlayerName));
                }
            }


            if (OldSettings.HasFlag(AdminSettingsEnum.Untargetable) != settings.HasFlag(AdminSettingsEnum.Untargetable))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.Untargetable) == true)
                {
                    Log.Warn(string.Format("{0} disabled Untargetable", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled Untargetable", PlayerName));
                }
            }


            if (OldSettings.HasFlag(AdminSettingsEnum.UseTerminals) != settings.HasFlag(AdminSettingsEnum.UseTerminals))
            {
                if (OldSettings.HasFlag(AdminSettingsEnum.UseTerminals) == true)
                {
                    Log.Warn(string.Format("{0} disabled CanUseAllTerminals", PlayerName));
                }
                else
                {
                    Log.Warn(string.Format("{0} enabled CanUseAllTerminals", PlayerName));
                }
            }

        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MySession), "OnCreativeToolsEnabled")]
        private static void CreativeChanged(bool value)
        {

            ulong value2 = MyEventContext.Current.Sender.Value;


            bool PreeviousUse = MySession.Static.CreativeToolsEnabled(value2);

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(value2);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            string PlayerName = Player.DisplayName;

            if (PreeviousUse != value)
            {
                if (PreeviousUse == false)
                {
                    Log.Warn(string.Format("{0} enabled CreativeModeTools", PlayerName));
                }
                else
                {

                    Log.Warn(string.Format("{0} disabled CreativeModeTools", PlayerName));
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyFloatingObjects), "RequestSpawnCreative_Implementation")]
        private static bool RequestItemSpawn(MyObjectBuilder_FloatingObject obj)
        {
            ulong player = MyEventContext.Current.Sender.Value;

            if (MySession.Static.CreativeMode || MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(player))
            {
                MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(player);

                //Sanity Vibe Checks
                if (Player == null || Player.DisplayName == null)
                {
                    return false;
                }


                MyEntity d = MyEntities.CreateFromObjectBuilderAndAdd(obj, false);
                string ObjectName = d.DisplayName;

                Log.Warn(string.Format("{0} spawned {1} {2}", Player.DisplayName, obj.Item.Amount, ObjectName));
                return false;
            }
            else
            {
                (MyMultiplayer.Static as MyMultiplayerServerBase).ValidationFailed(MyEventContext.Current.Sender.Value);
                Log.Warn(player + " attempted to spawn in items! Is this player hacking?");
            }

            return false;
        }


        private static void SpawnIntoContainer(long amount, SerializableDefinitionId item, long entityId, long playerId)
        {
            MyIdentity Player = MySession.Static.Players.TryGetIdentity(playerId);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }
 
            Log.Warn($"{item.TypeId} - {item.SubtypeId}");

            Log.Warn(string.Format("{0} spawned {1} {2} into container", Player.DisplayName, amount, item.SubtypeName));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyCubeGrid), "TryPasteGrid_Implementation")]
        private static void AfterSpawnGrid(MyPasteGridParameters parameters)
        {


            ulong PlayerID = MyEventContext.Current.Sender.Value;

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(PlayerID);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            StringBuilder b = new StringBuilder();
            b.AppendLine();
            foreach (MyObjectBuilder_CubeGrid grid in parameters.Entities)
            {
                if (grid == null || grid.DisplayName == null || grid.DisplayName == "")
                    continue;

                b.AppendLine(string.Format("{0} - {1} blocks", grid.DisplayName, grid.CubeBlocks.Count));
            }

            Log.Warn(string.Format("{0} spawned the following grids: {1}", Player.DisplayName, b.ToString()));


        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyCubeGrid), "OnGridClosedRequest")]
        private static void GridClose(MyCubeGrid __instance)
        {
            ulong PlayerID = MyEventContext.Current.Sender.Value;
            if (PlayerID == 0)
            {
                return;
            }


            if (!MySession.Static.IsUserAdmin(PlayerID))
            {
                //User is not an admin. (Could someone else delete stuff?)
                return;
            }

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(PlayerID);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            string PlayerName = Player.DisplayName;
            string GridNamne = __instance.DisplayName;


            Log.Warn(string.Format("{0} removed grid {1}", PlayerName, GridNamne));


        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyGuiScreenAdminMenu), "RequestChange")]
        private static void BalanceChange(long accountOwner, long balanceChange)
        {
            ulong PlayerID = MyEventContext.Current.Sender.Value;
            if (PlayerID == 0)
            {
                return;
            }

            if (!MySession.Static.IsUserAdmin(PlayerID))
            {
                //User is not an admin. (Could someone else delete stuff?)
                return;
            }

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(PlayerID);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            string PlayerName = Player.DisplayName;

            MyAccountInfo account;
            MyBankingSystem.Static.TryGetAccountInfo(accountOwner, out account);

            MyIdentity ToPlayer = MySession.Static.Players.TryGetIdentity(accountOwner);

            Log.Warn(string.Format("{0} requested balance change of {1}sc on player {2}", PlayerName, balanceChange, ToPlayer.DisplayName));
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyGuiScreenAdminMenu), "RequestChangeReputation")]
        private static void RepChange(long identityId, long factionId, int reputationChange, bool shouldPropagate)
        {
            ulong PlayerID = MyEventContext.Current.Sender.Value;
            if (PlayerID == 0)
            {
                return;
            }

            if (!MySession.Static.IsUserAdmin(PlayerID))
            {
                //User is not an admin. (Could someone else delete stuff?)
                return;
            }

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(PlayerID);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            string PlayerName = Player.DisplayName;
            IMyFaction fac = MySession.Static.Factions.TryGetFactionById(factionId);
            MyIdentity ToPlayer = MySession.Static.Players.TryGetIdentity(identityId);

            Log.Warn(string.Format("{0} requested reputation change of {1} on player {2} of faction {3}", PlayerName, reputationChange, ToPlayer.DisplayName, fac.Tag));
        }

        private static void TeleportRequest(ulong userId, Vector3D location)
        {
            ulong PlayerID = MyEventContext.Current.Sender.Value;
            if (PlayerID == 0)
            {
                return;
            }

            if (!MySession.Static.IsUserAdmin(PlayerID))
            {
                //User is not an admin. (Could someone else delete stuff?)
                return;
            }

            MyPlayer Player = MySession.Static.Players.TryGetPlayerBySteamId(PlayerID);

            //Sanity Vibe Checks
            if (Player == null || Player.DisplayName == null)
            {
                return;
            }

            string PlayerName = Player.DisplayName;

            Log.Warn(string.Format("{0} teleported to {1}", PlayerName, location.ToString()));

        }




    }
}
