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

using Amba.Report;
using Owin;
using System.Configuration;
using System.Web.Http;

namespace Owin
{
    /// <summary>
    /// Extention for OWIN IAppBuilder
    /// </summary>
    public static class AmbaReportAppBuilderExtension
    {
        /// <summary>
        /// Enable AmbaReport module, invoke before app.UseWebApi(config) method
        /// </summary>
        /// <param name="app"></param>
        /// <param name="config"></param>
        public static void UseAmbaReport(this IAppBuilder app, HttpConfiguration config)
        {
            var section = ConfigurationManager.GetSection("amba/amba.report") as ConfigSection;
            if (section == null)
            {
                return;
            }
            ConfigurationManager.RefreshSection("amba/amba.report");
            if (!section.Enabled)
            {
                return;
            }
            // TODO check section parameters, disable if something wrong
            config.Routes.MapHttpRoute(
               "amba.report",
               section.Uri,
               defaults: new { id = RouteParameter.Optional, controller = "Report" });
        }
    }
}
