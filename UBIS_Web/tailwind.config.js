// Production Tailwind build config (2026-07-22) - replaces the runtime "Play CDN" script
// (wwwroot/vendor/tailwind.3.4.17.js + wwwroot/js/tailwind-config.js /
// tailwind-config-login.js), which logs "cdn.tailwindcss.com should not be used in production"
// because it JIT-compiles utility classes in the browser on every page load instead of shipping
// a precompiled stylesheet. Regenerate wwwroot/css/tailwind.css after adding new pages/classes -
// see UBIS_Web/CLAUDE.md for the exact command.
//
// Theme extension merged from both of the old per-page configs (they only differed by the login
// page also defining animation keyframe names - animate-fade-in/animate-slide-up are used outside
// the login flow too, e.g. Views/User/Dashboard.cshtml, so this is now shared everywhere).
// Bug found 2026-09-07 ("focus ring still shows blue, not teal/green"): theme.extend.colors.teal
// used to be a plain string ("#06aeb8") - Tailwind's default `teal` is a whole 50-900 scale
// object, and extend.colors.teal being a bare string REPLACES that entire object rather than
// merging into it, so every numbered shade (teal-50, teal-100, ... - used all over this app,
// e.g. every focus:ring-teal-50) silently stopped being generatable, project-wide, the moment
// this line was added. Spreading Tailwind's own real teal scale back in and giving it a
// `DEFAULT` key (Tailwind's documented pattern for "bare `teal` resolves to our brand color, but
// every teal-NNN shade still works") fixes this without renaming the color or touching any of
// the many existing bg-teal/text-teal/border-teal usages across the app.
const tailwindColors = require("tailwindcss/colors");

module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Areas/**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        navy: "#063f66", teal: { ...tailwindColors.teal, DEFAULT: "#06aeb8" }, ambergov: "#f59e0b",
        // "Approved design for all appendixes" (2026-09-03, UBIS-One-Screen-html prototype) -
        // namespaced ubis-* palette so it can't collide with the existing navy/teal/ambergov
        // tokens above. First consumer: the redesigned Appendix VI-A drawer/grid.
        ubis: {
          navy: "#312E81", dark: "#1E1B4B", teal: "#0891B2", tealLight: "#ECFEFF",
          blue: "#4F46E5", bg: "#F6F7FB", border: "#DDE1EE", text: "#1E293B",
          muted: "#64748B", success: "#16A34A", warning: "#F59E0B", error: "#DC2626"
        }
      },
      boxShadow: {
        soft: "0 14px 35px rgba(6,63,102,.10)",
        panel: "0 10px 32px rgba(49,46,129,.08)",
        drawer: "-18px 0 55px rgba(30,27,75,.18)"
      },
      animation: { "fade-in": "fadeIn .35s ease both", "slide-up": "slideUp .45s ease both" }
    }
  },
  plugins: []
};
