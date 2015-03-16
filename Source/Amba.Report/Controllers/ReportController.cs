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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;

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
            return await Task.FromResult(GetReport(id, reportData));
        }

        private string GetFileName(string id)
        {
            var result = Request.GetQueryNameValuePairs()
                .FirstOrDefault(k => k.Key.Equals("downloadName", StringComparison.OrdinalIgnoreCase));
            string file;
            if (String.IsNullOrWhiteSpace(result.Key))
            {
                file = Config.Templates[id].DownloadName;
            }
            else
            {
                file = result.Value;
                if (!file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) file += ".xlsx";

            }
            var fileName = Path.GetFullPath(Path.Combine(Config.TemplatesPath, file));
            return fileName;
        }
        private IHttpActionResult GetReport(string id, JObject reportData)
        {
            if (!Config.Templates.ContainsKey(id))
            {
                // TODO try to find report in xml-file and register it in Config. Hot adding.
                return NotFound();
            }
            var templateFileName = Path.GetFullPath(Path.Combine(Config.TemplatesPath, Config.Templates[id].Path));
            if (!File.Exists(templateFileName))
            {
                //logger.Warn("Template for report with name='{0}' is not found. {1}", id, templateFileName);
                return NotFound();
            }

            var fileName = GetFileName(id);
            var fi = new FileInfo(fileName);

            var tempDirName = Path.GetRandomFileName();
            var tempDirFullName = Path.Combine(Config.DownloadsPath, tempDirName);
            try
            {
                if (!Directory.Exists(tempDirFullName))
                {
                    Directory.CreateDirectory(tempDirFullName);
                }
                using (var reporter = new SpreadsheetLightJsonReporter(templateFileName, reportData, Path.Combine(tempDirFullName, fi.Name)))
                {
                    reporter.Execute();
                }
                Uri uri = new Uri("." + Config.DownloadsUri + "/" + tempDirName + "/" + fi.Name, UriKind.Relative);
                return Created<string>(uri.ToString(),
                    String.Format("File will be deleted in {0} minutes.", Amba.Report.Config.DeleteOlderThanInMinutes));
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Unauthorized Access Exception"
                });
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = ex.Message // TODO not visible russian words correctly in sentinel-app
                });
            }
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
