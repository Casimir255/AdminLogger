﻿using AdminLogger.Utils;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
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
using VRage;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Replication;
using VRageMath;
using static Sandbox.Game.Entities.MyCubeGrid;

namespace AdminLogger.AdminLogging
{
    /*
    public class AntiCheatClass
    {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static Type MyClientType = Type.GetType("VRage.Network.MyClient, VRage");
        public static Type MyCubeGridReplicable = Type.GetType("Sandbox.Game.Replication.MyCubeGridReplicable, Sandbox.Game");

        private static PropertyInfo GridJumpSystemProp = typeof(MyUpdateableGridSystem).GetProperty("Grid");

        private static FieldInfo m_clientStates;
        private static FieldInfo Replicables;
        private static PropertyInfo UpdatableGrid;

        private static MethodInfo removeForClient;
        private static MethodInfo forceForClient;
        private static MethodInfo OnPerfomJump;

        public static void ApplyPatching()
        {

           
            Patcher.PrePatch<MyCubeGrid>("BuildBlocksRequest", BindingFlags.Instance | BindingFlags.NonPublic, nameof(BuildBlocksRequest));
            Patcher.PrePatch<MyCubeGrid>("OnStockpileFillRequest", BindingFlags.Instance | BindingFlags.NonPublic, nameof(OnStockpileFillRequest));
            Patcher.PrePatch<MyInventory>("PickupItem_Implementation", BindingFlags.Instance | BindingFlags.NonPublic, nameof(PickupItem_Implementation));
            Patcher.PrePatch<MyCubeGrid>("RazeBlocksRequest", BindingFlags.Instance | BindingFlags.Public, nameof(RazeBlocksRequest));

            //Patcher.PrePatch<MyGridJumpDriveSystem>("PerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(BeforeJump));

            Patcher.PrePatch<MyTimerBlock>("Start", BindingFlags.Instance | BindingFlags.Public, nameof(Start));
            Patcher.PrePatch<MyTimerBlock>("Trigger", BindingFlags.Instance | BindingFlags.NonPublic, nameof(Trigger));


            // Jump Drive Tracking fix 
            m_clientStates = typeof(MyReplicationServer).GetField("m_clientStates", BindingFlags.Instance | BindingFlags.NonPublic);
            Replicables = MyClientType.GetField("Replicables", BindingFlags.Instance | BindingFlags.Public);
            removeForClient = typeof(MyReplicationServer).GetMethod("RemoveForClient", BindingFlags.NonPublic | BindingFlags.Instance);
            UpdatableGrid = typeof(MyUpdateableGridSystem).GetProperty("Grid", BindingFlags.Instance | BindingFlags.NonPublic);
            //Patcher.PrePatch<MyGridJumpDriveSystem>("PerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(BeforeJump));
            Patcher.PrePatch<MyGridJumpDriveSystem>("SendPerformJump", BindingFlags.Instance | BindingFlags.NonPublic, nameof(SendPerformJump));
            OnPerfomJump = Patcher.GetMethod<MyGridJumpDriveSystem>("OnPerformJump", BindingFlags.Static | BindingFlags.NonPublic);

        }



        //Calls when you are trying to build blocks
        private static bool BuildBlocksRequest(MyBlockVisuals visuals, HashSet<MyBlockLocation> locations, long builderEntityId, bool instantBuild, long ownerId)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;


            ulong EventOwner = MyEventContext.Current.Sender.Value;
            if (!MySession.Static.CreativeMode && locations.Count > 1 && !MySession.Static.HasPlayerCreativeRights(EventOwner))
            {
                Log.Error($"{EventOwner} was denied buildblocks request! Attempting to build {locations.Count} blocks and they are not admin! How is this possible!? Cheats?");
                return false;
            }

            Log.Error($"{EventOwner} was building blocks!");
            return true;
        }



        //Calls every time you fill a block from your inventory
        private static bool OnStockpileFillRequest(Vector3I blockPosition, long ownerEntityId, byte inventoryIndex)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;

            ulong EventOwner = MyEventContext.Current.Sender.Value;
            Log.Error($"{EventOwner} Request fill stockpile");
            return true;
        }


        //Calls every time you fill a block from your inventory
        private static bool PickupItem_Implementation(MyInventory __instance, long entityId, MyFixedPoint amount)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;

            ulong EventOwner = MyEventContext.Current.Sender.Value;


            MyFloatingObject entity;
            if (!MyEntities.TryGetEntityById(entityId, out entity, false) || entity == null || entity.MarkedForClose || entity.WasRemovedFromWorld)
            {
                return false;
            }

            MyCharacter InvOwner = __instance.Owner as MyCharacter;
            if (InvOwner == null)
                return false;


            double Distance = Vector3D.Distance(entity.PositionComp.GetPosition(), InvOwner.PositionComp.GetPosition());
            if (Distance > 15)
            {
                Log.Error($"{EventOwner} tried to pick up an item that was {Distance}m away! Blocking and banning!");
                MyMultiplayer.Static.BanClient(EventOwner, true);
                return false;
            }

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

            Log.Error($"{EventOwner} tried to remove blocks using keen exploit and is not admin! Blocking and banning!");
            MyMultiplayer.Static.BanClient(EventOwner, true);
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

       

    }
*/


}

