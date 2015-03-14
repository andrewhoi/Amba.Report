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
using Amba.SpreadsheetLight;
using Newtonsoft.Json.Linq;

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
                ClearDefinedNames(doc);
                doc.SelectWorksheet(activeSheet);
                doc.SaveAs(outputFile);
            }
        }

        private void ClearDefinedNames(SLDocument doc)
        {
            foreach (var item in doc.GetDefinedNames())
            {
                doc.DeleteDefinedName(item.Name);
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

                SetDefinedNameValue(doc, namedRangeName, property.Value);

            }
        }

        private void SetDefinedNameValue(SLDocument doc, string namedRangeName, JToken property, int rowOffset = 0)
        {
            switch (property.Type)
            {
                case JTokenType.String:
                    doc.SetDefinedNameValue<string>(namedRangeName, property.Value<string>(), rowOffset, 0);
                    break;
                case JTokenType.Date:
                    doc.SetDefinedNameValue<DateTime>(namedRangeName, property.Value<DateTime>(), rowOffset, 0);
                    break;
                case JTokenType.Integer:
                    doc.SetDefinedNameValue<int>(namedRangeName, property.Value<int>(), rowOffset, 0);
                    break;
                case JTokenType.Float:
                    doc.SetDefinedNameValue<float>(namedRangeName, property.Value<float>(), rowOffset, 0);
                    break;
                case JTokenType.Boolean:
                    doc.SetDefinedNameValue<bool>(namedRangeName, property.Value<bool>(), rowOffset, 0);
                    break;
                default:
                    break;
            }
        }

        int? deleteFrom, deleteTo;
        // when first level array found
        private void FillArray(SLDocument doc, string propertyName, JArray jArray)
        {
            deleteFrom = deleteTo = null;
            var structure = new ArrayPrintStructure();
            FillPrintedStructure(structure, doc, propertyName, jArray);
            if (structure == null || !structure.Enabled) return;

            // TODO validate structure

            // TODO think about case when no header and have footer
            NormalizeStructure(structure);

            // print from the last row
            if (structure.HasFooter) nextRow = structure.Footer.RowEnd + 1;
            else nextRow = structure.Header.RowEnd + 1;

            deleteTo = nextRow - 1;
            PrintArraysWithStructure(doc, structure, propertyName, jArray);

            // TODO support separate pages
            doc.DeleteRow(deleteFrom.Value, deleteTo.Value - deleteFrom.Value + 1);
        }



        int nextRow = 0; // TODO separate row for every sheet
        private void PrintArraysWithStructure(SLDocument doc, ArrayPrintStructure structure, string propertyName, JArray jArray)
        {
            if (!structure.Enabled) return;
            foreach (var data in jArray)
            {
                if (structure.HasHeader)
                {
                    PrintRow(doc, structure.Header, propertyName, data as JObject, true);
                }
                if (structure.HasChildren)
                {

                }
                if (structure.HasFooter)
                {
                    PrintRow(doc, structure.Footer, propertyName, data as JObject, false);
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="templateInfo"></param>
        /// <param name="propertyName">row's property name</param>
        /// <param name="data">current row</param>
        /// <param name="isHeader"></param>
        private void PrintRow(SLDocument doc, TemplateInfo templateInfo, string propertyName, JObject data, bool isHeader)
        {
            if (data == null) return;
            doc.InsertRow(nextRow, templateInfo.RowCount);
            doc.CopyRow(templateInfo.RowStart, templateInfo.RowEnd, nextRow);
            nextRow += templateInfo.RowCount;
            if (!deleteFrom.HasValue) deleteFrom = templateInfo.RowStart;
            // Print all properties in current row
            foreach (var item in data)
            {
                var subProperty = propertyName + "." + item.Key;
                if (isHeader && item.Value.Type == JTokenType.Array)
                {
                    if (templateInfo.Parent.Children.ContainsKey(item.Key))
                    {
                        PrintArraysWithStructure(doc, templateInfo.Parent.Children[item.Key], subProperty, (JArray)item.Value);
                    }
                }
                else
                {
                    // print prop
                    int offset = nextRow - templateInfo.RowStart - 1;
                    if (doc.SelectWorksheet(templateInfo.SheetName))
                    {
                        // print only if property inside inserted row
                        int r1, r2;
                        SLDocument.WhatIsRowStartRowEnd(doc.GetDefinedNameText(subProperty), out r1, out r2);
                        if (r1 <= templateInfo.RowStart && templateInfo.RowEnd <= r2)
                        {
                            SetDefinedNameValue(doc, subProperty, item.Value, offset);
                        }
                    }
                }
            }
            // Replace values in brackets, e.g. {Name}
            
        }
        private void PrintValuesInBrackets(SLDocument doc, TemplateInfo templateInfo, string propertyName, JObject data)
        { 
            
        }

        private void NormalizeStructure(ArrayPrintStructure structure)
        {
            // TODO move header to footer if children started before header (case when no header, just footer).

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

            var temp = from r in jArray
                       select r;
            // look through all rows to find arrays
            foreach (var row in jArray)
            {
                // look throug all props
                foreach (var item in (JObject)row)
                {
                    if (item.Value.Type == JTokenType.Array)
                    {
                        // ! added before
                        if (!structure.Children.ContainsKey(item.Key))
                        {
                            var child = new ArrayPrintStructure();
                            var subProperty = propertyName + "." + item.Key;
                            FillPrintedStructure(child, doc, subProperty, (JArray)item.Value);
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
            Header = new TemplateInfo(this);
            Footer = new TemplateInfo(this);
            Enabled = false;
            Children = new Dictionary<string, ArrayPrintStructure>();
        }
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public TemplateInfo Header { get; private set; }
        public TemplateInfo Footer { get; private set; }


        public bool HasHeader { get { return Header.Enabled; } }
        public bool HasFooter { get { return Footer.Enabled; } }
        public bool HasChildren { get { return Children.Count > 0; } }
        public IDictionary<string, ArrayPrintStructure> Children { get; set; }

    }
    internal class TemplateInfo
    {
        public TemplateInfo(ArrayPrintStructure Parent)
        {
            this.Parent = Parent;
            Enabled = false;
            SheetName = _Range = String.Empty;
            RowStart = RowEnd = RowCount = 0;
        }

        public ArrayPrintStructure Parent { get; private set; }

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
