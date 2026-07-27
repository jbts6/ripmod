using System;
using System.Collections.Generic;

internal static class NativePatchTransaction
{
    public static int ApplyAll(
        IReadOnlyList<PreparedBinaryPatch> patches,
        INativePatchWriter writer)
    {
        if (patches == null)
            throw new ArgumentNullException(nameof(patches));
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        List<PatchEntry> entries = FlattenAndValidate(patches);
        Preflight(entries, writer);

        var applied = new List<PatchEntry>(entries.Count);
        try
        {
            foreach (PatchEntry entry in entries)
            {
                applied.Add(entry);
                writer.Write(entry.Replacement.Rva, entry.Replacement.ReplacementBytes);
                VerifyWritten(entry, writer);
            }

            return applied.Count;
        }
        catch (Exception exception)
        {
            IReadOnlyList<string> rollbackErrors = RollBack(applied, writer);
            string suffix = rollbackErrors.Count == 0
                ? " Rollback completed."
                : " Rollback errors: " + string.Join(" | ", rollbackErrors);
            throw new InvalidOperationException("Native patch transaction failed." + suffix, exception);
        }
    }

    private static List<PatchEntry> FlattenAndValidate(IReadOnlyList<PreparedBinaryPatch> patches)
    {
        var entries = new List<PatchEntry>();
        var occupiedRvas = new HashSet<int>();
        foreach (PreparedBinaryPatch patch in patches)
        {
            foreach (PreparedByteReplacement replacement in patch.Replacements)
            {
                for (int index = 0; index < replacement.ExpectedBytes.Length; index++)
                {
                    int rva = checked(replacement.Rva + index);
                    if (!occupiedRvas.Add(rva))
                        throw new InvalidOperationException("Overlapping native patch at RVA 0x" + rva.ToString("X") + ".");
                }

                entries.Add(new PatchEntry(patch.Name, replacement));
            }
        }

        return entries;
    }

    private static void Preflight(IReadOnlyList<PatchEntry> entries, INativePatchWriter writer)
    {
        foreach (PatchEntry entry in entries)
        {
            byte[] actual = writer.Read(entry.Replacement.Rva, entry.Replacement.ExpectedBytes.Length);
            if (!BytesEqual(actual, entry.Replacement.ExpectedBytes))
            {
                throw new InvalidOperationException(
                    entry.Name + " runtime bytes do not match at RVA 0x" +
                    entry.Replacement.Rva.ToString("X") + ".");
            }
        }
    }

    private static void VerifyWritten(PatchEntry entry, INativePatchWriter writer)
    {
        byte[] actual = writer.Read(entry.Replacement.Rva, entry.Replacement.ReplacementBytes.Length);
        if (!BytesEqual(actual, entry.Replacement.ReplacementBytes))
            throw new InvalidOperationException(entry.Name + " did not verify after write.");
    }

    private static IReadOnlyList<string> RollBack(IReadOnlyList<PatchEntry> applied, INativePatchWriter writer)
    {
        var errors = new List<string>();
        for (int index = applied.Count - 1; index >= 0; index--)
        {
            PatchEntry entry = applied[index];
            try
            {
                writer.Write(entry.Replacement.Rva, entry.Replacement.ExpectedBytes);
            }
            catch (Exception exception)
            {
                errors.Add(entry.Name + ": " + exception.Message);
            }
        }

        return errors;
    }

    private static bool BytesEqual(IReadOnlyList<byte> left, IReadOnlyList<byte> right)
    {
        if (left.Count != right.Count)
            return false;
        for (int index = 0; index < left.Count; index++)
        {
            if (left[index] != right[index])
                return false;
        }

        return true;
    }

    private sealed class PatchEntry
    {
        public PatchEntry(string name, PreparedByteReplacement replacement)
        {
            Name = name;
            Replacement = replacement;
        }

        public string Name { get; }
        public PreparedByteReplacement Replacement { get; }
    }
}
