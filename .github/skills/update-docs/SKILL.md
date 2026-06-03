---
name: update-docs
description: 'Update documentation after implementation is complete. Use when code is done, implementation finished, update docs, mark tasks complete, update backlog, update README, track progress, document completion, mark phase complete.'
argument-hint: 'Describe what was implemented (e.g., "completed Phase 1 of sprint-1-core-infrastructure")'
---

# Reflector - The System Improver

This skill is responsible for the continuous self-improvement of the development workflow, the upkeep of project documentation. You run at the end of each development session.

## Responsibilities

### 1. Documentation Updates
- Review the implemented code diffs in the `/src` directory.
- Autonomously update the `README.md`, API documentation, or any relevant Architectural Decision Records (ADRs) to reflect the new feature.
- Ensure all new API endpoints, DTOs, and environment variables are documented.

### 2. Skill Instruction Refinement ("Skills as Code")
- Analyze the chat history and the development session.
- Identify where the LLM/AI struggled (e.g., did it fail tests multiple times due to a specific library version? Did it hallucinate a UI component?).
- If a persistent error occurred, open the relevant `.md` instruction file in the `.github/reflector/commonErrors/` directory.
- Add a new, concise rule to this skill's "Strict Constraints" or "Core Rules" section to prevent the error in the future.
- *Constraint:* Do not alter the overarching personality or core models of the skill; only append specific technical constraints based on empirical failures.

### 3. Session Outcome Evaluation
- At the end of the session, evaluate the overall success of the development process.
- Explain to the user each change due to this is a learning process for the user and the system. The user should understand that the system is improving itself over time based on real-world feedback.

## Strict Constraints
- Always explain to the user what instructions you updated and why before you conclude the session.

## Implementation Lessons (if applicable)

**Common lesson categories to document:**
- Testing issues and patterns (fixtures, mocks, async, etc.)
- Architecture decisions and trade-offs
- Performance optimizations with measurements
- Security considerations or vulnerabilities found
- Dependency issues or compatibility problems
- Environment-specific configurations
- API design choices and their implications
- Database schema or migration gotchas

**Also update:**
- README.md with new prerequisites, setup steps, or usage notes
- requirements.txt with any dependencies that were added
- Add links from README to detailed guidance in implementation docs
- Update relevant conftest.py, config files, or setup scripts if patterns changed

## Provide Summary

After making updates, tell the user:
- ✅ Which implementation sections were marked complete
- ✅ Which backlog items were moved to completed
- ✅ What README changes were made
- 📝 What implementation lessons were documented (if any)
- 📋 Next steps from the implementation plan (if any remain)

## Important Notes

- **Link to code**: When updating docs, add links to actual implementation files
- **Preserve structure**: Keep the document's original structure and formatting
- **Be specific**: reference exact phase/section numbers
- **Document lessons proactively**: When issues are resolved, document them to help future developers
- **Show the why**: Don't just document solutions—explain the problem and reasoning

## Anti-patterns

❌ **Don't assume completion**: Always confirm with user what's actually done  
❌ **Don't reorganize**: Keep original document structure intact     
❌ **Don't skip lesson documentation**: If there were gotchas, document them—silence means the next developer will hit the same problems  
❌ **Don't document only the solution**: Explain the problem, why it failed, and when the pattern applies  
