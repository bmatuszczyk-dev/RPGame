using System;
using System.Collections.Generic;
using RPGame.Core.Spells;

namespace RPGame.Core.Effects
{
    internal static class RuntimeSpellBehaviorOrderer
    {
        public static IReadOnlyList<IRuntimeSpellBehavior> Order(
            IReadOnlyList<RuntimeSpellBehaviorOrderEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<IRuntimeSpellBehavior>();
            }

            Validate(entries);

            List<IndexedRuntimeSpellBehaviorOrderEntry> orderedEntries = new(entries.Count);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                orderedEntries.Add(new IndexedRuntimeSpellBehaviorOrderEntry(
                    entries[entryIndex],
                    entryIndex));
            }

            orderedEntries.Sort(CompareRuntimeBehaviors);

            List<IRuntimeSpellBehavior> behaviors = new(orderedEntries.Count);
            for (int entryIndex = 0; entryIndex < orderedEntries.Count; entryIndex++)
            {
                behaviors.Add(orderedEntries[entryIndex].Entry.Behavior);
            }

            return behaviors;
        }

        private static void Validate(IReadOnlyList<RuntimeSpellBehaviorOrderEntry> entries)
        {
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                RuntimeSpellBehaviorOrderEntry entry = entries[entryIndex];
                if (entry.ExecutionOrder < 0)
                {
                    throw new InvalidOperationException(
                        "Runtime spell behavior execution order cannot be negative.");
                }

                if (entry.Behavior is not ISpellBehavior spellBehavior)
                {
                    continue;
                }

                for (int nextIndex = entryIndex + 1; nextIndex < entries.Count; nextIndex++)
                {
                    RuntimeSpellBehaviorOrderEntry nextEntry = entries[nextIndex];
                    if (nextEntry.Behavior is ISpellBehavior nextSpellBehavior
                        && nextSpellBehavior.Phase == spellBehavior.Phase
                        && nextEntry.ExecutionOrder == entry.ExecutionOrder)
                    {
                        throw new InvalidOperationException(
                            $"Duplicate spell behavior execution order {entry.ExecutionOrder} for phase {spellBehavior.Phase}.");
                    }
                }
            }
        }

        private static int CompareRuntimeBehaviors(
            IndexedRuntimeSpellBehaviorOrderEntry first,
            IndexedRuntimeSpellBehaviorOrderEntry second)
        {
            if (first.Entry.Behavior is ISpellBehavior firstSpellBehavior
                && second.Entry.Behavior is ISpellBehavior secondSpellBehavior)
            {
                int phaseComparison = firstSpellBehavior.Phase.CompareTo(secondSpellBehavior.Phase);
                if (phaseComparison != 0)
                {
                    return phaseComparison;
                }

                int orderComparison =
                    first.Entry.ExecutionOrder.CompareTo(second.Entry.ExecutionOrder);
                if (orderComparison != 0)
                {
                    return orderComparison;
                }
            }

            if (first.Entry.Behavior is ISpellBehavior)
            {
                return -1;
            }

            if (second.Entry.Behavior is ISpellBehavior)
            {
                return 1;
            }

            return first.Index.CompareTo(second.Index);
        }

        private readonly struct IndexedRuntimeSpellBehaviorOrderEntry
        {
            public IndexedRuntimeSpellBehaviorOrderEntry(
                RuntimeSpellBehaviorOrderEntry entry,
                int index)
            {
                Entry = entry;
                Index = index;
            }

            public RuntimeSpellBehaviorOrderEntry Entry { get; }
            public int Index { get; }
        }
    }
}
