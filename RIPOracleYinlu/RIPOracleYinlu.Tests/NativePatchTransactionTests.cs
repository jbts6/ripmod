using System;
using System.Collections.Generic;

internal static class NativePatchTransactionTests
{
    public static void RunAll()
    {
        PreflightFailureWritesNothing();
        MidWriteFailureRollsBackEverything();
        SuccessfulTransactionWritesEachReplacementOnce();
    }

    private static void PreflightFailureWritesNothing()
    {
        var writer = new MemoryPatchWriter();
        writer.Set(0x10, 0x03);
        writer.Set(0x20, 0xFF);

        TestAssert.Throws<InvalidOperationException>(
            () => NativePatchTransaction.ApplyAll(CreatePatches(), writer),
            "preflight mismatch fails transaction");

        TestAssert.Equal(0, writer.WriteAttempts, "preflight mismatch performs zero writes");
        TestAssert.Equal(0x03, writer.Get(0x10), "first value unchanged after preflight failure");
    }

    private static void MidWriteFailureRollsBackEverything()
    {
        var writer = CreateReadyWriter();
        writer.FailWriteAttempt = 3;

        TestAssert.Throws<InvalidOperationException>(
            () => NativePatchTransaction.ApplyAll(CreatePatches(), writer),
            "third write failure aborts transaction");

        TestAssert.Equal(0x03, writer.Get(0x10), "first replacement rolled back");
        TestAssert.Equal(0x03, writer.Get(0x20), "second replacement rolled back");
        TestAssert.Equal(0x03, writer.Get(0x30), "failed replacement remains original");
    }

    private static void SuccessfulTransactionWritesEachReplacementOnce()
    {
        var writer = CreateReadyWriter();
        int count = NativePatchTransaction.ApplyAll(CreatePatches(), writer);

        TestAssert.Equal(3, count, "transaction replacement count");
        TestAssert.Equal(3, writer.WriteAttempts, "each replacement written once");
        TestAssert.Equal(0x02, writer.Get(0x10), "first replacement committed");
        TestAssert.Equal(0x02, writer.Get(0x20), "second replacement committed");
        TestAssert.Equal(0x02, writer.Get(0x30), "third replacement committed");
    }

    private static MemoryPatchWriter CreateReadyWriter()
    {
        var writer = new MemoryPatchWriter();
        writer.Set(0x10, 0x03);
        writer.Set(0x20, 0x03);
        writer.Set(0x30, 0x03);
        return writer;
    }

    private static IReadOnlyList<PreparedBinaryPatch> CreatePatches()
    {
        return new[]
        {
            new PreparedBinaryPatch("first", new[] { Replacement(0x10) }),
            new PreparedBinaryPatch("second", new[] { Replacement(0x20), Replacement(0x30) })
        };
    }

    private static PreparedByteReplacement Replacement(int rva)
    {
        return new PreparedByteReplacement(
            fileOffset: rva,
            rva: rva,
            expectedBytes: new byte[] { 0x03 },
            replacementBytes: new byte[] { 0x02 });
    }

    private sealed class MemoryPatchWriter : INativePatchWriter
    {
        private readonly Dictionary<int, byte> _memory = new Dictionary<int, byte>();
        private bool _failureRaised;

        public int FailWriteAttempt { get; set; }
        public int WriteAttempts { get; private set; }

        public byte[] Read(int rva, int length)
        {
            var result = new byte[length];
            for (int index = 0; index < length; index++)
                result[index] = Get(rva + index);
            return result;
        }

        public void Write(int rva, byte[] bytes)
        {
            WriteAttempts++;
            if (!_failureRaised && WriteAttempts == FailWriteAttempt)
            {
                _failureRaised = true;
                throw new InvalidOperationException("injected write failure");
            }

            for (int index = 0; index < bytes.Length; index++)
                _memory[rva + index] = bytes[index];
        }

        public void Set(int rva, byte value)
        {
            _memory[rva] = value;
        }

        public byte Get(int rva)
        {
            return _memory.TryGetValue(rva, out byte value) ? value : (byte)0;
        }
    }
}
