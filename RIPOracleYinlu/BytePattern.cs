using System;
using System.Collections.Generic;
using System.Globalization;

internal sealed class BytePattern
{
    private readonly byte[] _values;
    private readonly bool[] _fixed;
    private readonly int _anchorStart;
    private readonly int _anchorLength;

    private BytePattern(byte[] values, bool[] fixedBytes)
    {
        _values = values;
        _fixed = fixedBytes;
        FindLongestFixedRun(fixedBytes, out _anchorStart, out _anchorLength);
        if (_anchorLength == 0)
            throw new ArgumentException("Byte pattern must contain at least one fixed byte.");
    }

    public int Length => _values.Length;

    public static BytePattern Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Byte pattern cannot be empty.", nameof(text));

        string[] tokens = text.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        var values = new byte[tokens.Length];
        var fixedBytes = new bool[tokens.Length];

        for (int index = 0; index < tokens.Length; index++)
        {
            if (tokens[index] == "??")
                continue;

            if (tokens[index].Length != 2 ||
                !byte.TryParse(tokens[index], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out values[index]))
            {
                throw new FormatException("Invalid byte pattern token: " + tokens[index]);
            }

            fixedBytes[index] = true;
        }

        return new BytePattern(values, fixedBytes);
    }

    public IReadOnlyList<int> FindMatches(byte[] image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        var matches = new List<int>();
        if (image.Length < Length)
            return matches;

        var imageSpan = new ReadOnlySpan<byte>(image);
        var anchor = new ReadOnlySpan<byte>(_values, _anchorStart, _anchorLength);
        int searchFrom = 0;
        while (searchFrom <= image.Length - _anchorLength)
        {
            int relativeOffset = imageSpan.Slice(searchFrom).IndexOf(anchor);
            if (relativeOffset < 0)
                break;

            int anchorOffset = searchFrom + relativeOffset;
            int candidateOffset = anchorOffset - _anchorStart;
            if (candidateOffset >= 0 &&
                candidateOffset <= image.Length - Length &&
                MatchesAt(image, candidateOffset))
            {
                matches.Add(candidateOffset);
            }

            searchFrom = anchorOffset + 1;
        }

        return matches;
    }

    public void ValidateFixedBytes(int offset, IReadOnlyList<byte> expected)
    {
        if (expected == null)
            throw new ArgumentNullException(nameof(expected));
        if (offset < 0 || offset + expected.Count > Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        for (int index = 0; index < expected.Count; index++)
        {
            int patternIndex = offset + index;
            if (!_fixed[patternIndex])
                throw new ArgumentException("Replacement cannot target wildcard pattern bytes.");
            if (_values[patternIndex] != expected[index])
                throw new ArgumentException("Replacement expected bytes do not match the pattern.");
        }
    }

    private bool MatchesAt(byte[] image, int offset)
    {
        for (int index = 0; index < Length; index++)
        {
            if (_fixed[index] && image[offset + index] != _values[index])
                return false;
        }

        return true;
    }

    private static void FindLongestFixedRun(bool[] fixedBytes, out int bestStart, out int bestLength)
    {
        bestStart = 0;
        bestLength = 0;
        int currentStart = 0;
        int currentLength = 0;

        for (int index = 0; index <= fixedBytes.Length; index++)
        {
            if (index < fixedBytes.Length && fixedBytes[index])
            {
                if (currentLength == 0)
                    currentStart = index;
                currentLength++;
                continue;
            }

            if (currentLength > bestLength)
            {
                bestStart = currentStart;
                bestLength = currentLength;
            }

            currentLength = 0;
        }
    }
}
