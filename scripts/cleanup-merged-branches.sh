#!/bin/bash

# =============================================================================
# Cleanup Merged Branches Script
# =============================================================================
# This script deletes local branches that have been merged into main.
# It will NOT delete:
#   - main branch
#   - The currently checked out branch
#
# Usage:
#   ./scripts/cleanup-merged-branches.sh          # Dry run (shows what would be deleted)
#   ./scripts/cleanup-merged-branches.sh --delete # Actually delete the branches
# =============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get current branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)

echo "============================================="
echo "  Merged Branch Cleanup Script"
echo "============================================="
echo ""
echo -e "Current branch: ${YELLOW}${CURRENT_BRANCH}${NC}"
echo ""

# Get merged branches (excluding main and current branch)
MERGED_BRANCHES=$(git branch --merged main | grep -v "^\*" | grep -v "^  main$" | sed 's/^[ *]*//')

# Count branches
BRANCH_COUNT=$(echo "$MERGED_BRANCHES" | grep -c . || echo 0)

if [ "$BRANCH_COUNT" -eq 0 ]; then
    echo -e "${GREEN}No merged branches to clean up!${NC}"
    exit 0
fi

echo -e "Found ${YELLOW}${BRANCH_COUNT}${NC} merged branches to delete:"
echo ""
echo "$MERGED_BRANCHES" | while read branch; do
    echo "  - $branch"
done
echo ""

# Check for --delete flag
if [ "$1" == "--delete" ]; then
    echo -e "${RED}Deleting branches...${NC}"
    echo ""
    
    DELETED=0
    FAILED=0
    
    echo "$MERGED_BRANCHES" | while read branch; do
        if [ -n "$branch" ]; then
            if git branch -d "$branch" 2>/dev/null; then
                echo -e "  ${GREEN}✓${NC} Deleted: $branch"
                ((DELETED++)) || true
            else
                echo -e "  ${RED}✗${NC} Failed to delete: $branch"
                ((FAILED++)) || true
            fi
        fi
    done
    
    echo ""
    echo -e "${GREEN}Branch cleanup complete!${NC}"
    echo ""
    echo "Note: Remote branches were not deleted."
    echo "To delete remote branches, run:"
    echo "  git push origin --delete <branch-name>"
    echo ""
    echo "Or to delete ALL merged remote branches:"
    echo "  git branch -r --merged main | grep -v 'main' | grep 'origin/' | sed 's/origin\\///' | xargs -I {} git push origin --delete {}"
else
    echo -e "${YELLOW}DRY RUN${NC} - No branches were deleted."
    echo ""
    echo "To actually delete these branches, run:"
    echo -e "  ${GREEN}./scripts/cleanup-merged-branches.sh --delete${NC}"
fi

