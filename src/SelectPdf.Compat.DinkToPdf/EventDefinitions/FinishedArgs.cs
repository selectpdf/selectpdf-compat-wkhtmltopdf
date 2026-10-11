// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc. Public API kept unchanged for compatibility.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DinkToPdf.Contracts;

namespace DinkToPdf.EventDefinitions
{
    public class FinishedArgs : EventArgs
    {
        public IDocument Document { get; set; }

        public bool Success { get; set; }
    }
}
