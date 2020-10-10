﻿using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Monkland.UI
{
    internal class MUIButton : MUIHUD
    {
        public MUIBox box;
        public MUILabel label;
        public MultiplayerHUD owner;
        public Vector2 size;

        public MUIButton(MultiplayerHUD owner, Vector2 pos, string labelString)
        {
            Debug.Log($"creating MUIBUTTON {pos}");

            this.owner = owner;

            box = new MUIBox(owner, pos, new Vector2(110f, 30f));

            label = new MUILabel(owner, labelString, Color.white, pos + new Vector2(0, -MUIBox.lineHeight - 5f));

            size = box.drawSize;
            this.pos = pos - new Vector2(box.drawSize.x, 0);
        }

        public override void ClearSprites()
        {
            label.ClearSprites();
            box.ClearSprites();
        }

        public override void Draw(float timeStacker)
        {
            label.Draw(timeStacker);
            box.Draw(timeStacker);
        }

        public override void Update()
        {
            if (MouseOver && this.owner.mouseDown)
            {
                this.owner.ExitButton();
            }

            label.isVisible = this.isVisible;
            box.isVisible = this.isVisible;

            label.Update();
            box.Update();
        }

        internal Vector2 ScreenPos
        {
            get
            {
                if (this.owner == null) { return Vector2.zero; }
                return this.owner.screenPos;
            }
        }

        internal Vector2 MousePos
        {
            get
            {
                return new Vector2(this.owner.mousePos.x - this.ScreenPos.x, this.owner.mousePos.y - this.ScreenPos.y);
            }
        }

        public bool MouseOver
        {
            get
            {
                return this.MousePos.x > this.pos.x
                    && this.MousePos.x < this.pos.x + this.size.x
                    && this.MousePos.y > this.pos.y
                    && this.MousePos.y < this.pos.y + this.size.y;
            }
        }
    }
}