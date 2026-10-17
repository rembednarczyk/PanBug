// Motyw przed pierwszym malowaniem (jak theme-init.js w PortfolioCogitator): ładowany blokująco
// w <head>, więc użytkownik ciemnego motywu nie widzi błysku jasnej strony. Bez zapisanego wyboru
// motyw podąża za ustawieniem systemu. Klucz i klasa muszą zgadzać się z panbug.theme w app.js.
(function () {
    try {
        var stored = localStorage.getItem('panbug-theme');
        var dark = stored ? stored === 'dark' : window.matchMedia('(prefers-color-scheme: dark)').matches;
        if (dark) document.documentElement.classList.add('dark-theme');
    } catch (e) {
        /* brak localStorage/matchMedia: zostaje jasny motyw */
    }
})();
