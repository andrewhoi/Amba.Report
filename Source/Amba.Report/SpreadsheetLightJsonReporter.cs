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

                //FillReportWithData(doc, jsonData);
                //DeleteRowsForArrays(doc, jsonData);
                //ClearNamedRanges(doc);

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

        private void FillArray(SLDocument doc, string namedRangeName, JArray jArray)
        {
           
        }
        
        private PrintStructure GetPrintedStructure(SLDocument doc, JObject json)
        {
            var result = new PrintStructure();

            return result;
        }


        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
        }
    }

    internal class PrintStructure
    {
        public TemplateInfo Header { get; set; }
        public TemplateInfo[] Rows { get; set; }
        public TemplateInfo Footer { get; set; }
    }
    internal class TemplateInfo
    {
        public string Name { get; set; }
        public string Range { get; set; }
        public string PathToData { get; set; }
    }
}
