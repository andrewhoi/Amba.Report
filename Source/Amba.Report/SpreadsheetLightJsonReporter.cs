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
                DeleteRowsForArrays(doc, jsonData);
                ClearNamedRanges(doc);
                doc.SelectWorksheet(activeSheet);
                doc.SaveAs(outputFile);
            }
        }

        private void ClearNamedRanges(SLDocument doc)
        {
            foreach (var item in doc.GetDefinedNames(false))
            {
                doc.DeleteDefinedName(item.Name);
            }
        }

        private void DeleteRowsForArrays(SLDocument doc, JObject json, string prefix = "")
        {
            arrays = new HashSet<string>();
            GetAllArray(json);
            foreach (var item in arrays)
            {
                if (doc.HasDefinedName((string)item))
                {
                    var namedRangeText = doc.GetDefinedNameText((string)item);
                    var addrArray = namedRangeText.Split(',');
                    var address = addrArray[0]; // only first range
                    var sheet = address.Substring(0, address.IndexOf('!'));
                    if (!doc.SelectWorksheet(sheet))
                    {
                        continue;
                    }

                    var range = address.Substring(address.IndexOf('!') + 1).Replace("$", "");
                    var cells = range.Split(':');
                    var rowBegin = Convert.ToInt32(cells[0]);
                    var rowEnd = Convert.ToInt32(cells[1]);
                    var rowCount = rowEnd - rowBegin + 1;
                    doc.DeleteRow(rowBegin, rowCount);
                    doc.DeleteDefinedName((string)item);
                }
            }
        }
        private HashSet<string> arrays;
        private void GetAllArray(JObject json, string prefix = "")
        {
            foreach (var p in json)
            {
                if (p.Value.Type == JTokenType.Object)
                {
                    GetAllArray((JObject)p.Value);
                }
                if (p.Value.Type == JTokenType.Array)
                {

                    if (arrays.Contains(prefix)) continue;

                    arrays.Add(prefix + p.Key);
                    foreach (var row in (JArray)p.Value)
                    {
                        GetAllArray((JObject)row, prefix + p.Key + ".");
                    }
                }
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
                    currentRowToInsert = 0;
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

        private int currentRowToInsert;
        private void FillArray(SLDocument doc, string namedRangeName, JArray jArray)
        {
            var namedRangeText = doc.GetDefinedNameText(namedRangeName);
            if (string.IsNullOrWhiteSpace(namedRangeText)) return;
            // take only first range (must be like 'Sheet1!$1:$2')
            var addrArray = namedRangeText.Split(',');
            var address = addrArray[0];
            var sheet = address.Substring(0, address.IndexOf('!'));
            if (!doc.SelectWorksheet(sheet))
            {
                return;
            }
            var range = address.Substring(address.IndexOf('!') + 1).Replace("$", "");
            var cells = range.Split(':');
            var rowBegin = Convert.ToInt32(cells[0]);
            var rowEnd = Convert.ToInt32(cells[1]);
            var rowCount = rowEnd - rowBegin + 1;
            if (currentRowToInsert == 0)
            {
                currentRowToInsert = rowEnd + 1;
            }
            foreach (var row in jArray)
            {

                doc.InsertRow(currentRowToInsert, rowCount);
                // cause after inserting row move down, when row template below row of inserting
                var fixOffset = rowBegin >= currentRowToInsert ? rowCount : 0;
                rowBegin += fixOffset;
                rowEnd += fixOffset;
                doc.CopyRow(rowBegin, rowEnd, currentRowToInsert);
                var rowOffset = currentRowToInsert - rowBegin;

                currentRowToInsert += rowCount;
                if (row.GetType() == typeof(JObject))
                {
                    FillRangeWithData(doc, (JObject)row, namedRangeName + ".", rowOffset);
                }
            }

        }

        private void FillRangeWithData(SLDocument doc, JObject jObject, string prefix, int rowOffset)
        {
            foreach (var property in jObject)
            {
                var namedRangeName = prefix + property.Key;
                if (property.Value.Type == JTokenType.Array)
                {
                    // nested arrays
                    FillArray(doc, namedRangeName, (JArray)property.Value);
                    continue;
                }
                if (property.Value.Type == JTokenType.Object)
                {
                    FillRangeWithData(doc, (JObject)property.Value, namedRangeName + ".", rowOffset);
                    continue;
                }

                switch (property.Value.Type)
                {
                    case JTokenType.String:
                        doc.SetDefinedNameValue<string>(namedRangeName, property.Value.Value<string>(), rowOffset);
                        break;
                    case JTokenType.Date:
                        doc.SetDefinedNameValue<DateTime>(namedRangeName, property.Value.Value<DateTime>(), rowOffset);
                        break;
                    case JTokenType.Integer:
                        doc.SetDefinedNameValue<int>(namedRangeName, property.Value.Value<int>(), rowOffset);
                        break;
                    case JTokenType.Float:
                        doc.SetDefinedNameValue<float>(namedRangeName, property.Value.Value<float>(), rowOffset);
                        break;
                    case JTokenType.Boolean:
                        doc.SetDefinedNameValue<bool>(namedRangeName, property.Value.Value<bool>(), rowOffset);
                        break;
                    default:
                        break;
                }
            }
        }


        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
        }
    }
}
