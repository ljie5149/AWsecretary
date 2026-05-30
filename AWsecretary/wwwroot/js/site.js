//[cite: 1, 3]
const STORAGE_PREFIX = 'awsecretary:ui:';

// utility storage helpers
const storageSet = (key, value) => {
    try {
        localStorage.setItem(STORAGE_PREFIX + key, value);
    } catch (e) {
        // localStorage may be unavailable; silently ignore
    }
};

const storageGet = (key) => {
    try {
        return localStorage.getItem(STORAGE_PREFIX + key);
    } catch (e) {
        return null;
    }
};

// dropdown toggle with optional persistence
const toggleDropdown = (dropdown, menu, isOpen, persist = true) => {
    dropdown.classList.toggle("open", isOpen);
    menu.style.height = isOpen ? `${menu.scrollHeight}px` : 0;

    if (persist) {
        // ensure dropdown has a stable data-key; fallback to index if not present
        let key = dropdown.getAttribute('data-key');
        if (!key) {
            key = 'dropdown-' + Array.from(document.querySelectorAll('.dropdown-container')).indexOf(dropdown);
            dropdown.setAttribute('data-key', key);
        }
        storageSet('dropdown:' + key, isOpen ? '1' : '0');
    }
}

// close all open dropdowns (and persist state)
const closeAllDropdowns = (persist = true) => {
    document.querySelectorAll(".dropdown-container.open").forEach(openDropdown => {
        const menu = openDropdown.querySelector(".dropdown-menu");
        toggleDropdown(openDropdown, menu, false, persist);
    });
}

// bind click for dropdown toggles
document.querySelectorAll(".dropdown-toggle").forEach(btn => {
    btn.addEventListener("click", e => {
        e.preventDefault();
        const dropdown = e.target.closest(".dropdown-container");
        const menu = dropdown.querySelector(".dropdown-menu");
        const isOpen = dropdown.classList.contains("open");

        // close others and toggle current
        closeAllDropdowns();
        toggleDropdown(dropdown, menu, !isOpen);
    });
});

const updateBodySidebarState = () => {
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar) return;
    document.body.classList.toggle("sidebar-collapsed", sidebar.classList.contains("collapsed"));
};

// persist sidebar collapsed state
const setSidebarCollapsed = (collapsed) => {
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar) return;
    sidebar.classList.toggle("collapsed", collapsed);
    updateBodySidebarState();
    storageSet('sidebarCollapsed', collapsed ? '1' : '0');
};

// restore sidebar collapsed from storage (if present)
const restoreSidebarState = () => {
    const val = storageGet('sidebarCollapsed');
    if (val === '1' || val === '0') {
        const collapsed = val === '1';
        const sidebar = document.querySelector(".sidebar");
        if (sidebar) {
            sidebar.classList.toggle("collapsed", collapsed);
            updateBodySidebarState();
        }
    }
};

document.querySelectorAll(".sidebar-toggler, .sidebar-menu-button").forEach(btn => {
    btn.addEventListener("click", () => {
        closeAllDropdowns();
        const sidebar = document.querySelector(".sidebar");
        if (!sidebar) return;
        const willBeCollapsed = !sidebar.classList.contains("collapsed");
        sidebar.classList.toggle("collapsed");
        // 同步更新 body 類別，讓 topbar/main-content 能透過 CSS 變數回應
        updateBodySidebarState();
        // persist
        storageSet('sidebarCollapsed', willBeCollapsed ? '1' : '0');
    });
});

// restore dropdown open states from storage
const restoreDropdownStates = () => {
    const dropdowns = Array.from(document.querySelectorAll('.dropdown-container'));
    dropdowns.forEach((dropdown, idx) => {
        // determine key (use existing data-key or assign fallback)
        let key = dropdown.getAttribute('data-key');
        if (!key) {
            key = 'dropdown-' + idx;
            dropdown.setAttribute('data-key', key);
        }

        const stored = storageGet('dropdown:' + key);
        const defaultOpen = dropdown.getAttribute('data-default-open') === 'true';
        const shouldOpen = stored === '1' ? true : (stored === '0' ? false : defaultOpen);

        const menu = dropdown.querySelector('.dropdown-menu');
        if (!menu) return;

        // open/close after layout so scrollHeight is correct
        if (shouldOpen) {
            // requestAnimationFrame to ensure CSS/layout ready
            requestAnimationFrame(() => toggleDropdown(dropdown, menu, true, /*persist*/ false));
        } else {
            // ensure closed
            toggleDropdown(dropdown, menu, false, /*persist*/ false);
        }
    });
};

// 如果頁面載入時側邊欄已經是 collapsed（例如伺服端預設），確保 body 狀態同步，並從 localStorage 還原使用者設定
document.addEventListener("DOMContentLoaded", () => {
    // restore sidebar first
    restoreSidebarState();

    // then restore dropdowns
    restoreDropdownStates();

    // 如果沒有 localStorage 設定但 server-side 設了 data-default-open，保留原有行為（舊程式碼相容）
    updateBodySidebarState();
});