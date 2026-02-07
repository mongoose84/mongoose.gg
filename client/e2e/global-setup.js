import { chromium } from '@playwright/test';
import path from 'path';
import fs from 'fs';

/**
 * Global setup for E2E tests.
 * 
 * This file runs ONCE before all tests to:
 * 1. Register a new unique test user (auto-verified in non-production)
 * 2. Link a Riot account to the user
 * 3. Save authentication state for all tests to reuse
 * 
 * The user is deleted in global-teardown.js after all tests complete.
 * 
 * @see https://playwright.dev/docs/test-global-setup-teardown
 */

// Hardcoded Riot account for testing
// This account should exist in the Riot system and be dedicated for E2E testing
const RIOT_ACCOUNT = {
  gameName: 'Doend', 
  tagLine: 'EUW',               
  region: 'euw1',
};

// Test user credentials - generated fresh each run
const TEST_PASSWORD = 'E2ETestPassword123!';

// Auth state file path
const AUTH_FILE = path.join(process.cwd(), 'e2e/.auth/user.json');

// Metadata file to store user info for teardown
const METADATA_FILE = path.join(process.cwd(), 'e2e/.auth/test-user.json');

// Base API URL
const API_BASE = process.env.E2E_BASE_URL || 'http://localhost:5164';

export default async function globalSetup() {
  console.log('🚀 Starting E2E global setup...');
  
  // Create auth directory if it doesn't exist
  const authDir = path.dirname(AUTH_FILE);
  if (!fs.existsSync(authDir)) {
    fs.mkdirSync(authDir, { recursive: true });
  }

  // Generate unique test user for this run
  const timestamp = Date.now();
  const testUser = {
    username: `e2e_test_${timestamp}`,
    email: `e2e_test_${timestamp}@test.mongoose.gg`,
    password: TEST_PASSWORD,
  };

  console.log(`📝 Registering test user: ${testUser.username}`);

  // Launch browser for API calls with cookie handling
  const browser = await chromium.launch();
  const context = await browser.newContext({
    baseURL: API_BASE,
  });

  try {
    // Step 1: Register the test user
    const registerResponse = await context.request.post('/api/v2/auth/register', {
      data: {
        username: testUser.username,
        email: testUser.email,
        password: testUser.password,
      },
    });

    if (!registerResponse.ok()) {
      const error = await registerResponse.json().catch(() => ({}));
      throw new Error(`Registration failed: ${error.error || registerResponse.statusText()}`);
    }

    const registerData = await registerResponse.json();
    console.log(`✅ User registered: ${registerData.username} (ID: ${registerData.userId})`);
    console.log(`   Email verified: ${registerData.emailVerified}`);

    // Save user metadata for teardown
    const metadata = {
      userId: registerData.userId,
      username: testUser.username,
      email: testUser.email,
      password: testUser.password,
      createdAt: new Date().toISOString(),
    };
    fs.writeFileSync(METADATA_FILE, JSON.stringify(metadata, null, 2));

    // Step 2: Link Riot account
    console.log(`🎮 Linking Riot account: ${RIOT_ACCOUNT.gameName}#${RIOT_ACCOUNT.tagLine}`);
    
    const linkResponse = await context.request.post('/api/v2/users/me/riot-accounts', {
      data: {
        gameName: RIOT_ACCOUNT.gameName,
        tagLine: RIOT_ACCOUNT.tagLine,
        region: RIOT_ACCOUNT.region,
      },
    });

    if (!linkResponse.ok()) {
      const error = await linkResponse.json().catch(() => ({}));
      // Don't fail if account is already linked (from a previous failed run)
      if (error.code !== 'ACCOUNT_ALREADY_LINKED') {
        throw new Error(`Failed to link Riot account: ${error.error || linkResponse.statusText()}`);
      }
      console.log('⚠️ Riot account was already linked, continuing...');
    } else {
      const linkData = await linkResponse.json();
      console.log(`✅ Riot account linked: ${linkData.gameName}#${linkData.tagLine} (PUUID: ${linkData.puuid})`);
    }

    // Step 3: Save authentication state
    // We need to navigate to the app to ensure cookies are properly set for the domain
    const page = await context.newPage();
    await page.goto(process.env.E2E_BASE_URL || 'http://localhost:5174');
    
    // Wait briefly for any cookie syncing
    await page.waitForTimeout(500);
    
    // Save the storage state (cookies, localStorage, etc.)
    await context.storageState({ path: AUTH_FILE });
    
    console.log(`✅ Auth state saved to ${AUTH_FILE}`);
    console.log('✅ E2E global setup complete!');
    console.log('');
    console.log(`   Test User: ${testUser.username}`);
    console.log(`   Riot Account: ${RIOT_ACCOUNT.gameName}#${RIOT_ACCOUNT.tagLine}`);
    console.log('');

  } catch (error) {
    console.error('❌ E2E global setup failed:', error.message);
    
    // Clean up metadata file on failure
    if (fs.existsSync(METADATA_FILE)) {
      fs.unlinkSync(METADATA_FILE);
    }
    
    throw error;
  } finally {
    await browser.close();
  }
}

