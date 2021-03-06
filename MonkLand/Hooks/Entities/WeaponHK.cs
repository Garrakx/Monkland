﻿using Monkland.SteamManagement;
using UnityEngine;
using System.Xml.Schema;

namespace Monkland.Hooks.Entities
{
    internal static class WeaponHK
    {
        public static void ApplyHook()
        {
            On.Weapon.HitThisObject += new On.Weapon.hook_HitThisObject(HitThisObjectHK);
            On.Weapon.HitSomething += Weapon_HitSomething;
            On.Weapon.Thrown += Weapon_Thrown;
            On.Weapon.Update += Weapon_Update;
            On.Weapon.ctor += Weapon_ctor;
        }

        public static readonly int defaultNetworkLife = 60;

        private static void Weapon_ctor(On.Weapon.orig_ctor orig, Weapon self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            orig(self, abstractPhysicalObject, world);
            AbstractPhysicalObjectHK.GetField(self.abstractPhysicalObject).networkLife = defaultNetworkLife;
        }

        //public static void Sync(PhysicalObject self) => AbstractPhysicalObjectHK.GetField(self.abstractPhysicalObject).networkLife = 60;

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig(self, eu);

            AbstractObjFields fields = AbstractPhysicalObjectHK.GetField(self.abstractPhysicalObject);
            if (fields.isNetworkObject)
            {
                if (fields.networkLife > 0) { fields.networkLife--; }
                else
                {
                    fields.networkLife = defaultNetworkLife;
                    Debug.Log($"[{self.abstractPhysicalObject.type} EXPIRED] ID [{fields.networkID}] Owner [{fields.ownerName}]");
                    for (int i = 0; i < self.grabbedBy.Count; i++)
                    {
                        if (self.grabbedBy[i] != null)
                        {
                            self.grabbedBy[i].Release();
                            i--;
                        }
                    }
                    self.Destroy();
                }
            }
        }

        /*
        public static bool WeaponCheckNet(Weapon self)
        {
            if (self is Spear)
            {
                return SpearHK.CheckNet();
            }
            else if(self is Rock)
            {
                return RockHK.CheckNet();
            }
            else
            {
                return false;
            }
        }
        */

        
        private static bool isNet = false;

        public static bool CheckNet()
        {
            if (isNet) { isNet = false; return true; }
            return false;
        }
        public static void SetNet() => isNet = true;
        

        private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, UnityEngine.Vector2 thrownPos, UnityEngine.Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);

            if (CheckNet())
            {
                return;
            }

            if (MonklandSteamManager.isInGame && !AbstractPhysicalObjectHK.GetField(self.abstractPhysicalObject).isNetworkObject && MonklandSteamManager.WorldManager.commonRooms.ContainsKey(self.room.abstractRoom.name))
            {
                MonklandSteamManager.GraspStickManager.SendThrow(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc);
                MonklandSteamManager.EntityManager.SendPhysicalObject(self, MonklandSteamManager.WorldManager.commonRooms[self.room.abstractRoom.name], true);
            }
        }


        private static bool Weapon_HitSomething(On.Weapon.orig_HitSomething orig, Weapon self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool hit = orig(self, result, eu);

            if (CheckNet())
            {
                return hit;
            }

            if (hit && MonklandSteamManager.isInGame && !AbstractPhysicalObjectHK.GetField(self.abstractPhysicalObject).isNetworkObject && MonklandSteamManager.WorldManager.commonRooms.ContainsKey(self.room.abstractRoom.name))
            {
                MonklandSteamManager.GraspStickManager.SendHit(self, result.obj, result.chunk);
                MonklandSteamManager.EntityManager.SendPhysicalObject(self, MonklandSteamManager.WorldManager.commonRooms[self.room.abstractRoom.name], true);
            }
            return hit;
        }

        private static bool HitThisObjectHK(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
        {
            if (!(obj is Player) || !(self is Spear))
            { return true; }
            else if (self.thrownBy != null && (self.thrownBy is Player) && self.room.game.IsArenaSession && !self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.spearsHitPlayers)
            { return false; }
            else if ((self.thrownBy == null || (self.thrownBy is Player)) && MonklandSteamManager.isInGame && MonklandSteamManager.lobbyInfo != null && !MonklandSteamManager.lobbyInfo.spearsHit)
            { return false; }
            return true;
        }
    }
}