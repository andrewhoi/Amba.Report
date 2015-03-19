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
using System.Text.RegularExpressions;

namespace Amba.Report
{
    /// <summary>
    /// Generate Excel files based on SpreadsheetLigth library and json-data
    /// </summary>
    public class SpreadsheetLightJsonReporter : IDisposable
    {
        #region ================================== Fields ======================================
        SLDocument doc;
        private readonly string templateName;
        private readonly JObject jsonData;
        private readonly string outputFile;
        Regex regexReplace = new Regex(@"(?<=\{)(?<!\{{2})[^{}]*(?=\})(?=\})(?!\}{2})");
        /// <summary>
        /// key = property, sheetName
        /// </summary>
        List<ArrayTemplate> ArrayStructure;
        // Tuple<string, int> sheet+group 
        List<NextRowInfo> NextRow;
        #endregion


        #region ================================== Constructors ================================

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

        #endregion


        #region  ================================== Properties ==================================

        /// <summary>
        /// Generated file
        /// </summary>
        public string OutputFile { get { return outputFile; } }

        #endregion


        #region ================================== Public methods ==============================


        /// <summary>
        /// Execute report generation
        /// </summary>
        /// 
        public void Execute()
        {
            ArrayStructure = new List<ArrayTemplate>();
            using (doc = new SLDocument(templateName))
            {
                // save current worksheet
                var activeSheet = doc.GetCurrentWorksheetName();
                FillReportWithData(jsonData);
                ClearDefinedNames();
                // restore current worksheet
                doc.SelectWorksheet(activeSheet);
                doc.SaveAs(outputFile);
            }
            doc = null;
        }


        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
        }


        #endregion


        #region ================================== Private methods =============================



        /// <summary>
        /// Main method to fill report
        /// </summary>
        /// <param name="json"></param>
        /// <param name="prefix"></param>
        private void FillReportWithData(JObject json, string prefix = "")
        {
            foreach (var property in json)
            {
                var namedRangeName = prefix + property.Key;
                if (property.Value.Type == JTokenType.Array)
                {
                    // Special method for array
                    FillArray(namedRangeName, (JArray)property.Value);
                    continue;
                }
                if (property.Value.Type == JTokenType.Object)
                {
                    FillReportWithData((JObject)property.Value, namedRangeName + ".");
                    continue;
                }

                SetDefinedNameValue(namedRangeName, property.Value);
            }
            if (String.IsNullOrWhiteSpace(prefix))
            {
                ReplaceInBrackets(json);
            }
        }


        // when first level array found
        private void FillArray(string propertyName, JArray jArray)
        {
            BuildArrayStructure(propertyName, jArray);
            DivideToGroupsArrayStructure();
            InitNextRows();


            PrintArraysWithStructure(propertyName, jArray);

        }


        private void InitNextRows()
        {
            NextRow = new List<NextRowInfo>();
            ArrayStructure
                .GroupBy(g => new { Sheet = g.RowRange.SheetName, Group = g.Group })
                .Select(s => new { Group = s.Key, EndRow = s.Max(row => row.RowRange.RowEnd) })
                .ToList()
                .ForEach(a => NextRow.Add(new NextRowInfo
                {
                    SheetName = a.Group.Sheet,
                    Group = a.Group.Group,
                    NextRow = a.EndRow + 1
                }));
        }


        // Separate groups mean separate areas to print arrays
        private void DivideToGroupsArrayStructure()
        {
            foreach (var sheet in doc.GetSheetNames())
            {
                var list = ArrayStructure
                    .Where(s => s.RowRange.SheetName.Equals(sheet, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(o => o.RowRange.RowStart)
                    .ToList();
                if (list.Count == 0) continue;

                ArrayTemplate previous = null;
                int group = 1;
                foreach (var rng in list)
                {

                    if (previous != null)
                    {
                        if (previous.RowRange.RowEnd + 1 != rng.RowRange.RowStart)
                        {
                            group++;
                        }
                        if (rng.PropertyName.StartsWith(previous.PropertyName) && previous.Group == group)
                        {
                            previous.IsHeader = true;
                        }
                    }
                    rng.Group = group;
                    previous = rng;
                }
            }
        }


        private void PrintArraysWithStructure(string propertyName, JArray jArray)
        {
            var templateArray = ArrayStructure.Where(s => s.PropertyName.Equals(propertyName)).OrderBy(o => o.RowRange.RowStart).ToList();
            var sheets = templateArray.Select(s => s.RowRange.SheetName).Distinct();
            if (templateArray.Count == 0) return;
            foreach (var data in jArray)
            {
                foreach (var sheet in sheets)
                {
                    if (!doc.SelectWorksheet(sheet)) continue;

                    foreach (var template in templateArray.Where(t => t.RowRange.SheetName.Equals(sheet, StringComparison.OrdinalIgnoreCase)))
                    {
                        PrintRow(template, propertyName, data as JObject);
                    }
                }
            }

        }


        // print single row on single sheet
        private void PrintRow(ArrayTemplate template, string propertyName, JObject data)
        {
            if (data == null) return;
            var nextRow = NextRow
                .Where(s => s.SheetName.Equals(template.SheetName, StringComparison.OrdinalIgnoreCase)
                    && s.Group == template.Group)
                .Select(i => i.NextRow).FirstOrDefault();
            doc.InsertRow(nextRow, template.RowRange.RowCount);
            doc.CopyRow(template.RowRange.RowStart, template.RowRange.RowEnd, nextRow);
            IncrementNextRow(template, nextRow);
            // Print all properties in current row
            foreach (var item in data)
            {
                var subProperty = propertyName + "." + item.Key;
                if (template.IsHeader && item.Value.Type == JTokenType.Array)
                {
                    PrintArraysWithStructure(subProperty, (JArray)item.Value);
                }
                else
                {
                    // print prop
                    int offset = nextRow - template.RowRange.RowStart;
                    var ranges = doc.GetDefinedNameText(subProperty).Split(',');
                    foreach (var rng in ranges)
                    {
                        if (rng.StartsWith(template.SheetName))
                        {
                            // print only if property inside inserted row
                            RowRange rowRange = new RowRange(rng);
                            if (rowRange.RowStart <= template.RowRange.RowStart
                                && template.RowRange.RowEnd <= rowRange.RowEnd)
                            {
                                var refer = rng.Substring(rng.IndexOf('!') + 1).Replace("$", "");
                                int row, col;
                                if (SLDocument.WhatIsRowColumnIndex(refer, out row, out col))
                                {
                                    SetCellValue(row, col, item.Value, offset);
                                }
                            }
                        }
                    }
                }
            } // foreach (var item in data)

            // Replace values in curly braces, e.g. {Name}
            int rowStart = nextRow;
            int rowEnd = nextRow + template.RowRange.RowCount - 1;
            var cells = doc.GetCells().Where(c => c.Key.RowIndex >= rowStart && c.Key.RowIndex <= rowEnd);

            foreach (var cell in cells)
            {
                var cellValue = doc.GetCellValueAsString(cell.Key.RowIndex, cell.Key.ColumnIndex);
                var matches = regexReplace.Matches(cellValue);
                if (matches.Count > 0)
                {
                    ReplaceValueInBrackets(cell.Key.RowIndex, cell.Key.ColumnIndex, cellValue, data, matches);
                }
            }
        }


        private void IncrementNextRow(ArrayTemplate template, int fromRow)
        {
            NextRow
                .Where(n => n.SheetName.Equals(template.SheetName) && n.NextRow >= fromRow)
                .ToList()
                .ForEach(n =>
                {
                    n.NextRow += template.RowRange.RowCount;

                });
            ArrayStructure
                .Where(s => s.RowRange.SheetName.Equals(template.SheetName) && s.RowRange.RowStart >= fromRow)
                .ToList()
                .ForEach(e =>
                    {
                        e.RowRange.RowStart += template.RowRange.RowCount;
                        e.RowRange.RowEnd += template.RowRange.RowCount;
                    });
        }


        private void BuildArrayStructure(string propertyName, JArray jArray)
        {
            if (!doc.HasDefinedName(propertyName)) return;

            var ranges = doc.GetDefinedNameText(propertyName).Split(',');
            foreach (var rng in ranges)
            {
                var rowRange = new RowRange(rng);
                if (!ArrayStructure.Any(p => p.PropertyName.Equals(propertyName)
                    && p.RowRange.SheetName.Equals(rowRange.SheetName)
                    && p.RowRange.RowStart == rowRange.RowStart))
                {
                    ArrayStructure.Add(new ArrayTemplate
                    {
                        PropertyName = propertyName,
                        RowRange = rowRange
                    });
                }
            }
            // find nested arrays
            // look through all rows to find arrays
            foreach (var row in jArray)
            {
                // look throug all props
                foreach (var item in (JObject)row)
                {
                    if (item.Value.Type == JTokenType.Array)
                    {
                        var subProperty = propertyName + "." + item.Key;
                        BuildArrayStructure(subProperty, (JArray)item.Value);
                    }
                }
            }
        }


        private void ReplaceInBrackets(JObject json)
        {
            // 1. Find all cells with string in bracket excluding entire rows in DefinedNames (that is for arrays)
            //var statList = new Dictionary<string, SLWorksheetStatistics>();
            var sheets = doc.GetSheetNames();
            foreach (var sheet in sheets)
            {
                if (doc.SelectWorksheet(sheet))
                {
                    //var cells = doc.GetCells().Where(c => c.Value.DataType.ToString()=="1"); // Need reference to OpenXml.dll to filter by DataType
                    var cells = doc.GetCells();

                    foreach (var cell in cells)
                    {
                        var cellValue = doc.GetCellValueAsString(cell.Key.RowIndex, cell.Key.ColumnIndex);
                        var matches = regexReplace.Matches(cellValue);
                        if (matches.Count > 0)
                        {
                            ReplaceValueInBrackets(cell.Key.RowIndex, cell.Key.ColumnIndex, cellValue, json, matches);
                        }
                    }
                }
            }
        }


        private void ReplaceValueInBrackets(int row, int column, string cellValue, JObject json, MatchCollection matches)
        {
            object[] param = new object[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {

                string propName = matches[i].ToString();
                string formattedPropName;
                var propArr = propName.Split(':');

                formattedPropName = String.Format(@"{{{0}}}", propName);

                JToken token = TryGetJsonValue(json, propArr[0]);
                if (token != null)
                {
                    // TODO replacement must use regex instead of this strings
                    if (propArr.Length == 2)
                    {
                        cellValue = cellValue.Replace(formattedPropName, String.Format(@"{{{0}:{1}}}", i, propArr[1]));
                    }
                    else
                    {
                        cellValue = cellValue.Replace(formattedPropName, String.Format(@"{{{0}}}", i));
                    }
                    param[i] = token.Value<object>();
                }
                else
                {
                    // remove the value
                    cellValue = cellValue.Replace(formattedPropName, "");
                }
            }
            cellValue = String.Format(cellValue, param);
            doc.SetCellValue(row, column, cellValue);
        }


        // clear names on end
        private void ClearDefinedNames()
        {
            foreach (var item in ArrayStructure.OrderByDescending(o => o.RowRange.RowStart))
            {
                if (doc.SelectWorksheet(item.SheetName))
                {
                    doc.DeleteRow(item.RowRange.RowStart, item.RowRange.RowCount);
                }
            }
            foreach (var item in doc.GetDefinedNames())
            {
                doc.DeleteDefinedName(item.Name);
            }
        }


        private void SetDefinedNameValue(string namedRangeName, JToken property, int rowOffset = 0)
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


        private void SetCellValue(int rowIndex, int columnIndex, JToken property, int rowOffset = 0)
        {
            switch (property.Type)
            {
                case JTokenType.String:
                    doc.SetCellValue(rowIndex + rowOffset, columnIndex, property.Value<string>());
                    break;
                case JTokenType.Date:
                    doc.SetCellValue(rowIndex + rowOffset, columnIndex, property.Value<DateTime>());
                    break;
                case JTokenType.Integer:
                    doc.SetCellValue(rowIndex + rowOffset, columnIndex, property.Value<int>());
                    break;
                case JTokenType.Float:
                    doc.SetCellValue(rowIndex + rowOffset, columnIndex, property.Value<float>());
                    break;
                case JTokenType.Boolean:
                    doc.SetCellValue(rowIndex + rowOffset, columnIndex, property.Value<bool>());
                    break;
                default:
                    break;
            }
        }


        #endregion


        #region ================================== Private functions ===========================


        private bool IsEntireRow(string range)
        {
            range = range.Replace("$", "").Substring(range.IndexOf('!') + 1);
            var removedLetters = new string(range.Where(ch => !Char.IsLetter(ch)).ToArray());
            return range.Equals(removedLetters);
        }


        private JToken TryGetJsonValue(JObject json, string path)
        {
            if (json == null) return null;
            JToken token;
            var ar = path.Split('.');
            if (ar.Length == 1)
            {
                if (json.TryGetValue(path, out token))
                {
                    return token;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (json.TryGetValue(ar[0], out token))
                {
                    JObject jobj = token as JObject;
                    if (jobj != null)
                    {
                        return TryGetJsonValue(jobj, path.Replace(ar[0] + ".", ""));
                    }
                    else return null;
                }
                else
                {
                    return null;
                }
            }
        }


        #endregion

    }
}

//private void PrintRow(SLDocument doc, TemplateInfo templateInfo, string propertyName, JObject data, bool isHeader)
//{
//    if (data == null) return;
//    doc.InsertRow(nextRow, templateInfo.RowCount);
//    doc.CopyRow(templateInfo.RowStart, templateInfo.RowEnd, nextRow);
//    nextRow += templateInfo.RowCount;
//    if (!deleteFrom.HasValue) deleteFrom = templateInfo.RowStart;
//    // Print all properties in current row
//    foreach (var item in data)
//    {
//        var subProperty = propertyName + "." + item.Key;
//        if (isHeader && item.Value.Type == JTokenType.Array)
//        {
//            if (templateInfo.Parent.Children.ContainsKey(item.Key))
//            {
//                PrintArraysWithStructure(doc, templateInfo.Parent.Children[item.Key], subProperty, (JArray)item.Value);
//            }
//        }
//        else
//        {
//            // print prop
//            int offset = nextRow - templateInfo.RowStart - 1;
//            if (doc.SelectWorksheet(templateInfo.SheetName))
//            {
//                // print only if property inside inserted row
//                int r1, r2;
//                SLDocument.WhatIsRowStartRowEnd(doc.GetDefinedNameText(subProperty), out r1, out r2);
//                if (r1 <= templateInfo.RowStart && templateInfo.RowEnd <= r2)
//                {
//                    SetDefinedNameValue(doc, subProperty, item.Value, offset);
//                }
//            }
//        }
//    }
//    // Replace values in brackets, e.g. {Name}
//    int rowStart = nextRow - templateInfo.RowCount;
//    int rowEnd = nextRow - 1;
//    var cells = doc.GetCells().Where(c => c.Key.RowIndex >= rowStart && c.Key.RowIndex <= rowEnd);

//    foreach (var cell in cells)
//    {
//        var cellValue = doc.GetCellValueAsString(cell.Key.RowIndex, cell.Key.ColumnIndex);
//        var matches = regexReplace.Matches(cellValue);
//        if (matches.Count > 0)
//        {
//            ReplaceValueInBrackets(doc, cell.Key.RowIndex, cell.Key.ColumnIndex, cellValue, data, matches);
//        }
//    }
//}