// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc, in the WkHtmlToPdfDotNet namespace.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WkHtmlToPdfDotNet.Contracts;

namespace WkHtmlToPdfDotNet.EventDefinitions
{
    public class WarningArgs : EventArgs
    {
        public IDocument Document { get; set; }

        public string Message { get; set; }
    }
}
