document.addEventListener("DOMContentLoaded", function () {
    window.lucide && window.lucide.createIcons();

    // Prevent copying/cutting the password value or reaching Copy/Cut via the right-click menu
    // (client MOM: passwords shouldn't be copyable off the login screen). Paste is deliberately
    // left alone so password managers still work.
    var passwordInput = document.getElementById("password");
    if (passwordInput) {
        passwordInput.addEventListener("copy", function (event) { event.preventDefault(); });
        passwordInput.addEventListener("cut", function (event) { event.preventDefault(); });
        passwordInput.addEventListener("contextmenu", function (event) { event.preventDefault(); });
    }

    var toggle = document.getElementById("togglePassword");
    if (toggle) {
        toggle.addEventListener("click", function () {
            var input = document.getElementById("password");
            var shown = input.type === "text";
            input.type = shown ? "password" : "text";
            this.setAttribute("aria-label", shown ? "Show password" : "Hide password");
            this.innerHTML = '<i data-lucide="' + (shown ? "eye" : "eye-off") + '" aria-hidden="true"></i>';
            window.lucide && window.lucide.createIcons();
        });
    }

    // Fetches a fresh CAPTCHA in place instead of the link's old behavior (a full page GET,
    // which reloaded the whole Login page and wiped Username/Password).
    var refreshLink = document.getElementById("refreshCaptcha");
    if (refreshLink) {
        refreshLink.addEventListener("click", function (event) {
            event.preventDefault();
            refreshCaptcha();
        });
    }

    function refreshCaptcha() {
        return fetch("/User/RefreshCaptcha", { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
            .then(function (data) {
                var image = document.getElementById("captchaImage");
                var answer = document.getElementById("CaptchaAnswer");
                if (image) { image.src = data.captchaImageSvg; }
                if (answer) { answer.value = ""; }
            })
            .catch(function () {
                // Network hiccup — leave the current challenge in place rather than
                // navigating away, so the user doesn't lose what they've already typed.
            });
    }

    // AJAX login (2026-07-20): the form used to do a full postback for every attempt, which
    // reset the whole page (and, combined with the CAPTCHA refresh issue above, made every retry
    // feel like it wiped your input). UserController.Login now always returns JSON — see its
    // doc comment — so this submits via fetch and only ever navigates away on real success.
    var loginForm = document.getElementById("loginForm");
    if (loginForm) {
        var submitButton = loginForm.querySelector(".login-submit");
        var messageEl = document.getElementById("loginFormMessage");
        var statusEl = document.getElementById("loginStatus");
        var statusMessageEl = document.getElementById("loginStatusMessage");

        // Single message slot at the top of the form - every call replaces whatever was there
        // before (including a server-rendered InfoMessage from an earlier redirect, e.g. "Your
        // password has been reset..."), so there is never more than one alert visible at once.
        function setMessage(text, isError) {
            if (!messageEl) { return; }
            messageEl.textContent = text || "";
            messageEl.className = isError ? "error" : "info";
            messageEl.setAttribute("role", isError ? "alert" : "status");
        }

        // The whole Login round trip (AIM auth, role lookup, menu-tree resolution, compliance
        // snapshot) happens server-side in one request and can take a few seconds — longer still
        // if MenuGenerator is briefly unreachable and the resilience pipeline has to retry before
        // giving up gracefully. Fading the form out and showing progress text here is purely
        // cosmetic (there's no real multi-step progress signal from the server, just one
        // response), but it tells the user something is actually happening instead of leaving
        // what looks like a frozen page for that whole window.
        function setLoading(isLoading, message) {
            if (isLoading) {
                loginForm.classList.add("is-loading");
                if (statusMessageEl) { statusMessageEl.textContent = message || "Signing you in…"; }
                if (statusEl) { statusEl.classList.add("is-visible"); }
            } else {
                loginForm.classList.remove("is-loading");
                if (statusEl) { statusEl.classList.remove("is-visible"); }
            }
        }

        var usernameInput = document.getElementById("UserName");

        loginForm.addEventListener("submit", function (event) {
            event.preventDefault();

            // Respect the existing jQuery Unobtrusive Validation rules (data-val-* attributes
            // from the [Required] etc. DataAnnotations) before spending a round trip on the server.
            // Validated in two stages (client MOM: show Username/Password errors first; only show
            // the CAPTCHA's "required" message once both of those are actually filled in) rather
            // than validating the whole form at once, which surfaced all three at the same time
            // regardless of which fields the user had actually gotten to.
            if (window.jQuery && typeof window.jQuery(loginForm).valid === "function") {
                var usernameOk = window.jQuery(usernameInput).valid();
                var passwordOk = window.jQuery(passwordInput).valid();
                if (!usernameOk || !passwordOk) {
                    // Don't show the CAPTCHA error yet - clear any stale message from a prior
                    // attempt. data-valmsg-for is the same attribute jQuery Unobtrusive Validation
                    // itself uses to locate this span, so it stays correct even if the markup
                    // around it moves.
                    var captchaError = document.querySelector('[data-valmsg-for="CaptchaAnswer"]');
                    if (captchaError) { captchaError.textContent = ""; }
                    return;
                }
                if (!window.jQuery(loginForm).valid()) {
                    return;
                }
            }

            var token = loginForm.querySelector('input[name="__RequestVerificationToken"]');
            var payload = {
                UserName: document.getElementById("UserName").value,
                Password: document.getElementById("password").value,
                CaptchaAnswer: document.getElementById("CaptchaAnswer").value,
                ReturnUrl: document.getElementById("ReturnUrl").value
            };

            setMessage("", false);
            var priorCaptchaError = document.querySelector('[data-valmsg-for="CaptchaAnswer"]');
            if (priorCaptchaError) { priorCaptchaError.textContent = ""; }
            if (submitButton) { submitButton.disabled = true; }
            setLoading(true, "Signing you in…");

            fetch(loginForm.getAttribute("action") || "/User/Login", {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": token ? token.value : "",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify(payload)
            })
                .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
                .then(function (data) {
                    if (data.success) {
                        if (data.passwordResetRequired) {
                            // Signed in, but still on the default password — show the change
                            // dialog in place instead of navigating to Dashboard just to be
                            // bounced back to ForcePasswordReset by ComplianceInterceptorFilter.
                            // Uses the fresh post-auth token the server just minted (see
                            // UserController.CompleteSignInAsync) — the page's original token was
                            // issued anonymously and is no longer valid now that we're signed in.
                            setLoading(false);
                            openChangePasswordDialog(payload.Password, data.antiForgeryToken, data.redirectUrl || "/");
                            if (submitButton) { submitButton.disabled = false; }
                            return;
                        }

                        // Role/menu/compliance resolution no longer happens before this response
                        // (see UserController.CompleteSignInAsync) - they're fetched on Dashboard
                        // itself instead (dashboard-bootstrap.js), so there's nothing left to wait
                        // on here. Navigate immediately rather than an artificial pause.
                        window.location.href = data.redirectUrl || "/";
                        return;
                    }

                    setLoading(false);

                    // CAPTCHA-mismatch (bug fix 2026-08-06, matches the older UI): shown inline
                    // under the CAPTCHA box, not in the generic top-of-form banner - clear
                    // whichever one isn't being used so a stale message can't linger from a
                    // previous attempt.
                    var captchaErrorEl = document.querySelector('[data-valmsg-for="CaptchaAnswer"]');
                    if (data.field === "captcha" && captchaErrorEl) {
                        setMessage("", false);
                        captchaErrorEl.textContent = data.errorMessage || "You have entered incorrect Captcha code. Enter valid code.";
                        captchaErrorEl.classList.remove("field-validation-valid");
                        captchaErrorEl.classList.add("field-validation-error");
                    } else {
                        if (captchaErrorEl) { captchaErrorEl.textContent = ""; }
                        setMessage(data.errorMessage || "Login failed. Please try again.", true);
                    }

                    var image = document.getElementById("captchaImage");
                    var answer = document.getElementById("CaptchaAnswer");
                    if (image && data.captchaImageSvg) { image.src = data.captchaImageSvg; }
                    if (answer) { answer.value = ""; }
                    if (submitButton) { submitButton.disabled = false; }
                })
                .catch(function () {
                    setLoading(false);
                    setMessage("Could not reach the server. Please try again.", true);
                    if (submitButton) { submitButton.disabled = false; }
                    refreshCaptcha();
                });
        });
    }

    // Default-password change dialog (2026-07-20) — shown in place on a successful login whose
    // account is still on AIM's configured default password. Needs a fresh, post-authentication
    // antiforgery token (see openChangePasswordDialog's caller in the Login handler above) and
    // the real ChangePasswordAsync/AIM call server-side — see
    // UserController.CompleteForcedPasswordChange's doc comment for why this exists alongside,
    // not instead of, the page-based ForcePasswordReset flow.
    var changePasswordDialog = document.getElementById("changePasswordDialog");
    var changePasswordForm = document.getElementById("changePasswordForm");
    if (changePasswordDialog && changePasswordForm) {
        var changeError = document.getElementById("changePasswordError");
        var changeCancelButton = document.getElementById("changePasswordCancel");
        var currentPasswordForDialog = "";
        var antiForgeryTokenForDialog = "";
        var redirectUrlForDialog = "/";

        window.openChangePasswordDialog = function (currentPassword, antiForgeryToken, redirectUrl) {
            // Mutual exclusion with the Forgot Password dialog (both share the same full-screen
            // .modal-backdrop/z-index) - without this, if that dialog were ever left open, this one
            // would render stacked on top of it, and closing this one would reveal the other still
            // open underneath instead of a clean, single-dialog experience. Closed INSTANTLY (no
            // fade) - both dialogs otherwise animate opacity over 180ms, and fading this one out at
            // the same moment the other fades in makes them briefly cross-fade into each other.
            var forgotDialog = document.getElementById("forgotPasswordDialog");
            if (forgotDialog) {
                forgotDialog.classList.add("no-transition");
                forgotDialog.classList.remove("is-open");
                void forgotDialog.offsetWidth; // forces the browser to apply no-transition before it's removed
                forgotDialog.classList.remove("no-transition");
            }

            currentPasswordForDialog = currentPassword || "";
            antiForgeryTokenForDialog = antiForgeryToken || "";
            redirectUrlForDialog = redirectUrl || "/";
            if (changeError) { changeError.textContent = ""; }
            changePasswordForm.reset();
            changePasswordDialog.classList.add("is-open");
            var firstField = document.getElementById("NewPassword");
            if (firstField) { firstField.focus(); }
        };

        // Client requirement 2026-08-31: "during first time login, change password dialog screen
        // should have cancel button to close dialog, if user donot want to change password at the
        // moment." By the time this dialog is shown the user is already fully signed in server-
        // side (see the Login handler's own comment above - this is a client-side UX gate before
        // navigating to Dashboard, not an auth step) - closing it and continuing to `redirectUrl`
        // is safe: ComplianceInterceptorFilter still catches PasswordResetRequired on every
        // subsequent authenticated request and redirects to the full-page ForcePasswordReset flow
        // (which has its own "Cancel and log out instead" option), so declining here doesn't grant
        // any real access - it just defers the same prompt to the next navigation, same as if this
        // dialog didn't intercept the redirect at all.
        if (changeCancelButton) {
            changeCancelButton.addEventListener("click", function () {
                changePasswordDialog.classList.remove("is-open");
                window.location.href = redirectUrlForDialog;
            });
        }

        changePasswordForm.addEventListener("submit", function (event) {
            event.preventDefault();

            if (window.jQuery && typeof window.jQuery(changePasswordForm).valid === "function" && !window.jQuery(changePasswordForm).valid()) {
                return;
            }

            var newPassword = document.getElementById("NewPassword").value;
            var confirmNewPassword = document.getElementById("ConfirmNewPassword").value;
            if (newPassword !== confirmNewPassword) {
                if (changeError) { changeError.textContent = "Passwords do not match."; }
                return;
            }

            // Same #loginStatus overlay the main login form uses (z-index 9999, above the modal's
            // 20) — without this the button just silently disabled with no other feedback, so a
            // user had no way to tell whether their click had registered (client report 2026-08-18).
            var changeStatusEl = document.getElementById("loginStatus");
            var changeStatusMessageEl = document.getElementById("loginStatusMessage");

            var submitButton = changePasswordForm.querySelector(".confirm");
            if (changeError) { changeError.textContent = ""; }
            if (submitButton) { submitButton.disabled = true; }
            if (changeStatusMessageEl) { changeStatusMessageEl.textContent = "Changing your password…"; }
            if (changeStatusEl) { changeStatusEl.classList.add("is-visible"); }

            fetch("/User/CompleteForcedPasswordChange", {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": antiForgeryTokenForDialog,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify({
                    CurrentPassword: currentPasswordForDialog,
                    NewPassword: newPassword,
                    ConfirmNewPassword: confirmNewPassword
                })
            })
                .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
                .then(function (data) {
                    if (data.success) {
                        window.location.href = data.redirectUrl || "/";
                        return;
                    }
                    if (changeStatusEl) { changeStatusEl.classList.remove("is-visible"); }
                    if (changeError) { changeError.textContent = data.errorMessage || "Password change failed. Please try again."; }
                    if (data.redirectUrl) { window.location.href = data.redirectUrl; return; }
                    if (submitButton) { submitButton.disabled = false; }
                })
                .catch(function () {
                    if (changeStatusEl) { changeStatusEl.classList.remove("is-visible"); }
                    if (changeError) { changeError.textContent = "Could not reach the server. Please try again."; }
                    if (submitButton) { submitButton.disabled = false; }
                });
        });
    }
});
