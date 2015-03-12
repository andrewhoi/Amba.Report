using Amba.SpreadsheetLight;
using Newtonsoft.Json.Linq;
// Copyright 2015, Vladimir Kuznetsov. All rights reserved.
//
// This file is part of "Amba.Report" library.
// 
// "Amba.Report" library is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// "Amba.Report" library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with "Amba.Report" library. If not, see <http://www.gnu.org/licenses/>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amba.Report
{
    /// <summary>
    /// Generate Excel files based on SpreadsheetLigth library and json-data
    /// </summary>
    public class SpreadsheetLightJsonReporter : IDisposable
    {
        private readonly string templateName;
        private readonly JObject jsonData;
        private readonly string outputFile;
        /// <summary>
        /// Constructor of the class
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="jsonData"></param>
        /// <param name="outputFile"></param>
        public SpreadsheetLightJsonReporter(string templateName, JObject jsonData, string outputFile)
        {
            this.templateName = templateName;
            this.jsonData = jsonData;
            this.outputFile = outputFile;
        }
        /// <summary>
        /// Generated file
        /// </summary>
        public string OutputFile { get { return outputFile; } }
        /// <summary>
        /// Execute report generation
        /// </summary>
        public void Execute()
        {
            using (var doc = new SLDocument(templateName))
            {
                var activeSheet = doc.GetCurrentWorksheetName();

                FillReportWithData(doc, jsonData);

                doc.SelectWorksheet(activeSheet);
                doc.SaveAs(outputFile);
            }
        }


        private void FillReportWithData(SLDocument doc, JObject json, string prefix = "")
        {
            foreach (var property in json)
            {
                var namedRangeName = prefix + property.Key;
                if (property.Value.Type == JTokenType.Array)
                {
                    // Special method
                    FillArray(doc, namedRangeName, (JArray)property.Value);
                    continue;
                }
                if (property.Value.Type == JTokenType.Object)
                {
                    FillReportWithData(doc, (JObject)property.Value, namedRangeName + ".");
                    continue;
                }

                switch (property.Value.Type)
                {
                    case JTokenType.String:
                        doc.SetDefinedNameValue<string>(namedRangeName, property.Value.Value<string>());
                        break;
                    case JTokenType.Date:
                        doc.SetDefinedNameValue<DateTime>(namedRangeName, property.Value.Value<DateTime>());
                        break;
                    case JTokenType.Integer:
                        doc.SetDefinedNameValue<int>(namedRangeName, property.Value.Value<int>());
                        break;
                    case JTokenType.Float:
                        doc.SetDefinedNameValue<float>(namedRangeName, property.Value.Value<float>());
                        break;
                    case JTokenType.Boolean:
                        doc.SetDefinedNameValue<bool>(namedRangeName, property.Value.Value<bool>());
                        break;
                    default:
                        break;
                }
            }
        }
        // when first level array found
        private void FillArray(SLDocument doc, string propertyName, JArray jArray)
        {
            var structure = new ArrayPrintStructure();
            FillPrintedStructure(structure, doc, propertyName, jArray);
            if (structure == null) return;

            // TODO validate structure

        }

        private ArrayPrintStructure FillPrintedStructure(ArrayPrintStructure structure, SLDocument doc, string propertyName, JArray jArray)
        {
            if (!doc.HasDefinedName(propertyName)) return structure;


            var ranges = doc.GetDefinedNameText(propertyName).Split(',');
            if (!(1 <= ranges.Length && ranges.Length <= 2))
            {
                return structure; // TODO: add to log: ranges more than 2, we don't know what todo, add log to new sheet
            }
            structure.Enabled = true;
            structure.Path = propertyName;
            // Header
            structure.Header.Enabled = true;
            structure.Header.Range = ranges[0];
            if (ranges.Length == 2)
            {
                structure.Footer.Enabled = true;
                structure.Footer.Range = ranges[1];
            }

            foreach (var row in jArray)
            {
                foreach (var item in (JObject)row)
                {
                    if (item.Value.Type == JTokenType.Array)
                    {
                        if (!structure.Children.ContainsKey(item.Key))
                        {
                            var child = new ArrayPrintStructure();
                            FillPrintedStructure(child, doc, propertyName + "." + item.Key, (JArray)item.Value);
                            structure.Children.Add(item.Key, child);
                        }
                    }
                }
            }
            return structure;
        }


        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
        }
    }

    internal class ArrayPrintStructure
    {
        public ArrayPrintStructure()
        {
            Header = new TemplateInfo();
            Footer = new TemplateInfo();
            Enabled = false;
            Children = new Dictionary<string, ArrayPrintStructure>();
        }
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public TemplateInfo Header { get; private set; }
        public TemplateInfo Footer { get; private set; }

        public IDictionary<string, ArrayPrintStructure> Children { get; set; }
    }
    internal class TemplateInfo
    {
        public TemplateInfo()
        {
            Enabled = false;
            SheetName = _Range = String.Empty;
            RowStart = RowEnd = RowCount = 0;
        }
        public bool Enabled { get; set; }
        private string _Range;

        public string Range
        {
            get
            {
                return _Range;
            }
            set
            {
                if (_Range.Equals(value)) return;
                _Range = value;
                int r1, r2;
                if (SLDocument.WhatIsRowStartRowEnd(_Range, out r1, out r2))
                {
                    RowStart = r1;
                    RowEnd = r2;
                    RowCount = r2 - r1 + 1;
                    SheetName = _Range.Substring(0, _Range.IndexOf('!'));
                }
                else
                {
                    RowCount = 0;
                    SheetName = String.Empty;
                }
            }
        }
        public string SheetName { get; private set; }
        public int RowStart { get; private set; }
        public int RowEnd { get; private set; }
        public int RowCount { get; private set; }

        public override string ToString()
        {
            return Range;
        }
    }
}
