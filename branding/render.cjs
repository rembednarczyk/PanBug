// Renderuje obrazek OG i ikony PNG do src/PanBug.Web/wwwroot. Wymaga Node i Playwright (Chromium).
// Użycie: node branding/render.cjs   (opcjonalnie CHROMIUM=/ścieżka/do/chrome)
const { chromium } = require('playwright');
const path = require('node:path');

const here = __dirname;
(async () => {
  const out = path.join(here, '..', 'src', 'PanBug.Web', 'wwwroot');
  const browser = await chromium.launch(process.env.CHROMIUM ? { executablePath: process.env.CHROMIUM } : {});

  const og = await browser.newPage({ viewport: { width: 1200, height: 630 }, deviceScaleFactor: 1 });
  await og.goto('file://' + path.join(here, 'og-image.html'));
  await og.evaluate(() => document.fonts.ready);
  await og.screenshot({ path: path.join(out, 'og-image.png') });

  const icons = [
    ['apple-touch-icon.png', 180, 'square=1'],
    ['icon-192.png', 192, ''],
    ['icon-512.png', 512, ''],
    ['icon-maskable-512.png', 512, 'maskable=1'],
  ];
  for (const [name, size, extra] of icons) {
    const page = await browser.newPage({ viewport: { width: size, height: size }, deviceScaleFactor: 1 });
    await page.goto('file://' + path.join(here, 'icon.html') + `?size=${size}&${extra}`);
    await page.screenshot({ path: path.join(out, name), omitBackground: true });
    await page.close();
  }
  await browser.close();
  console.log('Zapisano og-image.png i ikony w', out);
})().catch(e => { console.error(e); process.exit(1); });
