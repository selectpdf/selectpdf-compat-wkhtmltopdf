// From DinkToPdf (https://github.com/rdvojmoc/DinkToPdf), MIT License, Copyright (c) 2017 Rok Dvojmoc. Public API kept unchanged for compatibility.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DinkToPdf.Contracts;

namespace DinkToPdf
{
    public class HtmlToPdfDocument : IDocument
    {
        public List<ObjectSettings> Objects { get;  private set;}
        
        private GlobalSettings globalSettings = new GlobalSettings();

        public GlobalSettings GlobalSettings {
            get { return globalSettings; }
            set {
                globalSettings = value;
            }
        }

        public HtmlToPdfDocument()
        {
            Objects = new List<ObjectSettings>();
        }

        public IEnumerable<IObject> GetObjects()
        {
            return Objects.ToArray();
        }   
    }
}
