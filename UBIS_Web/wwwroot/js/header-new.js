// includes.header = function () {
//     return markup;
//   };

(function () {
  let fontScale = Number(localStorage.getItem('ubis-font-scale') || 100);

  function applyFontScale() {
    fontScale = Math.max(90, Math.min(120, fontScale));
    document.documentElement.style.fontSize = `${fontScale}%`;
    localStorage.setItem('ubis-font-scale', String(fontScale));
  }

  function closeHeaderMenus(exceptId) {
    ['accessibilityMenu', 'profileMenu'].forEach((id) => {
      if (id === exceptId) return;
      document.getElementById(id)?.classList.add('is-hidden');
      const buttonId = id === 'profileMenu' ? 'profileButton' : 'accessibilityButton';
      document.getElementById(buttonId)?.setAttribute('aria-expanded', 'false');
    });
  }

  function toggleMenu(button, menu) {
    const opening = menu.classList.contains('is-hidden');
    closeHeaderMenus(opening ? menu.id : null);
    menu.classList.toggle('is-hidden', !opening);
    button.setAttribute('aria-expanded', String(opening));
    if (opening) menu.querySelector('[role="menuitem"]')?.focus();
  }

  applyFontScale();
  if (localStorage.getItem('ubis-high-contrast') === 'true') document.documentElement.classList.add('high-contrast');
  if (localStorage.getItem('ubis-dark-mode') === 'true') document.documentElement.classList.add('dark-mode');

  document.addEventListener('click', (event) => {
    const profileButton = event.target.closest('#profileButton');
    const accessibilityButton = event.target.closest('#accessibilityButton');
    if (profileButton) { toggleMenu(profileButton, document.getElementById('profileMenu')); return; }
    if (accessibilityButton) { toggleMenu(accessibilityButton, document.getElementById('accessibilityMenu')); return; }

    const fontAction = event.target.closest('[data-font-action]')?.dataset.fontAction;
    if (fontAction) {
      fontScale = fontAction === 'increase' ? fontScale + 10 : fontAction === 'decrease' ? fontScale - 10 : 100;
      applyFontScale();
      return;
    }
    if (event.target.closest('[data-contrast-toggle]')) {
      const enabled = document.documentElement.classList.toggle('high-contrast');
      localStorage.setItem('ubis-high-contrast', String(enabled));
      return;
    }
    if (event.target.closest('[data-dark-toggle]')) {
      const enabled = document.documentElement.classList.toggle('dark-mode');
      localStorage.setItem('ubis-dark-mode', String(enabled));
      const status = document.querySelector('[data-dark-status]');
      if (status) status.textContent = enabled ? 'On' : 'Off';
      return;
    }
    if (!event.target.closest('.header-menu')) closeHeaderMenus();
  });

  document.addEventListener('DOMContentLoaded', () => {
    const status = document.querySelector('[data-dark-status]');
    if (status) status.textContent = document.documentElement.classList.contains('dark-mode') ? 'On' : 'Off';
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      const openMenu = document.querySelector('.header-menu:not(.is-hidden)');
      if (openMenu) {
        const button = document.getElementById(openMenu.id === 'profileMenu' ? 'profileButton' : 'accessibilityButton');
        closeHeaderMenus();
        button?.focus();
      } else if (document.getElementById('sidebar')?.classList.contains('mobile-open')) toggleMobileNav();
    }
    const menu = event.target.closest('[role="menu"]');
    if (!menu || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const items = [...menu.querySelectorAll('[role="menuitem"]')];
    const current = items.indexOf(document.activeElement);
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : event.key === 'ArrowDown' ? (current + 1) % items.length : (current - 1 + items.length) % items.length;
    items[next]?.focus();
  });
})();