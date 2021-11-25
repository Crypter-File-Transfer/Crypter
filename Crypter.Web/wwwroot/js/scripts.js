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

   SetActivePage: function(page) {
      var pages = document.getElementsByClassName('page');
      for (var i = 0; i < pages.length; i++) {
         if (pages[i].textContent == page) {
            pages[i].classList.add('active');
         }
      }
   },

   SetPageUrl: function(urlString) {
      window.history.pushState(null, '', urlString);
   },

   SetPageTitle: function(title) {
      document.title = title;
   },

   CollapseNavBar: function() {
      var nav = document.getElementById('mainNavigation');
      nav.classList.remove('show');
   }
} 
