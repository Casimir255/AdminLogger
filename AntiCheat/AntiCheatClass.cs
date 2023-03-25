using AdminLogger.Utils;
using HarmonyLib;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Torch;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Replication;
using VRageMath;
using static Sandbox.Game.Entities.MyCubeGrid;

namespace AdminLogger.AdminLogging
{
    public class AntiCheatClass
    {
        private static readonly Logger Log = LogManager.GetLogger("AdminLogger");

        public static Type MyClientType = Type.GetType("VRage.Network.MyClient, VRage");
        public static Type MyCubeGridReplicable = Type.GetType("Sandbox.Game.Replication.MyCubeGridReplicable, Sandbox.Game");

        private static PropertyInfo GridJumpSystemProp = typeof(MyUpdateableGridSystem).GetProperty("Grid");

        private static FieldInfo m_clientStates;
        private static FieldInfo Replicables;
        private static PropertyInfo UpdatableGrid;

        private static MethodInfo removeForClient;
        private static MethodInfo forceForClient;
        private static MethodInfo OnPerfomJump;


        private static int BetterNotCopyThisDori = 1;


        private static Dictionary<ulong, int> stockPileFill = new Dictionary<ulong, int>();
        private static Dictionary<ulong, int> buildBlockRequest = new Dictionary<ulong, int>();


        private static Timer frequencyTimer = new Timer(1000);

        public static void ApplyPatching()
        {
            if (!Main.Config.AntCheat)
                return;


            Patcher.PrePatch<MyCubeGrid>("OnStockpileFillRequest", BindingFlags.Instance | BindingFlags.NonPublic, nameof(OnStockpileFillRequest));

            //Patcher.PrePatch<MyGridJumpDriveSystem>("PerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(BeforeJump));

            Patcher.PrePatch<MyTimerBlock>("Start", BindingFlags.Instance | BindingFlags.Public, nameof(Start));
            Patcher.PrePatch<MyTimerBlock>("Trigger", BindingFlags.Instance | BindingFlags.NonPublic, nameof(Trigger));

            // Jump Drive Tracking fix 
            m_clientStates = typeof(MyReplicationServer).GetField("m_clientStates", BindingFlags.Instance | BindingFlags.NonPublic);
            Replicables = MyClientType.GetField("Replicables", BindingFlags.Instance | BindingFlags.Public);
            removeForClient = typeof(MyReplicationServer).GetMethod("RemoveForClient", BindingFlags.NonPublic | BindingFlags.Instance);
            UpdatableGrid = typeof(MyUpdateableGridSystem).GetProperty("Grid", BindingFlags.Instance | BindingFlags.NonPublic);


            //Patcher.SuffixPatch<MyMultiplayerServerBase>("ValidationFailed", BindingFlags.Instance | BindingFlags.Public, nameof(ValidationFailed));
            //Patcher.PrePatch<MyGridJumpDriveSystem>("PerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(BeforeJump));
            
            //Patcher.PrePatch<MyGridJumpDriveSystem>("SendPerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(SendPerformJump));
            //OnPerfomJump = Patcher.GetMethod<MyGridJumpDriveSystem>("OnPerformJump", BindingFlags.Static | BindingFlags.NonPublic);

        }



        //Calls every time you fill a block from your inventory
        private static bool OnStockpileFillRequest(Vector3I blockPosition, long ownerEntityId, byte inventoryIndex)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;

            ulong EventOwner = MyEventContext.Current.Sender.Value;
            if (stockPileFill.ContainsKey(EventOwner))
            {
                stockPileFill[EventOwner] += 1;
            }
            else
            {
                stockPileFill.Add(EventOwner, 1);
            }

           
            Log.Warn($"{EventOwner} Requested fill stockpile");
            return true;
        }


        //Called to remove blocks (Creative request)
        private static bool RazeBlocksRequest(List<Vector3I> locations, long builderEntityId = 0L, ulong user = 0uL)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;


            ulong EventOwner = MyEventContext.Current.Sender.Value;
            if (MySession.Static.HasPlayerCreativeRights(EventOwner))
                return true;


            banClient(EventOwner, $"tried to remove {locations.Count} blocks using keen exploit and is not admin! Blocking and banning!");
            return false;
        }

        private static bool SendPerformJump(MyGridJumpDriveSystem __instance, Vector3D jumpTarget)
        {
            MyCubeGrid grid = (MyCubeGrid)UpdatableGrid.GetValue(__instance);


            if (Vector3D.Distance(grid.PositionComp.GetPosition(), jumpTarget) < MySession.Static.Settings.SyncDistance)
                return false;


            List<MyCubeGrid> allGrids = grid.GetConnectedGrids(VRage.Game.ModAPI.GridLinkTypeEnum.Logical);

            MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(grid.PositionComp.LocalAABB, grid.WorldMatrix);
            List<MyEntity> Entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInOBB(ref obb, Entities);

            //Loop through surrounding players
            List<ulong> AttachedPlayers = new List<ulong>();
            foreach (MyEntity item in Entities)
            {
                if (item is MyCharacter Character && Character.Parent == null && !Character.IsBot)
                {
                    Log.Info($"Char: {Character.ControlSteamId}");
                    AttachedPlayers.Add(Character.ControlSteamId);
                }
            }

            //Loop through all cockpits
            foreach (var s in allGrids)
            {
                foreach (var p in s.GetFatBlocks<MyCockpit>())
                {

                    if (p.Pilot == null || p.Pilot.ControlSteamId == 0)
                        continue;

                    //Log.Info($"Seat {p.Pilot.ControlSteamId}");
                    AttachedPlayers.Add(p.Pilot.ControlSteamId);
                }
            }




            var replicationServer = (MyReplicationServer)MyMultiplayer.ReplicationLayer;
            //Loop through all online players to remove grid from being synced
            foreach (MyPlayer playerID in MySession.Static.Players.GetOnlinePlayers())
            {
                if (AttachedPlayers.Count != 0 && AttachedPlayers.Contains(playerID.Client.SteamUserId))
                    continue;

                Log.Warn($"Trying to remove for {playerID.Client.DisplayName}-{playerID.Client.SteamUserId}");


                var playerEndpoint = new Endpoint(playerID.Client.SteamUserId, 0);


                //ConcurrentDictionary<Endpoint, MyClient>

                //Type dictType = typeof(IDictionary<,>).MakeGenericType(typeof(Endpoint), MyClientType);
                //IDictionary dict = (IDictionary)Activator.CreateInstance(dictType);


                IDictionary clientDataDict = (IDictionary)m_clientStates.GetValue(replicationServer);
                object clientData;
                try
                {
                    clientData = clientDataDict[playerEndpoint];

                    if (clientData == null)
                        throw new NullReferenceException("Client data is null!");
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return true;
                }

                var clientReplicables = (MyConcurrentDictionary<IMyReplicable, MyReplicableClientData>)Replicables.GetValue(clientData);

                var replicableList = new List<IMyReplicable>(clientReplicables.Count);
                foreach (var pair in clientReplicables)
                    replicableList.Add(pair.Key);

                Log.Warn($"Trying to remove {replicableList.Count} replicables!");
                foreach (var replicable in replicableList)
                {

                    if (replicable.GetType() != MyCubeGridReplicable)
                        continue;

                    Log.Info($"Replicable type: {replicable.GetType()} - name: {replicable.InstanceName}!");
                    removeForClient.Invoke(replicationServer, new object[] { replicable, clientData, true });
                }
            }


            //Send perfom jump
            foreach (var player in AttachedPlayers)
            {
                //Log.Warn($"Sending Perfom jump to {player}");
                Events.RaiseStaticEvent<long, Vector3D>(OnPerfomJump, grid.EntityId, jumpTarget, new EndpointId(player), null);
            }

            return false;
        }


        /// <summary>
        /// Adds a entry into the built in keen cheater menu.
        /// </summary>
        /// <param name="steamid">Steamid of detected player.</param>
        /// <param name="explanation">Description of suspicious activity detected.</param>
        private static void AddCheater(ulong steamid, string explanation)
        {
            LinkedList<ValidationFailedRecord> lastValidationErrors = (LinkedList<ValidationFailedRecord>)typeof(MyMultiplayerServerBase).GetField("LastValidationErrors", BindingFlags.Static | BindingFlags.NonPublic).GetValue(MyMultiplayerServerBase.Instance);

            ValidationFailedRecord validationError = new ValidationFailedRecord((uint)lastValidationErrors.Count, steamid, DateTime.Now, explanation);
            lastValidationErrors.AddLast(validationError);
        }

        /// <summary>
        /// Fix for the character death bag entity dupe.
        /// </summary>
        [HarmonyPatch(typeof(MyInventorySpawnComponent), "SpawnBackpack")]
        class SpawnBackpackPatch
        {
            static void Postfix(MyInventorySpawnComponent __instance, MyEntity obj)
            {
                __instance.Character.GetInventory().Clear(true);
            }
        }

        /// <summary>
        /// Fixes for a few jump drive hacks.
        /// </summary>
        [HarmonyPatch(typeof(MyGridJumpDriveSystem), "OnRequestJumpFromClient")]
        class JumpPatch
        {
            static bool Prefix(Vector3D jumpTarget, long userId, float jumpDriveDelay)
            {
                if (!Main.Config.AntCheat)
                    return true;

                var gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(jumpTarget);
                var player = MySession.Static.Players.TryGetPlayerBySteamId(MyEventContext.Current.Sender.Value);

                if (gravity != Vector3.Zero)
                {
                    banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to jump into gravity.");
                    AddCheater(MyEventContext.Current.Sender.Value, "Tried to jump into gravity.");
                    return false;
                }
                if (MyGravityProviderSystem.CalculateNaturalGravityInPoint(player.GetPosition()) != Vector3.Zero)
                {
                    banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to jump out of gravity.");
                    AddCheater(MyEventContext.Current.Sender.Value,"Tried to jump out of gravity.");
                    return false;
                }
                if (jumpDriveDelay != 10f)
                {
                    banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to jump with a non default delay of {jumpDriveDelay}.");
                    AddCheater(MyEventContext.Current.Sender.Value, "Non default jump delay.");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Fixes the DropItem and RemoveItemsAt duplication exploits.
        /// </summary>
        [HarmonyPatch(typeof(MyInventory), "RemoveItems")]
        class RemoveItemsPatch
        {
            static bool Prefix(MyInventory __instance, uint itemId, MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false, MatrixD? spawnPos = null, Action<MyDefinitionId, MyEntity> itemSpawned = null)
            {
                if (!Main.Config.AntCheat)
                    return true;

                var item = __instance.GetItemByID(itemId);
                var player = MySession.Static.Players.TryGetPlayerBySteamId(MyEventContext.Current.Sender.Value);

                if (item == null | amount == 0)
                    return true;

                if (spawn == true && item.Value.Amount < amount)
                {
                    var itemAmount = amount - item.Value.Amount;
                    AddCheater(MyEventContext.Current.Sender.Value, "Tried to duplicate items.");
                    banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to duplicate {itemAmount} {item.Value.Content.SubtypeName}.");
                    return false;
                }
                return true;
            }
        }


        /// <summary>
        /// Fix for Tada's bullshit.
        /// </summary>
        [HarmonyPatch(typeof(MyCubeGrid), "BuildBlockRequestInternal")]
        class BuildBlockRequestPatch
        {
            static bool Prefix(MyCubeGrid.MyBlockVisuals visuals, MyCubeGrid.MyBlockLocation location, MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId, bool instantBuild, long ownerId, ulong sender, bool isProjection = false)
            {
                if (!Main.Config.AntCheat)
                    return true;

                if (!isProjection)
                {
                    blockObjectBuilder.SetupForProjector();
                    var builder = blockObjectBuilder as MyObjectBuilder_TerminalBlock;
                    var player = MySession.Static.Players.TryGetPlayerBySteamId(MyEventContext.Current.Sender.Value);

                    if (builder.BuiltBy != player.Identity.IdentityId)
                    {
                        banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to build block(s) without their own authership.(Potential dupe)");
                        AddCheater(MyEventContext.Current.Sender.Value, "Built block(s) without their own authership.");
                        return false;
                    }
                    banClient(player.Id.SteamId, $"{player.DisplayName}:{MyEventContext.Current.Sender.Value} Tried to spawn in block via non accessible means (Potential dupe)");
                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(MyGuiScreenSafeZoneFilter), "EntityListRequest")]
        class EntityListRequestPatch
        {
            public static bool enabled = true;

            static bool Prefix(MyEntityList.MyEntityTypeEnum selectedType)
            {
                if (!enabled || !Main.Config.AntCheat)
                    return true;

                var entityListResponse = typeof(MyGuiScreenSafeZoneFilter).GetMethod("EntityListResponse", BindingFlags.NonPublic | BindingFlags.Static);
                var steamId = MyEventContext.Current.Sender.Value;
                MyPlayer player = null;
                MySession.Static.Players.TryGetPlayerBySteamId(steamId, out player);

                List<MyEntityList.MyEntityListShortInfoItem> list = new List<MyEntityList.MyEntityListShortInfoItem>();
                list.Add(new MyEntityList.MyEntityListShortInfoItem($"Sorry {player.DisplayName},", long.MaxValue));
                list.Add(new MyEntityList.MyEntityListShortInfoItem("Pools Closed.", long.MinValue));

                Events.RaiseStaticEvent(entityListResponse, list, MyEventContext.Current.Sender, null);
                return false;
            }
        }





        // Timer block patch
        private static bool Start(MyTimerBlock __instance)
        {


            if(__instance.OwnerId == 0)
            {

                ulong EventOwner = MyEventContext.Current.Sender.Value;
                Log.Error($"{EventOwner} is trying to trigger a timer owned by nobody! Blocking!");
                return false;
            }


            return true;
        }

        private static bool Trigger(MyTimerBlock __instance)
        {
            if (__instance.OwnerId == 0)
            {
                ulong EventOwner = MyEventContext.Current.Sender.Value;
                Log.Error($"{EventOwner} is trying to trigger a timer owned by nobody! Blocking!");
                return false;
            }
            return true;
        }

        private static void banClient(ulong id, string reason)
        {

            if (MyMultiplayer.Static.BannedClients.Contains(id))
                return;


            if (false)
            {
                Log.Warn($"User {id} was banned! Reason: {reason}");
                MyMultiplayer.Static.BanClient(id, true);
            }
            else
            {
                Log.Warn($"User {id} possibly cheated! Reason: {reason}");
            }
        }

       

    }

    



}


