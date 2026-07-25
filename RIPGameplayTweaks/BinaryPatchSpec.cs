using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal sealed class ByteReplacement
{
    public ByteReplacement(int offset, string expectedHex, string replacementHex)
    {
        Offset = offset;
        ExpectedBytes = ParseExactBytes(expectedHex);
        ReplacementBytes = ParseExactBytes(replacementHex);
        if (ExpectedBytes.Length == 0 || ExpectedBytes.Length != ReplacementBytes.Length)
            throw new ArgumentException("Expected and replacement byte lengths must be equal and non-zero.");
    }

    public int Offset { get; }
    public byte[] ExpectedBytes { get; }
    public byte[] ReplacementBytes { get; }

    private static byte[] ParseExactBytes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        return text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();
    }
}

internal sealed class BinaryPatchSpec
{
    private readonly BytePattern _pattern;

    public BinaryPatchSpec(string name, string pattern, params ByteReplacement[] replacements)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Patch name cannot be empty.", nameof(name))
            : name;
        _pattern = BytePattern.Parse(pattern);
        Replacements = replacements ?? throw new ArgumentNullException(nameof(replacements));
        if (Replacements.Count == 0)
            throw new ArgumentException("A patch must contain at least one replacement.", nameof(replacements));

        foreach (ByteReplacement replacement in Replacements)
            _pattern.ValidateFixedBytes(replacement.Offset, replacement.ExpectedBytes);
    }

    public string Name { get; }
    public IReadOnlyList<ByteReplacement> Replacements { get; }

    public PreparedBinaryPatch Prepare(byte[] image, PeImageFile pe)
    {
        if (pe == null)
            throw new ArgumentNullException(nameof(pe));

        IReadOnlyList<int> matches = _pattern.FindMatches(image);
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                Name + " expected exactly one signature match, found " + matches.Count + ".");
        }

        int matchOffset = matches[0];
        var prepared = new List<PreparedByteReplacement>(Replacements.Count);
        foreach (ByteReplacement replacement in Replacements)
        {
            int fileOffset = checked(matchOffset + replacement.Offset);
            prepared.Add(new PreparedByteReplacement(
                fileOffset,
                pe.FileOffsetToRva(fileOffset),
                replacement.ExpectedBytes,
                replacement.ReplacementBytes));
        }

        return new PreparedBinaryPatch(Name, prepared);
    }
}

internal sealed class PreparedBinaryPatch
{
    public PreparedBinaryPatch(string name, IReadOnlyList<PreparedByteReplacement> replacements)
    {
        Name = name;
        Replacements = replacements;
    }

    public string Name { get; }
    public IReadOnlyList<PreparedByteReplacement> Replacements { get; }
}

internal sealed class PreparedByteReplacement
{
    public PreparedByteReplacement(int fileOffset, int rva, byte[] expectedBytes, byte[] replacementBytes)
    {
        FileOffset = fileOffset;
        Rva = rva;
        ExpectedBytes = (byte[])expectedBytes.Clone();
        ReplacementBytes = (byte[])replacementBytes.Clone();
    }

    public int FileOffset { get; }
    public int Rva { get; }
    public byte[] ExpectedBytes { get; }
    public byte[] ReplacementBytes { get; }
}
