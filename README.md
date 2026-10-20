# PanBug

Gra do nauki testowania oprogramowania. Gracz przechodzi przez 24 zadania w ośmiu
małych aplikacjach. Każde zadanie zawiera celowo wstrzyknięty defekt: gracz porównuje
działanie aplikacji z wymaganiami z opisu, odtwarza defekt i zgłasza go z kategorią.

PanBug jest **stroną statyczną**: cała gra działa w przeglądarce (Blazor WebAssembly),
bez serwera aplikacji i bez bazy danych. Najlepszy wynik i historia ostatnich gier
zapisują się w `localStorage` przeglądarki.

## Zadania

Każda aplikacja ma trzy zadania. Zadania jednej aplikacji mają ten sam widok i ten sam
opis wymagań, a różnią się defektem. Są rozrzucone po grze i nigdy
nie stoją obok siebie, więc powtórzony opis nie zdradza od razu, że to ta sama aplikacja.
Na początku są zadania za 1 punkt, na końcu najtrudniejsze.

| Aplikacja | Zadania |
|---|---|
| Kino: wybór seansu, plan sali, bilety z ulgą wiekową | 1, 9, 18 |
| Automat paczkowy: nadanie paczki (wymiary, waga, telefon) i odbiór kodem | 2, 6, 13 |
| Kalkulator raty kredytu z harmonogramem spłaty | 7, 14, 20 |
| Planer urlopów z pulami dni na dwa lata | 3, 16, 22 |
| Logowanie z blokadą konta i resetem hasła | 5, 12, 19 |
| Lista zadań z terminami, filtrem zaległych i sortowaniem | 10, 15, 23 |
| Podział rachunku w restauracji z kodami rabatowymi | 4, 11, 21 |
| Formularz zamówienia z dostawą (walidacja i dostępność) | 8, 17, 24 |

Kategorie zgłoszenia (12) łączą techniki projektowania testów z sylabusa ISTQB
(wartości brzegowe, klasy równoważności, reguły biznesowe, przejścia stanów) z obszarami
jakości: obliczenia, daty, formaty, spójność danych, interfejs, bezpieczeństwo,
dostępność i niezawodność. Za zadanie można zdobyć od 1 do 4 punktów, łącznie 51.
Zgłoszenie wskazuje jedną albo dwie kategorie: każdy defekt ma ich najwyżej dwie,
a punkty dostaje się, gdy co najmniej jedna wskazana jest poprawna. Limit zamyka
obejście polegające na zaznaczeniu wszystkich kategorii naraz.

Pod opisem każdego zadania jest zwinięta karta misji, jak w sesji testów eksploracyjnych:
cel i kilka pomysłów na testy, wspólne dla trzech zadań aplikacji. Karta nie nazywa kategorii,
a każda zawiera co najmniej jeden pomysł z obszaru, w którym aplikacja nie ma defektu.

Po grze ekran wyniku pokazuje profil kategorii (ile zadań z każdej kategorii zaliczono)
i na żądanie kopiuje wynik do udostępnienia w stylu Wordle: kwadrat na zadanie, bez nazw
defektów i kategorii.

Opis defektów i kroki ich odtworzenia są w kluczu dla prowadzącego, który nie jest
częścią repozytorium. Aplikacja zapisuje odtworzenie defektu dopiero wtedy, gdy defekt
pojawi się na ekranie.

## Architektura

| Projekt | Rola |
|---|---|
| `PanBug.Domain` | Czysta logika: 8 aplikacji po 3 zadania, `TaskManager`, `DefectCatalog` (punkty i kategorie), punktacja. Bez UI i bez zależności od platformy. |
| `PanBug.Web` | Interfejs Blazor WebAssembly: strony gry, 8 komponentów zadań (zadania jednej aplikacji dzielą widok), sesja gry z lokalną punktacją, magazyn wyników w `localStorage`. |

Każda aplikacja ma klasę bazową z poprawnym zachowaniem i sprawdzeniem, czy to, co widzi
gracz, łamie wymagania. Klasa zadania nadpisuje jedną metodę i w ten sposób wprowadza
defekt. Filtry wpisywanych znaków działają w JavaScripcie (`wwwroot/app.js`).

## Wygląd

Układ i komponenty pochodzą z systemu UI aplikacji PortfolioCogitator: karty z nagłówkami
opisanymi małymi wersalikami, liczby czcionką mono, ikony [Lucide](https://lucide.dev)
(licencja ISC). Kolory są z pierwotnej aplikacji PanBug: zielony akcent #23C87C, czarny pasek
górny, pomarańczowe punkty #E07710, czerwień błędów #C62828 i neutralne szarości. Style są
w jednym pliku `wwwroot/app.css` z tokenami kolorów, bez kroku budowania CSS.

Przełącznik w pasku górnym zmienia motyw jasny i ciemny. Bez zapisanego wyboru motyw
podąża za ustawieniem systemu. `wwwroot/theme-init.js` ustawia go przed pierwszym
malowaniem, więc strona nie miga jasnym tłem.

## Strona i podgląd linków

`index.html` ma metadane do udostępniania: Open Graph i Twitter z obrazkiem 1200×630
(`og-image.png`), adres kanoniczny, dane strukturalne JSON-LD (`WebApplication`), manifest
z ikonami (instalacja na telefonie) oraz `robots.txt` i `sitemap.xml`.

Podgląd linków wymaga adresów bezwzględnych. Pliki źródłowe zawierają adres GitHub Pages,
a `build-static.sh` podmienia go na `SITE_URL`. Workflow GitHub Pages ustawia `SITE_URL`
sam, na Render skrypt bierze `RENDER_EXTERNAL_URL`. Przy własnej domenie ustaw `SITE_URL`
w zmiennych środowiskowych.

Obrazek OG i ikony PNG są generowane z `branding/og-image.html` i `branding/icon.html`:

```bash
node branding/render.cjs   # wymaga Node i pakietu playwright
```

## Uruchomienie lokalne

Wymagany .NET SDK 8.0.

```bash
dotnet run --project src/PanBug.Web
```

Zbudowanie wersji do publikacji (wynik w `publish/wwwroot`):

```bash
./build-static.sh
```

Skrypt sam doinstaluje .NET SDK 8, jeśli go nie ma.

## Wdrożenie na Render

Repozytorium zawiera Blueprint [`render.yaml`](render.yaml) dla strony statycznej:

1. Render Dashboard → **New** → **Blueprint** → wskaż to repozytorium.
2. Render uruchomi `./build-static.sh` (instaluje .NET SDK i publikuje aplikację)
   i opublikuje katalog `publish/wwwroot`.
3. Reguła przepisywania `/* → /index.html` obsługuje wejścia na adresy podstron
   (np. `/gra`, `/pomoc`); aplikacja i tak zaczyna od ekranu powitalnego.

Strona statyczna na Render nie usypia, więc nie ma czekania na rozruch serwera.
Pierwsze wejście pobiera ok. 7,5 MB (ok. 2,3 MB po kompresji brotli), kolejne
korzystają z pamięci podręcznej przeglądarki.

## Wdrożenie na GitHub Pages

Workflow [`.github/workflows/pages.yml`](.github/workflows/pages.yml) przy każdym
pushu do `main` uruchamia testy, buduje stronę i publikuje ją na GitHub Pages.

Jednorazowa konfiguracja: **Settings → Pages → Build and deployment → Source:
GitHub Actions**. Przy ustawieniu „Deploy from a branch” GitHub Pages pokazuje
surowe pliki repozytorium (stronę z README), a nie aplikację.

Strona projektu działa pod adresem `https://<użytkownik>.github.io/<repozytorium>/`,
więc workflow ustawia `BASE_PATH=/<repozytorium>/`. Skrypt budujący dodaje też
`404.html` (wejścia na adresy podstron) i `.nojekyll` (Jekyll pomija katalog
`_framework`).

Każde uruchomienie aplikacji (pierwsze wejście, odświeżenie, link z zakładki) zaczyna się
od ekranu powitalnego, niezależnie od adresu. Stan gry żyje tylko w pamięci przeglądarki,
więc odświeżenie `/gra` nie miałoby czego pokazać. Odpowiada za to `start-at-home.js`
w `<head>`, który zmienia adres na bazowy, zanim Blazor go odczyta. Nowa gra startuje
wyłącznie przyciskiem na ekranie powitalnym lub ekranie wyniku.

Tę samą zawartość `publish/wwwroot` można opublikować na innym hostingu statycznym
(Netlify, Cloudflare Pages), ustawiając `BASE_PATH` i przekierowanie nieznanych
ścieżek na `index.html`.

## Ograniczenia wersji statycznej

- **Brak kont i rankingu Top 100.** Wyniki są widoczne tylko na urządzeniu,
  na którym grano, i znikają po wyczyszczeniu danych przeglądarki. Ekran powitalny
  pozwala wyeksportować je do pliku JSON i zaimportować na innym urządzeniu (import
  dokłada gry do historii, nie nadpisuje jej).
- **Klucz odpowiedzi jest w przeglądarce.** Punktacja liczy się lokalnie, więc
  zdeterminowany gracz może podejrzeć poprawne kategorie w plikach aplikacji.
  Dla aplikacji treningowej bez rankingu to akceptowalne.
- **Bieżąca gra jest tylko w pamięci karty.** Zamknięcie lub przeładowanie karty
  przerywa grę (przeglądarka zapyta wcześniej o potwierdzenie).

## Testy

```bash
dotnet test
```

Testy domeny odtwarzają każdy z 24 defektów i sprawdzają, że te same kroki na poprawnym
wariancie aplikacji nie zapisują odtworzenia. Zadania zależne od daty (lista zadań) są
sprawdzane dla każdego dnia roku przestępnego. Testy Blazor (bUnit) odtwarzają każdy
defekt przez widok, przechodzą pełną grę i obejmują zapis wyników w `localStorage`,
ikony, logo, przełącznik motywu oraz metadane strony (obrazek OG, manifest, JSON-LD).

Zbudowana strona była dodatkowo sprawdzona w Chromium (Playwright): skrypt budujący
w czystym środowisku bez .NET, gra z odtworzeniem i zgłoszeniem defektów, wynik
zachowany po przeładowaniu, start od ekranu powitalnego przy wejściu na dowolny adres, polski format liczb
w przeglądarce ustawionej na inny język.
