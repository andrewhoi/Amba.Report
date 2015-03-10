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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
namespace Amba.Report
{
    /// <summary>
    /// Config data for Amba.Report module
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Default value for parameter deleteOlderThanInMinutes
        /// </summary>
        public const int DELETE_OLDER_THAN_IN_MINUTES = 60;
        /// <summary>
        /// Default valur for parameter deleteFrequencyInMinutes
        /// </summary>
        public const int DELETE_FREQUENCY_IN_MINUTES = 1;


        /// <summary>
        /// Fill config from configSection
        /// </summary>
        public static void LoadConfigFromConfiguration()
        {
            SpinLock sl = new SpinLock();
            bool gotLock = false;
            sl.Enter(ref gotLock);

            List<ConfigInfo> infos = new List<ConfigInfo>();
            List<ConfigWarning> warnings = new List<ConfigWarning>();
            List<ConfigError> errors = new List<ConfigError>();


            var section = ConfigurationManager.GetSection("amba/amba.report") as ConfigSection;
            if (section == null)
            {
                errors.Add(new ConfigError("configSection", "ConfigSection \"amba/amba.report\" not found."));
                Config.Enabled = false;
                return;
            }
            ConfigurationManager.RefreshSection("amba/amba.report");
            if (!section.Enabled)
            {
                infos.Add(new ConfigInfo("enabled", "Module disabled in app(web).config."));
                Config.Enabled = false;
                return;
            }
            // check uri
            if (string.IsNullOrWhiteSpace(section.Uri))
            {
                errors.Add(new ConfigError("uri",
                    "Uri is not defined in config section."));
            }
            else
            {
                Uri = section.Uri;
            }

            // check Templates data file
            TemplatesDataFile = section.Templates.DataFile;
            if (string.IsNullOrWhiteSpace(TemplatesDataFile))
            {
                errors.Add(new ConfigError("templates.dataFile",
                    "Templates datafile is not defined in config section."));
            }
            else
            {
                var fullPath = GetExistingFullPath(TemplatesDataFile);
                if (string.IsNullOrWhiteSpace(fullPath))
                {
                    errors.Add(new ConfigError("templates.dataFile",
                        "Templates datafile is not found: \"" + TemplatesDataFile + "\""));
                }
                else
                {
                    TemplatesDataFile = fullPath;
                }
            }
            // check Templates path
            TemplatesPath = section.Templates.Path;
            if (string.IsNullOrWhiteSpace(TemplatesPath))
            {
                errors.Add(new ConfigError("templates.path",
                   "Templates path is not defined in config section."));
            }
            else
            {
                var fullPath = GetExistingFullPath(TemplatesPath);
                if (string.IsNullOrWhiteSpace(fullPath))
                {
                    errors.Add(new ConfigError("templates.path",
                        "Templates path is not found: \"" + TemplatesPath + "\""));
                }
                else
                {
                    TemplatesPath = fullPath;
                }
            }

            // check Downloads path
            DownloadsPath = section.Downloads.Path;
            if (string.IsNullOrWhiteSpace(DownloadsPath))
            {
                errors.Add(new ConfigError("downloads.path",
                   "Downloads path is not defined in config section."));
            }
            else
            {
                var fullPath = GetExistingFullPath(DownloadsPath);
                if (string.IsNullOrWhiteSpace(fullPath))
                {
                    errors.Add(new ConfigError("downloads.path",
                        "Downloads path is not found: \"" + DownloadsPath + "\""));
                }
                else
                {
                    DownloadsPath = fullPath;
                }
            }

            // check deleteOlderThanInMinutes
            DeleteOlderThanInMinutes = section.Downloads.DeleteOlderThanInMinutes;
            if (DeleteOlderThanInMinutes <= 0)
            {
                DeleteOlderThanInMinutes = DELETE_OLDER_THAN_IN_MINUTES;
                warnings.Add(new ConfigWarning("downloads.deleteOlderThanInMinutes",
                    "downloads.deleteOlderThanInMinutes was less than one and set to " + DeleteOlderThanInMinutes.ToString()));
            }

            // check deleteFrequencyInMinutes
            DeleteFrequencyInMinutes = section.Downloads.DeleteFrequencyInMinutes;
            if (DeleteFrequencyInMinutes <= 0)
            {
                DeleteFrequencyInMinutes = DELETE_FREQUENCY_IN_MINUTES;
                warnings.Add(new ConfigWarning("downloads.deleteFrequencyInMinutes",
                    "downloads.deleteFrequencyInMinutes was less than one and set to " + DeleteFrequencyInMinutes.ToString()));
            }


            LoadReportInfo();

            Infos = new ReadOnlyCollection<ConfigInfo>(infos);
            Warnings = new ReadOnlyCollection<ConfigWarning>(warnings);
            Errors = new ReadOnlyCollection<ConfigError>(errors);

            Enabled = Errors.Count == 0;

            if (gotLock) sl.Exit();
        }

        private static void LoadReportInfo()
        {
            var doc = XDocument.Load(TemplatesDataFile);
            var items = doc.XPathSelectElements("Reports/Report");
            var dict = new Dictionary<string, ReportInfo>();
            foreach (var item in items)
            {
                string name = "";
                if (item.HasAttributes && item.Attribute("name") != null)
                {
                    name = item.Attribute("name").Value;
                    string path = item.Attribute("path").Value;
                    string downloadName = item.Attribute("downloadName").Value;
                    // TODO check File.Exists
                    dict.Add(name,
                        new Amba.Report.ReportInfo
                        {
                            Name = name,
                            Path = path,
                            DownloadName = downloadName
                        });
                }
            }
            Templates = new ReadOnlyDictionary<string, ReportInfo>(dict);
        }

        private static string GetExistingFullPath(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                path = Path.GetFullPath(path);
                return path;
            }
            else
            {
                // try combine path for running on IIS
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                path = Path.GetFullPath(path);
                if (Directory.Exists(path) || File.Exists(path))
                {
                    return path;
                }
            }
            return String.Empty;
        }


        /// <summary>
        /// Enable-disable Amba.Report module
        /// </summary>
        public static bool Enabled { get; private set; }

        /// <summary>
        /// Uri for api address, default value is 'api/report/{id}'
        /// </summary>
        public static string Uri { get; private set; }

        /// <summary>
        /// Full path to xml file with all reports description
        /// </summary>
        public static string TemplatesDataFile { get; private set; }


        /// <summary>
        /// Full path to root templates directory
        /// </summary>
        public static string TemplatesPath { get; private set; }

        /// <summary>
        /// Full path to temporary directory, using for download generated files
        /// </summary>
        public static string DownloadsPath { get; private set; }


        /// <summary>
        /// Filter for backgroud worker that deletes files in download directory.
        /// Files older than this value (in minutes) will be deleted.
        /// </summary>
        public static int DeleteOlderThanInMinutes { get; private set; }


        /// <summary>
        /// Interval for start process of deleting files in download directory (DownloadsPath).
        /// In minutes.
        /// </summary>
        public static int DeleteFrequencyInMinutes { get; private set; }

        /// <summary>
        /// List of registered reports
        /// </summary>
        public static ReadOnlyDictionary<string, ReportInfo> Templates { get; private set; }

        /// <summary>
        /// Collection of errors in configuration
        /// </summary>
        public static ReadOnlyCollection<ConfigError> Errors { get; private set; }

        /// <summary>
        /// Collection of info messages about configuration
        /// </summary>
        public static ReadOnlyCollection<ConfigInfo> Infos { get; private set; }

        /// <summary>
        /// Collection of warnings about configuration
        /// </summary>
        public static ReadOnlyCollection<ConfigWarning> Warnings { get; private set; }

    }
    /// <summary>
    /// Information about error in config parameters
    /// </summary>
    public class ConfigError
    {
        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="message"></param>
        public ConfigError(string parameterName, string message)
        {
            ParameterName = parameterName;
            Message = message;
        }
        /// <summary>
        /// Parameter's name
        /// </summary>
        public string ParameterName { get; private set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }
    }
    /// <summary>
    /// Information about warning in config parameters
    /// </summary>
    public class ConfigWarning
    {
        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="message"></param>
        public ConfigWarning(string parameterName, string message)
        {
            ParameterName = parameterName;
            Message = message;
        }
        /// <summary>
        /// Parameter's name
        /// </summary>
        public string ParameterName { get; private set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }
    }
    /// <summary>
    /// Information about config
    /// </summary>
    public class ConfigInfo
    {
        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="message"></param>
        public ConfigInfo(string parameterName, string message)
        {
            ParameterName = parameterName;
            Message = message;
        }
        /// <summary>
        /// Parameter's name
        /// </summary>
        public string ParameterName { get; private set; }
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Info about registered report
    /// </summary>
    public class ReportInfo
    {
        /// <summary>
        /// Unique report's name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Relative path (for Amba.Report.Config.TemplatesPath) to template
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Download file name
        /// </summary>
        public string DownloadName { get; set; }
    }
}
