using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

internal sealed class WindowsNativePatchWriter : INativePatchWriter
{
    private const uint PageExecuteReadWrite = 0x40;
    private readonly IntPtr _moduleBase;

    private WindowsNativePatchWriter(IntPtr moduleBase)
    {
        _moduleBase = moduleBase;
    }

    public static WindowsNativePatchWriter ForLoadedModule(string moduleName)
    {
        IntPtr moduleBase = GetModuleHandle(moduleName);
        if (moduleBase == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Loaded module not found: " + moduleName);
        return new WindowsNativePatchWriter(moduleBase);
    }

    public byte[] Read(int rva, int length)
    {
        if (rva < 0)
            throw new ArgumentOutOfRangeException(nameof(rva));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var result = new byte[length];
        Marshal.Copy(IntPtr.Add(_moduleBase, rva), result, 0, length);
        return result;
    }

    public void Write(int rva, byte[] bytes)
    {
        if (rva < 0)
            throw new ArgumentOutOfRangeException(nameof(rva));
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Patch bytes cannot be empty.", nameof(bytes));

        IntPtr address = IntPtr.Add(_moduleBase, rva);
        if (!VirtualProtect(address, (UIntPtr)bytes.Length, PageExecuteReadWrite, out uint oldProtect))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualProtect enable-write failed.");

        IReadOnlyList<Exception> errors = WriteAndRestore(address, bytes, oldProtect);
        if (errors.Count == 1)
            throw errors[0];
        if (errors.Count > 1)
            throw new AggregateException(errors);

        if (!FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)bytes.Length))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "FlushInstructionCache failed.");
    }

    private static IReadOnlyList<Exception> WriteAndRestore(IntPtr address, byte[] bytes, uint oldProtect)
    {
        var errors = new List<Exception>();
        try
        {
            Marshal.Copy(bytes, 0, address, bytes.Length);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        if (!VirtualProtect(address, (UIntPtr)bytes.Length, oldProtect, out _))
        {
            errors.Add(new Win32Exception(
                Marshal.GetLastWin32Error(),
                "VirtualProtect restore failed."));
        }

        return errors;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(
        IntPtr address,
        UIntPtr size,
        uint newProtect,
        out uint oldProtect);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
}
