function setCulture(culture) {
  document.cookie = '.AspNetCore.Culture=c=' + culture + '|uic=' + culture + '; path=/; max-age=' + (60 * 60 * 24 * 365);
  location.reload();
}

async function copyUserTag(userTag, button) {
  if (navigator.clipboard && window.isSecureContext) {
    await navigator.clipboard.writeText(userTag);
  } else {
    const input = document.createElement('input');
    input.value = userTag;
    input.style.position = 'fixed';
    input.style.opacity = '0';
    document.body.appendChild(input);
    input.focus();
    input.select();
    document.execCommand('copy');
    document.body.removeChild(input);
  }

  const status = button.querySelector('.copy-status');
  if (status) {
    status.classList.remove('d-none');
    setTimeout(() => status.classList.add('d-none'), 1500);
  }
}

document.addEventListener('click', event => {
  const option = event.target.closest('.language-option');
  if (!option) return;
  setCulture(option.dataset.culture);
});

document.addEventListener('DOMContentLoaded', () => {
  if (!window.isViewOnlySharedMode) return;

  document.querySelectorAll('.content-inner input, .content-inner select, .content-inner textarea, .content-inner button').forEach(element => {
    if (element.matches('[data-allow-view-mode-action="true"]') || element.closest('[data-allow-view-mode-action="true"]')) return;
    element.disabled = true;
    element.classList.add('disabled');
  });

  document.querySelectorAll('.content-inner .cascading-select, .content-inner .worklog-material-select, .content-inner .worklog-aggregate-select, .content-inner .material-select').forEach(element => {
    element.classList.add('disabled');
    element.setAttribute('aria-disabled', 'true');
    element.style.pointerEvents = 'none';
    element.style.opacity = '0.65';
  });

  document.addEventListener('click', event => {
    const customSelect = event.target.closest('.cascading-select, .worklog-material-select, .worklog-aggregate-select, .material-select');
    if (!customSelect || !document.querySelector('.content-inner')?.contains(customSelect)) return;

    event.preventDefault();
    event.stopPropagation();
    event.stopImmediatePropagation();
    document.querySelectorAll('.cascading-dropdown').forEach(dropdown => dropdown.classList.remove('show'));
  }, true);
});
