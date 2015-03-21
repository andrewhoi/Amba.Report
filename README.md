## Amba.Report

Generate Excel reports with http-POST request and WebAPI.

You can find demo-app with template and json-data example. [Amba.Report.Demo](https://github.com/AmbaCloud/Amba.Report/tree/master/Source)

##How to start

**1. Install nuget package:**
 
    PM> Install-Package Amba.Report

if OWIN-app hosted in console application, add listener:

    PM> Install-Package Microsoft.Owin.Host.HttpListener

**2. Add code to OWIN-Startup class:**

    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        app.UseAmbaReport(config);
        app.UseWebApi(config);
        // ....
     } 
**3. Configure app.config and templates in Reports.xml**
    
**app.config**    

    <amba>
        <amba.report enabled="true" uri="/api/report/{id}">
            <templates path=".\Amba.Report\Templates" 
                       dataFile=".\Amba.Report\Reports.xml" />
            <downloads path=".\Amba.Report\Downloads" 
                       uri="/downloads" 
                       deleteOlderThanInMinutes="1" 
                       deleteFrequencyInMinutes="1" />
        </amba.report>
    </amba>

**Reports.xml**

    <Reports>
        <Report name="Demo1" path="Demo1.xlsx" downloadName="Demo1_Report.xlsx"></Report>
    </Reports>
   

**4. Send http-POST request and download with link**
    
    // index.js
    $.ajax({
        type: "POST",
        url: "./api/report/Demo1",
        data: jsonData,
        dataType: "json",
        contentType: "application/json"
    })
    .done(function (msg, status, jqxhr) {
        var tempLocation = jqxhr.getResponseHeader("Location"); // return url in Location header
        if (tempLocation) {
            $("iframe[name='downloadFrame']").attr("src", tempLocation); // download it now
        }
    });

    // index.html
    <iframe name="downloadFrame" style='display: none;'></iframe>
    

## Changelog

**1.0.0** (March 22, 2015)

* Added: **POST api/report/{reportId}** that generates report and returns link to download

## License

GNU LGPL v3.0

Copyright (c) 2015 Vladimir Kuznetsov