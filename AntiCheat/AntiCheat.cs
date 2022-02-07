using HarmonyLib;
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Network;
using VRageMath;
using static Sandbox.Game.Entities.MyCubeGrid;

namespace AdminLogger.AntiCheat
{
    [HarmonyPatch]
    public class AntiCheat
    {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PropertyInfo GridJumpSystemProp = typeof(MyUpdateableGridSystem).GetProperty("Grid");

        public void ApplyPatching()
        {

        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyCubeGrid), "BuildBlocksRequest")]
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


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyCubeGrid), "OnStockpileFillRequest")]
        //Calls every time you fill a block from your inventory
        private static bool OnStockpileFillRequest(Vector3I blockPosition, long ownerEntityId, byte inventoryIndex)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return true;

            ulong EventOwner = MyEventContext.Current.Sender.Value;
            Log.Error($"{EventOwner} Request fill stockpile");
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyInventory), "PickupItem_Implementation")]
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


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyCubeGrid), "RazeBlocksRequest")]
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






        /* Timer block patch */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyTimerBlock), "Start")]
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyTimerBlock), "Trigger")]
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



        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyGridJumpDriveSystem), "OnRequestJumpFromClient")]
        private static bool OnRequestJumpFromClient(MyGridJumpDriveSystem __instance, Vector3D jumpTarget, long userId)
        {
            MyCubeGrid Grid = (MyCubeGrid)GridJumpSystemProp.GetValue(__instance);


            if (MyGravityProviderSystem.CalculateNaturalGravityInPoint(Grid.PositionComp.GetPosition()).LengthSquared() > 0f)
            {
                ulong EventOwner = MyEventContext.Current.Sender.Value;
                Log.Error($"{EventOwner} is trying to jump in gravity! Denying and banning!");
                MyMultiplayer.Static.BanClient(EventOwner, true);
                return false;
            }


            return true;
        }

    }
}


