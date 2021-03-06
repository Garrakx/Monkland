﻿using Monkland.SteamManagement;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monkland.UI
{
    public class MUIPlayerList : MUIHUD
    {
        public HashSet<ulong> playerHash = new HashSet<ulong>();
        public static Dictionary<ulong, MUILabel> playerLabels = new Dictionary<ulong, MUILabel>();
        public MUIBox box;
        // private Vector2 size;

        public MUIPlayerList(MultiplayerHUD owner, Vector2 pos) : base(owner, pos)
        {
            Debug.Log("Added MUIPlayer list");

            float longestSteamNameX = 0;
            float yPos = -MUIBox.lineHeight + 5f;

            foreach (ulong s in MonklandSteamManager.connectedPlayers)
            {
                yPos -= MUIBox.lineHeight;
                string steamName = SteamFriends.GetFriendPersonaName((CSteamID)s);

                Color bodyColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                try
                {
                    bodyColor = MonklandSteamManager.GameManager.playerColors[MonklandSteamManager.connectedPlayers.IndexOf(s)];
                }
                catch (Exception e)
                {
                    Debug.Log($"Error while trying to get color: {e.Message}");
                }
                MUILabel newLabel = new MUILabel(owner, steamName, bodyColor, pos + new Vector2(0, yPos));
                playerLabels.Add(s, newLabel);
                if (newLabel.label.textRect.xMax >= longestSteamNameX)
                {
                    longestSteamNameX = newLabel.label.textRect.xMax;
                }
            }

            box = new MUIBox(owner, pos, (int)longestSteamNameX, playerLabels.Count);

            foreach (KeyValuePair<ulong, MUILabel> kvp in playerLabels)
            {
                MUILabel item = kvp.Value;
                item.pos.y += 15f;
                //item.color = MonklandSteamManager.GameManager.readiedPlayers.Contains(kvp.Key) ? Color.green : Color.red;

                //i++;
            }
        }

        public void ClearList()
        {
            playerLabels.Clear();
        }

        public override void Update()
        {
            box.isVisible = this.isVisible;

            box.Update();
            //Update stuff
            {
                //The total height in pixels that the players take up on the scroll menu
                // float messagePixelHeight = 5 + (playerLabels.Count * 25);
                //The max height the scrollbar can display
                // float maxDisplayHeight = this.size.y - 30;
                // float maxDisplayTransition = this.size.y - 40;

                // float difference = messagePixelHeight - maxDisplayHeight;

                //this.pos = Input.mousePosition;

                /*
                if (difference < 0)
                {
                    scrollValue = 0;
                }
                */

                //int i = 0;

                foreach (KeyValuePair<ulong, MUILabel> kvp in playerLabels)
                {
                    MUILabel label = kvp.Value;

                    //item.color = MonklandSteamManager.GameManager.readiedPlayers.Contains(kvp.Key) ? Color.green : Color.red;
                    label.isVisible = this.isVisible;

                    label.Update();

                    //i++;
                }
            }
        }

        public override void Draw(float timeStacker)
        {
            foreach (KeyValuePair<ulong, MUILabel> kvp in playerLabels)
            {
                kvp.Value.Draw(timeStacker);
            }
            box.Draw(timeStacker);
        }

        public override void ClearSprites()
        {
            foreach (KeyValuePair<ulong, MUILabel> kvp in playerLabels)
            {
                kvp.Value.ClearSprites();
            }
            box.ClearSprites();
            ClearList();
        }
    }
}
