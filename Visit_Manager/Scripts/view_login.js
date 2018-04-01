function Remove_html() { // Prevent from insert html tag to input

    var login = $('#tbx_login').val();
    login = login.replace(/</g, "").replace(/>/g, "");
    $("#tbx_login").val(login);

    var password = $('#tbx_password').val();
    password = password.replace(/</g, "").replace(/>/g, "");
    $("#tbx_password").val(password);
}