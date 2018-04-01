function Go_to_view(view_name) {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Employee/Return_true_if_user_logged',

        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                window.location.href = '/Visit/' + view_name;
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Change_lang(lang) {

    $.ajax({
        type: 'GET',
        beforeSend: function () {

            $('#ajax-loader').css('visibility', 'visible');
        },
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        url: '/Employee/Return_true_if_user_logged',
        success: function (result) {

            if (result.If_session_expired) {

                Log_out_if_session_exp();
            }
            else {

                window.location.href = '/Language/Set?lang=' + lang;
            }
        },
        complete: function () {

            $('#ajax-loader').css('visibility', 'hidden');
        }
    });
}

function Display_results(result) {

    var $elm = '';
    var cont = '';
    var margin_top = 0;

    for (var i = 0; i < result.List_of_results.length; i++) {

        if (result.List_of_results[i].Result) { // If info from db is success

            cont += "<div class='alert alert-success' role='alert' style='margin-top:" + margin_top + "px'>"
                        + "<span class='glyphicon glyphicon-ok'></span> <b>" + Resources.Alert_success + " </b>"
                        + result.List_of_results[i].Content
                        + "</div>";
        }
        else { // If info from db is error

            cont += "<div class='alert alert-danger' role='alert' style='margin-top:" + margin_top + "px'>"
                          + "<span class='glyphicon glyphicon-ban-circle'></span> <b>" + Resources.Alert_danger + " </b>"
                              + result.List_of_results[i].Content
                              + "</div>";
        }

        margin_top += 50;
    }
    $elm = $(cont.toString());
    $('#result_container').prepend($elm);

    setTimeout(function () { // Hide <div> after time and then remove it
        $elm.fadeOut(1000, function () {
            $(this).remove();
        });
    }, 4000);
}

function Log_out_if_session_exp() {

    $('#ajax-loader').css('visibility', 'hidden'); // Hide ajax-loader which is shown in ajax processing

    swal({
        title: Resources.Session_expired,
        text: Resources.Session_expired_sign_in_again,
        type: 'warning',
        showCancelButton: false,
    }, function () {

        window.location.href = '/Employee/Login';
    });
}