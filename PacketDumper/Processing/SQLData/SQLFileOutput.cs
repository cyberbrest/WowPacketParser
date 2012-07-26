﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using PacketDumper.Enums;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.DataStructures;
using PacketParser.Processing;
using Guid = PacketParser.DataStructures.Guid;
using PacketParser.Misc;
using PacketDumper.Misc;
using PacketDumper.DataStructures;
using PacketParser.SQL;
using System.Diagnostics;
using System.Linq;

namespace PacketDumper.Processing.SQLData
{
    delegate string Builder();

    public class SQLFileOutput : IPacketProcessor
    {
        public bool LoadOnDepend { get { return false; } }
        public Type[] DependsOn { get { return null; } }

        public ProcessPacketEventHandler ProcessAnyPacketHandler { get { return null; } }
        public ProcessedPacketEventHandler ProcessedAnyPacketHandler { get { return null; } }
        public ProcessDataEventHandler ProcessAnyDataHandler { get { return null; } }

        string FileName;
        string LogPrefix;
        string Header;
        public bool Init(PacketFileProcessor file)
        {
            FileName = file.FileName;
            LogPrefix = file.LogPrefix;
            Header = file.GetHeader();
            return Settings.SQLOutput != SQLOutputFlags.None;
        }

        public void Finish() 
        {
            string sqlFileName;
            if (String.IsNullOrWhiteSpace(Settings.SQLFileName))
                sqlFileName = string.Format("{0}_{1}.sql",
                    Utilities.FormattedDateTimeForFiles(), Path.GetFileName(FileName));
            else
                sqlFileName = Settings.SQLFileName;

            DumpSQL(string.Format("{0}: Dumping sql", LogPrefix), sqlFileName, Header);
        }


        public void DumpSQL(string prefix, string fileName, string header)
        {
            var Objects = PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects;
            var units = Objects.IsEmpty() ? null : Objects.Where(obj => obj.Value.Item1.Type == ObjectType.Unit && obj.Key.GetHighType() != HighGuidType.Pet).ToDictionary(obj => obj.Key, obj => obj.Value.Item1 as Unit);
            var gameObjects = Objects.IsEmpty() ? null : Objects.Where(obj => obj.Value.Item1.Type == ObjectType.GameObject).ToDictionary(obj => obj.Key, obj => obj.Value.Item1 as GameObject);
            //var pets = Storage.Objects.Where(obj => obj.Value.Type == ObjectType.Unit && obj.Key.GetHighType() == HighGuidType.Pet).ToDictionary(obj => obj.Key, obj => obj.Value as Unit);
            //var players = Storage.Objects.Where(obj => obj.Value.Type == ObjectType.Player).ToDictionary(obj => obj.Key, obj => obj.Value as Player);
            //var items = Storage.Objects.Where(obj => obj.Value.Type == ObjectType.Item).ToDictionary(obj => obj.Key, obj => obj.Value as Item);

            if (units != null)
                foreach (var unit in units)
                    unit.Value.LoadValuesFromUpdateFields();

            List<Tuple<string, Builder>> writes = new List<Tuple<string, Builder>>();

            {
                var GameObjectTemplates = PacketFileProcessor.Current.GetProcessor<GameObjectTemplateStore>();
                if (GameObjectTemplates != null)
                    writes.Add(new Tuple<string, Builder>("WDB:GameObjectTemplate", () => { return GameObjectTemplates.Build(); }));
            }

            {
                if (gameObjects != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.GameObjectSpawns))
                    writes.Add(new Tuple<string, Builder>("Spawns:GameObject", () => { return BuildGameObject(gameObjects); }));
            }

            {
                var QuestTemplates = PacketFileProcessor.Current.GetProcessor<QuestTemplateStore>();
                if (QuestTemplates != null)
                    writes.Add(new Tuple<string, Builder>("WDB:QuestTemplate", () => { return QuestTemplates.Build(); }));
            }

            {
                var QuestPOIs = PacketFileProcessor.Current.GetProcessor<QuestPOIStore>();
                if (QuestPOIs != null)
                    writes.Add(new Tuple<string, Builder>("QuestPOI", () => { return QuestPOIs.Build(); }));
            }

            {
                var QuestOffers = PacketFileProcessor.Current.GetProcessor<QuestOffersStore>();
                if (QuestOffers != null)
                    writes.Add(new Tuple<string, Builder>("QuestOffer", () => { return QuestOffers.Build(); }));
            }

            {
                var QuestRewards = PacketFileProcessor.Current.GetProcessor<QuestRewardStore>();
                if (QuestRewards != null)
                    writes.Add(new Tuple<string, Builder>("QuestReward", () => { return QuestRewards.Build(); }));
            }

            {
                var CreatureTemplates = PacketFileProcessor.Current.GetProcessor<CreatureTemplateStore>();
                if (CreatureTemplates != null)
                    writes.Add(new Tuple<string, Builder>("WDB:CreatureTemplate", () => { return CreatureTemplates.Build(); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureTemplate))
                    writes.Add(new Tuple<string, Builder>("NonWDB:CreatureTemplate", () => { return BuildNpcTemplateNonWDB(units); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureTemplate))
                    writes.Add(new Tuple<string, Builder>("CreatureAddon", () => { return BuildAddon(units); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureTemplate))
                    writes.Add(new Tuple<string, Builder>("CreatureModelData", () => { return BuildModelData(units); }));
            }

            {
                var CreatureSpellsX = PacketFileProcessor.Current.GetProcessor<CreatureSpellsXStore>();
                if (CreatureSpellsX != null)
                    writes.Add(new Tuple<string, Builder>("CreatureSpellsX", () => { return CreatureSpellsX.Build(); }));
            }

            {
                var CreatureTexts = PacketFileProcessor.Current.GetProcessor<CreatureTextStore>();
                if (CreatureTexts != null)
                    writes.Add(new Tuple<string, Builder>("CreatureText", () => { return CreatureTexts.Build(); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureEquip))
                    writes.Add(new Tuple<string, Builder>("CreatureEquip", () => { return BuildCreatureEquip(units); }));
            }

    
            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureMovement))
                    writes.Add(new Tuple<string, Builder>("CreatureMovement", () => { return BuildCreatureMovement(units); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureSpawns))
                    writes.Add(new Tuple<string, Builder>("Spawns:Creature", () => { return BuildCreature(units); }));
            }

            {
                var NpcTrainers = PacketFileProcessor.Current.GetProcessor<NpcTrainerStore>();
                if (NpcTrainers != null)
                    writes.Add(new Tuple<string, Builder>("NpcTrainer", () => { return NpcTrainers.Build(); }));
            }

            {
                var NpcVendors = PacketFileProcessor.Current.GetProcessor<NpcVendorStore>();
                if (NpcVendors != null)
                    writes.Add(new Tuple<string, Builder>("NpcVendor", () => { return NpcVendors.Build(); }));
            }

            {
                var PageTexts = PacketFileProcessor.Current.GetProcessor<PageTextStore>();
                if (PageTexts != null)
                    writes.Add(new Tuple<string, Builder>("WDB:PageText", () => { return PageTexts.Build(); }));
            }

            {
                var NpcTexts = PacketFileProcessor.Current.GetProcessor<NpcTextStore>();
                if (NpcTexts != null)
                    writes.Add(new Tuple<string, Builder>("WDB:NpcText", () => { return NpcTexts.Build(); }));
            }

            {
                var Gossips = PacketFileProcessor.Current.GetProcessor<NpcGossipStore>();
                if (Gossips != null)
                    writes.Add(new Tuple<string, Builder>("Gossip", () => { return Gossips.Build(); }));
            }

            {
                var Loots = PacketFileProcessor.Current.GetProcessor<LootStore>();
                if (Loots != null)
                    writes.Add(new Tuple<string, Builder>("Loot", () => { return Loots.Build(); }));
            }

            {
                var SniffDatas = PacketFileProcessor.Current.GetProcessor<SniffDataStore>();
                if (SniffDatas != null)
                    writes.Add(new Tuple<string, Builder>("SniffData", () => { return SniffDatas.Build(); }));
            }

            {
                var StartInformations = PacketFileProcessor.Current.GetProcessor<StartInformationStore>();
                if (StartInformations != null)
                    writes.Add(new Tuple<string, Builder>("StartInformation", () => { return StartInformations.Build(); }));
            }

            {
                if (units != null && Settings.SQLOutput.HasFlag(SQLOutputFlags.ObjectNames))
                    writes.Add(new Tuple<string, Builder>("ObjectNames", () => { return BuildObjectNames(); }));
            }

            // only overwrite file if no global file name was specified
            using (var store = new SQLFile(fileName, string.IsNullOrWhiteSpace(Settings.SQLFileName)))
            {
                int max = writes.Count;
                int i = 0;
                foreach (var w in writes)
                {
                    ++i;
                    Trace.WriteLine(String.Format("{0}/{1} - Write {2}", i, max, w.Item1));
                    store.WriteData(w.Item2());
                }

                Trace.WriteLine(store.WriteToFile(header)
                                    ? String.Format("{0}: Saved file to '{1}'", prefix, fileName)
                                    : "No SQL files created -- selected data stores are empty.");
            }
        }

        public static string BuildCreature(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature";
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            uint count = 0;
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var unit in units)
            {
                var row = new QueryBuilder.SQLInsertRow();

                var creature = unit.Value;

                if (Settings.SpawnDumpFilterArea.Length > 0)
                    if (!(creature.Area.ToString().MatchesFilters(Settings.SpawnDumpFilterArea)))
                        continue;

                UpdateField uf;
                if (!creature.UpdateFields.TryGetValue((int)UpdateFields.GetUpdateFieldOffset(ObjectField.OBJECT_FIELD_ENTRY), out uf))
                    continue;   // broken entry, nothing to spawn

                var entry = uf.UInt32Value;

                var spawnTimeSecs = creature.GetDefaultSpawnTime();
                var movementType = 0; // TODO: Find a way to check if our unit got random movement
                var spawnDist = (movementType == 1) ? 5 : 0;

                row.AddValue("guid", "@CGUID+" + count, noQuotes: true);
                row.AddValue("id", entry);
                row.AddValue("map", creature.Map);
                row.AddValue("spawnMask", 1);
                row.AddValue("phaseMask", creature.PhaseMask);
                row.AddValue("position_x", creature.Movement.Position.X);
                row.AddValue("position_y", creature.Movement.Position.Y);
                row.AddValue("position_z", creature.Movement.Position.Z);
                row.AddValue("orientation", creature.Movement.Orientation);
                row.AddValue("spawntimesecs", spawnTimeSecs);
                row.AddValue("spawndist", spawnDist);
                row.AddValue("MovementType", movementType);
                row.Comment = names.GetName(StoreNameType.Unit, (int)unit.Key.GetEntry(), false);
                row.Comment += " (Area: " + names.GetName(StoreNameType.Area, creature.Area, false) + ")";

                if (creature.IsTemporarySpawn())
                {
                    row.CommentOut = true;
                    row.Comment += " - !!! might be temporary spawn !!!";
                }
                else
                    ++count;

                if (creature.Movement.HasWpsOrRandMov)
                    row.Comment += " (possible waypoints or random movement)";

                rows.Add(row);
            }

            var result = new StringBuilder();
            // delete query for GUIDs
            var delete = new QueryBuilder.SQLDelete(Tuple.Create("@CGUID+0", "@CGUID+" + --count), "guid", tableName);
            result.Append(delete.Build());

            var sql = new QueryBuilder.SQLInsert(tableName, rows, withDelete: false);
            result.Append(sql.Build());
            return result.ToString();
        }

        public static string BuildGameObject(Dictionary<Guid, GameObject> gameObjects)
        {
            if (gameObjects.Count == 0)
                return string.Empty;

            const string tableName = "gameobject";
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            uint count = 0;
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var gameobject in gameObjects)
            {
                var row = new QueryBuilder.SQLInsertRow();

                var go = gameobject.Value;

                if (Settings.SpawnDumpFilterArea.Length > 0)
                    if (!(go.Area.ToString().MatchesFilters(Settings.SpawnDumpFilterArea)))
                        continue;

                uint animprogress = 0;
                uint state = 0;
                UpdateField uf;
                if (!go.UpdateFields.TryGetValue((int)UpdateFields.GetUpdateFieldOffset(ObjectField.OBJECT_FIELD_ENTRY), out uf))
                    continue;   // broken entry, nothing to spawn

                var entry = uf.UInt32Value;

                if (go.UpdateFields.TryGetValue((int)UpdateFields.GetUpdateFieldOffset(GameObjectField.GAMEOBJECT_BYTES_1), out uf))
                {
                    var bytes = uf.UInt32Value;
                    state = (bytes & 0x000000FF);
                    animprogress = Convert.ToUInt32((bytes & 0xFF000000) >> 24);
                }

                row.AddValue("guid", "@OGUID+" + count, noQuotes: true);
                row.AddValue("id", entry);
                row.AddValue("map", go.Map);
                row.AddValue("spawnMask", 1);
                row.AddValue("phaseMask", go.PhaseMask);
                row.AddValue("position_x", go.Movement.Position.X);
                row.AddValue("position_y", go.Movement.Position.Y);
                row.AddValue("position_z", go.Movement.Position.Z);
                row.AddValue("orientation", go.Movement.Orientation);

                var rotation = go.GetRotation();
                if (rotation != null && rotation.Length == 4)
                {
                    row.AddValue("rotation0", rotation[0]);
                    row.AddValue("rotation1", rotation[1]);
                    row.AddValue("rotation2", rotation[2]);
                    row.AddValue("rotation3", rotation[3]);
                }
                else
                {
                    row.AddValue("rotation0", 0);
                    row.AddValue("rotation1", 0);
                    row.AddValue("rotation2", 0);
                    row.AddValue("rotation3", 0);
                }

                row.AddValue("spawntimesecs", go.GetDefaultSpawnTime());
                row.AddValue("animprogress", animprogress);
                row.AddValue("state", state);
                row.Comment = names.GetName(StoreNameType.GameObject, (int)gameobject.Key.GetEntry(), false);
                row.Comment += " (Area: " + names.GetName(StoreNameType.Area, go.Area, false) + ")";

                if (go.IsTemporarySpawn())
                {
                    row.CommentOut = true;
                    row.Comment += " - !!! might be temporary spawn !!!";
                }
                else if (go.IsTransport())
                {
                    row.CommentOut = true;
                    row.Comment += " - !!! transport !!!";
                }
                else
                    ++count;

                rows.Add(row);
            }

            var result = new StringBuilder();

            // delete query for GUIDs
            var delete = new QueryBuilder.SQLDelete(Tuple.Create("@OGUID+0", "@OGUID+" + --count), "guid", tableName);
            result.Append(delete.Build());

            var sql = new QueryBuilder.SQLInsert(tableName, rows, withDelete: false);
            result.Append(sql.Build());
            return result.ToString();
        }

        public static string BuildAddon(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature_template_addon";
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var unit in units)
            {
                var npc = unit.Value;

                var row = new QueryBuilder.SQLInsertRow();
                row.AddValue("entry", unit.Key.GetEntry());
                row.AddValue("mount", npc.Mount);
                row.AddValue("bytes1", npc.Bytes1, true);
                row.AddValue("bytes2", npc.Bytes2, true);

                var auras = string.Empty;
                var commentAuras = string.Empty;
                if (npc.Auras != null && npc.Auras.Count() != 0)
                {
                    foreach (var aura in npc.Auras)
                    {
                        if (aura == null) continue;
                        if (!aura.AuraFlags.HasAnyFlag(AuraFlag.NotCaster)) continue; // usually "template auras" do not have caster
                        auras += aura.SpellId + " ";
                        commentAuras += names.GetName(StoreNameType.Spell, (int)aura.SpellId, false) + ", ";
                    }
                    auras = auras.TrimEnd(' ');
                    commentAuras = commentAuras.TrimEnd(',', ' ');
                }
                row.AddValue("auras", auras);

                row.Comment += names.GetName(StoreNameType.Unit, (int)unit.Key.GetEntry(), false);
                if (!String.IsNullOrWhiteSpace(auras))
                    row.Comment += " - " + commentAuras;

                rows.Add(row);
            }

            return new QueryBuilder.SQLInsert(tableName, rows).Build();
        }

        public static string BuildModelData(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature_model_info";

            // Build a dictionary with model data; model is the key
            var models = new SortedDictionary<uint, Tuple<float, float, Gender>>();
            foreach (var unit in units)
            {
                var npc = unit.Value;

                if (npc.Model == null)
                    continue;
                var model = (uint)npc.Model;

                // Do not add duplicate models
                if (models.ContainsKey(model))
                    continue;

                var boundingRadius = 0.0f;
                if (npc.BoundingRadius != null)
                    boundingRadius = (float)npc.BoundingRadius;

                var combatReach = 0.0f;
                if (npc.CombatReach != null)
                    combatReach = (float)npc.CombatReach;

                var gender = Gender.None;
                if (npc.Gender != null)
                    gender = (Gender)npc.Gender;

                models.Add(model, Tuple.Create(boundingRadius, combatReach, gender));
            }

            Dictionary<uint, dynamic> modelsDb = null;
            if (SQLConnector.Enabled)
            {
                modelsDb = SQLDatabase.GetDict<uint>(string.Format(
                    "SELECT `modelid`, `bounding_radius`, `combat_reach`," +
                    "`gender` FROM `{0}`.{1} WHERE `modelid` IN ({2});", ParserSettings.MySQL.TDBDB, tableName, String.Join(",", models.Keys)));
            }

            var rowsUpd = new List<QueryBuilder.SQLUpdateRow>();
            var rowsIns = new List<QueryBuilder.SQLInsertRow>();
            foreach (var model in models)
            {
                if (modelsDb != null && modelsDb.Count != 0)
                {
                    if (modelsDb.ContainsKey(model.Key)) // possible update
                    {
                        var row = new QueryBuilder.SQLUpdateRow();

                        if (!Utilities.EqualValues(modelsDb[model.Key].Item1, model.Value.Item1))
                            row.AddValue("bounding_radius", model.Value.Item1);

                        if (!Utilities.EqualValues(modelsDb[model.Key].Item2, model.Value.Item2))
                            row.AddValue("combat_reach", model.Value.Item2);

                        if (!Utilities.EqualValues(modelsDb[model.Key].Item3, model.Value.Item3))
                            row.AddValue("gender", model.Value.Item3);

                        row.AddWhere("modelid", model.Key);
                        row.Table = tableName;

                        if (row.ValueCount != 0)
                            rowsUpd.Add(row);
                    }
                    else // insert new
                    {
                        var row = new QueryBuilder.SQLInsertRow();
                        row.AddValue("modelid", model.Key);
                        row.AddValue("bounding_radius", model.Value.Item1);
                        row.AddValue("combat_reach", model.Value.Item2);
                        row.AddValue("gender", model.Value.Item3);
                        rowsIns.Add(row);
                    }
                }
                else // no db values, simply do inserts
                {
                    var row = new QueryBuilder.SQLInsertRow();
                    row.AddValue("modelid", model.Key);
                    row.AddValue("bounding_radius", model.Value.Item1);
                    row.AddValue("combat_reach", model.Value.Item2);
                    row.AddValue("gender", model.Value.Item3);
                    rowsIns.Add(row);
                }
            }

            return new QueryBuilder.SQLInsert(tableName, rowsIns).Build() +
                   new QueryBuilder.SQLUpdate(rowsUpd).Build();
        }

        public static string BuildCreatureEquip(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature_equip_template";
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var unit in units)
            {
                var row = new QueryBuilder.SQLInsertRow();
                var creature = unit.Value;
                var equipData = creature.Equipment;

                // check if fields are empty
                if (equipData == null || equipData.All(value => value == 0))
                    continue;

                row.AddValue("entry", unit.Key.GetEntry());
                row.AddValue("itemEntry1", equipData[0]);
                row.AddValue("itemEntry2", equipData[1]);
                row.AddValue("itemEntry3", equipData[2]);
                row.Comment = names.GetName(StoreNameType.Unit, (int)unit.Key.GetEntry(), false);
                rows.Add(row);
            }

            return new QueryBuilder.SQLInsert(tableName, rows).Build();
        }

        public static string BuildCreatureMovement(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature_movement";
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var unit in units)
            {
                var row = new QueryBuilder.SQLInsertRow();

                var npc = unit.Value;

                row.AddValue("Id", unit.Key.GetEntry());
                row.AddValue("MovementFlags", npc.Movement.Flags, true);
                row.AddValue("MovementFlagsExtra", npc.Movement.FlagsExtra, true);
                row.AddValue("ufBytes1", npc.Bytes1, true);
                row.AddValue("ufBytes2", npc.Bytes2, true);
                row.AddValue("ufFlags", npc.UnitFlags, true);
                row.AddValue("ufFlags2", npc.UnitFlags2, true);

                row.Comment = names.GetName(StoreNameType.Unit, (int)unit.Key.GetEntry(), false);
                /*
                row.Comment += " - MoveFlags: " + npc.Movement.Flags + " - MoveFlags2: " + npc.Movement.FlagsExtra;
                row.Comment += " - Bytes1: " + npc.Bytes1 + " - Bytes2: " + npc.Bytes2 + " - UnitFlags: " + npc.UnitFlags;
                row.Comment += " - UnitFlags2: " + npc.UnitFlags2;
                 */
                rows.Add(row);
            }

            return new QueryBuilder.SQLInsert(tableName, rows, ignore: true, withDelete: false).Build();
        }

        // Non-WDB data but nevertheless data that should be saved to creature_template
        public static string BuildNpcTemplateNonWDB(Dictionary<Guid, Unit> units)
        {
            if (units.Count == 0)
                return string.Empty;

            const string tableName = "creature_template";

            var rows = new List<QueryBuilder.SQLUpdateRow>();
            ICollection<uint> key = new Collection<uint>();
            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            foreach (var unit in units)
            {
                // don't save duplicates
                if (key.Contains(unit.Key.GetEntry()))
                    continue;

                var row = new QueryBuilder.SQLUpdateRow();
                var npc = unit.Value;

                var name = names.GetName(StoreNameType.Unit, (int)unit.Key.GetEntry(), false);

                // Only movement flags in 335 are being read correctly - fix them and remove this if
                if (ClientVersion.Build == ClientVersionBuild.V3_3_5a_12340)
                {
                    if (npc.Movement.Flags.HasAnyFlag(MovementFlag.CanFly) && npc.Movement.Flags.HasAnyFlag(MovementFlag.WalkMode))
                        row.AddValue("InhabitType", InhabitType.Ground | InhabitType.Air, true);
                    else if (npc.Movement.Flags.HasAnyFlag(MovementFlag.DisableGravity))
                        row.AddValue("InhabitType", InhabitType.Air, true);
                }

                row.AddValue("HoverHeight", npc.HoverHeight, 1);
                row.AddValue("WalkSpeed", npc.Movement.WalkSpeed, 1);
                row.AddValue("RunSpeed", npc.Movement.RunSpeed, 1.142857);
                row.AddValue("VehicleId", npc.Movement.VehicleId, 0u);
                row.AddValue("Size", npc.Size, 1u);
                row.AddValue("Level", npc.Level, 1u); // min/max
                row.AddValue("Faction", npc.Faction, 35u); // faction_A, faction_H
                row.AddValue("UnitFlags", npc.UnitFlags, UnitFlags.None, true);
                row.AddValue("BaseAttackTime", npc.MeleeTime, 2000u);
                row.AddValue("RangeAttackTime", npc.RangedTime, 0u); // 2000?
                row.AddValue("Model", npc.Model, 0u); // model1, model2, ...
                row.AddValue("DynamicFlags", npc.DynamicFlags, UnitDynamicFlags.None, true);
                row.AddValue("NpcFlags", npc.NpcFlags, NPCFlags.None, true);

                if (npc.Resistances != null)
                    for (var i = 1; i < npc.Resistances.Length; ++i) // No armor
                        row.AddValue("Resistances" + i, npc.Resistances[i], 0u);

                // row.AddValue("ManaMod", npc.ManaMod, 1); this is not mod, it needs to be calculated
                // row.AddValue("HealthMod", npc.HealthMod, 1);
                row.AddValue("Class", npc.Class, Class.Warrior);
                //row.AddValue("Race", npc.Race, Race.None);

                row.AddWhere("entry", unit.Key.GetEntry());
                row.Table = tableName;
                row.Comment = name;

                rows.Add(row);
                key.Add(unit.Key.GetEntry());
            }

            return new QueryBuilder.SQLUpdate(rows).Build();
        }

        public static string BuildObjectNames()
        {
            var EntryNames = PacketFileProcessor.Current.GetProcessor<NameStore>().EntryNames;
            if (EntryNames.IsEmpty())
                return String.Empty;

            const string tableName = "ObjectNames";

            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var data in EntryNames)
            {
                var row = new QueryBuilder.SQLInsertRow();

                row.AddValue("ObjectType", data.Key.Item2.ToString());
                row.AddValue("Id", data.Key.Item1);
                row.AddValue("Name", data.Value.Item1);

                rows.Add(row);
            }

            return new QueryBuilder.SQLInsert(tableName, rows, 2, ignore: true, withDelete: false).Build();
        }
    }
}
