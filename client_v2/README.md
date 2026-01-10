# Pulse.gg Client v2

> The only LoL improvement tracker built for duos and teams, powered by AI coaching that turns your stats into actionable goals you can actually achieve.

## Overview

This is the **standalone Vue 3 + Vite application** for Pulse.gg v2, completely independent from the legacy client. Built with a modern tech stack and Vercel Developer aesthetic.

## Tech Stack

- **Vue 3** (Composition API, `<script setup>`)
- **Vite** - Fast build tool with HMR
- **Tailwind CSS** - Utility-first styling with custom Vercel theme
- **Vue Router 4** - Client-side routing
- **Pinia** - State management
- **Vue Query** (TanStack Query) - Data fetching and caching
- **Axios** - HTTP client
- **Headless UI** - Accessible component primitives
- **Heroicons** - Icon library

## Project Structure

```
client_v2/
├── public/
│   ├── pulse-icon.svg          # App logo/icon
│   ├── gaming-bg.jpg           # Global background image
│   └── hero-bg.svg             # Hero section background
├── src/
│   ├── components/             # Reusable components
│   │   ├── NavBar.vue         # Navigation bar
│   │   └── VersionBadge.vue   # Version display badge
│   ├── views/                  # Page components
│   │   ├── LandingPage.vue    # Homepage with hero & pricing
│   │   ├── AuthPage.vue       # Login/Signup
│   │   ├── PrivacyPage.vue    # Privacy policy
│   │   └── TermsPage.vue      # Terms of service
│   ├── router/
│   │   └── index.js           # Route definitions
│   ├── App.vue                # Root component
│   ├── main.js                # App entry point
│   └── style.css              # Global styles + Vercel theme
├── test/
│   └── unit/
│       ├── setup.spec.js      # Vitest configuration test
│       ├── NavBar.spec.js     # NavBar component tests
│       ├── VersionBadge.spec.js  # VersionBadge tests
│       ├── LandingPage.spec.js   # LandingPage view tests
│       ├── AuthPage.spec.js      # AuthPage view tests
│       └── App.spec.js        # App integration tests
├── index.html
├── package.json
├── vite.config.js
├── vitest.config.js
├── tailwind.config.js
└── postcss.config.js
```

## Getting Started

### Install Dependencies

```bash
npm install
```

### Run Development Server

```bash
npm run dev
```

The app will be available at [http://localhost:5174](http://localhost:5174)

### Build for Production

```bash
npm run build
```

Output will be in `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

### Run Unit Tests

```bash
npm run test:unit        # Run all tests once
npm run test:unit:watch  # Watch mode for development
npm run test:unit:coverage  # Generate coverage report
```

## Theme System

The app uses **CSS custom properties** for theming, currently set to the **Vercel Developer** aesthetic (adapted for gaming):

- **Primary Color**: `#6d28d9` (Purple)
- **Background**: Pure black with gaming-themed image overlay (`gaming-bg.jpg`) 
- **Overlay**: Dark translucent gradient (0.90/0.85 opacity) for readability
- **Typography**: Inter font with tight letter spacing
- **Shadows**: Purple-tinted shadows for depth
- **Style**: Technical, cutting-edge, minimal with gaming aesthetic

### Changing Themes

All theme tokens are defined in [src/style.css](src/style.css). To switch themes, update the CSS variables in `:root`. The design system supports easy theme swapping for future iterations.

## Testing

The project uses **Vitest** + **Vue Test Utils** for unit testing.

### Test Coverage

- **Components**: NavBar, VersionBadge
- **Views**: LandingPage, AuthPage
- **Integration**: App routing and layering

### Running Tests

```bash
# Run all tests
npm run test:unit

# Watch mode for development
npm run test:unit:watch

# Generate coverage report
npm run test:unit:coverage
```

Test files are located in [test/unit/](test/unit/) directory.

## Features Implemented

### ✅ Landing Page
- Hero section with value proposition
- Features grid (6 key features)
- How It Works section (4-step process)
- Pricing comparison (Free, Pro, Team tiers)
- Footer with links

### ✅ Navigation Bar
- Fixed top navigation with blur effect
- Responsive mobile menu
- Logo and branding
- CTA buttons

### ✅ Auth Page
- Toggle between login/signup
- Form validation (placeholder)
- Consistent Vercel styling

### ✅ Legal Pages
- Privacy Policy
- Terms of Service

## Next Steps (Backlog)

1. **Testing Expansion**
   - Add tests for all views and components
   - Increase coverage to >80%
   - Add E2E tests for critical flows

2. **Authentication System** (Epic G2)
   - Implement real auth endpoints
   - Email verification flow
   - Password reset functionality

3. **Solo Dashboard** (Epic F2)
   - Connect to `/api/v2/solo/dashboard` endpoint
   - Performance charts and metrics
   - Queue filtering

4. **AI Goal Recommendations** (Epic B)
   - Goal recommendation UI
   - Progress tracking components

5. **Duo/Team Dashboards** (Epic F3, F4)
   - Duo synergy analytics
   - Team coordination views

## Deployment

The app is designed for deployment to **Vercel** or any static hosting provider:

```bash
npm run build
# Deploy the `dist/` folder
```

### Environment Variables

For production, configure:

- `VITE_API_BASE_URL` - Backend API URL (default: `http://localhost:5000`)

## Development Guidelines

- **Components**: Use `<script setup>` syntax
- **Styling**: Use Tailwind utilities + CSS variables
- **Routing**: Add routes in `src/router/index.js`
- **API Calls**: Create services in `src/api/` (coming soon)
- **State**: Use Pinia stores for global state
- **Testing**: Write tests in `test/unit/` using Vitest + Vue Test Utils
  - Follow the pattern: `ComponentName.spec.js`
  - Test component rendering, user interactions, and routing
  - Use `mount()` from Vue Test Utils for component mounting
  - Use `createRouter()` for router-dependent tests

## Branding

- **Name**: Pulse.gg
- **Tagline**: "The only LoL improvement tracker built for duos and teams"
- **Icon**: Stylized pulse/heartbeat graphic
- **Colors**: Vercel blue (`#0070f3`) + black background

## References

- [Information Architecture v2 Doc](../docs/information_architecture_v2.md)
- [Product Backlog](../docs/product_backlog.md)
- [API v2 Integration Guide](../docs/frontend_integration_solo_v2.md)
- [Database Schema v2](../docs/database_schema_v2.md)

---

Built with ❤️ by Agile Astronaut | First 500 users get free Pro tier!
