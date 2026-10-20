// Filtry pól wejściowych: pole z atrybutem data-filter przyjmuje tylko pasujące znaki.
// Działają w przeglądarce przy każdym wpisanym znaku; poprawność wartości i tak sprawdza symulacja.
(() => {
    const filters = {
        // Same cyfry ASCII.
        digits: (v, o) => [...v].filter(c => /[0-9]/.test(c)).join('').slice(0, o.max),
        // Kwota: cyfry, jeden separator dziesiętny (kropka i przecinek zamieniane na data-sep), maks. 2 miejsca po nim.
        amount: (v, o) => {
            let out = '';
            let sepAt = -1;
            for (const raw of v) {
                const c = raw === '.' || raw === ',' ? o.sep : raw;
                if (c === o.sep) {
                    if (sepAt < 0) { sepAt = out.length; out += c; }
                    continue;
                }
                if (/[0-9]/.test(c) && (sepAt < 0 || out.length - sepAt <= 2)) out += c;
            }
            return out.slice(0, o.max);
        },
    };

    document.addEventListener('input', e => {
        const el = e.target;
        const kind = el?.dataset?.filter;
        if (!kind || !filters[kind]) return;
        const options = {
            max: Number(el.dataset.max) || Number.MAX_SAFE_INTEGER,
            sep: el.dataset.sep || ',',
        };
        const before = el.value;
        const after = filters[kind](before, options);
        if (after === before) return;
        const caret = Math.max(0, (el.selectionStart ?? after.length) + after.length - before.length);
        el.value = after;
        el.setSelectionRange(caret, caret);
    }, true);
})();

// Pomocnicze funkcje wywoływane z C# (IJSRuntime).
window.panbug = {
    // localStorage może być niedostępny (tryb prywatny, blokada) - wtedy wyniki nie są zapisywane.
    storage: {
        get: key => { try { return localStorage.getItem(key); } catch { return null; } },
        // Zwraca, czy zapis się udał (tryb prywatny, brak miejsca lub zablokowane dane strony: false).
        set: (key, value) => { try { localStorage.setItem(key, value); return true; } catch { return false; } },
    },
    // Zapis pliku na dysku (eksport wyników): tekst trafia do Bloba i jest pobierany przez link.
    download: (fileName, text, mime) => {
        const url = URL.createObjectURL(new Blob([text], { type: mime || 'application/json' }));
        const link = Object.assign(document.createElement('a'), { href: url, download: fileName });
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
    // Kopiowanie do schowka (wynik do udostępnienia). Zwraca false, gdy przeglądarka odmówi
    // (brak HTTPS, brak uprawnień) - wtedy UI pokazuje tekst do ręcznego skopiowania.
    copy: async text => {
        try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
    },
    // Zaznacza całą treść pola tekstowego (ręczne kopiowanie wyniku).
    select: el => el?.select(),
    // Okno modalne: fokus wchodzi do okna i krąży w nim (Tab/Shift+Tab), Escape klika przycisk
    // oznaczony data-escape (Anuluj/Nie), a po zamknięciu fokus wraca tam, skąd okno otwarto.
    modal: (() => {
        const stack = [];
        const focusables = el => [...el.querySelectorAll(
            'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href]')];
        const onKey = e => {
            const top = stack[stack.length - 1];
            if (!top) return;
            if (!document.body.contains(top.el)) { stack.pop(); return; }
            if (e.key === 'Escape') {
                const cancel = top.el.querySelector('[data-escape]');
                if (cancel) { e.preventDefault(); cancel.click(); }
            } else if (e.key === 'Tab') {
                const items = focusables(top.el);
                if (items.length === 0) { e.preventDefault(); return; }
                const first = items[0], last = items[items.length - 1];
                const inside = top.el.contains(document.activeElement);
                if (!inside || (e.shiftKey && document.activeElement === first) || (!e.shiftKey && document.activeElement === last)) {
                    e.preventDefault();
                    (e.shiftKey ? last : first).focus();
                }
            }
        };
        document.addEventListener('keydown', onKey, true);
        return {
            open: el => { stack.push({ el, returnTo: document.activeElement }); el?.focus(); },
            close: () => {
                const top = stack.pop();
                if (top?.returnTo && document.body.contains(top.returnTo)) top.returnTo.focus();
            },
        };
    })(),
    // Ostrzeżenie przy zamykaniu lub przeładowaniu karty w trakcie gry (stan gry jest tylko w pamięci).
    setLeaveGuard: active => {
        window.onbeforeunload = active ? e => { e.preventDefault(); e.returnValue = ''; } : null;
    },
    // Motyw jasny/ciemny: klasa .dark-theme na <html> (ustawiana wcześniej przez theme-init.js).
    theme: {
        isDark: () => document.documentElement.classList.contains('dark-theme'),
        toggle: () => {
            const dark = document.documentElement.classList.toggle('dark-theme');
            try { localStorage.setItem('panbug-theme', dark ? 'dark' : 'light'); } catch { /* brak zapisu */ }
            return dark;
        },
    },
};
