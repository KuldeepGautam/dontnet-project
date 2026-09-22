// Shared desktop-collapse / mobile-drawer toggle behavior for every dummy
// page. UBIS_Drawer.html defines its own equivalents inside assets/app.js,
// so this file is only included on the dummy pages.

function toggleSidebar() {
  const collapsed = document.body.classList.toggle('nav-collapsed');
  const button = document.getElementById('sidebarToggle');
  button?.setAttribute('aria-expanded', String(!collapsed));
  button?.setAttribute('aria-label', collapsed ? 'Expand sidebar' : 'Collapse sidebar');
  if (button) {
    button.innerHTML = `<i data-lucide="${collapsed ? 'panel-left-open' : 'panel-left-close'}" class="h-4 w-4"></i>`;
    window.lucide?.createIcons();
  }
}

function toggleMobileNav() {
  const sidebar = document.getElementById('sidebar');
  const overlay = document.getElementById('mobileOverlay');
  if (!sidebar || !overlay) return;
  const isOpen = sidebar.classList.toggle('mobile-open');
  overlay.classList.toggle('is-hidden', !isOpen);
  document.body.classList.toggle('drawer-page-locked', isOpen);
  const button = document.getElementById('mobileNavToggle');
  button?.setAttribute('aria-expanded', String(isOpen));
  button?.setAttribute('aria-label', isOpen ? 'Close navigation menu' : 'Open navigation menu');
}
