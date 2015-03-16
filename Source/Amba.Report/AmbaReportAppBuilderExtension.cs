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

namespace Owin
{
    using Amba.Report;
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Web.Http;


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
            Amba.Report.Config.LoadConfigFromConfiguration();


            config.Routes.MapHttpRoute(
               "amba.report",
               Amba.Report.Config.Uri,
               defaults: new { id = RouteParameter.Optional, controller = "Report" });


            if (Amba.Report.Config.Enabled)
            {
                var downloadOption = new FileServerOptions()
                {
                    RequestPath = new PathString(Amba.Report.Config.DownloadsUri),
                    FileSystem = new PhysicalFileSystem(Amba.Report.Config.DownloadsPath),
                };
                app.UseFileServer(downloadOption);

                app.UseReportCleaner(ClearFilesForDownload, 
                    TimeSpan.FromMinutes(Amba.Report.Config.DeleteFrequencyInMinutes), 
                    "Amba.Report's download directory cleaner");
            }

        }

        private static void ClearFilesForDownload()
        {
            DirectoryInfo di = new DirectoryInfo(Amba.Report.Config.DownloadsPath);
            var list = from d in di.GetDirectories()
                       where d.LastWriteTime < DateTime.Now.AddMinutes(-1 * 
                       Amba.Report.Config.DeleteOlderThanInMinutes)
                       select d;
            var count = list.Count();
            foreach (var item in list)
            {
                item.Delete(true);
            }
           // logger.Trace("Clear files from download's folder. Files' count is {0}", count);
        }
    }

    internal static class BackgroundWorkerBootstrapper
    {
        public static void UseReportCleaner(
                this IAppBuilder app, Action action, TimeSpan interval, string name)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (action == null) throw new ArgumentNullException("action");

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            var worker = new BackgroundWorker(action, interval, name);
            if (token != CancellationToken.None)
            {
                token.Register(worker.Dispose);
            }
            worker.Start();
        }
    }
}
