// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc, in the WkHtmlToPdfDotNet namespace.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WkHtmlToPdfDotNet.EventDefinitions;

namespace WkHtmlToPdfDotNet.Contracts
{
    public interface IConverter : IDisposable
    {

        /// <summary>
        ///  Converts document based on given settings
        /// </summary>
        /// <param name="document">Document to convert</param>
        /// <returns>Returns converted document in bytes</returns>
        byte[] Convert(IDocument document);

        event EventHandler<PhaseChangedArgs> PhaseChanged;

        event EventHandler<ProgressChangedArgs> ProgressChanged;

        event EventHandler<FinishedArgs> Finished;

        event EventHandler<ErrorArgs> Error;

        event EventHandler<WarningArgs> Warning;

    }
}
