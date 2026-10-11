// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc. Public API kept unchanged for compatibility.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DinkToPdf.Contracts
{
    public interface IDocument : ISettings
    {
       IEnumerable<IObject> GetObjects();
    }
}
