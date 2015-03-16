// #region

var json1 = {
    "Date": new Date("2015-03-22T00:00:00"),
    "Company": {
        "Name": "Example company"
    },
    "Rows": [
      {
          "Item": "Apples",
          "Qnt": 100,
      },
      {
          "Item": "Oranges",
          "Qnt": 50,
      },
    ]
}

// #endregion



$(document).ready(function () {
    $("#reportData").text(JSON.stringify(json1, null, '    '));

    $('#submit').click(function () {
        var formData = $("#reportData").val();

        var $btn = $(this).button('loading');

        $.ajax({
            type: "POST",
            url: "./api/report/Demo1?downloadName=ReportFromDemo.xlsx",
            data: formData,
            dataType: "json",
            contentType: "application/json"
        })
        .done(function (msg, status, jqxhr) {
            // return url in Location header
            var tempLocation = jqxhr.getResponseHeader("Location");
            if (tempLocation) {
                // download it
                $("iframe[name='downloadFrame']").attr("src", tempLocation);
            }
        })
        .always(function () {
            $btn.button('reset');
        });
    });
});



