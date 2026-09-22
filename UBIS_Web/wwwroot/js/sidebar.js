window.UBISIncludes = window.UBISIncludes || {};

(function (includes) {
  const escapeHtml = (value) => String(value ?? '').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
  const root = window.UBISBasePath || '';
  const resolveHref = href => !href || href === '#' || /^(https?:|\/)/.test(href) ? href : root + href;
  const normalizedPath = value => String(value || '').replace(/\\/g, '/').replace(/^\.\//, '').replace(/\/+/g, '/');
  const currentPath = normalizedPath(window.location.pathname);
  const isActiveHref = href => href && href !== '#' && currentPath.endsWith(normalizedPath(href));
  const itemActive = item => isActiveHref(item.href) || (item.children || []).some(itemActive);
  const idFor = (label, trail) => `nav-${[...trail, label].join('-').replace(/[^a-z0-9]+/gi, '-').toLowerCase()}`;

  function renderItems(items, level = 0, trail = []) {
    return items.map(item => renderItem(item, level, trail)).join('');
  }

  function renderItem(item, level, trail) {
    const children = Array.isArray(item.children) ? item.children : [];
    const hasChildren = children.length > 0;
    const active = itemActive(item);
    const expanded = hasChildren && active;
    const id = idFor(item.label, trail);
    const levelClass = `nav-link--level-${Math.min(level, 3)}`;
    const activeClass = active ? 'bg-ubis-tealLight font-semibold text-ubis-navy ring-1 ring-inset ring-teal-100' : 'text-slate-600 hover:bg-slate-50';
    const icon = level === 0 ? `<i data-lucide="${escapeHtml(item.icon || 'circle')}" class="h-4 w-4 shrink-0 ${active ? 'text-ubis-teal' : ''}"></i>` : `<span class="nav-dot ${active ? 'nav-dot--active' : 'nav-dot--inactive'}"></span>`;

    if (!hasChildren) return `<a href="${escapeHtml(resolveHref(item.href || '#'))}" class="nav-link ${levelClass} relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-[13px] ${activeClass}">${icon}<span class="sidebar-label">${escapeHtml(item.label)}</span></a>`;

    return `<div class="nav-group" data-nav-level="${level}">
      <div class="inline-row inline-row--tight">
        <a href="${escapeHtml(resolveHref(item.href || '#'))}" class="nav-link ${levelClass} relative flex min-w-0 flex-1 items-center gap-3 rounded-xl px-3 py-2.5 text-[13px] ${activeClass}">${icon}<span class="sidebar-label sidebar-label--truncate">${escapeHtml(item.label)}</span></a>
        <button type="button" data-nav-toggle aria-label="Toggle ${escapeHtml(item.label)} submenu" aria-controls="${id}" aria-expanded="${expanded}" class="nav-submenu-toggle"><i data-lucide="chevron-down" class="sidebar-chevron h-3.5 w-3.5 transition-transform duration-300 ${expanded ? 'rotate-180' : ''}"></i></button>
      </div>
      <div id="${id}" class="nav-children-wrap ${expanded ? 'open' : ''}" ${expanded ? '' : 'inert'}><div class="nav-children-inner nav-children-panel">${renderItems(children, level + 1, [...trail, item.label])}</div></div>
    </div>`;
  }

  includes.sidebar = function () {
    return `<aside id="sidebar" aria-label="Primary navigation" class="sidebar fixed bottom-0 left-0 top-1 z-40 w-[238px] border-r border-slate-200 bg-white transition-all duration-300 lg:translate-x-0">
      <div class="sidebar-brand"><div class="inline-row inline-row--medium"><img src="${root}image/emblem-dark.png" alt="Government of India" width="30" height="30"><div class="brand-copy"><p class="text-[10px] font-bold uppercase tracking-[.16em] text-ubis-teal">Ministry of Finance</p><p class="mt-0.5 text-sm font-bold text-ubis-navy">Government of India</p></div></div></div>
      <nav aria-label="Main menu" class="sidebar-nav space-y-1 overflow-y-auto px-3 py-4"><p class="sidebar-label mb-2 px-3 text-[10px] font-bold uppercase tracking-[.15em] text-slate-400">Main Menu</p>${renderItems(window.UBISNavItems || [])}</nav>
    </aside>`;
  };

  document.addEventListener('click', event => {
    const toggle = event.target.closest('[data-nav-toggle]');
    if (!toggle) return;
    const group = toggle.closest('.nav-group');
    const wrap = group?.querySelector(':scope > .nav-children-wrap');
    if (!wrap) return;
    const level = group.dataset.navLevel;
    const opening = !wrap.classList.contains('open');
    document.querySelectorAll(`.nav-group[data-nav-level="${level}"]`).forEach(other => {
      if (other === group) return;
      const otherWrap = other.querySelector(':scope > .nav-children-wrap');
      const otherToggle = other.querySelector(':scope > div > [data-nav-toggle]');
      otherWrap?.classList.remove('open'); otherWrap?.setAttribute('inert', '');
      otherToggle?.setAttribute('aria-expanded', 'false');
      otherToggle?.querySelector('.sidebar-chevron')?.classList.remove('rotate-180');
    });
    wrap.classList.toggle('open', opening);
    opening ? wrap.removeAttribute('inert') : wrap.setAttribute('inert', '');
    toggle.setAttribute('aria-expanded', String(opening));
    toggle.querySelector('.sidebar-chevron')?.classList.toggle('rotate-180', opening);
  });

  document.addEventListener('pointerover', event => {
    if (!document.body.classList.contains('nav-collapsed')) return;
    const group = event.target.closest('.nav-group');
    const wrap = group?.querySelector(':scope > .nav-children-wrap');
    if (wrap && !wrap.classList.contains('open')) {
      wrap.removeAttribute('inert');
      wrap.dataset.hoverOpen = 'true';
    }
  });

  document.addEventListener('pointerout', event => {
    const group = event.target.closest('.nav-group');
    if (!group || group.contains(event.relatedTarget)) return;
    const wrap = group.querySelector(':scope > .nav-children-wrap[data-hover-open="true"]');
    if (wrap && !wrap.classList.contains('open')) {
      wrap.setAttribute('inert', '');
      delete wrap.dataset.hoverOpen;
    }
  });
})(window.UBISIncludes);
