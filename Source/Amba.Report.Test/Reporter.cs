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

using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Amba.Report.Test
{
    public class Reporter
    {
        [Fact]
        public void Test1()
        {
            // Arrange
            var tempFile = GetTempFileName();
            var reporter = new SpreadsheetLightJsonReporter(
                "Amba.Report.Test\\Templates\\ReportTemplate1.xlsx",
                //jsonDataWithRows,
                jsonData1,
                tempFile);
            // Act
            reporter.Execute();
            // Assert
            Assert.True(File.Exists(tempFile));
            Process.Start(tempFile);
            // TODO: check final report, then delete it
            //File.Delete(tempFile);
        }
        private string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".xlsx";
        }

        private JObject jsonData1 = JObject.Parse(@"
{   'Num': '12345678-15',
    'Date': '2015-05-22T21:01:02',
    'Org': { 'Name': 'Customer\'s name here' },
    'Items': [
        { 'Item': 'Item #1', 'Qnt': 12.5, 'Price': 20.0 },
        { 'Item': 'Item #2', 'Qnt': 1, 'Price': 55.65 },
        { 'Item': 'Item #3', 'Qnt': 2.0, 'Price': 50.1 }
    ],
    'GroupRows': [{
        'Item' : 'Item\'s Group #1',
        'Details': [
            { 'Item': 'Item #1-1', 'Qnt': 11.5, 'Price': 21.0 },
            { 'Item': 'Item #1-2', 'Qnt': 10, 'Price': 45.99 },   
        ]},
        {
        'Item' : 'Item\'s Group #2',
        'Details': [
            { 'Item': 'Item #2-1', 'Qnt': 11.5, 'Price': 21.0 },
        ]}
    ]
}");
    }
}
    
