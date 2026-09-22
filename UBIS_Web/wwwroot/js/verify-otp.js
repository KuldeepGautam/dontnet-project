document.addEventListener("DOMContentLoaded", function () {
    window.lucide && window.lucide.createIcons();

    var errorEl = document.getElementById("verifyError");
    var infoEl = document.getElementById("verifyInfo");

    // Method switcher (2026-07-22) — purely visual today: AIM only delivers OTPs over one fixed,
    // server-configured channel (Mfa:Channel), so there's nothing to actually re-request here yet.
    // Kept interactive (among enabled buttons only — disabled ones never fire a click) so this
    // isn't dead markup once Mfa:PhoneOtpEnabled and a real SMS gateway land.
    var methodButtons = document.querySelectorAll(".method-switch .method:not(:disabled)");
    methodButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            methodButtons.forEach(function (b) {
                b.classList.remove("is-active");
                b.setAttribute("aria-pressed", "false");
            });
            button.classList.add("is-active");
            button.setAttribute("aria-pressed", "true");
        });
    });

    function csrfToken(form) {
        var input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function postJson(form, body) {
        return fetch(form.getAttribute("action"), {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken(form),
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(body || {})
        }).then(function (res) { return res.ok ? res.json() : Promise.reject(); });
    }

    // AJAX verify (2026-07-20): UserController.VerifyOtp now always returns JSON — see its doc
    // comment — instead of re-rendering this page or redirecting on failure.
    var verifyForm = document.getElementById("verifyForm");
    if (verifyForm) {
        var confirmButton = verifyForm.querySelector(".confirm");
        verifyForm.addEventListener("submit", function (event) {
            event.preventDefault();

            if (window.jQuery && typeof window.jQuery(verifyForm).valid === "function" && !window.jQuery(verifyForm).valid()) {
                return;
            }

            if (errorEl) { errorEl.textContent = ""; }
            if (confirmButton) { confirmButton.disabled = true; }

            postJson(verifyForm, { Otp: document.getElementById("Otp").value })
                .then(function (data) {
                    if (data.success) {
                        window.location.href = data.redirectUrl || "/";
                        return;
                    }

                    if (errorEl) { errorEl.textContent = data.errorMessage || "Incorrect or expired code."; }
                    if (data.redirectUrl) {
                        // Challenge expired server-side (TempData gone) — nothing left to retry here.
                        window.location.href = data.redirectUrl;
                        return;
                    }
                    if (confirmButton) { confirmButton.disabled = false; }
                })
                .catch(function () {
                    if (errorEl) { errorEl.textContent = "Could not reach the server. Please try again."; }
                    if (confirmButton) { confirmButton.disabled = false; }
                });
        });
    }

    // AJAX resend (2026-07-20): shows the "new code sent" status inline instead of a full
    // redirect back to this same page via a TempData info message.
    var resendForm = document.getElementById("resendForm");
    if (resendForm) {
        var resendButton = resendForm.querySelector(".resend");
        resendForm.addEventListener("submit", function (event) {
            event.preventDefault();

            if (errorEl) { errorEl.textContent = ""; }
            if (resendButton) { resendButton.disabled = true; }

            postJson(resendForm)
                .then(function (data) {
                    if (data.redirectUrl && !data.success) {
                        window.location.href = data.redirectUrl;
                        return;
                    }
                    if (infoEl) { infoEl.textContent = data.message || "A new code has been sent."; }
                    if (resendButton) { resendButton.disabled = false; }
                })
                .catch(function () {
                    if (errorEl) { errorEl.textContent = "Could not reach the server. Please try again."; }
                    if (resendButton) { resendButton.disabled = false; }
                });
        });
    }
});
