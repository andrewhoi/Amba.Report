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
using System.Configuration;

namespace Amba.Report
{
    /// <summary>
    /// Configuration section in app.config or web.config
    /// </summary>
    public class ConfigSection : ConfigurationSection
    {
        

        /// <summary>
        /// Determine if this api enabled
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = "false", IsRequired = false)]
        public Boolean Enabled
        {
            get
            {
                return (Boolean)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }

        /// <summary>
        /// Uri for api address, default value is 'api/report/{id}'
        /// </summary>
        [ConfigurationProperty("uri", DefaultValue = "api/report/{id}", IsRequired = false)]
        public String Uri
        {
            get
            {
                return (String)this["uri"];
            }
            set
            {
                this["uri"] = value;
            }
        }

        /// <summary>
        /// Configuration for templates
        /// </summary>
        [ConfigurationProperty("templates")]
        public TemplatesElement Templates
        {
            get
            {
                return (TemplatesElement)this["templates"];
            }
            set
            { this["templates"] = value; }
        }

        /// <summary>
        /// Configuration for downloads
        /// </summary>
        [ConfigurationProperty("downloads")]
        public DownloadsElement Downloads
        {
            get
            {
                return (DownloadsElement)this["downloads"];
            }
            set
            { this["downloads"] = value; }
        }

    }
    /// <summary>
    /// Configuration element for templates
    /// </summary>
    public class TemplatesElement : ConfigurationElement
    {
        /// <summary>
        /// Path to xml file with registered templates
        /// </summary>
        [ConfigurationProperty("dataFile", DefaultValue = ".\\Reports.xml", IsRequired = false)]
        public String DataFile
        {
            get
            {
                return (String)this["dataFile"];
            }
            set
            {
                this["dataFile"] = value;
            }
        }

        /// <summary>
        /// Path to templates directory
        /// </summary>
        [ConfigurationProperty("path", DefaultValue = ".\\Templates", IsRequired = false)]
        public String Path
        {
            get
            {
                return (String)this["path"];
            }
            set
            {
                this["path"] = value;
            }
        }


    }
    /// <summary>
    /// Configuration element for downloads
    /// </summary>
    public class DownloadsElement : ConfigurationElement
    {
        /// <summary>
        /// Path to temporary directory with generated reports
        /// </summary>
        [ConfigurationProperty("path", DefaultValue = ".\\Downloads", IsRequired = false)]
        public String Path
        {
            get
            {
                return (String)this["path"];
            }
            set
            {
                this["path"] = value;
            }
        }

        /// <summary>
        /// Interval for save file without deleting in minutes
        /// </summary>
        [ConfigurationProperty("deleteOlderThanInMinutes", DefaultValue = "60", IsRequired = false)]
        [IntegerValidator(ExcludeRange = false, MaxValue = Int32.MaxValue, MinValue = 1)]
        public int DeleteOlderThanInMinutes
        {
            get
            { return (int)this["deleteOlderThanInMinutes"]; }
            set
            { this["deleteOlderThanInMinutes"] = value; }
        }
        /// <summary>
        /// Delete operation frequency in minutes
        /// </summary>
        [ConfigurationProperty("deleteFrequencyInMinutes", DefaultValue = "1", IsRequired = false)]
        [IntegerValidator(ExcludeRange = false, MaxValue = Int32.MaxValue, MinValue = 1)]
        public int DeleteFrequencyInMinutes
        {
            get
            { return (int)this["deleteFrequencyInMinutes"]; }
            set
            { this["deleteFrequencyInMinutes"] = value; }
        }
    }
}
