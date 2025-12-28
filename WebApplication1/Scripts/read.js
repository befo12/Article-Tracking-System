// ------- SPLITTER (Sürükleyerek boyut değiştirme) -------
const leftPane = document.getElementById("leftPane");
const rightPane = document.getElementById("rightPane");
const splitter = document.getElementById("splitter");

let isDragging = false;

splitter.addEventListener("mousedown", function () {
    isDragging = true;
    document.body.style.userSelect = "none";
});

window.addEventListener("mousemove", function (e) {
    if (!isDragging) return;

    let totalWidth = splitter.parentElement.offsetWidth;
    let newLeftWidth = e.clientX;

    if (newLeftWidth < 200) newLeftWidth = 200;
    if (newLeftWidth > totalWidth - 200) newLeftWidth = totalWidth - 200;

    leftPane.style.flex = "none";
    rightPane.style.flex = "none";

    leftPane.style.width = newLeftWidth + "px";
    rightPane.style.width = (totalWidth - newLeftWidth - 6) + "px";
});

window.addEventListener("mouseup", function () {
    isDragging = false;
    document.body.style.userSelect = "auto";
});

// --- DARK MODE TOGGLE ---
document.addEventListener("DOMContentLoaded", function () {

    const toggle = document.getElementById("themeToggle");
    const root = document.documentElement;

    // Kayıtlı tema var mı?
    if (localStorage.getItem("reader-theme") === "dark") {
        root.classList.add("dark-mode");
        toggle.textContent = "☀️ Light Mode";
    }

    toggle.addEventListener("click", () => {
        root.classList.toggle("dark-mode");

        if (root.classList.contains("dark-mode")) {
            localStorage.setItem("reader-theme", "dark");
            toggle.textContent = "☀️ Light Mode";
        } else {
            localStorage.setItem("reader-theme", "light");
            toggle.textContent = "🌙 Dark Mode";
        }
    });

    // --- PANE SÜRÜKLEME ---
    const splitter = document.getElementById("splitter");
    const leftPane = document.getElementById("leftPane");
    const rightPane = document.getElementById("rightPane");

    let isDragging = false;

    splitter.addEventListener("mousedown", () => {
        isDragging = true;
        document.body.style.userSelect = "none";
    });

    document.addEventListener("mousemove", (e) => {
        if (!isDragging) return;

        let percent = (e.clientX / window.innerWidth) * 100;

        // Min-max limit
        if (percent < 20) percent = 20;
        if (percent > 80) percent = 80;

        leftPane.style.width = percent + "%";
        rightPane.style.width = (100 - percent) + "%";
    });

    document.addEventListener("mouseup", () => {
        isDragging = false;
        document.body.style.userSelect = "auto";
    });

});

