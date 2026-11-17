using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // C1 is evidence gathering, NOT a purchase decision or an icon source.
    internal enum SchematicOwnershipEvidence
    {
        MatchFound,
        NoMatchInScannedScope,
        InspectionFailed
    }

    internal enum SchematicOwnershipDecision
    {
        Owned,
        NotOwned,
        Unknown
    }

    internal sealed class SchematicOwnershipSnapshot
    {
        internal readonly Dictionary<string, int> FoundKeys =
            new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> ConfirmedKeys =
            new Dictionary<string, int>(StringComparer.Ordinal);
        internal int MapsVisited;
        internal int CaravansVisited;
        internal int ColonyTradeablesVisited;
        internal int ThingsExamined;
        internal int RootsExamined;
        internal int SchematicBooks;
        internal int InvalidBooks;
        internal int DuplicateBookReferences;
        internal int ForeignRootsSkipped;
        internal int AmbiguousHoldersSkipped;
        internal int GroundBooksAssumedColony;
        // L1: metadata only, no Thing/Pawn/Book references stored in snapshots.
        internal readonly Dictionary<string, SchematicOwnershipTraceRecord> TraceBooks =
            new Dictionary<string, SchematicOwnershipTraceRecord>(StringComparer.Ordinal);
        internal int TraceMissingIds;
        internal int TraceIdCollisions;
        internal bool TraceFailed;
        internal bool TraceCountMismatch;
        internal long ElapsedMilliseconds;
        internal bool Truncated;
        internal bool InspectionFailed;
        internal string FailureReason;
        // C1 cannot prove that arbitrary mod-held/world-hidden items were scanned.
        // Missing a book is ALWAYS Unknown, not NotOwned, even if all the known
        // vanilla sources were traversed without error.
        internal const string Coverage = "KNOWN_PLAYER_MAPS+OWNED_HOLDERS+CARAVANS+TRADE_COLONY;OTHER_MOD_CONTAINERS_UNVERIFIED";

        internal SchematicOwnershipEvidence GetEvidence(string fullKey)
        {
            if (string.IsNullOrEmpty(fullKey)) return SchematicOwnershipEvidence.InspectionFailed;
            if (FoundKeys.ContainsKey(fullKey)) return SchematicOwnershipEvidence.MatchFound;
            return Truncated || InspectionFailed || InvalidBooks > 0
                ? SchematicOwnershipEvidence.InspectionFailed
                : SchematicOwnershipEvidence.NoMatchInScannedScope;
        }

        internal SchematicOwnershipDecision GetDecision(string fullKey)
        {
            // A confirmed positive can be used even if a different source was
            // truncated. A negative CANNOT be asserted in the present scope.
            // A loose book on the ground of a player-home map may have been
            // dropped by a visitor. Keep its positive MATCH_FOUND evidence,
            // but do not claim verified player ownership until corroborated.
            return !string.IsNullOrEmpty(fullKey) && ConfirmedKeys.ContainsKey(fullKey)
                ? SchematicOwnershipDecision.Owned : SchematicOwnershipDecision.Unknown;
        }

        internal int CountMatches(string fullKey)
        {
            int count;
            return !string.IsNullOrEmpty(fullKey) && FoundKeys.TryGetValue(fullKey, out count)
                ? count : 0;
        }
    }

    /// <summary>
    /// S3-C1: one bounded read-only ownership snapshot per trade deal / manual
    /// probe. Searches only player-controlled sources; never adds/removes/moves
    /// Things, modifies research, or assumes trader inventory is player-owned.
    /// </summary>
    internal static partial class SchematicOwnershipResolver
    {
        private const int MaxThings = 50000;
        private const int MaxSchematics = 2048;
        private const int MaxHolderDepth = 16;

        private sealed class RefComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly RefComparer<T> Instance = new RefComparer<T>();
            public bool Equals(T a, T b) { return object.ReferenceEquals(a, b); }
            public int GetHashCode(T value)
            {
                return value == null ? 0 : RuntimeHelpers.GetHashCode(value);
            }
        }

        private struct HolderEntry
        {
            internal IThingHolder Holder;
            internal int Depth;
            internal HolderEntry(IThingHolder holder, int depth)
            {
                Holder = holder;
                Depth = depth;
            }
        }

        private sealed class ScanContext
        {
            internal SchematicOwnershipSnapshot Result = new SchematicOwnershipSnapshot();
            internal HashSet<Thing> SeenThings =
                new HashSet<Thing>(RefComparer<Thing>.Instance);
            internal HashSet<Book> SeenBooks =
                new HashSet<Book>(RefComparer<Book>.Instance);
            internal HashSet<IThingHolder> SeenHolders =
                new HashSet<IThingHolder>(RefComparer<IThingHolder>.Instance);
            internal Stack<HolderEntry> PendingHolders = new Stack<HolderEntry>();
            // Scanner-lifetime only; the result retains no strong Thing references.
            internal Dictionary<Book, SchematicOwnershipTraceRecord> TraceByBook =
                new Dictionary<Book, SchematicOwnershipTraceRecord>(RefComparer<Book>.Instance);
            internal Dictionary<string, Book> TraceById =
                new Dictionary<string, Book>(StringComparer.Ordinal);

            private void CaptureTrace(Book book, Thing outer, string key, string source)
            {
                // T1: the SAME once-per-trade production scan now records immutable
                // ThingID/title/provenance evidence with Debug OFF as well.
                // No extra traversal and no change to ownership classification.
                try
                {
                    SchematicOwnershipTrace.Capture(Result, TraceByBook, TraceById,
                        book, outer, key, source);
                }
                catch (Exception)
                {
                    // Trace failures must never modify GetDecision or the scanner.
                    Result.TraceFailed = true;
                }
            }

            internal void CheckTraceCounts()
            {
                // This consistency gate also protects production tooltip names.
                int expected = 0;
                foreach (KeyValuePair<string, int> entry in Result.FoundKeys)
                    expected += entry.Value;
                if (Result.TraceBooks.Count != expected)
                    Result.TraceCountMismatch = true;
                foreach (KeyValuePair<string, SchematicOwnershipTraceRecord> entry in Result.TraceBooks)
                {
                    if (entry.Value == null || string.IsNullOrEmpty(entry.Value.BookThingId))
                        Result.TraceCountMismatch = true;
                }
            }

            internal void Fail(string why)
            {
                Result.InspectionFailed = true;
                if (string.IsNullOrEmpty(Result.FailureReason)) Result.FailureReason = why;
            }

            internal bool Budget()
            {
                if (Result.ThingsExamined + Result.RootsExamined < MaxThings &&
                    Result.SchematicBooks < MaxSchematics)
                    return true;
                Result.Truncated = true;
                if (string.IsNullOrEmpty(Result.FailureReason))
                    Result.FailureReason = "SCAN_LIMIT";
                return false;
            }

            internal void Visit(Thing thing, string source, int depth)
            {
                if (Result.Truncated || thing == null) return;
                // Player-controlled holders can contain captured/guest pawns;
                // never recurse into a foreign pawn's private possessions.
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.Faction != Faction.OfPlayer)
                {
                    Result.ForeignRootsSkipped++;
                    return;
                }
                if (!Budget()) return;
                if (!SeenThings.Add(thing))
                {
                    Result.DuplicateBookReferences++;
                    // One actual book can be observed in both map loose stock
                    // and the trade-colony list. Promote the provenance from
                    // home-map assumption to verified player trade stock.
                    if (source != "HOME_MAP_LOOSE")
                    {
                        try
                        {
                            Book seenBook = thing.GetInnerIfMinified() as Book;
                            if (seenBook != null && SchematicStatusIconResolver.IsSchematic(seenBook))
                            {
                                string knownKey;
                                if (SchematicResearchSignature.TryCreate(
                                    SchematicResearchResolver.Resolve(seenBook), out knownKey) &&
                                    !Result.ConfirmedKeys.ContainsKey(knownKey))
                                    Result.ConfirmedKeys[knownKey] = 1;
                                if (!string.IsNullOrEmpty(knownKey))
                                    CaptureTrace(seenBook, thing, knownKey, source);
                            }
                        }
                        catch (Exception ex) { Fail("DUPLICATE_THING:" + ex.GetType().Name); }
                    }
                    return;
                }
                Result.ThingsExamined++;
                try
                {
                    Thing real = thing.GetInnerIfMinified();
                    Book book = real as Book;
                    if (book != null && SchematicStatusIconResolver.IsSchematic(book))
                    {
                        // Count the actual Book, not the minified outer Thing.
                        if (!SeenBooks.Add(book))
                        {
                            Result.DuplicateBookReferences++;
                            string repeatedKey;
                            if (SchematicResearchSignature.TryCreate(
                                SchematicResearchResolver.Resolve(book), out repeatedKey))
                                CaptureTrace(book, thing, repeatedKey, source);
                        }
                        else
                        {
                            Result.SchematicBooks++;
                            SchematicResearchState state = SchematicResearchResolver.Resolve(book);
                            string key;
                            if (SchematicResearchSignature.TryCreate(state, out key))
                            {
                                int prior;
                                Result.FoundKeys.TryGetValue(key, out prior);
                                Result.FoundKeys[key] = prior + 1;
                                CaptureTrace(book, thing, key, source);
                                if (source != "HOME_MAP_LOOSE")
                                {
                                    int verified;
                                    Result.ConfirmedKeys.TryGetValue(key, out verified);
                                    Result.ConfirmedKeys[key] = verified + 1;
                                }
                            }
                            else Result.InvalidBooks++;
                            if (source == "HOME_MAP_LOOSE") Result.GroundBooksAssumedColony++;
                        }
                    }
                    // Nested player-owned containers are traversed through the
                    // actual holder contracts, including mod-defined child holders.
                    IThingHolder innerHolder = real as IThingHolder;
                    if (innerHolder != null) QueueHolder(innerHolder, depth + 1);
                    IThingHolder outerHolder = thing as IThingHolder;
                    if (outerHolder != null && !object.ReferenceEquals(innerHolder, outerHolder))
                        QueueHolder(outerHolder, depth + 1);
                }
                catch (Exception ex) { Fail("THING:" + ex.GetType().Name); }
            }

            internal void QueueHolder(IThingHolder holder, int depth)
            {
                if (holder == null || Result.Truncated) return;
                // GetChildHolders may directly return inventory trackers rather
                // than their Pawn. Walk the holder parent chain and reject
                // foreign/guest pawn ownership before reading any contents.
                try
                {
                    IThingHolder parent = holder;
                    for (int i = 0; i < MaxHolderDepth && parent != null; i++)
                    {
                        Pawn pawn = parent as Pawn;
                        if (pawn != null && pawn.Faction != Faction.OfPlayer)
                        {
                            Result.ForeignRootsSkipped++;
                            return;
                        }
                        IThingHolder next = parent.ParentHolder;
                        if (object.ReferenceEquals(next, parent)) break;
                        parent = next;
                    }
                }
                catch (Exception ex)
                {
                    Fail("HOLDER_PARENT:" + ex.GetType().Name);
                    return;
                }
                if (depth > MaxHolderDepth)
                {
                    Result.Truncated = true;
                    Result.FailureReason = "HOLDER_DEPTH_LIMIT";
                    return;
                }
                PendingHolders.Push(new HolderEntry(holder, depth));
            }

            internal void DrainHolders(string source)
            {
                List<IThingHolder> children = new List<IThingHolder>();
                while (PendingHolders.Count > 0 && !Result.Truncated)
                {
                    HolderEntry current = PendingHolders.Pop();
                    if (current.Holder == null || !SeenHolders.Add(current.Holder)) continue;
                    if (current.Depth > MaxHolderDepth)
                    {
                        Result.Truncated = true;
                        Result.FailureReason = "HOLDER_DEPTH_LIMIT";
                        break;
                    }
                    try
                    {
                        ThingOwner contents = current.Holder.GetDirectlyHeldThings();
                        if (contents != null)
                        {
                            for (int i = 0; i < contents.Count && !Result.Truncated; i++)
                                Visit(contents[i], source, current.Depth + 1);
                        }
                        children.Clear();
                        current.Holder.GetChildHolders(children);
                        for (int i = 0; i < children.Count && !Result.Truncated; i++)
                            QueueHolder(children[i], current.Depth + 1);
                    }
                    catch (Exception ex) { Fail("HOLDER:" + ex.GetType().Name); }
                }
            }
        }

        internal static SchematicOwnershipSnapshot Scan(TradeDeal deal)
        {
            ScanContext ctx = new ScanContext();
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                if (deal != null) ScanTradeColony(deal, ctx);
                else ctx.Fail("NO_TRADE_DEAL");
                ScanPlayerMaps(ctx);
                ScanPlayerCaravans(ctx);
                ctx.CheckTraceCounts();
            }
            catch (Exception ex) { ctx.Fail("SCAN:" + ex.GetType().Name); }
            finally
            {
                timer.Stop();
                ctx.Result.ElapsedMilliseconds = timer.ElapsedMilliseconds;
            }
            return ctx.Result;
        }

    }
}
