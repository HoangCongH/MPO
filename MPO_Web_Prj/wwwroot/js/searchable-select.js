/**
 * Turns report selects into accessible searchable dropdowns.
 * Selects with data-lazy-options-url fetch at most a small suggestion batch on demand.
 */
document.addEventListener('DOMContentLoaded', () => {
    initSearchableSelects();
    initReportBatchLoaders();
});

function initSearchableSelects() {
    const selects = document.querySelectorAll('form.pp-report__filter select, form.production-report__filter select');

    selects.forEach(select => {
        if (select.dataset.searchableInitialized === 'true') return;
        select.dataset.searchableInitialized = 'true';

        const lazyOptionsUrl = select.dataset.lazyOptionsUrl;
        const lazyField = select.dataset.lazyField;
        let lazyOptions = [];
        let requestSequence = 0;
        let searchTimer;

        select.style.display = 'none';
        const wrapper = document.createElement('div');
        wrapper.className = 'searchable-select';
        if (select.parentElement.classList.contains('pp-report__field--part')) {
            wrapper.classList.add('searchable-select--part');
        } else if (select.closest('.production-report__field')) {
            wrapper.classList.add('searchable-select--prod');
        }

        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'searchable-select__trigger';
        trigger.classList.add(...Array.from(select.classList));
        const triggerText = document.createElement('span');
        triggerText.className = 'searchable-select__trigger-text';
        const triggerIcon = document.createElement('span');
        triggerIcon.className = 'searchable-select__trigger-icon';
        triggerIcon.textContent = '▾';
        trigger.append(triggerText, triggerIcon);

        const menu = document.createElement('div');
        menu.className = 'searchable-select__menu';
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'searchable-select__search';
        searchInput.placeholder = 'Search...';
        const optionsList = document.createElement('ul');
        optionsList.className = 'searchable-select__options';
        const emptyMessage = document.createElement('div');
        emptyMessage.className = 'searchable-select__empty';
        emptyMessage.textContent = 'No matching results found.';
        menu.append(searchInput, optionsList, emptyMessage);
        wrapper.append(trigger, menu);
        select.parentNode.insertBefore(wrapper, select.nextSibling);

        const selectedOption = () => select.options[select.selectedIndex];
        const sourceOptions = () => lazyOptionsUrl ? lazyOptions : Array.from(select.options).map(option => ({ value: option.value, text: option.text }));

        function renderOptions(options) {
            optionsList.replaceChildren();
            const selectedValue = select.value;
            options.forEach(option => {
                const li = document.createElement('li');
                li.className = 'searchable-select__option';
                li.textContent = option.text;
                li.dataset.value = option.value;
                li.classList.toggle('is-selected', option.value === selectedValue);
                li.addEventListener('click', () => selectValue(option));
                optionsList.appendChild(li);
            });
            emptyMessage.style.display = options.length ? 'none' : 'block';
        }

        function selectValue(option) {
            let nativeOption = Array.from(select.options).find(item => item.value === option.value);
            if (!nativeOption) {
                nativeOption = new Option(option.text, option.value);
                select.add(nativeOption);
            }
            select.value = option.value;
            triggerText.textContent = option.text;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            closeMenu();
        }

        async function loadLazyOptions(search) {
            const sequence = ++requestSequence;
            emptyMessage.textContent = 'Loading...';
            emptyMessage.style.display = 'block';
            optionsList.replaceChildren();

            const form = select.closest('form');
            const formData = new FormData(form);
            formData.append('field', lazyField);
            const requestUrl = new URL(lazyOptionsUrl, window.location.origin);
            requestUrl.searchParams.set('search', search);
            requestUrl.searchParams.set('limit', '50');

            try {
                const response = await fetch(requestUrl, { method: 'POST', body: formData, credentials: 'same-origin' });
                if (!response.ok) throw new Error('Unable to load options.');
                const payload = await response.json();
                if (sequence !== requestSequence) return;

                lazyOptions = payload.map(item => ({ value: item.value ?? item.Value ?? '', text: item.text ?? item.Text ?? '' }));
                lazyOptions = lazyOptions.filter(option => option.value !== '');
                lazyOptions.unshift({ value: '', text: 'All' });
                const current = selectedOption();
                if (current && !lazyOptions.some(option => option.value === current.value)) {
                    lazyOptions.splice(1, 0, { value: current.value, text: current.text });
                }
                renderOptions(lazyOptions);
            } catch {
                if (sequence !== requestSequence) return;
                emptyMessage.textContent = 'Unable to load options.';
                emptyMessage.style.display = 'block';
            }
        }

        function filterOptions(query) {
            const normalizedQuery = query.toLowerCase();
            renderOptions(sourceOptions().filter(option => option.text.toLowerCase().includes(normalizedQuery)));
        }

        function closeMenu() {
            menu.classList.remove('is-open');
        }

        const initial = selectedOption();
        triggerText.textContent = initial ? initial.text : 'All';
        renderOptions(sourceOptions());

        select.addEventListener('change', () => {
            const selected = selectedOption();
            if (selected) triggerText.textContent = selected.text;
            renderOptions(sourceOptions());
        });

        trigger.addEventListener('click', async event => {
            event.preventDefault();
            event.stopPropagation();
            const isOpen = menu.classList.contains('is-open');
            document.querySelectorAll('.searchable-select__menu.is-open').forEach(item => item.classList.remove('is-open'));
            if (isOpen) return;

            menu.classList.add('is-open');
            searchInput.value = '';
            if (lazyOptionsUrl) {
                await loadLazyOptions('');
            } else {
                filterOptions('');
            }
            searchInput.focus();
        });

        searchInput.addEventListener('input', event => {
            const value = event.target.value;
            clearTimeout(searchTimer);
            if (!lazyOptionsUrl) {
                filterOptions(value);
                return;
            }
            searchTimer = setTimeout(() => loadLazyOptions(value), 250);
        });

        searchInput.addEventListener('keydown', event => {
            if (event.key === 'Enter') event.preventDefault();
            if (event.key === 'Escape') {
                closeMenu();
                trigger.focus();
            }
        });

        document.addEventListener('click', event => {
            if (!wrapper.contains(event.target)) closeMenu();
        });
    });
}

window.initSearchableSelects = initSearchableSelects;

function initReportBatchLoaders() {
    document.querySelectorAll('[data-report-batch-url]').forEach(tableWrap => {
        const form = tableWrap.closest('.pp-report')?.querySelector('form.pp-report__filter');
        const tbody = tableWrap.querySelector('[data-report-batch-rows]');
        const columns = (tableWrap.dataset.reportColumns || '').split(',').filter(Boolean);
        if (!form || !tbody || columns.length === 0 || tableWrap.dataset.hasMore !== 'True') return;

        let nextOffset = Number(tableWrap.dataset.nextOffset || 0);
        let hasMore = true;
        let loading = false;

        const formatValue = (column, value) => {
            if (value === null || value === undefined) return '';
            if (column === 'startTime' || column === 'endTime') {
                const date = new Date(value);
                return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
            }
            if (column.includes('Time') || column === 'ppm' || column === 'scrapRatio') {
                return Number(value).toLocaleString(undefined, { maximumFractionDigits: 2 });
            }
            return value;
        };

        const appendRows = rows => {
            rows.forEach(row => {
                const tr = document.createElement('tr');
                columns.forEach(column => {
                    const td = document.createElement('td');
                    td.textContent = formatValue(column, row[column]);
                    tr.appendChild(td);
                });
                tbody.appendChild(tr);
            });
        };

        const loadNextBatch = async () => {
            if (loading || !hasMore) return;
            loading = true;
            const body = new FormData(form);
            body.append('offset', String(nextOffset));
            try {
                const response = await fetch(tableWrap.dataset.reportBatchUrl, { method: 'POST', body, credentials: 'same-origin' });
                if (!response.ok) throw new Error('Unable to load report records.');
                const batch = await response.json();
                appendRows(batch.rows || []);
                nextOffset = batch.nextOffset ?? nextOffset;
                hasMore = batch.hasMore === true;
            } catch (error) {
                console.error(error);
            } finally {
                loading = false;
            }
        };

        tableWrap.addEventListener('scroll', () => {
            if (tableWrap.scrollHeight - tableWrap.scrollTop - tableWrap.clientHeight < 240) loadNextBatch();
        });
    });
}
