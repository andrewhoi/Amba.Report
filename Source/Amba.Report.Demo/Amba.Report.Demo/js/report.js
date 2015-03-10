var json1 = {
    DocName: 'Акт выполненных работ',
    DocDate: '2012-04-23T21:01:02',
    DocNum: 123,
    DocSum: 1234.56,
    IsApproved: true,
    Org: { 'Name': 'Наименование контрагента', 'Address': { 'Name': 'Россия, Татарстан, г. Казань' } },

    Rows: [
        { 'Name': 'Товар 1', 'Quantity': 12.5, 'Measure': { 'Name': 'шт.' } },
        { 'Name': 'Товар 2', 'Quantity': 1, 'Measure': { 'Name': 'шт.' } },
        { 'Name': 'Товар 3', 'Quantity': 2.0, 'Measure': { 'Name': 'кг.' } }
    ],
    NestedRows: [
        {
            Name: 'Group1',
            Details: [
                { Name: 'Detail_1_1' },
                { Name: 'Detail_1_2' },
            ]
        },
        {
            Name: 'Group2',
            Details: [
                { Name: 'Detail_2_1' },
                { Name: 'Detail_2_2' },
            ]
        }
    ],
    PrintDate: '2015-02-23T21:01:02',
    Note: 'Абма: горизонтальные приложения',
    MergedCells: 'Вывод информации в объединенные ячейки',
    SeveralCells: 'Вывод информации в несколько ячеек'
};



$(document).ready(function () {
    $("#reportData").text(JSON.stringify(json1, null, '    '));
    var demo = "Demo1";

    $('#btn1').on('click', function () {
        demo = "Demo1";
        $("#reportData").text(JSON.stringify(json1, null, '    '));
    });

    $('#btn2').on('click', function () {
        demo = "Demo2";
        $("#reportData").text(JSON.stringify({}, null, '    '));
    });


    $('#submit').click(function () {
        var formData = $("#reportData").val();

        var $btn = $(this).button('loading');
        debugger;
        $.ajax({
            type: "POST",
            url: "./api/report/" + demo,
            data: formData,
            dataType: "json",
            contentType: "application/json"
        })
        .done(function (msg, status, jqxhr) {
            var tempLocation = jqxhr.getResponseHeader("Location");
            if (tempLocation) {
                $("iframe[name='downloadFrame']").attr("src", tempLocation);
            }
            $btn.button('reset');
        });
    });
});



