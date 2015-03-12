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

        dynamic jsonOrders = new {
            Date = new DateTime(2015,3,22),
            Company = new { Name = "Example company", Address = "Paris" },
            PrintedAt = DateTime.Now,
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
            // creating report structure based on json and template
            JObject json = JObject.FromObject(jsonOrders);
            var tempFile = GetTempFileName();
            var reporter = new SpreadsheetLightJsonReporter(
                "Amba.Report.Test\\Templates\\Orders.xlsx",
                json,
                tempFile);

            reporter.Execute();

            Process.Start(tempFile);
        }


        private string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".xlsx";
        }

    }
}

