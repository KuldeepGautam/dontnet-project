// Shared app-wide dialog control (client request 2026-08-25: "create a tool/control type and call
// with heading and message from everywhere required" - for PreBudget, ECL, and every future web
// module, not just whichever appendix screen happened to need one first).
//
// ROOT CAUSE this file fixes: window.openInfoDialog/openConfirmDialog already existed in
// wwwroot/js/app.js, and pre-budget-meeting.js has been calling them (guarded with
// `if (window.openInfoDialog)`) since 2026-08-24 - but app.js was never actually <script>-included
// by Views/Shared/_Layout.cshtml (or anywhere else in the real app; only the static designer
// mockups loaded it). Every one of those guarded calls has been silently skipping the dialog this
// whole time - "still having issue on dialog box of validation and record saving" was this file
// simply never running, not a bug in the dialog logic itself. This is a small, purpose-built file
// (NOT the ~1500-line app.js, which is designer-mockup code full of unrelated CRUD_CONFIGS/toast/
// grid logic never vetted for the real app) so it's safe to load on every page.
//
// Visual design matches the Login screen's Change Password dialog exactly (.modal-backdrop/
// .verify-modal/.modal-header/.modal-body/.modal-actions - see styles.css), per client direction
// to use that look everywhere instead of the older, smaller .confirm-backdrop/.confirm-card style.
(function () {
  "use strict";

  function escapeHtml(value) {
    var div = document.createElement("div");
    div.textContent = value == null ? "" : String(value);
    return div.innerHTML;
  }

  // Built fresh and appended to document.body on every call rather than requiring each page to
  // pre-declare a dialog element in its own markup. `is-open` is added one animation frame after
  // insertion so the CSS transition (opacity/transform) still plays, matching how Login's own
  // pre-declared dialogs animate in.
  function buildAppDialog(heading, bodyHtml, actionsHtml) {
    var wrap = document.createElement("div");
    wrap.className = "modal-backdrop";
    wrap.setAttribute("role", "presentation");
    wrap.innerHTML = [
      '<section class="verify-modal" role="dialog" aria-modal="true" aria-labelledby="appDialogTitle">',
      '<header class="modal-header"><div><h2 id="appDialogTitle">' + escapeHtml(heading) + "</h2></div></header>",
      '<div class="modal-body">' + bodyHtml + '<div class="modal-actions">' + actionsHtml + "</div></div>",
      "</section>"
    ].join("");

    document.body.appendChild(wrap);
    requestAnimationFrame(function () {
      wrap.classList.add("is-open");
    });
    return wrap;
  }

  function closeAppDialog(wrap) {
    wrap.remove();
  }

  // Confirmation dialog: a cancel action + a labelled confirm action (e.g. "Are you sure to
  // freeze?"). cancelLabel is optional (added 2026-08-31 for the Freeze confirmation's "Yes"/"No"
  // wording - every existing caller keeps getting the original "Cancel" since they don't pass a
  // 6th argument).
  window.openConfirmDialog = function (title, message, confirmLabel, onConfirm, danger, cancelLabel) {
    var wrap = buildAppDialog(
      title,
      '<p style="margin:0;color:var(--ubis-muted);font-size:.92rem;line-height:1.5">' + escapeHtml(message) + "</p>",
      '<button class="cancel" type="button" data-cancel>' + escapeHtml(cancelLabel || "Cancel") + "</button>" +
        '<button class="confirm" type="button" data-confirm' + (danger ? ' style="background:#dc2626"' : "") + ">" + escapeHtml(confirmLabel) + "</button>"
    );

    function close() {
      closeAppDialog(wrap);
      document.removeEventListener("keydown", onKeydown);
    }
    function onKeydown(e) {
      if (e.key === "Escape") close();
    }

    wrap.querySelector("[data-cancel]").addEventListener("click", close);
    wrap.querySelector("[data-confirm]").addEventListener("click", function () {
      onConfirm();
      close();
    });
    document.addEventListener("keydown", onKeydown);
  };

  // Single-OK informational dialog - the canonical way every module shows a validation summary
  // ("Please Enter required Fields" / "Select Scheme, Enter values for User Remarks, RE 2025-2026")
  // or a save/modify confirmation ("Record Saved" / "Record has been successfully saved."), per
  // the client's exact wording (2026-08-25). `message` may contain newlines - rendered as separate
  // lines, not escaped/joined into one.
  window.openInfoDialog = function (title, message) {
    var lines = String(message)
      .split("\n")
      .map(function (line) {
        return '<div style="margin-bottom:4px">' + escapeHtml(line) + "</div>";
      })
      .join("");
    var wrap = buildAppDialog(
      title,
      '<div style="color:var(--ubis-muted);font-size:.92rem;line-height:1.5">' + lines + "</div>",
      '<button class="confirm" type="button" data-close>OK</button>'
    );

    function close() {
      closeAppDialog(wrap);
      document.removeEventListener("keydown", onKeydown);
    }
    function onKeydown(e) {
      if (e.key === "Escape") close();
    }

    wrap.querySelector("[data-close]").addEventListener("click", close);
    document.addEventListener("keydown", onKeydown);
  };
})();
