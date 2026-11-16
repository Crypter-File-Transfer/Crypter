import { randomBytes } from 'node:crypto';
import { expect, Locator, test } from '@playwright/test';

let browserErrors: string[] = [];

// Blazor's @bind updates on `change`, which fill() does not always raise in Firefox.
async function fillField(field: Locator, value: string) {
  await field.fill(value);
  await field.dispatchEvent('change');
}

test.beforeEach(async ({ page, browser, browserName }) => {
  console.log(`${browserName} ${browser.version()}`);

  browserErrors = [];
  page.on('console', message => {
    if (message.type() === 'error') {
      browserErrors.push(message.text());
    }
  });
  page.on('pageerror', error => browserErrors.push(error.message));
});

test.afterEach(async ({}, testInfo) => {
  if (testInfo.status !== testInfo.expectedStatus) {
    console.log(`Browser errors:\n${browserErrors.join('\n')}`);
  }
});

test('a logged-in user can upload a file and decrypt it', async ({ page }) => {
  const username = `e2e_${randomBytes(8).toString('hex')}`;
  const password = randomBytes(16).toString('hex');

  await page.goto('/register');
  await fillField(page.locator('#usernameFormControl'), username);
  await fillField(page.locator('#passwordFormControl'), password);
  await fillField(page.locator('#passwordConfirmFormControl'), password);
  await page.getByRole('button', { name: 'Register', exact: true }).click();
  await expect(page.getByText('Registration Successful!')).toBeVisible();

  await page.goto('/login');
  await fillField(page.locator('#usernameFormControl'), username);
  await fillField(page.locator('#passwordFormControl'), password);
  await page.getByRole('button', { name: 'Login', exact: true }).click();
  await page.waitForURL('**/user/transfers');

  await page.getByRole('button', { name: 'I understand and accept the risks' }).click();

  // A page load drops the private key held in memory, so stay inside the app.
  await page.getByRole('link', { name: 'Crypter.dev' }).click();

  // The navigation bar holds a second upload form in a hidden modal.
  const uploadForm = page.locator('#encrypt');
  await uploadForm.locator('#fileInput').setInputFiles({
    name: 'e2e.bin',
    mimeType: 'application/octet-stream',
    buffer: randomBytes(200_000),
  });
  await uploadForm.getByRole('button', { name: 'Encrypt', exact: true }).click();

  const downloadUrlInput = page.locator('#downloadUrl');
  await expect(downloadUrlInput).toHaveValue(/\/decrypt\/file\//, { timeout: 60_000 });
  const downloadUrl = await downloadUrlInput.inputValue();

  await page.goto(downloadUrl);
  await page.getByRole('button', { name: 'Decrypt', exact: true }).click();
  await expect(page.getByText('Decryption successful!')).toBeVisible({ timeout: 60_000 });
});
