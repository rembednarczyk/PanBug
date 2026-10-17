#!/usr/bin/env bash
# Buduje statyczną wersję PanBug do katalogu publish/wwwroot.
# Używany przez Render (render.yaml) i GitHub Pages (.github/workflows/pages.yml);
# działa też lokalnie.
#
# BASE_PATH - ścieżka, pod którą strona będzie dostępna (domyślnie "/").
#             Dla GitHub Pages projektu: "/<nazwa-repozytorium>/".
# SITE_URL  - pełny adres strony do metadanych (canonical, Open Graph, sitemap).
#             Domyślnie RENDER_EXTERNAL_URL na Render, a bez niego adres GitHub Pages.
set -euo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
# SDK nie potrzebuje bibliotek ICU systemu (aplikacja w przeglądarce ma własne dane ICU).
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

DOTNET=dotnet
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then
    # Środowisko budowania stron statycznych Render nie ma .NET - instalujemy SDK 8.
    DOTNET_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
    curl -sSfL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$DOTNET_DIR"
    DOTNET="$DOTNET_DIR/dotnet"
fi

BASE_PATH="${BASE_PATH:-/}"
[[ "$BASE_PATH" == /* ]] || BASE_PATH="/$BASE_PATH"
[[ "$BASE_PATH" == */ ]] || BASE_PATH="$BASE_PATH/"

rm -rf publish
"$DOTNET" publish src/PanBug.Web/PanBug.Web.csproj -c Release -o publish

OUT=publish/wwwroot
# Adres bazowy aplikacji (linki w aplikacji są względne wobec <base href>).
sed -i "s#<base href=\"/\" />#<base href=\"$BASE_PATH\" />#" "$OUT/index.html"
grep -q "<base href=\"$BASE_PATH\" />" "$OUT/index.html"
# Hostingi bez reguł przepisywania (GitHub Pages) zwracają 404.html dla nieznanych
# ścieżek - kopia index.html sprawia, że bezpośrednie linki (np. /gra) działają.
# Pełny adres strony w metadanych (podgląd linków wymaga adresów bezwzględnych).
DEFAULT_SITE_URL="https://rembednarczyk.github.io/PanBug/"
SITE_URL="${SITE_URL:-${RENDER_EXTERNAL_URL:+${RENDER_EXTERNAL_URL%/}/}}"
SITE_URL="${SITE_URL:-$DEFAULT_SITE_URL}"
[[ "$SITE_URL" == */ ]] || SITE_URL="$SITE_URL/"
for f in index.html robots.txt sitemap.xml; do
    sed -i "s#$DEFAULT_SITE_URL#$SITE_URL#g" "$OUT/$f"
done
grep -q "<link rel=\"canonical\" href=\"$SITE_URL\" />" "$OUT/index.html"

cp "$OUT/index.html" "$OUT/404.html"
# Wyłącza Jekylla na GitHub Pages (pomija on katalogi zaczynające się od "_", np. _framework).
touch "$OUT/.nojekyll"

echo "Gotowe: $OUT (base href: $BASE_PATH, adres: $SITE_URL)"
