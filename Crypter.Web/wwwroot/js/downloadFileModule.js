var _downloadBlob = null;
var _downloadLink = null;
var _downloadUrl = "";
var _fileName = "";

export function createBlob(fileName, contentType, fileBytes) {
   _fileName = fileName;
   _downloadBlob = new Blob([fileBytes.slice()], { type: contentType });
}

export function download() {
   _downloadLink = document.createElement('a');
   _downloadLink.download = _fileName;
   _downloadLink.target = '_self';
   _downloadLink.style.display = 'none';

   _downloadUrl = URL.createObjectURL(_downloadBlob);
   _downloadLink.href = _downloadUrl;
   document.body.appendChild(_downloadLink);
   _downloadLink.click();
}

export function reset() {
   try {
      URL.revokeObjectURL(_downloadUrl);
      document.body.removeChild(_downloadLink);
   } catch { }
   _downloadBlob = null;
   _downloadLink = null;
   _downloadUrl = "";
}
