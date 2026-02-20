#!/bin/bash

# Start the .NET backend with E2E test configuration
# This script ensures all required environment variables are set

echo "🚀 Starting .NET backend for E2E tests..."
echo ""
echo "Configuration:"
echo "  - Auth__AutoVerifyEmail=true (auto-verify new users)"
echo "  - RateLimiting__Enabled=false (disable rate limiting)"
echo "  - Email__DevMode=true (dev mode for emails)"
echo ""

Auth__AutoVerifyEmail=true \
RateLimiting__Enabled=false \
Email__DevMode=true \
dotnet run

