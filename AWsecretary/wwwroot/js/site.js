//[cite: 1, 3]
const toggleDropdown = (dropdown, menu, isOpen) => {
    dropdown.classList.toggle("open", isOpen);
    menu.style.height = isOpen ? `${menu.scrollHeight}px` : 0;
}

document.querySelectorAll(".dropdown-toggle").forEach(btn => {
    btn.addEventListener("click", e => {
        e.preventDefault();
        const dropdown = e.target.closest(".dropdown-container");
        const menu = dropdown.querySelector(".dropdown-menu");
        const isOpen = dropdown.classList.contains("open");
        toggleDropdown(dropdown, menu, !isOpen);
    });
});

const updateBodySidebarState = () => {
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar) return;
    document.body.classList.toggle("sidebar-collapsed", sidebar.classList.contains("collapsed"));
};

document.querySelectorAll(".sidebar-toggler, .sidebar-menu-button").forEach(btn => {
    btn.addEventListener("click", () => {
        document.querySelector(".sidebar").classList.toggle("collapsed");
        // 同步更新 body 類別，讓 topbar/main-content 能透過 CSS 變數回應
        updateBodySidebarState();
    });
});

// 如果頁面載入時側邊欄已經是 collapsed（例如伺服端預設），確保 body 狀態同步
document.addEventListener("DOMContentLoaded", () => {
    updateBodySidebarState();
});