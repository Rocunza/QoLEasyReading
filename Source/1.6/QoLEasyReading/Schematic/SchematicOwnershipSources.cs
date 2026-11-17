using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C1 source adapters. Preserve ownership provenance: trader stock is
    /// NEVER scanned here, and non-player holders are skipped deliberately.
    /// Collection APIs are the verified RimWorld 1.6 signatures.
    /// </summary>
    internal static partial class SchematicOwnershipResolver
    {
        private static void ScanTradeColony(TradeDeal deal, ScanContext ctx)
        {
            try
            {
                List<Tradeable> rows = deal.AllTradeables;
                if (rows == null) { ctx.Fail("TRADE_ROWS_MISSING"); return; }
                for (int i = 0; i < rows.Count && !ctx.Result.Truncated; i++)
                {
                    Tradeable row = rows[i];
                    if (row == null) { ctx.Fail("NULL_TRADE_ROW"); continue; }
                    if (!row.IsThing) continue;
                    ctx.Result.ColonyTradeablesVisited++;
                    // NEVER include row.thingsTrader. Trading stock is not proof
                    // of player ownership even when its research key matches.
                    List<Thing> colony = row.thingsColony;
                    if (colony == null) continue;
                    for (int j = 0; j < colony.Count && !ctx.Result.Truncated; j++)
                        ctx.Visit(colony[j], "TRADE_COLONY", 0);
                }
                ctx.DrainHolders("TRADE_COLONY");
            }
            catch (Exception ex) { ctx.Fail("TRADE_COLONY:" + ex.GetType().Name); }
        }

        private static void ScanPlayerMaps(ScanContext ctx)
        {
            try
            {
                List<Map> maps = Find.Maps;
                if (maps == null) { ctx.Fail("MAP_LIST_MISSING"); return; }
                for (int i = 0; i < maps.Count && !ctx.Result.Truncated; i++)
                {
                    Map map = maps[i];
                    if (map == null || map.Disposed) continue;
                    if (!map.IsPlayerHome) continue; // no unproven neutral maps
                    ctx.Result.MapsVisited++;
                    if (map.listerThings == null) { ctx.Fail("MAP_LISTER_MISSING"); continue; }
                    List<Thing> things = map.listerThings.AllThings;
                    if (things == null) { ctx.Fail("MAP_THINGS_MISSING"); continue; }
                    for (int j = 0; j < things.Count && !ctx.Result.Truncated; j++)
                    {
                        Thing thing = things[j];
                        if (thing == null) { ctx.Fail("NULL_MAP_THING"); continue; }
                        if (!ctx.Budget()) break;
                        ctx.Result.RootsExamined++;
                        // A spawned loose book on a player-home map is normally
                        // colony stock, but is tagged as an assumption in logs.
                        if (thing.Spawned && thing.Map == map &&
                            (thing.Faction == null || thing.Faction == Faction.OfPlayer) &&
                            thing.GetInnerIfMinified() is Book)
                        {
                            ctx.Visit(thing, "HOME_MAP_LOOSE", 0);
                            continue;
                        }
                        // Only player-faction holders. Visiting a trader Pawn's
                        // inventory would otherwise make every offered book Owned.
                        if (thing.Faction == Faction.OfPlayer && thing is IThingHolder)
                        {
                            ctx.QueueHolder((IThingHolder)thing, 0);
                        }
                        else if (thing is IThingHolder)
                        {
                            if (thing.Faction == null) ctx.Result.AmbiguousHoldersSkipped++;
                            else ctx.Result.ForeignRootsSkipped++;
                        }
                    }
                    ctx.DrainHolders("PLAYER_MAP_HOLDER");
                }
            }
            catch (Exception ex) { ctx.Fail("PLAYER_MAPS:" + ex.GetType().Name); }
        }

        private static void ScanPlayerCaravans(ScanContext ctx)
        {
            try
            {
                WorldObjectsHolder world = Find.WorldObjects;
                if (world == null) { ctx.Fail("WORLD_OBJECTS_MISSING"); return; }
                List<Caravan> caravans = world.Caravans;
                if (caravans == null) { ctx.Fail("CARAVAN_LIST_MISSING"); return; }
                for (int i = 0; i < caravans.Count && !ctx.Result.Truncated; i++)
                {
                    Caravan caravan = caravans[i];
                    if (caravan == null || !caravan.IsPlayerControlled) continue;
                    ctx.Result.CaravansVisited++;
                    ctx.QueueHolder(caravan, 0);
                    ctx.DrainHolders("PLAYER_CARAVAN");
                }
            }
            catch (Exception ex) { ctx.Fail("CARAVANS:" + ex.GetType().Name); }
        }
    }
}
