
 
    document.addEventListener('DOMContentLoaded', () => {
      const $ = (selector) => document.querySelector(selector);
      const loginForm = $('#loginForm'), dialog = $('#verifyDialog'), contact = $('#contact');
      let answer = 8, method = 'email', lastTrigger;
      const iconify = () => window.lucide && window.lucide.createIcons();
      iconify();
      function makeCaptcha() { const a = Math.floor(Math.random() * 7) + 2, b = Math.floor(Math.random() * 7) + 2; answer = a + b; $('#captchaCode').textContent = `${a} + ${b}`; $('#captchaAnswer').value = ''; }
      function openDialog(trigger) { lastTrigger = trigger; dialog.classList.add('is-open'); contact.focus(); }
      function closeDialog() { dialog.classList.remove('is-open'); $('#verifyError').textContent = ''; if (lastTrigger) lastTrigger.focus(); }
      $('#refreshCaptcha').addEventListener('click', makeCaptcha);
      $('#togglePassword').addEventListener('click', () => { const input = $('#password'), shown = input.type === 'text'; input.type = shown ? 'password' : 'text'; $('#togglePassword').setAttribute('aria-label', shown ? 'Show password' : 'Hide password'); $('#togglePassword').innerHTML = `<i data-lucide="${shown ? 'eye' : 'eye-off'}" aria-hidden="true"></i>`; iconify(); });
      loginForm.addEventListener('submit', (event) => { event.preventDefault(); const error = $('#loginError'); error.textContent = ''; if (!loginForm.checkValidity()) { error.textContent = 'Please complete your username and password.'; return; } if (Number($('#captchaAnswer').value) !== answer) { error.textContent = 'The captcha answer does not match. Please try again.'; makeCaptcha(); return; } openDialog(event.submitter); });
      document.querySelectorAll('.method').forEach((button) => button.addEventListener('click', () => { method = button.dataset.method; document.querySelectorAll('.method').forEach((item) => item.classList.toggle('is-active', item === button)); $('#contactLabel').textContent = method === 'email' ? 'Email address' : 'Phone number'; contact.type = method === 'email' ? 'email' : 'tel'; contact.autocomplete = method === 'email' ? 'email' : 'tel'; contact.placeholder = method === 'email' ? 'name@department.gov.in' : '10-digit mobile number'; contact.value = ''; contact.focus(); }));
      $('#verifyForm').addEventListener('submit', (event) => { event.preventDefault(); const code = $('#verificationCode').value.trim(), validContact = method === 'email' ? contact.validity.valid : /^\d{10}$/.test(contact.value.replace(/\D/g, '')); if (!validContact || !/^\d{6}$/.test(code)) { $('#verifyError').textContent = 'Enter a valid contact detail and 6-digit verification code.'; return; } window.location.assign('dashboard.html'); });
      dialog.addEventListener('click', (event) => { if (event.target === dialog) closeDialog(); }); document.querySelector('.close-modal').addEventListener('click', closeDialog); document.querySelector('.cancel').addEventListener('click', closeDialog); document.addEventListener('keydown', (event) => { if (event.key === 'Escape' && dialog.classList.contains('is-open')) closeDialog(); });
    });
  