document.addEventListener('DOMContentLoaded', () => {
    const filterSelector = 'form.pp-report__filter, form.production-report__filter';

    const resizeCharts = () => {
        if (typeof Chart === 'undefined') return;
        document.querySelectorAll('canvas').forEach(canvas => Chart.getChart(canvas)?.resize());
    };

    const requestChartResize = () => {
        window.requestAnimationFrame(() => {
            resizeCharts();
            window.dispatchEvent(new Event('resize'));
        });
    };

    document.querySelectorAll(filterSelector).forEach((form, index) => {
        if (form.dataset.collapsibleFilterInitialized === 'true') return;
        form.dataset.collapsibleFilterInitialized = 'true';

        const panel = document.createElement('section');
        panel.className = 'collapsible-filter';
        const content = document.createElement('div');
        content.className = 'collapsible-filter__content';
        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'collapsible-filter__toggle';
        toggle.setAttribute('aria-label', 'Collapse filters');
        toggle.setAttribute('title', 'Collapse filters');

        form.parentNode.insertBefore(panel, form);
        panel.append(toggle, content);
        content.appendChild(form);

        const storageKey = `mpo-filter-collapsed:${window.location.pathname}:${index}`;
        const renderToggle = isCollapsed => {
            panel.classList.toggle('is-collapsed', isCollapsed);
            toggle.setAttribute('aria-expanded', (!isCollapsed).toString());
            toggle.setAttribute('aria-label', isCollapsed ? 'Expand filters' : 'Collapse filters');
            toggle.setAttribute('title', isCollapsed ? 'Expand filters' : 'Collapse filters');
            toggle.replaceChildren();
            const icon = document.createElement('i');
            icon.setAttribute('data-lucide', isCollapsed ? 'chevron-down' : 'chevron-up');
            toggle.appendChild(icon);
            window.lucide?.createIcons();
            requestChartResize();
        };

        const setCollapsed = isCollapsed => {
            if (isCollapsed) {
                content.style.overflow = 'hidden';
                content.style.maxHeight = `${content.scrollHeight}px`;
                window.requestAnimationFrame(() => {
                    content.style.maxHeight = '0px';
                    content.style.opacity = '0';
                });
            } else {
                content.style.overflow = 'hidden';
                content.style.maxHeight = `${content.scrollHeight}px`;
                content.style.opacity = '1';
                content.addEventListener('transitionend', () => {
                    if (!panel.classList.contains('is-collapsed')) {
                        content.style.maxHeight = 'none';
                        content.style.overflow = 'visible';
                    }
                }, { once: true });
            }
            localStorage.setItem(storageKey, isCollapsed ? 'true' : 'false');
            renderToggle(isCollapsed);
        };

        const isCollapsed = localStorage.getItem(storageKey) === 'true';
        if (isCollapsed) {
            content.style.maxHeight = '0px';
            content.style.opacity = '0';
        } else {
            content.style.maxHeight = 'none';
            content.style.overflow = 'visible';
        }
        renderToggle(isCollapsed);
        toggle.addEventListener('click', () => setCollapsed(!panel.classList.contains('is-collapsed')));
    });

    const mainContent = document.querySelector('.app-main__content');
    if (mainContent && typeof ResizeObserver !== 'undefined') {
        let resizeTimer;
        new ResizeObserver(() => {
            window.clearTimeout(resizeTimer);
            resizeTimer = window.setTimeout(resizeCharts, 80);
        }).observe(mainContent);
    }

    window.addEventListener('mpo:layoutchange', requestChartResize);
});
