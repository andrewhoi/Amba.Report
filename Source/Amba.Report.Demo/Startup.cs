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

using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.Diagnostics;
using System.IO;
using System.Web.Http;

[assembly: OwinStartup(typeof(Amba.Report.Demo.Startup))]

namespace Amba.Report.Demo
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            app.UseAmbaReport(config);
            app.UseWebApi(config);


            var staticOptions = new FileServerOptions()
            {
                RequestPath = PathString.Empty,
                FileSystem = new PhysicalFileSystem(IsRunningInConsoleOwin ? @".\Amba.Report.Demo" : @".\bin\Amba.Report.Demo"), 
                EnableDefaultFiles = true,
            };
#if DEBUG
            if (IsRunningInConsoleOwin)
            {
                staticOptions.FileSystem = new PhysicalFileSystem(@"..\..\Amba.Report.Demo"); // Path to sources to edit html/js/css in IDE
            }
#endif
            app.UseFileServer(staticOptions);

            app.Run(async (IOwinContext context) =>
            {
                var bytes = System.Text.Encoding.UTF8
                    .GetBytes("404. Not found.");
                context.Response.StatusCode = 404;
                context.Response.ContentLength = bytes.Length;
                await context.Response.WriteAsync(bytes);
            });
        }

        public static bool IsRunningInConsoleOwin
        {
            get
            {
                //Amba.Report.Demo.vshost.exe
                var processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                //c:\windows\system32\inetsrv\w3wp.exe for IIS
                if (processName.Equals("Amba.Report.Demo.exe") ||
                    processName.Equals("Amba.Report.Demo.vshost.exe"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

}
