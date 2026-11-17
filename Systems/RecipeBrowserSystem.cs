using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Newtonsoft.Json.Linq;
using SeldomDespArchipelago.Players;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;
using Terraria.WorldBuilding;
using Terraria.UI.Chat;
using SeldomDespArchipelago.Items;
using System.Linq;
using SeldomDespArchipelago.Systems.Data;
using SeldomDespArchipelago.NPCs;
using static SeldomDespArchipelago.Systems.Data.SessionState;
using System.Formats.Tar;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Diagnostics.Metrics;
using Terraria.GameContent.UI.States;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Terraria.ModLoader.Config;
using System.Text;
using System.Data;
using Microsoft.Build.Tasks;
using log4net.Repository.Hierarchy;
using Mono.Cecil.Cil;
using Terraria.IO;
using Archipelago.MultiClient.Net.Exceptions;
using System.Net.WebSockets;
using SeldomDespArchipelago.UI;
using ReLogic.Content.Sources;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Archipelago.MultiClient.Net.Helpers;
using Microsoft.Build.Evaluation;
using System.IO;
using SeldomDespArchipelago.Depth;

namespace SeldomDespArchipelago.Systems
{
    public class RecipeBrowserSystem : ModSystem
    {
        public override void PostSetupContent() {
            if (ModLoader.TryGetMod("RecipeBrowser", out Mod mod) && !Main.dedServ) {
                mod.Call(new object[5]
                {
                    "AddItemFilter",
                    "AP Location (Researchsanity)",
                    "",
                    Mod.Assets.Request<Texture2D>("ItemOverlay/TooltipGivesLocation"), // 24x24 icon
                    (Predicate<Item>)((Item item) =>
                    {
                        var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
                        return archipelagoSystem.idToColors.ContainsKey(ItemOverlay.ItemOverlay.returnResearchsanityString(item.Name,item.ModItem?.Mod.Name));
                    })
                });
                mod.Call(new object[5]
                {
                    "AddItemFilter",
                    "Researchable (Researchsanity)",
                    "",
                    Mod.Assets.Request<Texture2D>("ItemOverlay/TooltipCantResearch"), // 24x24 icon
                    (Predicate<Item>)((Item item) =>
                    {
                        var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
                        return ItemOverlay.ItemOverlay.canTrueResearchItem(item);
                    })
                });
            }
        }
    }
}