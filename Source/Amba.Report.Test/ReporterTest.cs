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

using Amba.SpreadsheetLight;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using Xunit;

namespace Amba.Report.Test
{
    public class ReporterTest
    {
        #region Test data

        dynamic jsonOrders = new
        {
            Date = new DateTime(2015, 3, 22),
            Company = new { Name = "Example company", Address = "Paris" },
            PrintedAt = new DateTime(2015, 3, 13, 21, 01, 02),
            Categories = new[]{
                new { 
                    Name = "Foods", 
                    Qnt = 100, 
                    Amount = 1000.00,
                    Products = new[]{
                        new { 
                            Name = "Apple",
                            Qnt = 40,
                            Amount = 400.0,
                            Orders = new[] {
                                new { Number = "2015-1", Customer = "USA Goverment", Qnt = 1, Amount = 10.0 },
                                new { Number = "2015-2", Customer = "John Hock", Qnt = 29, Amount = 290.0 },
                                new { Number = "2015-3", Customer = "Adam Smith", Qnt = 10, Amount = 100.0 },
                            }
                        },
                        new { 
                            Name = "Cherry",
                            Qnt = 60,
                            Amount = 600.0,
                            Orders = new[] {
                                new { Number = "2015-1", Customer = "USA Goverment", Qnt = 1, Amount = 599.99 },
                                new { Number = "2015-4", Customer = "Jeam Bean", Qnt = 59, Amount = 0.01 },
                            }
                        }
                    }
                } // Category Foods
            }, // Categories
        };


        #endregion

        [Fact]
        public void GetStructure()
        {
            // Arrange

            // creating report structure based on json and template
            JObject json = JObject.FromObject(jsonOrders);
            var tempFile = GetTempFileName();
            var reporter = new SpreadsheetLightJsonReporter(
                "Amba.Report.Test\\Templates\\Orders.xlsx",
                json,
                tempFile);

            // Act
            reporter.Execute();

            // Assert

            using (var doc = new SLDocument(tempFile))
            {
                Assert.Equal(new DateTime(2015, 03, 22), doc.GetCellValueAsDateTime("B2"));
                Assert.Equal("Example company", doc.GetCellValueAsString("B3"));
                Assert.Equal("Paris", doc.GetCellValueAsString("B4"));
                Assert.Equal("Foods", doc.GetCellValueAsString("A7"));
                Assert.Equal("Apple", doc.GetCellValueAsString("A8"));
                // order row 1
                Assert.Equal(1m, doc.GetCellValueAsDecimal("D9"));
                Assert.Equal(10m, doc.GetCellValueAsDecimal("E9"));
                // order row 2
                Assert.Equal(29m, doc.GetCellValueAsDecimal("D10"));
                Assert.Equal(290m, doc.GetCellValueAsDecimal("E10"));
                // order row 3
                Assert.Equal(10m, doc.GetCellValueAsDecimal("D11"));
                Assert.Equal(100m, doc.GetCellValueAsDecimal("E11"));
                // footer for apple
                Assert.Equal(40.0m, doc.GetCellValueAsDecimal("D12"));
                Assert.Equal(400.0m, doc.GetCellValueAsDecimal("E12"));
                // header for cheryy
                Assert.Equal("Cherry", doc.GetCellValueAsString("A13"));
                // order row 4
                Assert.Equal(1m, doc.GetCellValueAsDecimal("D14"));
                Assert.Equal(599.99m, Math.Round(doc.GetCellValueAsDecimal("E14"), 2));
                // order row 5
                Assert.Equal(59m, doc.GetCellValueAsDecimal("D15"));
                Assert.Equal(0.01m, Math.Round(doc.GetCellValueAsDecimal("E15"), 2));
                // footer for cherry
                Assert.Equal(60.0m, doc.GetCellValueAsDecimal("D16"));
                Assert.Equal(600.0m, doc.GetCellValueAsDecimal("E16"));
                // footer for Foods
                Assert.Equal(100.0m, doc.GetCellValueAsDecimal("D17"));
                Assert.Equal(1000.0m, doc.GetCellValueAsDecimal("E17"));
                // Report footer
                Assert.Equal(new DateTime(2015, 3, 13, 21, 01, 02), doc.GetCellValueAsDateTime("B19"));

            }
            //File.Delete(tempFile);
            //Assert.False(File.Exists(tempFile));
            Process.Start(tempFile);
        }


        private string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".xlsx";
        }

    }
}

