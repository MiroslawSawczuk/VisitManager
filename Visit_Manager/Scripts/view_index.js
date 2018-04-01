$(document).ready(function () {

    var date = new Date();

    Set_top_datepicker(date); // Initialize datepicker in #top_datepicker
    Set_all_time_pickers(); // Initialize all timepickers
    Display_visits();

    // Prevent from insert other characters than numbers
    $('#add_client_tel_number, #add_price_visit,#edit_client_tel_number, #edit_price_visit')
        .keydown(function (e) {
            // Allow: backspace, delete, tab, escape, enter
            if ($.inArray(e.keyCode, [46, 8, 9, 27, 13]) !== -1 ||
                // Allow: Ctrl/cmd+A
                (e.keyCode == 65 && (e.ctrlKey === true || e.metaKey === true)) ||
                // Allow: Ctrl/cmd+C
                (e.keyCode == 67 && (e.ctrlKey === true || e.metaKey === true)) ||
                // Allow: Ctrl/cmd+X
                (e.keyCode == 88 && (e.ctrlKey === true || e.metaKey === true)) ||
                // Allow: home, end, left, right
                (e.keyCode >= 35 && e.keyCode <= 39)) {
                // let it happen, don't do anything
                return;
            }
            // Ensure that it is a number and stop the keypress
            if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {

                e.preventDefault();
            }
        });

    // Prevent from insert space to input
    $('input').on('keypress', function (e) {
        return !(e.keyCode == 32);
    });

    $('#add_visit_form, #edit_visit_form').on('submit', function (e) {

        var btn = '';
        e.preventDefault();

        if (this.id == 'add_visit_form') {

            btn = $('#btn_add_visit');

            Prevent_double_click(btn);
            Add_new_visit();
        }
        else if (this.id == 'edit_visit_form') {

            btn = $('#btn_save_edited_visit');

            Prevent_double_click(btn);
            Edit_visit();
        }
    });

    $('#add_visit_form, #edit_visit_form').on('hidden.bs.modal', function (e) {

        Clear_inputs_modal_add_visit(e);
    });

    // Change date in #top_datepicker after choose the date from picker
    $('#top_datepicker').on('change', function () {

        Display_visits();

        var date = $('#top_datepicker').val();
        $('.hidden_visit_date').val(date); // Set the date in the hidden field necessary to download visits in pdf/xlsx
    });

    // onClick the btn #btn_new_visit, the date value is assigned from #top_datepicker to the datepicker in #add_visit_form
    $('#btn_new_visit').click(function (e) {

        var date_string_type = $('#top_datepicker').val();
        var date_in_date_type = Convert_string_to_date(date_string_type);

        Set_datepicker_in_modal(date_in_date_type);
        Set_time_in_add_end_time();
    });

    // When setting the start of the visit in '#add_start_time, the end of the visit is automatically set to half an hour ahead
    // When we change the final hour of the visit, nothing happens
    $('#add_start_time').on('changeTime.timepicker', function (e) {

        var date = new Date();
        var time = new Date(date.getFullYear(), date.getMonth(), date.getDay(), e.time.hours, e.time.minutes, 0, 0);

        time = new Date(time.getFullYear(), time.getMonth(), time.getDay(), time.getHours(), time.getMinutes() + 30, 0, 0);
        $('#add_end_time').timepicker('setTime', time.getHours() + ':' + time.getMinutes());
    });

    // When setting the start of the visit in #edit_start_time, the end of the visit is automatically set to half an hour ahead
    // When we change the final hour of the visit, nothing happens
    $('#edit_start_time').on('changeTime.timepicker', function (e) {

        var date = new Date();
        var time = new Date(date.getFullYear(), date.getMonth(), date.getDay(), e.time.hours, e.time.minutes, 0, 0);

        time = new Date(time.getFullYear(), time.getMonth(), time.getDay(), time.getHours(), time.getMinutes() + 30, 0, 0);
        $('#edit_end_time').timepicker('setTime', time.getHours() + ':' + time.getMinutes());
    });


});// End  $(document).ready -->


function Display_visits() {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data:
            {
                date: $('#top_datepicker').val()
            },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_visits_from_db',

        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                var is_clients = '';
                var cont = '';
                var cont_mobile = '';

                if (result.length != 0) {

                    $('#is_clients').html(is_clients);
                    var employee_id = '';

                    for (var i = 0; i < result.length; i++) {

                        employee_id = result[i].Employee_id;

                        // Larger device

                        if (i == 0) {

                            cont += "<h4 class='employee_icon_name'> <span class='glyphicon glyphicon-user' aria-hidden='true'></span>"
                                    + result[i].Employee_name + " " + result[i].Employee_surname + "</h4>"
                                    + "<br/>"
                                    + "<table class='table table-hover table-condensed table_visit'>"
                                    + "<thead class='blue_color'>"
                                    + "<td class='col-md-1'>" + Resources.Visit_time1
                                    + "</br>"
                                    + Resources.Visit_time2 + "</td>"
                                    + "<td class='col-md-2'>"
                                    + Resources.Client_name_and
                                    + "</br>"
                                    + Resources.Client_surname + "</td>"
                                    + "<td class='col-md-3'>" + Resources.Visit_type + "</td>"
                                    + "<td class='col-md-3'>" + Resources.Visit_describe + "</td>"
                                    + "<td class='col-md-1'>" + Resources.Price + "</td>"
                                    + "<td class='col-md-1'>" + Resources.Client_tel_number + "</td>"
                                    + "<td class='col-md-1'></td>"
                                    + "</thead>"
                                    + "<tbody>";
                        }
                        else if (employee_id != result[i - 1].Employee_id) {

                            cont += "</tbody>"
                                  + "</table>"
                                  + "<br/>"
                                  + "<br/>"
                                  + "<h4 class='employee_icon_name'> <span class='glyphicon glyphicon-user' aria-hidden='true'></span>"
                                  + result[i].Employee_name + " " + result[i].Employee_surname + "</h4>"
                                  + "<br/>"
                                  + "<table class='table table-hover table-condensed table_visit'>"
                                  + "<thead></thead>"
                                  + "<tbody>";
                        }

                        cont += "<tr>"
                                + "<td class='col-md-1'>"
                                + To_java_script_time(result[i].Start_time)
                                + " - "
                                + To_java_script_time(result[i].End_time)
                                + "</td>"
                                + "<td class='col-md-2'>"
                                + result[i].Client_name
                                + "</br>"
                                + result[i].Client_surname
                                + "</td>"
                                + "<td class='col-md-3'><b>"
                                + result[i].Type_name;

                        if (result[i].Type_id == 1) {

                            cont += "<br/>"
                                  + "<span class='blue_color'>" + result[i].Type_unclassified + "</span>";
                        }

                        cont += "</b></td>"
                            + "<td class='col-md-3'>"
                            + result[i].Describe
                            + "</td>"
                            + "<td class='col-md-1'>"
                            + result[i].Price + " zł"
                            + "</td>"
                            + "<td class='col-md-1'>"
                            + result[i].Client_tel_number
                            + "</td>"
                            + "<td class='col-md-1'>"
                            + "<button type='button' id='btn_edit' class='btn btn-info' value='" + result[i].Id
                            + "'onclick='Complete_inputs_in_edit_modal(this.value)' data-toggle='modal' data-target='#modal_edit_visit'>"
                            + "<span class='glyphicon glyphicon-edit' aria-hidden='true'></span>" + Resources.Edit + "</button>"
                            + "</td>"
                            + "</tr>";

                        if (i + 1 == result.length) {
                            cont += "</tbody></table>";
                        }
                        $('#div_table_visit').html(cont);


                        // Mobile device

                        cont_mobile += "<h4 class='employee_icon_name'> <span class='glyphicon glyphicon-user' aria-hidden='true'></span>"
                                     + result[i].Employee_name + " " + result[i].Employee_surname + "</h4>"
                                     + "<div class='col-12'><span class='blue_color'>" + Resources.Visit_time + " : </span>"
                                     + To_java_script_time(result[i].Start_time) + " - " + To_java_script_time(result[i].End_time) + "</div>"
                                     + "<div class='col-12'><span class='blue_color'> " + Resources.Client_name_surname + " : </span>"
                                     + result[i].Client_name + " " + result[i].Client_surname + "</div>"
                                     + "<div class='col-12'><span class='blue_color'>" + Resources.Visit_type + " : </span>"
                                     + result[i].Type_name;

                        if (result[i].Type_id == 1) {

                            cont_mobile += ": <span class='grey_color'>" + result[i].Type_unclassified + "</span></div>";
                        }
                        else {
                            cont_mobile += "</div>";
                        }

                        cont_mobile += "<div class='col-12'><span class='blue_color'>" + Resources.Visit_describe + " : </span>"
                                     + result[i].Describe + "</div>"
                                     + "<div class='col-12'><span class='blue_color'>" + Resources.Price + " : </span>"
                                     + result[i].Price + " zł" + "</div>"
                                     + "<div class='col-12'><span class='blue_color'>" + Resources.Client_tel_number + " : </span>"
                                     + result[i].Client_tel_number + "</div>"
                                     + "<div class='col-12'>" + "<button type='button' id='btn_edit' class='btn btn-info' value='" + result[i].Id
                                     + "'onclick='Complete_inputs_in_edit_modal(this.value)' data-toggle='modal' data-target='#modal_edit_visit'>"
                                     + "<span class='glyphicon glyphicon-edit' aria-hidden='true'></span>" + Resources.Edit + "</button>"
                                     + "</div>"
                                     + "<br/>";

                        $('#div_table_mobile_visit').html(cont_mobile);
                    }
                }
                else {

                    is_clients = Resources.No_visits_to_display;
                    $('#div_table_visit').html(cont); // Clear div when no visits
                    $('#div_table_mobile_visit').html(cont_mobile); // Clear div when no visits
                    $('#is_clients').html(is_clients);
                }
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Add_new_visit() {

    $.ajax({
        type: 'POST',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: JSON.stringify(
           {
               Visit_date: $('#add_visit_date').val(),
               Client_name: $('#add_client_name').val(),
               Client_surname: $('#add_client_surname').val(),
               Client_tel_number: $('#add_client_tel_number').val(),
               Client_email: $('#add_client_email').val(),
               Employee_id: $('#add_ddl_employee_id').val(),
               Start_time: $('#add_start_time').val(),
               End_time: $('#add_end_time').val(),
               Describe: $('#add_describe_visit').val(),
               Price: $('#add_price_visit').val(),
               Type_id: $('#add_ddl_visit_type_id').val(),
               Type_unclassified: $('#add_type_unclassified').val()
           }),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Add_new_visit',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                if (result.If_action_successed) {

                    Display_visits();
                    $('[data-dismiss=modal]').trigger({ type: 'click' }); // Hide modal window
                }
                Display_results(result);
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Edit_visit() {

    $.ajax({
        type: 'POST',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: JSON.stringify(
          {
              Visit_id: $('#btn_remove_edited_visit').val(),
              Visit_date: $('#edit_visit_date').val(),
              Client_name: $('#edit_client_name').val(),
              Client_surname: $('#edit_client_surname').val(),
              Client_tel_number: $('#edit_client_tel_number').val(),
              Client_email: $('#edit_client_email').val(),
              Employee_id: $('#edit_ddl_employee_id').val(),
              Start_time: $('#edit_start_time').val(),
              End_time: $('#edit_end_time').val(),
              Describe: $('#edit_describe_visit').val(),
              Price: $('#edit_price_visit').val(),
              Type_id: $('#edit_ddl_visit_type_id').val(),
              Type_unclassified: $('#edit_type_unclassified').val()
          }),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Edit_visit',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                if (result.If_action_successed) {

                    Display_visits();
                    $('[data-dismiss=modal]').trigger({ type: 'click' }); // Hide modal window
                }
                Display_results(result);
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Remove_visit(id) {

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: Resources.Confirm_cancel,
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'btn-danger',
        confirmButtonText: Resources.Alert_btn_yes_remove,
        cancelButtonText: Resources.Alert_btn_no_cancel,
        closeOnConfirm: false,
        closeOnCancel: false
    },
     function (is_confirm) {

         $('.sweet-alert').css('display', 'none');
         $('.sweet-overlay').css('display', 'none');

         if (is_confirm) {

             $.ajax({
                 type: 'POST',

                 beforeSend: function () {

                     $('#ajax-loader').css('visibility', 'visible');
                 },
                 data: JSON.stringify(
                      {
                          id: id
                      }),
                 dataType: 'json',
                 contentType: 'application/json; charset=utf-8',
                 url: '/Visit/Remove_visit',
                 success: function (result) {

                     $('html, body').css('overflowY', 'auto');

                     if (result.If_session_expired) {

                         Log_out_if_session_exp();
                     }
                     else {

                         if (result.If_action_successed) {

                             Display_visits();
                             $('[data-dismiss=modal]').trigger({ type: 'click' }); // Hide modal window
                         }
                         Display_results(result);
                     }
                 },
                 complete: function () {

                     $('#ajax-loader').css('visibility', 'hidden');
                 }
             });
         } else {

             sweetAlert('', Resources.Warning_cancel_visit, 'error');
         }
     });
}

function Complete_inputs_in_edit_modal(id) {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: {

            id: id
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_visit_to_edit',
        success: function (result) {

            if (logged_user_id == 1) // If user is Admin
            {
                $('#btn_remove_edited_visit').prop('disabled', false);
            }
            else {
                $('#btn_remove_edited_visit').prop('disabled', true);
            }

            var visit_date = To_java_script_date(result.Start_time);
            Set_datepicker_in_modal(visit_date)

            $('#edit_start_time').val(To_java_script_time(result.Start_time));
            $('#edit_end_time').val(To_java_script_time(result.End_time));
            $('#edit_client_name').val(result.Client_name);
            $('#edit_client_surname').val(result.Client_surname);
            if (result.Employee_id == 0) { // If employee was removed from db

                $('#edit_ddl_employee_id').val(''); // Set value Select doctor
            }
            else {

                $('#edit_ddl_employee_id').val(result.Employee_id);
            }
            $('#edit_describe_visit').val(result.Describe);
            $('#edit_client_tel_number').val(result.Client_tel_number);
            $('#edit_client_email').val(result.Client_email);
            $('#btn_remove_edited_visit').val(result.Id);
            $('#edit_price_visit').val(result.Price);
            if (result.Type_id == 0) { // If visit type was removed from db

                $('#edit_ddl_visit_type_id').val(''); // Set value Select visit type
                $('#edit_type_unclassified').prop('disabled', true);
                $('#edit_type_unclassified').val('');
            }
            else if (result.Type_id == 1) {

                $('#edit_ddl_visit_type_id').val(result.Type_id);
                $('#edit_type_unclassified').prop('disabled', false);
                $('#edit_type_unclassified').val(result.Type_unclassified);
            }
            else {

                $('#edit_ddl_visit_type_id').val(result.Type_id);
                $('#edit_type_unclassified').prop('disabled', true);
                $('#edit_type_unclassified').val('');
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Add_day() { // OnClick add one day ahead

    var date_string_type = $('#top_datepicker').val();
    var date_in_date_type = Convert_string_to_date(date_string_type);
    date_in_date_type.setDate(date_in_date_type.getDate() + 1);

    Set_top_datepicker(date_in_date_type);
}

function Subtract_day() { // OnClick subtracts one day back

    var date_string_type = $('#top_datepicker').val();
    var date_in_date_type = Convert_string_to_date(date_string_type);
    date_in_date_type.setDate(date_in_date_type.getDate() - 1);

    Set_top_datepicker(date_in_date_type);
}

function Set_top_datepicker(date) {

    $('#top_datepicker').daterangepicker({
        singleDatePicker: true,
        showDropdowns: false,
        startDate: date,
        endDate: date,
        locale: {
            'format': 'DD.MM.YYYY',
            'firstDay': 1,
            'daysOfWeek': [
                     Resources.Sunday,
                     Resources.Monday,
                     Resources.Tuesday,
                     Resources.Wednesday,
                     Resources.Thursday,
                     Resources.Friday,
                     Resources.Saturday
            ],
            'monthNames': [
                   Resources.January,
                   Resources.February,
                   Resources.March,
                   Resources.April,
                   Resources.May,
                   Resources.June,
                   Resources.July,
                   Resources.August,
                   Resources.September,
                   Resources.October,
                   Resources.November,
                   Resources.December
            ],
        }
    });

    var date = $('#top_datepicker').val();
    $('.hidden_visit_date').val(date); // Set the date in the hidden field needed to download visits in pdf/xlsx
}

function Set_datepicker_in_modal(date) { // Initialize datepickers

    $('.datepicker').daterangepicker({
        singleDatePicker: true,
        showDropdowns: false,
        startDate: date,
        endDate: date,
        locale: {
            'format': 'DD.MM.YYYY',
            'firstDay': 1,
            'daysOfWeek': [
                       Resources.Sunday,
                       Resources.Monday,
                       Resources.Tuesday,
                       Resources.Wednesday,
                       Resources.Thursday,
                       Resources.Friday,
                       Resources.Saturday
            ],
            'monthNames': [
                   Resources.January,
                   Resources.February,
                   Resources.March,
                   Resources.April,
                   Resources.May,
                   Resources.June,
                   Resources.July,
                   Resources.August,
                   Resources.September,
                   Resources.October,
                   Resources.November,
                   Resources.December
            ],
        }
    });
}

function Set_all_time_pickers() { // Initialize timepickers

    $('.timepicker_in_modal').timepicker({ // Set time in all timepickers
        minuteStep: 30,
        appendWidgetTo: 'body',
        showMeridian: false
    });
}

function Set_time_in_add_end_time() { // Set timepicker in #add_end_time

    var date = new Date();
    var start_time = $('#add_start_time').val().split(':');

    var end_time = new Date(date.getFullYear(), date.getMonth(), date.getDate(), start_time[0], start_time[1], 0);
    end_time = new Date(date.getFullYear(), end_time.getMonth(), end_time.getDate(), end_time.getHours(), end_time.getMinutes() + 30, 0);

    $('#add_end_time').timepicker('setTime', end_time.getHours() + ':' + end_time.getMinutes());
}

function Convert_string_to_date(string_date) {

    var array = string_date.split('.');
    var date = new Date(array[2], (array[1] - 1), array[0]); // Substract 1 month - January is [0]

    return date;
}

function Convert_string_to_time(string_time, date) {

    var array = string_time.split(':');
    var time = new Date(date.getFullYear(), date.getMonth(), date.getDate(), array[0], array[1], 0);

    return time;
}

function Convert_date_to_string(date) {

    var dd = date.getDate();
    var mm = date.getMonth() + 1; // Add 1 month - January is [0]
    var yyyy = date.getFullYear();

    if (dd < 10) {
        dd = '0' + dd; // Adding 0 in front of a one-digit number
    }
    if (mm < 10) {
        mm = '0' + mm;
    }
    var date = dd + '.' + mm + '.' + yyyy;

    return date;
}

function Convert_time_to_string(time) {

    var hh = time.getHours();
    var mm = time.getMinutes();

    if (hh < 10) {
        hh = '0' + hh; // Adding 0 in front of a one-digit number
    }
    if (mm < 10) {
        mm = mm + '0';
    }
    time = hh + ':' + mm;

    return time;
}

function To_java_script_date(value) { // Converts date from the Date format 412954000 to 20.08.2017

    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var date = new Date(parseFloat(results[1]));
    date = Convert_date_to_string(date);

    return date;
}

function To_java_script_time(value) { // Converts time from the Date format 412954000 to 12:30

    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var time = new Date(parseFloat(results[1]));
    time = Convert_time_to_string(time);

    return time;
}

function Clear_inputs_modal_add_visit(e) {

    var date = $('#add_start_time').val();

    $(e.target)
     .find('input,textarea')
        .val('')
        .end()
     .find('select')
       .val('')
       .end()
       .find('textarea[id=add_type_unclassified]')
         .prop('disabled', true)
         .end();

    $('#add_start_time').val(date);
}

function Add_disable_if_val_1(value) { // Set textarea diabled on false, after select from ddl Inny Zabieg in #add_visit_form

    if (value == 1) {

        $('#add_type_unclassified').prop('disabled', false);
    }
    else {

        $('#add_type_unclassified').prop('disabled', true);
        $('#add_type_unclassified').val('');
    }
}

function Edit_disable_if_val_1(value) { // Set textarea diabled on false, after select from ddl Other in #edit_visit_form

    if (value == 1) {

        $('#edit_type_unclassified').prop('disabled', false);
    }
    else {

        $('#edit_type_unclassified').prop('disabled', true);
        $('#edit_type_unclassified').val('');
    }
}

function Prevent_double_click(btn) {

    btn.prop('disabled', true);

    setTimeout(function () {
        btn.prop('disabled', false);
    }, 3000);
}