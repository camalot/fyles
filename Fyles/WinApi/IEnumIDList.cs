using System;
using System.Runtime.InteropServices;

namespace Fyles.WinApi
{
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F2-0000-0000-C000-000000000046")]
    public interface IEnumIDList
    {
        [PreserveSig()]
        Int32 Next(
            int celt,
            out IntPtr rgelt,
            out int pceltFetched);

        [PreserveSig()]
        Int32 Skip(
            int celt);

        [PreserveSig()]
        Int32 Reset();


        [PreserveSig()]
        Int32 Clone(
            out IEnumIDList ppenum);
    }
}
