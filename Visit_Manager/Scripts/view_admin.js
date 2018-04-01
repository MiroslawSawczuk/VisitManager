$(document).ready(function () {

    Get_data_to_top_chart();
    Set_daterangepicker();

    $('input').on('keypress', function (e) { // Prevent from insert space to input
        return !(e.keyCode == 32);
    });

    $('#add_employee_form, #add_visit_type_form, #edit_employee_form, #edit_visit_type_form, #change_password_form')
        .on('submit', function (e) {

            e.preventDefault(); // Prevent from refreshing page after validating the form

            if (this.id == 'add_employee_form') {

                if ($('#add_employee_password').val() == $('#add_employee_password_confirm').val()) {

                    Add_new_employee();
                }
                else {

                    sweetAlert('', Resources.Warning_password_the_same, 'error');
                }
            }
            else if (this.id == 'add_visit_type_form') {

                Add_new_visit_type();
            }
            else if (this.id == 'edit_employee_form') {

                Edit_employee();
            }
            else if (this.id == 'edit_visit_type_form') {

                Edit_visit_type();
            }
            else if (this.id == 'change_password_form') {

                if ($('#change_new_password').val() == $('#change_new_password_confirm').val()) {

                    Change_password();
                }
                else {

                    sweetAlert('', Resources.Warning_password_the_same, 'error');
                }
            }
        });

    $('#add_employee_form, #add_visit_type_form, #modal_edit_employee, #modal_edit_visit_type, #change_password_form')
        .on('hidden.bs.modal', function (e) {

            Clear_inputs(this.id);
        });
});

function Get_data_to_top_chart() {

    var label_array = [];
    var data_array = [];

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible'); // Show gif while $ajax is processing
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_data_to_top_chart',
        success: function (result) {

            for (var i in result) {

                label_array.push(result[i].Month + ' ' + result[i].Year);
                data_array.push(result[i].Visit_count);
            }

            Create_top_chart(label_array, data_array);
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden'); // Hide gif when $ajax processing end
        }
    });
}

function Get_data_to_bottom_charts() {

    var date_range = $('#dp_range').val();

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: {

            date_range: date_range
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_data_to_bottom_charts',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                var el = $('#bottom_charts')[0];
                el.innerHTML = '';
                date_range = Convert_string_to_date($('#dp_range').val());

                for (var i in result) {

                    Create_bootom_charts(date_range, result[i], i);
                }
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Create_top_chart(label_array, data_array) {

    Highcharts.chart('top_chart', {
        chart: {
            type: 'areaspline',
        },
        title: {
            text: Resources.Total_visit_count,
            style: {
                color: '#777',
                fontFamily: 'Helvetica Neue',
                fontSize: '20px',
            },
        },
        xAxis: {
            allowDecimals: false,
            categories: label_array
        },
        yAxis: {
            title: {
                text: Resources.Visit_count,
                style: {
                    color: '#777',
                    fontFamily: 'Helvetica Neue',
                    fontSize: '17px',
                },
            },
        },
        series: [{
            name: Resources.Visit_count,
            showInLegend: false,
            data: data_array,
            color: '#8adbdb',
        }]
    });
}

function Create_bootom_charts(date, Users_data_list, i) {

    //==========all charts==========//
    var employee_name = Users_data_list[0].Employee_name;
    var is_needed_to_create_doughnut_chart = false;

    var doughnut_label_array = [];
    var doughnut_data_array = [];

    var horizontal_label_array = [];
    var horizontal_data_array = [];

    for (var item in Users_data_list) {

        //==========doughnut chart==========//
        if (Users_data_list[item].Visit_count != 0) {

            doughnut_label_array.push(Users_data_list[item].Visit_type);
            doughnut_data_array.push(Users_data_list[item].Visit_count);
            is_needed_to_create_doughnut_chart = true;
        }

        //==========horizontal chart==========//
        if (Users_data_list[item].Visit_type_total_price != 0) {

            horizontal_label_array.push('');
            horizontal_data_array.push(Users_data_list[item].Visit_type_total_price);
        }
    }

    //==========all charts==========//
    if (is_needed_to_create_doughnut_chart) {

        var doughnut_chart_id = 'doughnut_chart_' + i;
        var horizontal_chart_id = 'horizontal_chart_' + i;

        var cont = "<br/><hr />"
                    + "<div class='row'>"
                    + "<div class='col-md-5'>"
                    + "<h2>" + employee_name + "</h2>"
                    + "<br/>"
                    + "</div>"
                    + "<div class='col-md-7'>"
                    + "<h2>" + date + "</h2>"
                    + "<br/>"
                    + "</div>"
                    + "</div>"
                    + "<div class='row'>"
                    + "<div class='col-md-4'>"
                    + "<canvas id='" + doughnut_chart_id + "'height='250px'></canvas>"
                    + "</div>"
                    + "<div class='col-md-8'>"
                    + "<canvas id='" + horizontal_chart_id + "'height='100px'></canvas>"
                    + "</div>"
                    + "</div>";

        $("#bottom_charts").append(cont);


        //==========doughnut chart==========//
        var doughnut_ctx = $('#' + doughnut_chart_id)[0].getContext('2d');
        var doughnut_chart = new Chart(doughnut_ctx, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: doughnut_data_array,
                    backgroundColor: [
                             '#FF6384',
                             '#4BC0C0',
                             '#FFCE56',
                             '#E7E9ED',
                             '#36A2EB',
                             '#3e95cd',
                             '#8e5ea2',
                             '#3cba9f',
                             '#e8c3b9',
                             '#c45850',
                    ],
                }],
                labels: doughnut_label_array,
            },
            options: {
                events: false,
                legend: {
                    display: true,
                    position: 'left',
                },
                title: {
                    text: Resources.Visit_count,
                    display: true,
                },
                animation: {
                    duration: 2000,
                    easing: 'easeOutQuart',
                    onComplete: function () {
                        var ctx = this.chart.ctx;
                        ctx.font = Chart.helpers.fontString(Chart.defaults.global.defaultFontFamily,
                            'normal', Chart.defaults.global.defaultFontFamily);
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';

                        this.data.datasets.forEach(function (dataset) {

                            for (var i = 0; i < dataset.data.length; i++) {
                                var model = dataset._meta[Object.keys(dataset._meta)[0]].data[i]._model,
                                    total = dataset._meta[Object.keys(dataset._meta)[0]].total,
                                    mid_radius = model.innerRadius + (model.outerRadius - model.innerRadius) / 2,
                                    start_angle = model.startAngle,
                                    end_angle = model.endAngle,
                                    mid_angle = start_angle + (end_angle - start_angle) / 2;

                                var x = mid_radius * Math.cos(mid_angle);
                                var y = mid_radius * Math.sin(mid_angle);

                                ctx.fillStyle = '#fff';
                                if (i == 3) { // Darker text color for lighter background
                                    ctx.fillStyle = '#444';
                                }
                                var percent = String(Math.round(dataset.data[i] / total * 100)) + '%';
                                ctx.fillText(dataset.data[i], model.x + x, model.y + y);
                                // Display percent in another line, line break doesn't work for fillText
                                ctx.fillText(percent, model.x + x, model.y + y + 15);
                            }
                        });
                    }
                }
            },
        });
        //==========END doughnut chart==========//

        //==========horizontal chart==========//
        var horizontal_ctx = $('#' + horizontal_chart_id)[0].getContext('2d');
        var horizontal_chart = new Chart(horizontal_ctx, {
            type: 'horizontalBar',
            responsive: true,
            label: Resources.Total_visit_cost,
            data: {
                labels: horizontal_label_array,
                datasets: [{
                    data: horizontal_data_array,
                    backgroundColor: [
                         '#FF6384',
                         '#4BC0C0',
                         '#FFCE56',
                         '#E7E9ED',
                         '#36A2EB',
                         '#3e95cd',
                         '#8e5ea2',
                         '#3cba9f',
                         '#e8c3b9',
                         '#c45850',
                    ],
                    borderColor: [
                         '#FF6384',
                         '#4BC0C0',
                         '#FFCE56',
                         '#E7E9ED',
                         '#36A2EB',
                         '#3e95cd',
                         '#8e5ea2',
                         '#3cba9f',
                         '#e8c3b9',
                         '#c45850',
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }]
                },
                scales: {
                    xAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }]
                },
                animation: {
                    duration: 2000,
                    easing: 'easeOutQuart',
                },
                title: {
                    text: Resources.Total_visit_cost,
                    display: true,
                },
                legend: {
                    display: false
                },
                tooltips: {
                    callbacks:
                        {
                            label: function (tooltipItem, data) {

                                return tooltipItem.xLabel + ' zł';
                            }
                        }
                },
            }
        });
    }
    else { // If nothing to display

        var cont = "<br/><hr />"
                    + "<div class='row'>"
                    + "<div class='col-md-12'>"
                    + "<h2>" + employee_name + "</h2>"
                    + "</div>"
                    + "</row>"
                    + "<row>"
                    + "<div class='col-md-12'>"
                    + "<h3>" + Resources.No_data_to_display + "</h3>"
                    + "</div>"
                    + "</div>";

        $("#bottom_charts").append(cont);
    }
}

function Add_new_employee() {

    $.ajax({
        type: 'POST',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: JSON.stringify(
            {
                name: $('#add_employee_name').val(),
                surname: $('#add_employee_surname').val(),
                password: $('#add_employee_password').val()
            }),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Add_new_employee',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                if (result.If_action_successed) {

                    Get_data_to_bottom_charts();
                    $('#modal_add_employee').modal('hide');
                }
                Display_results(result);
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Add_new_visit_type() {

    $.ajax({
        type: 'POST',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        data: JSON.stringify
            ({
                name: $('#add_visit_type_name').val()
            }),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Add_new_visit_type',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                if (result.If_action_successed) {

                    $('#modal_add_visit_type').modal('hide');
                }
                Display_results(result);
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        },
        error: function () {
            alert(Error);
        }
    });
}

function Edit_employee() {

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: Resources.Confirm_edit_employee,
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'btn-danger',
        confirmButtonText: Resources.Alert_btn_yes_change,
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
                data: JSON.stringify
                    ({
                        id: $('#btn_remove_employee').val(),
                        name: $('#edit_employee_name').val(),
                        surname: $('#edit_employee_surname').val(),
                    }),
                dataType: 'json',
                contentType: 'application/json; charset=utf-8',
                url: '/Visit/Edit_employee',
                success: function (result) {

                    $('html, body').css('overflowY', 'auto');

                    if (result.If_session_expired) {

                        Log_out_if_session_exp();
                    }
                    else {

                        if (result.If_action_successed) {

                            Get_data_to_bottom_charts();
                            $('#modal_edit_employee').modal('hide');
                        }
                        Display_results(result);
                    }
                },
                complete: function () {

                    $('#ajax-loader').css('visibility', 'hidden');
                }
            });

        } else {

            sweetAlert('', Resources.Warning_edit_employee_cancel, 'error');
        }
    });
}

function Edit_visit_type() {

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: Resources.Confirm_edit_visit_type,
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'btn-danger',
        confirmButtonText: Resources.Alert_btn_yes_change,
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
                data: JSON.stringify
                    ({
                        id: $('#btn_remove_visit_type').val(),
                        name: $('#edit_visit_type_name').val()
                    }),
                dataType: 'json',
                contentType: 'application/json; charset=utf-8',
                url: '/Visit/Edit_visit_type',
                success: function (result) {

                    $('html, body').css('overflowY', 'auto');

                    if (result.If_session_expired) {

                        Log_out_if_session_exp();
                    }
                    else {

                        if (result.If_action_successed) {

                            $('#modal_edit_visit_type').modal('hide');
                        }
                        Display_results(result);
                    }
                },
                complete: function () {

                    $('#ajax-loader').css('visibility', 'hidden');
                }
            });

        } else {

            sweetAlert('', Resources.Warning_edit_visit_type_cancel, 'error');
        }
    });
}

function Remove_employee(id) {

    var alert_text = '';
    var if_removing_logged_user = false;

    if (logged_user_id == id) { // If removing user is logged user

        alert_text = Resources.Confirm_remove_employee_logg;
        if_removing_logged_user = true;
    }
    else {

        alert_text = Resources.Confirm_remove_employee;
    }

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: alert_text,
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
                 data: JSON.stringify
                     ({
                         id: id,
                     }),
                 dataType: 'json',
                 contentType: 'application/json; charset=utf-8',
                 url: '/Visit/Remove_employee',
                 success: function (result) {

                     $('html, body').css('overflowY', 'auto');

                     if (result.If_session_expired) {

                         Log_out_if_session_exp();
                     }
                     else {

                         if (result.If_action_successed) {

                             if (if_removing_logged_user) {

                                 swal({
                                     title: Resources.Employee_removed,
                                     text: Resources.Employee_removed_login_again,
                                     type: 'success',
                                     showCancelButton: false,
                                 }, function () {

                                     window.location.href = '/Employee/Log_out';
                                 });
                             }
                             else {

                                 Get_data_to_top_chart();
                                 Get_data_to_bottom_charts();
                                 $('#modal_edit_employee').modal('hide');
                             }
                         }
                         Display_results(result);
                     }
                 },
                 complete: function () {

                     $('#ajax-loader').css('visibility', 'hidden');
                 }
             });

         } else {

             sweetAlert('', Resources.Warning_remove_employee_cancel, 'error');
         }
     });
}

function Remove_visit_type(id) {

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: Resources.Confirm_remove_visit_type,
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
                data: JSON.stringify
                    ({
                        id: id,
                    }),
                dataType: 'json',
                contentType: 'application/json; charset=utf-8',
                url: '/Visit/Remove_visit_type',
                success: function (result) {

                    $('html, body').css('overflowY', 'auto');

                    if (result.If_session_expired) {

                        Log_out_if_session_exp();
                    }
                    else {

                        if (result.If_action_successed) {

                            $('#modal_edit_visit_type').modal('hide');
                        }
                        Display_results(result);
                    }
                },
                complete: function () {

                    $('#ajax-loader').css('visibility', 'hidden');
                }
            });

        } else {

            sweetAlert('', Resources.Warning_remove_visit_type_cancel, 'error');
        }
    });
}

function Change_password() {

    swal({
        title: Resources.Confirm_title_are_you_sure,
        text: Resources.Confirm_change_password,
        type: 'warning',
        showCancelButton: true,
        confirmButtonClass: 'btn-danger',
        confirmButtonText: Resources.Alert_btn_yes_change,
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
                 data: JSON.stringify
                     ({
                         old_pass: $('#change_old_password').val(),
                         new_pass: $('#change_new_password').val()
                     }),
                 dataType: 'json',
                 contentType: 'application/json; charset=utf-8',
                 url: '/Visit/Change_password',
                 success: function (result) {

                     $('html, body').css('overflowY', 'auto');

                     if (result.If_session_expired) {

                         Log_out_if_session_exp();
                     }
                     else {

                         if (result.If_action_successed) {

                             swal({
                                 title: Resources.Alert_success,
                                 text: Resources.Warning_password_changed,
                                 type: 'success',
                                 showCancelButton: false
                             }, function () {

                                 $('#modal_change_password').modal('hide');
                                 window.location.href = '/Employee/Log_out';
                             });
                         }
                         else {

                             Display_results(result);
                         }
                     }
                 },
                 complete: function () {

                     $('#ajax-loader').css('visibility', 'hidden');
                 }
             });
         } else {

             sweetAlert('', Resources.Warning_change_password_cancel, 'error');
         }
     });
}

function Get_users_to_ddl() {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_users_to_ddl',
        success: function (result) {

            var select_tag = $('#edit_ddl_employee_id')[0]; // Return DOM object
            select_tag.innerHTML = '';

            var cont = '';
            cont += "<option value='0'>" + Resources.Select_doctor + "</option>";

            for (var i in result) {

                cont += "<option value='" + result[i].id + "'>" + result[i].name + " " + result[i].surname + "</option>";
            }
            select_tag.innerHTML = cont;
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Get_visit_type_to_ddl() {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Visit/Get_visit_type_to_ddl',
        success: function (result) {

            var select_tag = $('#edit_ddl_visit_type_id')[0];
            select_tag.innerHTML = '';

            var cont = '';
            cont += "<option value='0'>" + Resources.Select_visit_type + "</option>";

            for (var i in result) {

                cont += "<option value='" + result[i].id + "'>" + result[i].name + "</option>";
            }
            select_tag.innerHTML = cont;
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Set_employee_data_to_inputs(id) {

    if (id != 0) {

        $.ajax({
            type: 'GET',
            beforeSend: function () {

                $('#ajax-loader').css('visibility', 'visible');
            },
            data: {

                id: id,
            },
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            url: '/Visit/Set_employee_data_to_inputs',
            success: function (result) {

                if (result.If_session_expired) {

                    Log_out_if_session_exp();
                }
                else {

                    if (result.If_action_successed) {

                        $('#edit_employee_name').val(result.Employee.name);
                        $('#edit_employee_surname').val(result.Employee.surname);
                        $('#btn_remove_employee').val(result.Employee.id);
                        $('#edit_employee_name').prop('disabled', false);
                        $('#edit_employee_surname').prop('disabled', false);

                        if (logged_user_id == 1) // If user is Admin
                        {
                            $('#btn_remove_employee').prop('disabled', false);
                            $('#btn_edit_employee').prop('disabled', false);
                        }
                        else {
                            $('#btn_remove_employee').prop('disabled', true);
                            $('#btn_edit_employee').prop('disabled', true);
                        }
                    }
                    else {

                        Display_results(result);
                    }
                }
            },
            complete: function () {

                $('#ajax-loader').css('visibility', 'hidden');
            }
        });
    }
    else {

        Clear_inputs('modal_edit_employee');
    }
}

function Set_visit_type_to_input(id) {

    if (id != 0) {

        $.ajax({
            type: 'GET',
            beforeSend: function () {

                $('#ajax-loader').css('visibility', 'visible');
            },
            data: {

                id: id,
            },
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            url: '/Visit/Set_visit_type_to_input',
            success: function (result) {

                if (result.If_session_expired) {

                    Log_out_if_session_exp();
                }
                else {

                    if (result.If_action_successed) {

                        $('#edit_visit_type_name').val(result.Visit_type.name);
                        $('#btn_remove_visit_type').val(result.Visit_type.id);
                        $('#edit_visit_type_name').prop('disabled', false);
                        $('#btn_remove_visit_type').prop('disabled', false);
                        $('#btn_edit_visit_type').prop('disabled', false);
                    }
                    else {

                        Display_results(result);
                    }
                }
            },
            complete: function () {

                $('#ajax-loader').css('visibility', 'hidden');
            }
        });
    }
    else {

        Clear_inputs('modal_edit_visit_type');
    }
}

function Set_daterangepicker() {

    $('input[name="daterange"]').daterangepicker(
           {
               startDate: moment().subtract(1, 'months'),
               endDate: moment(),
               locale: {
                   'format': 'DD.MM.YYYY',
                   'firstDay': 1,
                   'separator': ' - ',
                   'applyLabel': Resources.Confirm,
                   'cancelLabel': Resources.Cancel,
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
           }
           );
}

function Convert_string_to_date(date) {

    var monthNames = [
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
    ];
    var range_date = date.replace(/\s/g, '').split('-');

    var start_date_array = range_date[0].split('.');
    var end_date_array = range_date[1].split('.');

    var start_date = start_date_array[0] + '&nbsp;' + monthNames[start_date_array[1] - 1] + '&nbsp;' + start_date_array[2];
    var end_date = end_date_array[0] + '&nbsp;' + monthNames[end_date_array[1] - 1] + '&nbsp;' + end_date_array[2];

    return start_date + ' - ' + end_date;
}

function Clear_inputs(e) {

    var elem = $('#' + e)[0];

    $(elem)
      .find('input')
        .val('')
        .end()
      .find('select')
        .val(0)
        .end()

    if (e == 'modal_edit_employee' || e == 'modal_edit_visit_type') {

        $(elem)
         .find('input')
           .prop('disabled', true)
           .end()
         .find('button')
           .prop('disabled', true)
           .end()
    }
}
