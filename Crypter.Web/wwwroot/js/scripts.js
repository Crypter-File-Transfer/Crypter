window.Crypter = {
   CopyToClipboard: function (text) {
      navigator.clipboard.writeText(text).then(() => {
         $('.copiedToolTip').css('display', 'block');
         $('.copiedToolTip').animate({
            opacity: 1
         }, 500, function () {
            $('.copiedToolTip').delay(500).animate({
               opacity: 0
            }, 500, function () {
               $('.copiedToolTip').css('display', 'none');
            });
         });
      }).catch((error) => {
         console.log(error);
      })
   },

   SetActivePage: function (page) {
      var pages = document.getElementsByClassName('page');
      for (var i = 0; i < pages.length; i++) {
         if (pages[i].textContent == page) {
            pages[i].classList.add('active');
         }
      }
   },

   SetPageUrl: function (urlString) {
      window.history.pushState(null, '', urlString);
   },

   SetPageTitle: function (title) {
      document.title = title;
   },

   CollapseNavBar: function () {
      var nav = document.getElementById('mainNavigation');
      nav.classList.remove('show');
   },

   DownloadFile: {
      _downloadFileBuffer: new Array(),
      _downloadFileBufferIndex: 0,
      _downloadBlob: null,
      _downloadLink: null,
      _downloadUrl: "",

      InitializeBuffer: function (size) {
         this._downloadFileBuffer = new Array(size);
         this._downloadFileBufferIndex = 0;
      },

      ClearBuffer: function () {
         this._downloadFileBuffer = new Array();
         this._downloadFileBufferIndex = 0;
      },

      InsertBuffer: function (bytes) {
         this._downloadFileBuffer[this._downloadFileBufferIndex] = bytes;
         this._downloadFileBufferIndex++;
      },

      Download: function (fileName, contentType) {
         this._downloadBlob = new Blob(this._downloadFileBuffer, { type: contentType });
         this.ClearBuffer();

         this._downloadLink = document.createElement('a');
         this._downloadLink.download = fileName;
         this._downloadLink.target = '_self';
         this._downloadLink.style.display = 'none';

         this._downloadUrl = URL.createObjectURL(this._downloadBlob);
         this._downloadLink.href = this._downloadUrl;
         document.body.appendChild(this._downloadLink);
         this._downloadLink.click();
      },

      ResetDownload: function () {
         try {
            URL.revokeObjectURL(this._downloadUrl);
            document.body.removeChild(this._downloadLink);
         } catch { }
         this._downloadBlob = null;
         this._downloadLink = null;
         this._downloadUrl = "";
      }
   }
}
