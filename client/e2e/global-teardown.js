import { chromium } from '@playwright/test';
import path from 'path';
import fs from 'fs';

/**
 * Global teardown for E2E tests.
 * 
 * This file runs ONCE after all tests complete to:
 * 1. Delete the test user created in global-setup.js
 * 2. Clean up auth state files
 * 
 * @see https://playwright.dev/docs/test-global-setup-teardown
 */

// File paths
const AUTH_FILE = path.join(process.cwd(), 'e2e/.auth/user.json');
const METADATA_FILE = path.join(process.cwd(), 'e2e/.auth/test-user.json');

// Base API URL
const API_BASE = process.env.E2E_BASE_URL || 'http://localhost:5164';

export default async function globalTeardown() {
  console.log('');
  console.log('🧹 Starting E2E global teardown...');

  // Check if metadata file exists
  if (!fs.existsSync(METADATA_FILE)) {
    console.log('⚠️ No test user metadata found, skipping cleanup');
    return;
  }

  // Load test user metadata
  let metadata;
  try {
    metadata = JSON.parse(fs.readFileSync(METADATA_FILE, 'utf-8'));
  } catch (error) {
    console.error('❌ Failed to read test user metadata:', error.message);
    return;
  }

  console.log(`🗑️ Deleting test user: ${metadata.username} (ID: ${metadata.userId})`);

  // Launch browser with saved auth state to make authenticated API calls
  const browser = await chromium.launch();
  
  try {
    // Load the auth state from global setup
    const context = await browser.newContext({
      baseURL: API_BASE,
      storageState: fs.existsSync(AUTH_FILE) ? AUTH_FILE : undefined,
    });

    // Delete the user account
    const deleteResponse = await context.request.delete('/api/v2/auth/account', {
      data: {
        password: metadata.password,
      },
    });

    if (!deleteResponse.ok()) {
      const error = await deleteResponse.json().catch(() => ({}));
      console.error(`⚠️ Failed to delete test user: ${error.error || deleteResponse.statusText()}`);
      console.log('   The user may need to be deleted manually from the database.');
    } else {
      console.log(`✅ Test user deleted: ${metadata.username}`);
    }

    await context.close();
  } catch (error) {
    console.error('❌ Error during user deletion:', error.message);
    console.log('   The user may need to be deleted manually from the database.');
  } finally {
    await browser.close();
  }

  // Clean up auth files
  try {
    if (fs.existsSync(AUTH_FILE)) {
      fs.unlinkSync(AUTH_FILE);
      console.log('✅ Auth state file cleaned up');
    }
    if (fs.existsSync(METADATA_FILE)) {
      fs.unlinkSync(METADATA_FILE);
      console.log('✅ Metadata file cleaned up');
    }
  } catch (error) {
    console.error('⚠️ Failed to clean up auth files:', error.message);
  }

  console.log('✅ E2E global teardown complete!');
}

