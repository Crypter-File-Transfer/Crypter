﻿window.Crypter = {
   CopyToClipboard: function (text) {
      const animationTiming = {
         duration: 500,
         iterations: 1
      };

      navigator.clipboard.writeText(text).then(() => {
         const tooltip = document.querySelector('.toolTipText');
         tooltip.style.display = 'block'
         tooltip.animate([{ opacity: 0 }, { opacity: 1 }], animationTiming)
            .onfinish = (e) => {
               tooltip.style.opacity = 1;
               setTimeout(() => {
                  document.querySelector('.toolTipText').animate([{ opacity: 1 }, { opacity: 0 }], animationTiming)
                     .onfinish = (e) => {
                        document.querySelector('.toolTipText').style.display = 'none';
                     }
               }, 500);
            }
      });
   },

   SetActivePage: function (page) {
      let pages = document.getElementsByClassName('page');
      for (let i = 0; i < pages.length; i++) {
         if (pages[i].textContent === page) {
            pages[i].classList.add('active');
         }
      }
   },

   SetPageTitle: function (title) {
      document.title = title;
   },

   CollapseNavBar: function () {
      let nav = document.getElementById('mainNavigation');
      nav.classList.remove('show');
    },

    InitVersionPopover: function () {
        const infoBtn = document.getElementById('info-icon');
        if (infoBtn) {
            const popoverOptions = {
                content: document.getElementById('version-content'),
                html: true,
                placement: 'top',
                trigger: 'click focus'
            };
            new bootstrap.Popover(infoBtn, popoverOptions);
        }
    }
}

