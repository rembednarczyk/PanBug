// Każde uruchomienie aplikacji zaczyna się od ekranu powitalnego. Stan gry żyje tylko w pamięci,
// więc odświeżenie /gra albo /wynik i tak nie ma czego pokazać, a wejście z zakładki czy linku
// na podstronę ma prowadzić na start, nie w środek gry. Ładowany blokująco w <head>, przed
// blazor.webassembly.js: router odczytuje adres dopiero przy starcie, więc żadna podstrona nie
// zostaje wyrenderowana nawet na chwilę. <base href> stoi wyżej w <head>, stąd document.baseURI
// jest już adresem bazowym (na GitHub Pages /PanBug/). Na GitHub Pages podstrony obsługuje 404.html,
// które jest kopią index.html, więc ta sama reguła działa także dla wejść spoza aplikacji.
(function () {
    try {
        var home = new URL(document.baseURI).pathname;
        if (window.location.pathname !== home || window.location.search || window.location.hash)
            window.history.replaceState(null, '', home);
    } catch (e) {
        /* brak URL/history: router pokaże stronę z adresu, a /gra i tak wraca na start */
    }
})();
