/**
 * Searchable Select - Vanilla JS Autocomplete Dropdown
 * Converts standard <select> elements into searchable custom dropdowns.
 */
document.addEventListener('DOMContentLoaded', () => {
    initSearchableSelects();
});

function initSearchableSelects() {
    // Find all selects inside forms that are used for filtering
    const selects = document.querySelectorAll('form.pp-report__filter select, form.production-report__filter select');
    
    selects.forEach(select => {
        // Skip if already initialized
        if (select.dataset.searchableInitialized === 'true') return;
        select.dataset.searchableInitialized = 'true';

        // Hide the original select
        select.style.display = 'none';

        // Create the custom wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'searchable-select';
        
        // Inherit min-width from parent or original select if needed
        if (select.parentElement.classList.contains('pp-report__field--part')) {
            wrapper.classList.add('searchable-select--part');
        } else if (select.closest('.production-report__field')) {
            wrapper.classList.add('searchable-select--prod');
        }

        // Create the trigger button
        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'searchable-select__trigger';
        // Add original select classes to trigger
        trigger.classList.add(...Array.from(select.classList));
        
        const triggerText = document.createElement('span');
        triggerText.className = 'searchable-select__trigger-text';
        trigger.appendChild(triggerText);
        
        const triggerIcon = document.createElement('span');
        triggerIcon.className = 'searchable-select__trigger-icon';
        triggerIcon.innerHTML = '&#9662;'; // Down arrow
        trigger.appendChild(triggerIcon);

        // Create the dropdown menu
        const menu = document.createElement('div');
        menu.className = 'searchable-select__menu';
        
        // Create the search input
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'searchable-select__search';
        searchInput.placeholder = 'Search...';
        
        // Create the options list
        const optionsList = document.createElement('ul');
        optionsList.className = 'searchable-select__options';
        
        const emptyMessage = document.createElement('div');
        emptyMessage.className = 'searchable-select__empty';
        emptyMessage.textContent = 'No matching results found.';
        emptyMessage.style.display = 'none';

        menu.appendChild(searchInput);
        menu.appendChild(optionsList);
        menu.appendChild(emptyMessage);
        
        wrapper.appendChild(trigger);
        wrapper.appendChild(menu);
        
        select.parentNode.insertBefore(wrapper, select.nextSibling);

        // Populate options
        const populateOptions = () => {
            optionsList.innerHTML = '';
            Array.from(select.options).forEach((option, index) => {
                const li = document.createElement('li');
                li.className = 'searchable-select__option';
                li.textContent = option.text;
                li.dataset.value = option.value;
                li.dataset.index = index;
                
                if (option.selected) {
                    li.classList.add('is-selected');
                    triggerText.textContent = option.text;
                }
                
                li.addEventListener('click', () => {
                    select.selectedIndex = index;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    closeMenu();
                });
                
                optionsList.appendChild(li);
            });
        };

        populateOptions();

        // Update trigger text when original select changes
        select.addEventListener('change', () => {
            const selectedOption = select.options[select.selectedIndex];
            if (selectedOption) {
                triggerText.textContent = selectedOption.text;
                
                // Update selected class on list items
                Array.from(optionsList.children).forEach(li => {
                    li.classList.toggle('is-selected', li.dataset.value === selectedOption.value);
                });
            }
        });
        
        // Ensure options list updates if the select's innerHTML is updated dynamically
        // Since it's server-side rendered, this is usually just for the initial load
        // But for completeness we could add a MutationObserver

        // Toggle menu
        trigger.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const isOpen = menu.classList.contains('is-open');
            
            // Close all other open menus
            document.querySelectorAll('.searchable-select__menu.is-open').forEach(m => m.classList.remove('is-open'));
            
            if (!isOpen) {
                // Populate options again in case they changed dynamically via the session restore logic
                populateOptions();
                menu.classList.add('is-open');
                searchInput.value = '';
                filterOptions('');
                searchInput.focus();
                
                // Scroll selected into view
                const selected = optionsList.querySelector('.is-selected');
                if (selected) {
                    selected.scrollIntoView({ block: 'nearest' });
                }
            }
        });

        // Close menu when clicking outside
        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) {
                closeMenu();
            }
        });

        // Filter options as user types
        searchInput.addEventListener('input', (e) => {
            filterOptions(e.target.value);
        });
        
        // Prevent form submission when pressing enter in search input
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
            }
        });

        function filterOptions(query) {
            query = query.toLowerCase();
            let hasMatch = false;
            
            Array.from(optionsList.children).forEach(li => {
                const text = li.textContent.toLowerCase();
                if (text.includes(query)) {
                    li.style.display = '';
                    hasMatch = true;
                } else {
                    li.style.display = 'none';
                }
            });
            
            emptyMessage.style.display = hasMatch ? 'none' : 'block';
        }

        function closeMenu() {
            menu.classList.remove('is-open');
        }
        
        // Handle keyboard navigation inside search input
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                closeMenu();
                trigger.focus();
            }
        });
    });
}

// Make it globally available so we can call it after layout logic
window.initSearchableSelects = initSearchableSelects;
