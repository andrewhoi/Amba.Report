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
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using System.Diagnostics;
    using System.IO;
    public static class AmbaReportDemoAppBuilderExtension
    {
        public static void UseAmbaReportDemoPages(this IAppBuilder app)
        {
            // add static html for demo
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

            // uri for download templates
            var templatesOption = new FileServerOptions()
            {
                RequestPath = new PathString("/templates"),
                FileSystem = new PhysicalFileSystem(Amba.Report.Config.TemplatesPath),
            };
            app.UseFileServer(templatesOption);
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
