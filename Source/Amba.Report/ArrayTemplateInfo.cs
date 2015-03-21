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

namespace Amba.Report
{
    using Amba.SpreadsheetLight;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ArrayTemplate
    {
        public string PropertyName { get; set; }
        public int Group { get; set; }
        public RowRange RowRange { get; set; }
        public bool IsHeader { get; set; }
        public string SheetName { get { return RowRange.SheetName; } }
        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3}", Group, RowRange, IsHeader, PropertyName);
        }
    }

    public class NextRowInfo
    {
        public string SheetName { get; set; }
        public int Group { get; set; }
        public int NextRow { get; set; }
    }

    public class RowRange
    {
        public RowRange(string range)
        {
            this.Range = range;
        }

        private string _Range = String.Empty;

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
                    SheetName = _Range.Substring(0, _Range.IndexOf('!'));
                }
                else
                {
                    RowStart = RowEnd = 0;
                    SheetName = String.Empty;
                }
            }
        }

        public string SheetName { get; private set; }
        public int RowStart { get; set; }
        public int RowEnd { get; set; }
        public int RowCount { get { return RowEnd - RowStart + 1; } }

        public override string ToString()
        {
            return String.Format("{0}, rows {1}:{2}", Range, RowStart, RowEnd);
        }
    }
}
