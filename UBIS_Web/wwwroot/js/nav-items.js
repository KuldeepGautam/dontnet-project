// Central three-level navigation used by every root and nested module page.
const moduleNav = (label, slug, icon, child = 'Overview') => ({
  label, icon, href: `modules/${slug}/index.html`, children: [{
    label: child, href: `modules/${slug}/${slug}-overview/index.html`, children: [
      { label: `${child} Details`, href: `modules/${slug}/${slug}-overview/details.html` }
    ]
  }]
});

window.UBISNavItems = [
  moduleNav('Dashboard', 'dashboard', 'layout-dashboard'),
  { label: 'Pre-Budget Meeting', icon: 'calendar-days', href: 'modules/pre-budget-meeting/index.html', children: [
    { label: 'Pre-Budget Meeting', href: 'UBIS_Drawer.html', children: [{ label: 'Meeting Details', href: 'modules/pre-budget-meeting/pre-budget-meeting/details.html' }] },
    { label: 'Autonomous Master/Grantee Bodies', href: 'autonomous-master-grantee-bodies.html', children: [{ label: 'Body Details', href: 'modules/pre-budget-meeting/autonomous-bodies/details.html' }] }
  ]},
  moduleNav('DDG', 'ddg', 'users-round'),
  { label: 'Pre-Budget Meeting', icon: 'calendar-days', href: 'modules/pre-budget-meeting/index.html', children: [
    { label: 'Pre-Budget Meeting', href: 'UBIS_Drawer.html', children: [{ label: 'Meeting Details', href: 'modules/pre-budget-meeting/pre-budget-meeting/details.html' }] },
    { label: 'Autonomous Master/Grantee Bodies', href: 'autonomous-master-grantee-bodies.html', children: [{ label: 'Body Details', href: 'modules/pre-budget-meeting/autonomous-bodies/details.html' }] }
  ]},  
  moduleNav('ECL', 'ecl', 'settings-2'),
  moduleNav('Exp Budget (SBE)', 'exp-budget-sbe', 'indian-rupee'),
  moduleNav('Exp Profile', 'exp-profile', 'chart-column'),
  moduleNav('Supplementary Budget', 'supplementary-budget', 'file-plus-2'),
  moduleNav('Receipt Budget', 'receipt-budget', 'receipt'),
  moduleNav('Reappropriation', 'reappropriation', 'arrow-left-right'),
  moduleNav('Autonomous/Grantee Bodies', 'autonomous-grantee-bodies', 'building-2'),
  moduleNav('Contingency Advance', 'contingency-advance', 'shield-check')
];
