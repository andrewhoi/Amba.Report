// #region
//"2015-03-22T00:00:00"
var json1 = {
    "Date": new Date("2015-03-22T00:00:00"),
    "Company": {
        "Name": "Example company",
        "Address": "Paris"
    },
    "PrintedAt": "2015-03-13T21:01:02",
    "Categories": [
      {
          "Name": "Foods",
          "Qnt": 100,
          "Amount": 1000.0,
          "Products": [
            {
                "Name": "Apple",
                "Qnt": 40,
                "Amount": 400.0,
                "Orders": [
                  {
                      "Number": "2015-1",
                      "Customer": "USA Goverment",
                      "Qnt": 1,
                      "Amount": 10.0
                  },
                  {
                      "Number": "2015-2",
                      "Customer": "John Hock",
                      "Qnt": 29,
                      "Amount": 290.0
                  },
                  {
                      "Number": "2015-3",
                      "Customer": "Adam Smith",
                      "Qnt": 10,
                      "Amount": 100.0
                  }
                ]
            },
            {
                "Name": "Cherry",
                "Qnt": 60,
                "Amount": 600.0,
                "Orders": [
                  {
                      "Number": "2015-1",
                      "Customer": "USA Goverment",
                      "Qnt": 1,
                      "Amount": 599.99
                  },
                  {
                      "Number": "2015-4",
                      "Customer": "Jeam Bean",
                      "Qnt": 59,
                      "Amount": 0.01
                  }
                ]
            }
          ]
      }
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
            url: "./api/report/Demo1",
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



