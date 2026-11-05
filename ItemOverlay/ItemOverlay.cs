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
using System.Data;
using System.IO;

namespace SeldomDespArchipelago.ItemOverlay
{
    public class ItemOverlay : GlobalItem
    {
        public void drawMyImages(Item item, SpriteBatch spriteBatch, Vector2 position, float scale)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            string itemName = item.Name;//Lang.GetItemNameValue(item.type);
            float myAngle = archipelagoSystem.gameTimer/30f;//Main.fpsTimer.ElapsedMilliseconds/333.33f;//Main.GameUpdateCount/20f;
            float usedOffset = 0f;
            float outwardsCount = (float)(12+2*MathF.Cos((float)myAngle*2f));
            float alpha = 0.5f;
            int[] whichTextureToUse = [0,1,2,3,4,5]; // This is set to the correct textures via the generated-on-world-load Dict.
            if (archipelagoSystem.idToColors.ContainsKey(itemName))
            {
                if (canTrueResearchItem(item))
                {
                    whichTextureToUse = archipelagoSystem.idToColors[itemName];
                }
                else
                {
                    whichTextureToUse = [6,6,6,6,6,6];
                    myAngle = 0;
                    outwardsCount = 12;
                }

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
        }

        public static string[] allResearchsanity = [
            "Can Research All Items",
            "Can Research Stackable Items","Can Research 1-To-Research Accessories","Can Research 1-To-Research Weapons","Can Research 1-To-Research Other Items",
            "Can Research 2-To-Research Items",
            "Can Research 3-To-Research Items",
            "Can Research 5-To-Research Items",
            "Can Research 10-To-Research Items",
            "Can Research 15-To-Research Items",
            "Can Research 20-To-Research Items",
            "Can Research 25-To-Research Items",
            "Can Research 30-To-Research Items",
            "Can Research 50-To-Research Items",
            "Can Research 99-To-Research Items",
            "Can Research 100-To-Research Non-Tile Items",
            "Can Research 100-To-Research Tiles",
            "Can Research 200-To-Research Items",
            "Can Research 400-To-Research Items",
            "Can Research Miscellaneous-Count Items"
        ];
        public static Dictionary<int, int> resCountToAll = new Dictionary<int,int>
        {
            {2,5},
            {3,6},
            {5,7},
            {10,8},
            {15,9},
            {20,10},
            {25,11},
            {30,12},
            {50,13},
            {99,14},
            {200,17},
            {400,18},
        };
        public static string canResearchItem(Item item)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            int resLock = archipelagoSystem.researchLockedBehindItems;
            
            //if (Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[item.type] == 25)
            //if (item.prefix == 0 || item.prefix == -1)
            int sacrificeCount = Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[item.type];
            if (resLock == 0 || archipelagoSystem.offResearch)
            {
                return "true";
            }
            else if (resLock == 1)
            {
                return allResearchsanity[0];
            }
            else if (resLock == 2 || resLock == 3)
            {
                if (sacrificeCount != 1)
                {
                    if (resLock == 2)
                    {
                        return allResearchsanity[1];
                    }
                    else if (sacrificeCount == 100)
                    {
                        if (item.createTile != -1)
                        {
                            return allResearchsanity[16];
                        }
                        else
                        {
                            return allResearchsanity[15];
                        }
                    }
                    else if (resCountToAll.Keys.Contains(sacrificeCount))
                    {
                        return allResearchsanity[resCountToAll[sacrificeCount]];
                    }
                    else
                    {
                        return allResearchsanity[19];
                    }
                }
                else
                {
                    if (item.accessory)
                    {
                        return allResearchsanity[2];
                    }
                    else if (item.damage > 0)
                    {
                        return allResearchsanity[3];
                    }
                    else
                    {
                        return allResearchsanity[4];
                    }
                }
            }
            //throw new Exception("canResearchItem failed!");
            return "false";
        }

        public static bool canTrueResearchItem(Item item)
        {
            string resultResearch = canResearchItem(item);
            if (resultResearch == "true")
            {
                return true;
            }
            else if (resultResearch == "false")
            {
                return false;
            }
            else
            {
                return ModContent.GetInstance<ArchipelagoSystem>().Received(resultResearch);
            }
        }

        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            drawMyImages(item, spriteBatch, position, scale);
        }

        public override void PostDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            drawMyImages(item, spriteBatch, item.position - Main.screenPosition + new Vector2(item.getRect().Width/2f, item.getRect().Height/4f+0f), scale);
        }

        public override bool CanResearch(Item item)
        {
            return canTrueResearchItem(item);
        }

        public override void OnResearched(Item item, bool fullyResearched)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            string itemName = item.Name;
            if (fullyResearched && canTrueResearchItem(item) && archipelagoSystem.idToColors.ContainsKey(itemName))
            {
                string awesomeName = "Researchsanity - "+itemName;
                if (item.ModItem?.Mod.Name == "CalamityMod")
                {
                    awesomeName = awesomeName+" [Calamity Mod]";
                }
                if (item.ModItem?.Mod.Name == "Fargowiltas")
                {
                    awesomeName = awesomeName+" [Fargo's Mutant Mod]";
                }
                if (item.ModItem?.Mod.Name == "FargowiltasSouls")
                {
                    awesomeName = awesomeName+" [Fargo's Souls Mod]";
                }
                archipelagoSystem.QueueLocation(awesomeName);
                archipelagoSystem.idToColors.Remove(itemName);
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            string itemName = item.Name;
            if (archipelagoSystem.idToColors.ContainsKey(itemName))
            {
                TooltipLine myLine = new TooltipLine(ModLoader.GetMod("SeldomDespArchipelago"),"ResearchsanityLocation","Fully researching this item sends an AP item to the multiworld!");
                myLine.OverrideColor = Color.Coral;
                tooltips.Add(myLine);
            }
            string resultResearch = canResearchItem(item);
            if (resultResearch == "false")
            {
                TooltipLine myLine = new TooltipLine(ModLoader.GetMod("SeldomDespArchipelago"),"ResearchsanityError","grievous error");
                myLine.OverrideColor = Color.Red;
                tooltips.Add(myLine);
            }
            else if (resultResearch != "true" && !archipelagoSystem.Received(resultResearch))
            {
                TooltipLine myLine = new TooltipLine(ModLoader.GetMod("SeldomDespArchipelago"),"ResearchsanityUnresearchable","You cannot research this item yet until AP item\n\""+resultResearch+"\" is found.");
                myLine.OverrideColor = Color.MediumOrchid;
                tooltips.Add(myLine);
            }
        }
    }
}