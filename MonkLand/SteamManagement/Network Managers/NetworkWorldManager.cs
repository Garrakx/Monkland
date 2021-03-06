﻿using Monkland.Hooks;
using Monkland.Hooks.Entities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Monkland.SteamManagement
{
    internal class NetworkWorldManager : NetworkManager
    {
        public NetworkWorldManager()
        {
            this.sessionTotalCycles = 0;
        }

        private static ulong playerID
        {
            get
            {
                return NetworkGameManager.playerID;
            }
        }

        private static ulong managerID
        {
            get
            {
                return NetworkGameManager.managerID;
            }
        }

        private static bool isManager { get { return playerID == managerID; } }

        public HashSet<ulong> ingamePlayers = new HashSet<ulong>();

        //dictionary for each player in the game and a list of there currently loaded rooms
        public Dictionary<ulong, List<string>> roomDict = new Dictionary<ulong, List<string>>(); 
        public Dictionary<string, List<ulong>> commonRooms = new Dictionary<string, List<ulong>>();

        //is my game running
        public bool gameRunning = false;

        // cyclelength to sync ingame cycle length with other players
        public int cycleLength = 36000; 
        //Timer to sync ingame time with other players
        public int timer = 0; 
        //Delay to establish P2P connection before
        public int joinDelay = -1; 
        //Cycle periodic sync
        public int syncDelay = 1000; 
                                    //public int roomVerify = 1000;

            // Used for BattleRoyale
        public int sessionTotalCycles = 0;


        public byte WorldHandler = 0;

        public float AmountLeft
        {
            get
            {
                return (float)(this.cycleLength - this.timer) / (float)this.cycleLength;
            }
        }

        public override void Update()
        {
            if (joinDelay > 0)
            {
                joinDelay -= 1;
            }
            else if (joinDelay == 0)
            {
                if (gameRunning)
                {
                    GameStart();
                }
                else
                {
                    GameEnd();
                }
                joinDelay = -1;
            }
        }

        public override void Reset()
        {
            this.cycleLength = 36000;
            this.timer = 0;
            ingamePlayers.Clear();
            commonRooms.Clear();
            roomDict.Clear();
            roomDict.Add(playerID, new List<string>());
            this.gameRunning = false;
            this.joinDelay = -1;
        }

        public override void PlayerJoined(ulong steamID)
        {
            if (!roomDict.ContainsKey(steamID))
                roomDict.Add(steamID, new List<string>());
            joinDelay = 80;
        }

        public override void PlayerLeft(ulong steamID)
        {
            if (roomDict.ContainsKey(steamID))
                roomDict.Remove(steamID);
            if (ingamePlayers.Contains(steamID))
                ingamePlayers.Remove(steamID);
        }

        #region Logistics

        public string GetRegionName(string roomName)
        {
            roomName = roomName.Substring(0, 2);
            string text = "";
            switch (roomName)
            {
                case "CC":
                    text = "Chimney Canopy";
                    break;

                case "DS":
                    text = "Drainage System";
                    break;

                case "HI":
                    text = "Industrial Complex";
                    break;

                case "GW":
                    text = "Garbage Wastes";
                    break;

                case "SI":
                    text = "Sky Islands";
                    break;

                case "SU":
                    text = "Outskirts";
                    break;

                case "SH":
                    text = "Shaded Citadel";
                    break;

                case "IS":
                    text = "Intake System";
                    break;

                case "SL":
                    text = "Shoreline";
                    break;

                case "LF":
                    text = "Farm Arrays";
                    break;

                case "UW":
                    text = "The Exterior";
                    break;

                case "SB":
                    text = "Subterranean";
                    break;

                case "SS":
                    text = "Five Pebbles";
                    break;

                case "RW":
                    text = "Side House";
                    break;

                case "AB":
                    text = "Arid Barrens";
                    break;

                case "TR":
                    text = "The Root";
                    break;

                case "BL":
                    text = "Badlands";
                    break;

                case "AR":
                    text = "Aether Ridge";
                    break;

                case "LM":
                    text = "Looks To the Moon";
                    break;

                case "MW":
                    text = "The Fragmented Exterior";
                    break;

                case "FS":
                    text = "Forest Sanctuary";
                    break;
            }
            return text;
        }

        public void CheckForCommonRooms()
        {
            commonRooms.Clear();
            foreach (ulong player in roomDict.Keys)
            {
                if (player != playerID && roomDict.ContainsKey(player))
                {
                    foreach (string otherRoom in roomDict[player])
                    {
                        if (roomDict.ContainsKey(playerID))
                        {
                            foreach (string myRoom in roomDict[playerID])
                            {
                                if (myRoom.Equals(otherRoom) && !string.IsNullOrEmpty(myRoom)) // WHEN DOES THIS HAPPEN
                                {
                                    if (!commonRooms.ContainsKey(myRoom))
                                    {
                                        commonRooms.Add(myRoom, new List<ulong>());
                                    }
                                    if (!commonRooms[myRoom].Contains(player))
                                    { commonRooms[myRoom].Add(player); }
                                }
                            }
                        }
                    }
                }
            }
            foreach (string roomName in commonRooms.Keys)
            {
                AbstractRoom abstractRoom = RainWorldGameHK.mainGame.world.GetAbstractRoom(roomName);
                if (abstractRoom != null && abstractRoom.realizedRoom != null)
                { 
                    RoomHK.MultiplayerNewToRoom(abstractRoom.realizedRoom, commonRooms[roomName]); 
                }
            }
            if (MonklandSteamManager.DEBUG)
            {
                string roomlist = "";
                foreach (string room in commonRooms.Keys)
                { 
                    roomlist = roomlist + room + ", "; 
                }
                if (roomlist.Equals(""))
                {
                    roomlist = "[NO ROOMS]";
                }
                MonklandSteamManager.Log("[World] Room Packet: Player shares " + roomlist + " rooms with other players.");
            }
        }

        public void TickCycle()
        {
            this.timer++;
            if (isManager)
            {
                if (syncDelay > 0)
                {
                    syncDelay--;
                }
                else
                {
                    SyncCycle();
                    if (RainWorldGameHK.mainGame != null && RainWorldGameHK.mainGame.overWorld != null && RainWorldGameHK.mainGame.overWorld.activeWorld != null)
                    {
                        RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.cycleLength = this.cycleLength;
                        RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.timer = this.timer;
                    }
                    syncDelay = 1000;
                }
            }
        }

        #endregion Logistics

        private enum WorldPacketType
        {
            WorldLoadOrExit,
            RainSync,
            RealizeRoom,
            AbstractizeRoom
        }

        #region Packet Handler

        public override void RegisterHandlers()
        {
            WorldHandler = MonklandSteamManager.instance.RegisterHandler(WORLD_CHANNEL, HandleWorldPackets);
        }


        // USE ENUM HERE
        public void HandleWorldPackets(BinaryReader br, CSteamID sentPlayer)
        {
            WorldPacketType messageType = (WorldPacketType)br.ReadByte();
            switch (messageType)// up to 256 message types
            {
                case WorldPacketType.WorldLoadOrExit:// World Loaded or Exited
                    ReadLoadPacket(br, sentPlayer);
                    return;

                case WorldPacketType.RainSync:// Rain Sync
                    ReadRainPacket(br, sentPlayer);
                    return;

                case WorldPacketType.RealizeRoom:// Realize Room
                    ReadActivateRoom(br, sentPlayer);
                    return;

                case WorldPacketType.AbstractizeRoom:// Abstractize Room
                    ReadDeactivateRoom(br, sentPlayer);
                    return;
            }
        }

        #endregion Packet Handler

        #region Outgoing Packets

        public void ActivateRoom(string roomName)
        {
            MonklandSteamManager.Log("[World] Sending room activate: " + roomName);

            if (!roomDict.ContainsKey(playerID))
            { roomDict.Add(playerID, new List<string>()); }

            if (!roomDict[playerID].Contains(roomName))
            { roomDict[playerID].Add(roomName); }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.RealizeRoom);
            writer.Write(roomName);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            //MonklandSteamManager.instance.SendPacket(packet, (CSteamID)managerID), EP2PSend.k_EP2PSendReliable);
            MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
            CheckForCommonRooms();
        }

        public void DeactivateRoom(string roomName)
        {
            MonklandSteamManager.Log("[World] Sending room deactivate: " + roomName);
            //if (string.IsNullOrEmpty(roomName)) { return; }

            if (!roomDict.ContainsKey(playerID))
            { roomDict.Add(playerID, new List<string>()); }
            if (roomDict[playerID].Contains(roomName))

            { roomDict[playerID].Remove(roomName); }

            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.AbstractizeRoom);
            writer.Write(roomName);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            //MonklandSteamManager.instance.SendPacket(packet, (CSteamID)managerID), EP2PSend.k_EP2PSendReliable);
            MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
            CheckForCommonRooms();
        }

        public void PrepareNextCycle()
        {
            float minutes = Mathf.Lerp(400f, 800f, UnityEngine.Random.value) / 60f;
            this.cycleLength = (int)(minutes * 40f * 60f);
            this.timer = 0;
            SyncCycle();
            if (RainWorldGameHK.mainGame != null && RainWorldGameHK.mainGame.overWorld != null && RainWorldGameHK.mainGame.overWorld.activeWorld != null)
            {
                RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.cycleLength = this.cycleLength;
                RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.timer = this.timer;
            }
            syncDelay = 1000;
        }

        public void GameStart()
        {
            this.sessionTotalCycles++;
            this.timer = 0;
            if (!ingamePlayers.Contains(playerID))
            { ingamePlayers.Add(playerID); }

            if (!roomDict.ContainsKey(playerID))
            { roomDict.Add(playerID, new List<string>()); }

            MonklandSteamManager.Log($"[World] Sending game start packet: {ingamePlayers.Count} players ingame");
            foreach (ulong player in ingamePlayers)
            {
                MonklandSteamManager.Log(player);
            }
            gameRunning = true;
            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.WorldLoadOrExit);
            writer.Write(true);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            //MonklandSteamManager.instance.SendPacket(packet, (CSteamID)managerID), EP2PSend.k_EP2PSendReliable);
            MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
            if (isManager)
            {
                foreach (ulong pl in ingamePlayers)
                {
                    if (pl != playerID)
                        SyncCycle((CSteamID)pl);
                    syncDelay = 1000;
                }
            }
        }

        public void GameEnd()
        {
            if (ingamePlayers.Contains(playerID))
                ingamePlayers.Remove(playerID);
            MonklandSteamManager.Log($"[World] Sending game end packet: {ingamePlayers.Count} players ingame");
            roomDict.Clear();
            roomDict.Add(playerID, new List<string>());
            gameRunning = false;
            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.WorldLoadOrExit);
            writer.Write(false);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            //MonklandSteamManager.instance.SendPacket(packet, (CSteamID)managerID), EP2PSend.k_EP2PSendReliable);
            MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
        }

        public void SyncCycle(CSteamID target)// Syncs rain values for an individual player called by manager after each player loads the game
        {
            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.RainSync);
            writer.Write(cycleLength);
            writer.Write(timer);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            MonklandSteamManager.instance.SendPacket(packet, target, EP2PSend.k_EP2PSendReliable);
            //MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
        }

        public void SyncCycle()// Syncs rain values for all players called by manager before game loads
        {
            MonklandSteamManager.DataPacket packet = MonklandSteamManager.instance.GetNewPacket(WORLD_CHANNEL, WorldHandler);
            BinaryWriter writer = MonklandSteamManager.instance.GetWriterForPacket(packet);

            //Write message type
            writer.Write((byte)WorldPacketType.RainSync);
            writer.Write(cycleLength);
            writer.Write(timer);

            MonklandSteamManager.instance.FinalizeWriterToPacket(writer, packet);
            //MonklandSteamManager.instance.SendPacket(packet, target, EP2PSend.k_EP2PSendReliable);
            MonklandSteamManager.instance.SendPacketToAll(packet, true, EP2PSend.k_EP2PSendReliable);
        }

        #endregion Outgoing Packets

        #region Incoming Packets

        public void ReadLoadPacket(BinaryReader reader, CSteamID sent)
        {
            if (reader.ReadBoolean())
            {
                if (!ingamePlayers.Contains(sent.m_SteamID))
                { ingamePlayers.Add(sent.m_SteamID); }

                if (!roomDict.ContainsKey(sent.m_SteamID))
                { roomDict.Add(sent.m_SteamID, new List<string>()); }

                MonklandSteamManager.Log($"[World] Incomming game start packet: {ingamePlayers.Count} players ingame");

                if (isManager && gameRunning)
                { SyncCycle(sent); }
            }
            else
            {
                if (ingamePlayers.Contains(sent.m_SteamID))
                { ingamePlayers.Remove(sent.m_SteamID); }

                if (roomDict.ContainsKey(sent.m_SteamID))
                { roomDict.Remove(sent.m_SteamID); }

                MonklandSteamManager.Log($"[World] Incomming game end packet: {ingamePlayers.Count} players ingame");
            }
        }

        public void ReadDeactivateRoom(BinaryReader reader, CSteamID sent)
        {
            if (sent.m_SteamID == playerID)
                return;

            string roomName = reader.ReadString();
            if (!roomDict.ContainsKey(sent.m_SteamID))
                roomDict.Add(sent.m_SteamID, new List<string>());
            if (roomDict[sent.m_SteamID].Contains(roomName))
                roomDict[sent.m_SteamID].Remove(roomName);

            MonklandSteamManager.Log("[World] Incomming room deactivate: " + roomName);
            CheckForCommonRooms();
        }

        public void ReadActivateRoom(BinaryReader reader, CSteamID sent)
        {
            if (sent.m_SteamID == playerID)
                return;

            string roomName = reader.ReadString();
            if (!roomDict.ContainsKey(sent.m_SteamID))
                roomDict.Add(sent.m_SteamID, new List<string>());
            if (!roomDict[sent.m_SteamID].Contains(roomName))
                roomDict[sent.m_SteamID].Add(roomName);

            MonklandSteamManager.Log("[World] Incomming room activate: " + roomName);
            CheckForCommonRooms();
        }

        public void ReadRainPacket(BinaryReader reader, CSteamID sent)
        {
            this.cycleLength = reader.ReadInt32();
            this.timer = reader.ReadInt32();
            MonklandSteamManager.Log($"[World] Incomming rain packet: {this.cycleLength}, {this.timer}");
            if (RainWorldGameHK.mainGame != null && RainWorldGameHK.mainGame.overWorld != null && RainWorldGameHK.mainGame.overWorld.activeWorld != null)
            {
                RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.cycleLength = this.cycleLength;
                RainWorldGameHK.mainGame.overWorld.activeWorld.rainCycle.timer = this.timer;
            }
        }

        #endregion Incoming Packets
    }
}