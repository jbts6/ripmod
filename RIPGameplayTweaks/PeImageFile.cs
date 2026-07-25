using System;
using System.Collections.Generic;

internal sealed class PeImageFile
{
    private readonly IReadOnlyList<Section> _sections;

    private PeImageFile(IReadOnlyList<Section> sections)
    {
        _sections = sections;
    }

    public static PeImageFile Parse(byte[] image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        if (image.Length < 0x40 || ReadUInt16(image, 0) != 0x5A4D)
            throw new InvalidOperationException("Invalid DOS header.");

        int peOffset = ReadInt32(image, 0x3C);
        EnsureRange(image, peOffset, 24);
        if (ReadUInt32(image, peOffset) != 0x00004550)
            throw new InvalidOperationException("Invalid PE signature.");

        int sectionCount = ReadUInt16(image, peOffset + 6);
        int optionalHeaderSize = ReadUInt16(image, peOffset + 20);
        int sectionOffset = checked(peOffset + 24 + optionalHeaderSize);
        EnsureRange(image, sectionOffset, checked(sectionCount * 40));

        var sections = new List<Section>(sectionCount);
        for (int index = 0; index < sectionCount; index++)
        {
            int offset = sectionOffset + index * 40;
            sections.Add(new Section(
                ReadInt32(image, offset + 20),
                ReadInt32(image, offset + 16),
                ReadInt32(image, offset + 12)));
        }

        return new PeImageFile(sections);
    }

    public static PeImageFile ForRawImage(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        return new PeImageFile(new[] { new Section(0, length, 0) });
    }

    public int FileOffsetToRva(int fileOffset)
    {
        foreach (Section section in _sections)
        {
            if (fileOffset >= section.RawOffset &&
                fileOffset < checked(section.RawOffset + section.RawSize))
            {
                return checked(section.VirtualAddress + fileOffset - section.RawOffset);
            }
        }

        throw new InvalidOperationException("File offset is not contained in a PE section: " + fileOffset);
    }

    private static ushort ReadUInt16(byte[] image, int offset)
    {
        EnsureRange(image, offset, 2);
        return BitConverter.ToUInt16(image, offset);
    }

    private static uint ReadUInt32(byte[] image, int offset)
    {
        EnsureRange(image, offset, 4);
        return BitConverter.ToUInt32(image, offset);
    }

    private static int ReadInt32(byte[] image, int offset)
    {
        EnsureRange(image, offset, 4);
        return BitConverter.ToInt32(image, offset);
    }

    private static void EnsureRange(byte[] image, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > image.Length - length)
            throw new InvalidOperationException("PE structure is outside the image bounds.");
    }

    private readonly struct Section
    {
        public Section(int rawOffset, int rawSize, int virtualAddress)
        {
            RawOffset = rawOffset;
            RawSize = rawSize;
            VirtualAddress = virtualAddress;
        }

        public int RawOffset { get; }
        public int RawSize { get; }
        public int VirtualAddress { get; }
    }
}
