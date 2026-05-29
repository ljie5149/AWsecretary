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

document.querySelectorAll(".sidebar-toggler, .sidebar-menu-button").forEach(btn => {
    btn.addEventListener("click", () => {
        document.querySelector(".sidebar").classList.toggle("collapsed");
    });
});