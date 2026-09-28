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
                    ValidateCallbackBehavior(entries, entryIndex, entry);
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

            int callbackOrderComparison =
                first.Entry.ExecutionOrder.CompareTo(second.Entry.ExecutionOrder);
            if (callbackOrderComparison != 0)
            {
                return callbackOrderComparison;
            }

            return first.Index.CompareTo(second.Index);
        }

        private static void ValidateCallbackBehavior(
            IReadOnlyList<RuntimeSpellBehaviorOrderEntry> entries,
            int entryIndex,
            RuntimeSpellBehaviorOrderEntry entry)
        {
            Type[] callbackTypes = GetCallbackBehaviorTypes(entry.Behavior);
            if (callbackTypes.Length == 0)
            {
                return;
            }

            for (int nextIndex = entryIndex + 1; nextIndex < entries.Count; nextIndex++)
            {
                RuntimeSpellBehaviorOrderEntry nextEntry = entries[nextIndex];
                if (nextEntry.Behavior is ISpellBehavior)
                {
                    continue;
                }

                Type duplicateCallbackType = GetDuplicateCallbackType(
                    callbackTypes,
                    GetCallbackBehaviorTypes(nextEntry.Behavior));
                if (duplicateCallbackType != null
                    && nextEntry.ExecutionOrder == entry.ExecutionOrder)
                {
                    throw new InvalidOperationException(
                        $"Duplicate runtime callback behavior execution order {entry.ExecutionOrder} for {duplicateCallbackType.Name}.");
                }
            }
        }

        private static Type[] GetCallbackBehaviorTypes(IRuntimeSpellBehavior behavior)
        {
            if (behavior == null)
            {
                return Type.EmptyTypes;
            }

            Type[] interfaces = behavior.GetType().GetInterfaces();
            List<Type> callbackTypes = new();
            for (int interfaceIndex = 0; interfaceIndex < interfaces.Length; interfaceIndex++)
            {
                Type interfaceType = interfaces[interfaceIndex];
                if (interfaceType == typeof(IRuntimeSpellBehavior)
                    || interfaceType == typeof(IInitializableRuntimeSpellBehavior)
                    || interfaceType == typeof(IPhasedSpellBehavior)
                    || interfaceType == typeof(ISpellBehavior)
                    || !typeof(IRuntimeSpellBehavior).IsAssignableFrom(interfaceType))
                {
                    continue;
                }

                callbackTypes.Add(interfaceType);
            }

            return callbackTypes.Count > 0
                ? callbackTypes.ToArray()
                : Type.EmptyTypes;
        }

        private static Type GetDuplicateCallbackType(
            IReadOnlyList<Type> firstTypes,
            IReadOnlyList<Type> secondTypes)
        {
            for (int firstIndex = 0; firstIndex < firstTypes.Count; firstIndex++)
            {
                for (int secondIndex = 0; secondIndex < secondTypes.Count; secondIndex++)
                {
                    if (firstTypes[firstIndex] == secondTypes[secondIndex])
                    {
                        return firstTypes[firstIndex];
                    }
                }
            }

            return null;
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
