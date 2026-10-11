// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc, in the WkHtmlToPdfDotNet namespace.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WkHtmlToPdfDotNet
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VoidCallback(IntPtr converter);
}
