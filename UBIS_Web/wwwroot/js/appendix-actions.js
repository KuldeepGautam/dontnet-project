(function () {
  'use strict';

  var ACTIONS = {
    save: { icon: 'save', label: 'Save', className: 'appendix-action-save' },
    cancel: { icon: 'x', label: 'Cancel', className: 'appendix-action-cancel' },
    delete: { icon: 'trash-2', label: 'Delete', className: 'appendix-action-delete' },
    reset: { icon: 'rotate-ccw', label: 'Reset', className: 'appendix-action-reset' },
    edit: { icon: 'pencil', label: 'Edit', className: 'appendix-action-edit' }
  };

  function createActionButton(actionName) {
    var config = ACTIONS[actionName];
    if (!config) return null;

    var button = document.createElement('button');
    button.type = 'button';
    button.className = 'appendix-action-button table-icon-btn ' + config.className;
    button.dataset.action = actionName;
    button.title = config.label;
    button.setAttribute('aria-label', config.label);
    button.innerHTML = '<i data-lucide="' + config.icon + '" aria-hidden="true"></i><span class="sr-only">' + config.label + '</span>';
    return button;
  }

  function renderActions(container, actions) {
    var requested = (actions || container.dataset.actions || '')
      .split(',')
      .map(function (item) { return item.trim().toLowerCase(); })
      .filter(Boolean);

    container.dataset.actions = requested.join(',');
    container.classList.add('appendix-action-control');
    container.innerHTML = '';

    requested.forEach(function (actionName) {
      var button = createActionButton(actionName);
      if (button) container.appendChild(button);
    });

    if (window.lucide) window.lucide.createIcons();
  }

  function initializeScope(scope) {

    (scope || document).querySelectorAll('[data-actions]').forEach(function (container) {
      var row = container.closest('tr');
      if (row && !row.dataset.originalState) {
        var initialValues = captureValues(row);
        saveSnapshot(row, 'originalState', initialValues);
        saveSnapshot(row, 'savedState', initialValues);
      }
      renderActions(container, container.dataset.actions || 'edit,delete,reset');
    });
  }

  function getEditableCells(row) {
    return Array.prototype.filter.call(row.cells, function (cell) {
      if (cell.hasAttribute('data-actions')) return false;
      if (cell.hasAttribute('data-readonly')) return false;
      if (cell.querySelector('input[type="checkbox"], input[type="radio"]')) return false;
      return true;
    });
  }

  function getCellValue(cell) {
    var editor = cell.querySelector('.appendix-cell-editor');
    return editor ? editor.value.trim() : cell.textContent.trim();
  }

  function captureValues(row) {
    return getEditableCells(row).map(function (cell) {
      return getCellValue(cell);
    });
  }

  function setValues(row, values) {
    getEditableCells(row).forEach(function (cell, index) {
      cell.textContent = values && values[index] != null ? values[index] : '';
    });
  }

  function parseSnapshot(value) {
    try { return JSON.parse(value || ''); } catch (error) { return null; }
  }

  function saveSnapshot(row, key, values) {
    row.dataset[key] = JSON.stringify(values);
  }

  function createEditor(cell, value) {
    var type = (cell.dataset.inputType || 'text').toLowerCase();
    var editor;

    if (type === 'select') {
      editor = document.createElement('select');
      var options = (cell.dataset.options || '')
        .split('|')
        .map(function (item) { return item.trim(); })
        .filter(Boolean);
      if (!options.length) options = ['Approved', 'Pending', 'Review'];
      if (options.indexOf(value) === -1 && value) options.unshift(value);
      options.forEach(function (optionText) {
        var option = document.createElement('option');
        option.value = optionText;
        option.textContent = optionText;
        option.selected = optionText === value;
        editor.appendChild(option);
      });
    } else {
      editor = document.createElement('input');
      editor.type = ['number', 'date', 'email', 'tel'].indexOf(type) !== -1 ? type : 'text';
      editor.value = value;
      if (editor.type === 'number') editor.step = cell.dataset.step || 'any';
    }

    editor.className = 'appendix-cell-editor';
    editor.setAttribute('aria-label', cell.dataset.label || 'Edit table value');
    return editor;
  }

  function enableRowEdit(row) {
    if (row.classList.contains('is-editing')) return;

    saveSnapshot(row, 'beforeEdit', captureValues(row));
    row.classList.add('is-editing');

    getEditableCells(row).forEach(function (cell) {
      var value = cell.textContent.trim();
      cell.innerHTML = '';
      cell.appendChild(createEditor(cell, value));
    });

    var firstEditor = row.querySelector('.appendix-cell-editor');
    if (firstEditor) {
      firstEditor.focus();
      if (typeof firstEditor.select === 'function') firstEditor.select();
    }
  }

  function commitRow(row) {
    var values = captureValues(row);
    setValues(row, values);
    row.classList.remove('is-editing');
    saveSnapshot(row, 'savedState', values);
    delete row.dataset.beforeEdit;
  }

  function cancelRow(row) {
    var beforeEdit = parseSnapshot(row.dataset.beforeEdit);
    setValues(row, beforeEdit || parseSnapshot(row.dataset.savedState));
    row.classList.remove('is-editing');
    delete row.dataset.beforeEdit;
  }

  function resetRow(row) {
    var original = parseSnapshot(row.dataset.originalState);
    setValues(row, original);
    row.classList.remove('is-editing');
    saveSnapshot(row, 'savedState', original || []);
    delete row.dataset.beforeEdit;
  }

  function handleAction(button) {
    var action = button.dataset.action;
    var container = button.closest('[data-actions]');
    var row = button.closest('tr');
    if (!row || !container) return;

    if (action === 'delete') {
      if (window.confirm('Are you sure you want to delete this row?')) row.remove();
      return;
    }

    if (action === 'edit') {
      enableRowEdit(row);
      renderActions(container, 'save,cancel,delete,reset');
      return;
    }

    if (action === 'save') {
      commitRow(row);
      renderActions(container, 'edit,delete,reset');
      return;
    }

    if (action === 'cancel') {
      cancelRow(row);
      renderActions(container, 'edit,delete,reset');
      return;
    }

    if (action === 'reset') {
      resetRow(row);
      renderActions(container, 'edit,delete,reset');
    }
  }

  document.addEventListener('click', function (event) {
    var button = event.target.closest('.appendix-action-button');
    if (button) handleAction(button);
  });

  document.addEventListener('keydown', function (event) {
    var row = event.target.closest('tr.is-editing');
    if (!row) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      var cancelButton = row.querySelector('[data-action="cancel"]');
      if (cancelButton) cancelButton.click();
    }

    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      event.preventDefault();
      var saveButton = row.querySelector('[data-action="save"]');
      if (saveButton) saveButton.click();
    }
  });

  window.UBISAppendixActions = window.UBISAppendixActions || {};
  window.UBISAppendixActions.refresh = initializeScope;

  document.addEventListener('DOMContentLoaded', function () {
    initializeScope(document);
  });
})();
