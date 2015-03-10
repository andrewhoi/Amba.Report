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
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amba.Report.Controllers
{
    /// <summary>
    /// ReportController class
    /// </summary>
    public class ReportController : ApiController
    {
        /// <summary>
        /// Generate report
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IHttpActionResult> PostReport(string id, JObject reportData)
        {
            if (!Config.Enabled)
            {
                if (Config.Errors.Count > 0)
                {
                    throw new HttpResponseException(
                        Request.CreateResponse<string>(
                            HttpStatusCode.InternalServerError,
                            "Amba.Report module is disabled due to an error(s) in the configuration."));
                }
                else
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }
            }
            return await Task.FromResult(Created<JObject>("demouri", reportData));
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IHttpActionResult> Get(string operation = "")
        {
            //throw new HttpResponseException(
            //            Request.CreateResponse<ReadOnlyCollection<ConfigError>>(
            //                HttpStatusCode.InternalServerError,
            //                Config.Errors));
            if (operation.Equals("checkHealth", System.StringComparison.OrdinalIgnoreCase))
            {
                return await Task.FromResult(Ok<string>("checkHealth!!!"));
            }
            return await Task.FromResult(Ok<string>(String.Format("operation=", operation)));
        }
    }
}
