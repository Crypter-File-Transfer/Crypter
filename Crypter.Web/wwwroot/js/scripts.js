function EditAccountDetails() {
   var fields = [
      document.getElementById("aliasFormControl"),
      document.getElementById("appearPublicly"),
      document.getElementById("acceptAnonymousMessages"),
      document.getElementById("acceptAnonymousFiles")
   ];

   for (var i in fields) {
      fields[i].readOnly = false;
      fields[i].disabled = false;
   }
}

function SaveAccountDetails() {
   var fields = [
      document.getElementById("aliasFormControl"),
      document.getElementById("appearPublicly"),
      document.getElementById("acceptAnonymousMessages"),
      document.getElementById("acceptAnonymousFiles")
   ];

   for (var i in fields) {
      fields[i].readOnly = true;
      fields[i].disabled = true;
   }
}

function copyToClipboard(text) {
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
   });
}

function setActivePage(page) {
   var pages = document.getElementsByClassName('page');
   for (var i = 0; i < pages.length; i++) {
      if (pages[i].textContent == page) {
         pages[i].classList.add('active');
      }
   }
}

function setPageUrl(urlString) {
   window.history.pushState(null, '', urlString);
}

function setPageTitle(title) {
   document.title = title;
}