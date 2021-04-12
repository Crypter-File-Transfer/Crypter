// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/* Detect Features */
var div = document.createElement('div');

var isAdvancedUpload = function () {
    var div = document.createElement('div');
    return (('draggable' in div) || ('ondragstart' in div && 'ondrop' in div)) && 'FormData' in window && 'FileReader' in window;
}();

if (isAdvancedUpload) {
    var $form = $('.box');
    $form.addClass('has-advanced-upload');
}

if (isAdvancedUpload) {

    var droppedFiles = false;

    $form.on('drag dragstart dragend dragover dragenter dragleave drop', function (e) {
        e.preventDefault();
        e.stopPropagation();
    })
        .on('dragover dragenter', function () {
            $form.addClass('is-dragover');
        })
        .on('dragleave dragend drop', function () {
            $form.removeClass('is-dragover');
        })
        .on('drop', function (e) {
            droppedFiles = e.originalEvent.dataTransfer.files;
        });

}