const pw = require('./node_modules/playwright/index.js');
const chromium = pw.chromium;
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('http://localhost:4201/login', { waitUntil: 'networkidle', timeout: 15000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: 'C:/Temp/ab_login_new.png' });
  console.log('Done');
  await browser.close();
})();
