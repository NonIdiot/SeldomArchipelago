using ReLogic.Utilities;
using SeldomDespArchipelago.NPCs;
using SeldomDespArchipelago.Systems;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;

namespace SeldomDespArchipelago.ItemOverlay
{
    public class ItemOverlay : GlobalItem
    {
        public void drawMyImages(Item item, SpriteBatch spriteBatch, Vector2 position, float scale)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            float myAngle = archipelagoSystem.gameTimer/30f;//Main.fpsTimer.ElapsedMilliseconds/333.33f;//Main.GameUpdateCount/20f;
            float usedOffset = 0f;
            float outwardsCount = (float)(12+2*MathF.Cos((float)myAngle*2f));
            float alpha = 0.5f;
            List<int> whichTextureToUse = [0,1,2,3,4,5]; // This is set to the correct textures via the generated-on-world-load Dict.
            for (int a=0; a<6; a++)
            {
                Texture2D usedTexture = SeldomArchipelago.overlayTextures[whichTextureToUse[a]].Value;
                spriteBatch.Draw
                (
                    usedTexture,
                    new Vector2
                    (
                        position.X - usedTexture.Width * usedOffset + outwardsCount*(float)Math.Sin(myAngle+(float)(60f*a*(Math.PI/180f))),//.25
                        position.Y - usedTexture.Height * usedOffset + outwardsCount*(float)Math.Cos(myAngle+(float)(60f*a*(Math.PI/180f)))//.25 + item.height 
                    ),
                    new Rectangle(0, 0, usedTexture.Width, usedTexture.Height),
                    new Color(alpha,alpha,alpha,alpha), // all four are set, since if only the alpha is set, it gets REALLY bright.
                    0,
                    usedTexture.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            drawMyImages(item, spriteBatch, position, scale);
        }

        public override void PostDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            drawMyImages(item, spriteBatch, item.position, scale);
        }
    }
}