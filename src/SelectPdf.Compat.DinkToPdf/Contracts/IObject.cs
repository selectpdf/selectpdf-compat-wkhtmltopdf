// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc. Public API kept unchanged for compatibility.
using System;
using System.Collections.Generic;
using System.Linq;

namespace DinkToPdf.Contracts
{
    public interface IObject : ISettings
    {
        byte[] GetContent();
    }
}
