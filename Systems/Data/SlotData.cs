using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Linq;
using System.Data;
using static SeldomDespArchipelago.Systems.ArchipelagoSystem;
using System.IO;
using System.Configuration;

namespace SeldomDespArchipelago.Systems.Data
{
    /// <summary>
    /// Contains static information about an Archipelago slot. 
    /// </summary>
    public readonly struct SlotData : TagSerializable
    {
        public int Slot {get; init; }
        public string Name {get; init; }
        public string Seed {get; init; }
        public ImmutableArray<string> Goals {get; init; }
        public bool Deathlink {get; init;}
        public bool NpcRando {get; init; }
        public ImmutableHashSet<int> RandomizedNPCs {get; init; }
        // Dict of loc npc ids to item npc ids, if a player's npc item happens to be placed in one of their npc locations.
        // If this is the case, we can transform the ghost/bound npc into the item npc as soon as it is activated, for both expediency and cuteness.
        public ImmutableDictionary<int, int> ItemsByNPC {get; init; }
        public bool DepthRando {get; init; }
        public int Researchsanity {get; init; }
        public int ResearchLockedBehindItems {get; init; }
        public bool Calamity {get; init; }
        public bool Fargo {get; init; }
        public SlotData() {}
        public SlotData(ArchipelagoSession session, LoginSuccessful login)
        {
            Slot = login.Slot;
            Name = session.Players.GetPlayerName(Slot);
            Seed = session.RoomState.Seed;
            Goals = ((JArray)login.SlotData["goal"]).ToObject<string[]>().ToImmutableArray();
            bool isEnabled(string key) => (long)login.SlotData[key] > 0;
            Deathlink = (bool)login.SlotData["deathlink"];
            NpcRando = isEnabled("npc_rando");
            DepthRando = isEnabled("depth_rando");
            Researchsanity = Convert.ToInt32(login.SlotData["researchsanity"]);
            ResearchLockedBehindItems = Convert.ToInt32(login.SlotData["research_locked_behind_items"]);
            Calamity = isEnabled("calamity");
            Fargo = isEnabled("fargo");
            
            #region  NPCRando
            RandomizedNPCs = null;
            var itemsByNPC = new Dictionary<int, int>();
            if (NpcRando)
            {
                string[] randomizedNPCnames = ((JArray)login.SlotData["randomize_npcs"]).ToObject<string[]>();
                RandomizedNPCs = (from name in randomizedNPCnames select npcNameToID[name]).ToImmutableHashSet();
                string[] allNPCnames = npcNameToID.Keys.ToArray();
                var locNamesByID = new Dictionary<long, string>();
                foreach (string loc in allNPCnames)
                {
                    locNamesByID[session.Locations.GetLocationIdFromName(SessionState.APWorldName, loc)] = loc;
                }
                if (locNamesByID.ContainsKey(-1))
                {
                    throw new Exception($"Some retrieved NPC locations turned up -1 ids.");
                }
                var task = session.Locations.ScoutLocationsAsync(locNamesByID.Keys.ToArray());
                if (task.Wait(1000))
                {
                    var locByID = task.Result;
                    foreach (long key in locByID.Keys)
                    {
                        var itemInfo = locByID[key];
                        if (itemInfo.Player.Slot == Slot && allNPCnames.Contains(itemInfo.ItemName))
                        {
                            int npcType = npcNameToID[locNamesByID[key]];
                            itemsByNPC[npcType] = npcNameToID[itemInfo.ItemName];
                        }
                    }
                }
                else  // TODO: Have better backup plan
                {
                    throw new Exception("Failed to properly initialize " + nameof(ItemsByNPC) + ". Please reconnect.");
                }
            }
            ItemsByNPC = NpcRando ? itemsByNPC.ToImmutableDictionary() : null;
            #endregion

            #region  Researchsanity
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            // Researchsanity Dicts for finding the preset to use.
            // These are simple, and there are exceptions that occur before and after these run.

            // Occurs first, if the item name is exact. If true, it also needs to be a Progression or Trap item.
            Dictionary<string,string[]> itemExactToPreset = new Dictionary<string,string[]> {
                {"grub", ["Grub","false"]},
                {"not a grub", ["Grub","false"]}
            };
            // Occurs second, if the item name contains it. If true, it also needs to be a Progression or Trap item.
            Dictionary<string,string[]> itemContainsToPreset = new Dictionary<string,string[]> {
                {"strawberry",["Strawberry","false"]},
                {"sword",["Sword","false"]},
                {"knife",["Sword","false"]},
                {"blade",["Sword","false"]},
                {"reward: ",["Reward","false"]},
                {"puzzle",["Puzzle","true"]},
                {"raspberry",["Raspberry","false"]},
                {"rassberry",["Raspberry","false"]},
                {"razzberry",["Raspberry","false"]}
            };
            // Occurs third, if the world name is exact. If true, it also needs to be a Progression or Trap item.
            Dictionary<string,string[]> worldExactToPreset = new Dictionary<string,string[]> {
                {"paint",["Windows","false"]}
            };
            // Occurs fourth, if the world name contains it. If true, it also needs to be a Progression or Trap item.
            Dictionary<string,string[]> worldContainsToPreset = new Dictionary<string,string[]> {
                {"terraria",["Terraria","true"]},
                {"pokemon",["Pokemon","true"]},
                {"poké",["Pokemon","true"]}
            };
            if (Researchsanity > 0)
            {
                var missingLocations = session.Locations.AllMissingLocations;
                List<long> idList = new List<long>();
                foreach (long loc in missingLocations)
                {
                    string awesomeName = session.Locations.GetLocationNameFromId(loc, SessionState.APWorldName);
                    if (awesomeName != null && awesomeName.StartsWith("Researchsanity - "))
                    {
                        idList.Add(loc);
                    }
                }
                var task = session.Locations.ScoutLocationsAsync(HintCreationPolicy.None,idList.ToArray());
                if (task.Wait(1000))
                {
                    var locByID = task.Result;
                    foreach (long key in locByID.Keys)
                    {
                        var itemInfo = locByID[key];
                        string[] myArray = archipelagoSystem.Presets["Default"];
                        string awesomeName = session.Locations.GetLocationNameFromId(key, SessionState.APWorldName);
                        //awesomeName = awesomeName.Replace("Researchsanity - ","");
                        //if (awesomeName.Contains("["))
                        //{
                        //    awesomeName = awesomeName.Remove(awesomeName.IndexOf("[")-1);
                        //}
                        for (int literallyOne=0;literallyOne<1;literallyOne++)
                        {
                            if (archipelagoSystem.specialColors)
                            {
                                // advOrTrap can be changed to not use Trap if needed, in case it gets too annoying. Hello devs, how ya doin'.
                                bool advOrTrap = (itemInfo.Flags == ItemFlags.Advancement || itemInfo.Flags == ItemFlags.Trap);
                                bool useful = itemInfo.Flags == ItemFlags.NeverExclude;
                                string worldFrom = itemInfo.ItemGame.ToLower();
                                string thisItemName = itemInfo.ItemName.ToLower();
                                if (false) // Set to false when not testing, as this basically spoils all the Researchsanity locations if kept on.
                                {
                                    ModContent.GetInstance<SeldomArchipelago>().Logger.Info($"Info for check \"{awesomeName}\": Behind it is \"{thisItemName}\" from world \"{worldFrom}\". Is it Useful? {useful} Is it Progression or Trap? {advOrTrap}");
                                }
                                //if ((npcNameToID.Keys).Contains(itemInfo.ItemName))
                                //{
                                //    myArray = archipelagoSystem.Presets["NPC Name"];
                                //    //int npcType = idToPreset[key];
                                //    //itemsByNPC[npcType] = npcNameToID[itemInfo.ItemName];
                                //}
                                //else 
                                bool shouldMoveOn = false;
                                if (advOrTrap && itemInfo.Player.Slot == Slot)
                                {
                                    myArray = archipelagoSystem.Presets["Local Progression"];
                                    continue;
                                }

                                if (itemExactToPreset.Keys.Contains(thisItemName))
                                {
                                    string[] outputt = itemExactToPreset[thisItemName];
                                    if (advOrTrap || outputt[1] == "false")
                                    {
                                        myArray = archipelagoSystem.Presets[outputt[0]];
                                        continue;
                                    }
                                }

                                foreach (var a in itemContainsToPreset)
                                {
                                    string kee = a.Key;
                                    string[] outputt = a.Value;
                                    if ((advOrTrap || outputt[1] == "false") && thisItemName.Contains(kee))
                                    {
                                        myArray = archipelagoSystem.Presets[outputt[0]];
                                        shouldMoveOn = true;
                                        break;
                                    }
                                }
                                if (shouldMoveOn) { continue; }

                                if (worldExactToPreset.Keys.Contains(worldFrom))
                                {
                                    string[] outputt = worldExactToPreset[thisItemName];
                                    if (advOrTrap || outputt[1] == "false")
                                    {
                                        myArray = archipelagoSystem.Presets[outputt[0]];
                                        continue;
                                    }
                                }

                                foreach (var a in worldContainsToPreset)
                                {
                                    string kee = a.Key;
                                    string[] outputt = a.Value;
                                    if ((advOrTrap || outputt[1] == "false") && worldFrom.Contains(kee))
                                    {
                                        myArray = archipelagoSystem.Presets[outputt[0]];
                                        shouldMoveOn = true;
                                        break;
                                    }
                                }
                                if (shouldMoveOn) { continue; }
                            }
                        }
                        archipelagoSystem.idToColors[awesomeName] = myArray;
                    }

                    // Change here if the way Researchsanity AP-items are handled is changed.
                    int theLength = locByID.Keys.ToArray().Length;
                    if (theLength < 18)
                    {
                        if (theLength < 4)
                        {
                            if (theLength < 1)
                            {
                                // If no Researchsanity locations available, this is 0.
                                ResearchLockedBehindItems = 0;
                            }
                            else
                            {
                                // If 1-3 locations available, this is 1 or lower.
                                ResearchLockedBehindItems = Math.Min(ResearchLockedBehindItems,1);
                            }
                        }
                        else
                        {
                            // If 4-17 locations available, this is 2 or lower.
                            ResearchLockedBehindItems = Math.Min(ResearchLockedBehindItems,2);
                        }
                    }
                }
                else  // TODO: Have better backup plan here as well
                {
                    ModContent.GetInstance<ArchipelagoSystem>().Mod.Logger.Info("Failed to properly initialize idToColors");
                }
            }
            archipelagoSystem.researchLockedBehindItems = ResearchLockedBehindItems;
            ModContent.GetInstance<ArchipelagoSystem>().Mod.Logger.Info("Researchsanity: "+Researchsanity+" Locked: "+ResearchLockedBehindItems);
            #endregion
        }
        public TagCompound SerializeData()
        {
            var tag = new TagCompound
            {
                [nameof(Slot)] = Slot,
                [nameof(Name)] = Name,
                [nameof(Seed)] = Seed,
                [nameof(Goals)] = Goals.ToList(),
                [nameof(Deathlink)] = Deathlink,
                [nameof(NpcRando)] = NpcRando,
                [nameof(DepthRando)] = DepthRando,
                [nameof(Researchsanity)] = Researchsanity,
                [nameof(ResearchLockedBehindItems)] = ResearchLockedBehindItems,
                [nameof(Calamity)] = Calamity,
                [nameof(Fargo)] = Fargo,
            };
            if (NpcRando)
            {
                tag[nameof(RandomizedNPCs)] = RandomizedNPCs.ToList();
                tag[nameof(ItemsByNPC)+"Keys"] = ItemsByNPC.Keys.ToList();
                tag[nameof(ItemsByNPC)+"Values"] = ItemsByNPC.Values.ToList();
            }
            return tag;
        }
        public static readonly Func<TagCompound, SlotData> DESERIALIZER = (tag) =>
        {
            // Outside constructor for slightly enhanced readability
            bool npcRando = tag.GetBool(nameof(NpcRando));
            var npcs = npcRando ? tag.Get<List<int>>(nameof(ItemsByNPC)+"Keys") : null;
            var items = npcRando ? tag.Get<List<int>>(nameof(ItemsByNPC)+"Values") : null;
            return new SlotData()
            {
                Slot = tag.GetInt(nameof(Slot)),
                Name = tag.GetString(nameof(Name)),
                Seed = tag.GetString(nameof(Seed)),
                Goals = tag.GetList<string>(nameof(Goals)).ToImmutableArray(),
                Deathlink = tag.GetBool(nameof(Deathlink)),
                NpcRando = npcRando,
                RandomizedNPCs = npcRando ? tag.GetList<int>(nameof(RandomizedNPCs)).ToImmutableHashSet() : null,
                ItemsByNPC = npcRando ? npcs.Zip(items, (k, v) => new { Key = k, Value = v}).ToImmutableDictionary(x => x.Key, x => x.Value) : null,
                DepthRando = tag.GetBool(nameof(DepthRando)),
                Researchsanity = tag.GetInt(nameof(Researchsanity)),
                ResearchLockedBehindItems = tag.GetInt(nameof(ResearchLockedBehindItems)),
                Calamity = tag.GetBool(nameof(Calamity)),
                Fargo = tag.GetBool(nameof(Fargo)),
            };
        };
        public void Write(BinaryWriter writer)
        {
            writer.Write(Slot);
            writer.Write(Name);
            writer.Write(Seed);
            writer.Write(string.Join(';', Goals));
            writer.Write(Deathlink);
            writer.Write(NpcRando);
            writer.Write(string.Join(';', RandomizedNPCs));
            writer.Write(string.Join(';', ItemsByNPC.Keys));
            writer.Write(string.Join(';', ItemsByNPC.Values));
            writer.Write(DepthRando);
            writer.Write(Researchsanity);
            writer.Write(ResearchLockedBehindItems);
            writer.Write(Calamity);
            writer.Write(Fargo);
        }
        public static SlotData? ReadSlotData(BinaryReader reader)
        {
            try
            {
                int slot = reader.ReadInt32();
                string name = reader.ReadString();
                string seed = reader.ReadString();
                ImmutableArray<string> goals = reader.ReadString().Split(';').ToImmutableArray();
                bool deathlink = reader.ReadBoolean();
                bool npcRando = reader.ReadBoolean();
                ImmutableHashSet<int> randomizedNPCs = (from i in reader.ReadString().Split(';') select int.Parse(i)).ToImmutableHashSet();
                var itemsByNPCKeys = from i in reader.ReadString().Split(';') select int.Parse(i);
                var itemsByNPCValues = from i in reader.ReadString().Split(';') select int.Parse(i);
                bool depthRando = reader.ReadBoolean();
                int researchsanity = reader.ReadInt32();
                int research_locked_behind_items = reader.ReadInt32();
                bool calamity = reader.ReadBoolean();
                bool fargo = reader.ReadBoolean();
                return new SlotData
            {
                Slot = slot,
                Name = name,
                Seed = seed,
                Goals = goals,
                Deathlink = deathlink,
                NpcRando = npcRando,
                RandomizedNPCs = randomizedNPCs,
                ItemsByNPC = itemsByNPCKeys.Zip(itemsByNPCValues, (k, v) => KeyValuePair.Create(k, v)).ToImmutableDictionary(),
                DepthRando = depthRando,
                Researchsanity = researchsanity,
                ResearchLockedBehindItems = research_locked_behind_items,
                Calamity = calamity,
                Fargo = fargo,
            };
            }
            catch (Exception e)
            {
                ModContent.GetInstance<SeldomArchipelago>().Logger.Info($"Failed to receive slotdata: {e}");
                return null;
            }
        }
    }
}