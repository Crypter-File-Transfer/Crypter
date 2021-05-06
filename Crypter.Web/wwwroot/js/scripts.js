function EditAccountDetails() {
    var fields = document.getElementById("DetailsForm").elements;

    for (var i in fields) {
        fields[i].readOnly = false;
        fields[i].disabled = false;
    }
}

function SaveAccountDetails() {
    var fields = document.getElementsByTagName('input');

    for (var i in fields) {
        fields[i].readOnly = true;
        fields[i].disabled = true;
    }
}