# How to Update Model Information

This guide explains how to keep the AI tooling research current with the latest model releases and pricing.

## Why Regular Updates Are Critical

AI models evolve rapidly:
- **New releases**: Models are superseded every 3-6 months
- **Pricing changes**: Costs often decrease as models mature
- **Capability improvements**: Performance increases with new versions
- **Deprecations**: Older models may be sunset by providers

## Update Schedule

### Weekly Quick Check (10 minutes)
- Scan vendor blogs for major announcements
- Check LMSYS Chatbot Arena for ranking changes
- Monitor r/LocalLLaMA for community buzz

### Monthly Review (1-2 hours)
- Review all vendor pricing pages
- Update cost tables if changes detected
- Check for new model releases
- Update version numbers

### Quarterly Deep Dive (4-6 hours)
- Comprehensive review of all sections
- Update community sentiment from surveys
- Revise recommendations based on benchmarks
- Test new models personally if available
- Update comparison matrices

## Vendor Information Sources

### OpenAI
**Pricing & Models**: https://openai.com/api/pricing/
**Documentation**: https://platform.openai.com/docs/models
**Blog**: https://openai.com/blog
**Key Models to Track**: GPT-4 Turbo, GPT-4o, o1, o1-mini

**What to Check**:
- [ ] Latest model versions and availability
- [ ] Context window sizes
- [ ] Input/output token pricing
- [ ] New capabilities (vision, audio, etc.)
- [ ] Deprecation notices

### Anthropic
**Pricing**: https://www.anthropic.com/pricing
**Documentation**: https://docs.anthropic.com/
**Blog**: https://www.anthropic.com/news
**Key Models**: Claude 3 Opus, Claude 3.5 Sonnet, Claude 3 Haiku

**What to Check**:
- [ ] Claude 3.x version updates
- [ ] Context window improvements
- [ ] Pricing per token
- [ ] New features (computer use, etc.)

### Google (Gemini)
**Pricing**: https://ai.google.dev/pricing
**Documentation**: https://ai.google.dev/docs
**Blog**: https://blog.google/technology/ai/
**Key Models**: Gemini 1.5 Pro, Gemini 1.5 Flash

**What to Check**:
- [ ] Gemini version numbers
- [ ] Context window limits (often experimental)
- [ ] Multi-modal capabilities
- [ ] API pricing vs AI Studio pricing

### Meta (Llama)
**Models**: https://llama.meta.com/
**Hugging Face**: https://huggingface.co/meta-llama
**GitHub**: https://github.com/meta-llama/llama
**Key Models**: Llama 3.1, Llama 3.2

**What to Check**:
- [ ] Latest Llama releases
- [ ] Parameter counts (8B, 70B, 405B)
- [ ] License terms
- [ ] Community benchmarks vs closed models

### Mistral AI
**Pricing**: https://mistral.ai/technology/#pricing
**Documentation**: https://docs.mistral.ai/
**Key Models**: Mistral Large, Mistral Small

**What to Check**:
- [ ] Model versions
- [ ] Pricing tiers
- [ ] EU data residency options

### Code Assistants

**GitHub Copilot**
- Website: https://github.com/features/copilot
- Pricing: https://github.com/features/copilot#pricing
- Check: Subscription tiers, IDE support, new features

**Cursor**
- Website: https://cursor.com/
- Pricing: https://cursor.com/pricing
- Check: Feature updates, model options, pricing

**Codeium**
- Website: https://codeium.com/
- Pricing: https://codeium.com/pricing
- Check: Free tier limits, pro features

### Image Generation

**Midjourney**
- Website: https://www.midjourney.com/
- Discord: Announcements in #announcements channel
- Check: Version releases (v6.1, v7, etc.), subscription tiers

**OpenAI (DALL-E)**
- Pricing: https://openai.com/api/pricing/
- Check: Per-image costs, resolution options

**Stability AI**
- Website: https://stability.ai/
- Hugging Face: https://huggingface.co/stabilityai
- Check: SD3, SDXL updates, API pricing

**Ideogram**
- Website: https://ideogram.ai/
- Check: Version releases, subscription options

**Flux (Black Forest Labs)**
- Website: https://blackforestlabs.ai/
- Hugging Face: https://huggingface.co/black-forest-labs
- Check: Model variants (Pro/Dev/Schnell), availability

## Community Research Sources

### Benchmarks & Leaderboards
1. **LMSYS Chatbot Arena**: https://chat.lmsys.org/?leaderboard
   - Real-world model rankings from user votes
   - Update monthly or when major shifts occur

2. **Open LLM Leaderboard**: https://huggingface.co/spaces/HuggingFaceH4/open_llm_leaderboard
   - Open-source model comparisons
   - Academic benchmark scores

3. **Artificial Analysis**: https://artificialanalysis.ai/
   - Quality, speed, and price comparisons
   - API performance metrics

### Developer Communities

**Reddit**
- r/LocalLLaMA - Open-source models
- r/MachineLearning - Academic research
- r/OpenAI - OpenAI discussions
- r/ClaudeAI - Anthropic discussions

**Discord Servers**
- OpenAI Developer Community
- Anthropic Discord
- Hugging Face Discord
- LocalLLaMA Discord

**Twitter/X Accounts to Follow**
- @OpenAI
- @AnthropicAI
- @GoogleAI
- @AIatMeta
- @simonw (Simon Willison - excellent AI coverage)
- @karpathy (Andrej Karpathy)
- @goodside (Riley Goodside - prompt engineering)

### Industry Analysis

**Blogs & Newsletters**
- Simon Willison's Weblog: https://simonwillison.net/
- The Batch (by deeplearning.ai): https://www.deeplearning.ai/the-batch/
- Import AI by Jack Clark: https://jack-clark.net/
- Last Week in AI: https://lastweekin.ai/

**Surveys**
- State of AI Report (annual): https://www.stateof.ai/
- Stack Overflow Developer Survey (annual)
- LLM usage surveys on Reddit/Twitter

## Update Process

### 1. Check for New Models
```markdown
- [ ] Visit vendor websites listed above
- [ ] Note any new model names or versions
- [ ] Check release dates and availability
- [ ] Review capabilities and limitations
```

### 2. Update Pricing Information
```markdown
- [ ] Check current pricing pages
- [ ] Convert to per-1M-token format if needed
- [ ] Note any pricing tier changes
- [ ] Update cost comparison tables
- [ ] Add pricing disclaimer with update date
```

### 3. Gather Community Feedback
```markdown
- [ ] Review recent Reddit discussions
- [ ] Check LMSYS Arena rankings
- [ ] Look for production case studies
- [ ] Note common praise or complaints
- [ ] Update community rating (⭐) if needed
```

### 4. Update Documentation
```markdown
- [ ] MODEL_MATRIX.md - Update comparison tables
- [ ] README.md - Update model descriptions
- [ ] QUICK_REFERENCE.md - Update quick picks
- [ ] Add notes about superseded models
- [ ] Update "Last Updated" dates
```

### 5. Test and Verify
```markdown
- [ ] Verify all external links work
- [ ] Cross-check pricing across sources
- [ ] Ensure consistency across documents
- [ ] Review for outdated references
```

## Template for Adding New Models

When a new model is released:

```markdown
### [Model Name] (Provider)
**Release Date**: [Month Year]
**Version**: [Version Number]
**Status**: [GA/Preview/Beta]

**Specifications**:
- Context Window: [Size]
- Pricing: $[X] input / $[Y] output per 1M tokens
- Modalities: [Text/Vision/Audio/etc.]

**Key Features**:
- [Feature 1]
- [Feature 2]
- [Feature 3]

**Best For**: [Primary use cases]

**Early Feedback** (update as reviews come in):
- Community Rating: ⭐⭐⭐ (preliminary)
- Strengths: [Based on early testing]
- Weaknesses: [Known limitations]

**Comparison to Previous Version**:
- [How it differs from predecessor]

**Links**:
- Documentation: [URL]
- Pricing: [URL]
- Announcement: [URL]
```

## Deprecation Tracking

When models are deprecated:

1. **Add deprecation notice** to the model entry:
   ```markdown
   ⚠️ **DEPRECATED**: This model is being phased out. 
   Migration deadline: [Date]
   Recommended alternative: [Model Name]
   ```

2. **Move to "Legacy Models" section** after deprecation date

3. **Remove from "Quick Reference"** recommendations

4. **Update code examples** to use current models

## Quality Assurance Checklist

Before publishing updates:

- [ ] All pricing is current (within 30 days)
- [ ] Model versions are latest available
- [ ] External links are valid
- [ ] Comparison tables are consistent
- [ ] No contradictions between documents
- [ ] "Last Updated" dates are current
- [ ] Community ratings reflect recent sentiment
- [ ] Deprecated models are clearly marked
- [ ] New models are added to all relevant sections

## Automation Opportunities

Consider automating these tasks to reduce manual effort:

1. **Price Monitoring**: Scripts to check vendor pricing pages for changes
2. **Benchmark Tracking**: Auto-update rankings from LMSYS API
3. **Link Validation**: Regular broken link checks using tools like `linkchecker`
4. **Change Detection**: Alerts when vendor pages change (e.g., using GitHub Actions)

### Example: Link Validation with GitHub Actions

Create `.github/workflows/check-links.yml`:
```yaml
name: Check Documentation Links
on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday
  workflow_dispatch:

jobs:
  check-links:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Check links
        uses: lycheeverse/lychee-action@v1
        with:
          args: --verbose --no-progress 'Modern-AI-Tooling/*.md'
```

### Example: Price Change Detection

**Note**: Vendor pricing pages often require JavaScript rendering and may have anti-scraping measures. Manual checking is recommended for accuracy. If automation is needed, consider:
- Using official APIs when available
- Playwright/Selenium for JS-rendered pages
- Being respectful of rate limits
- Having fallback to manual verification

## Notes

- **Browser Access**: Some vendor sites may be blocked. Use VPN or alternative access methods if needed.
- **Rate Limiting**: Be respectful of vendor servers when automating checks
- **Accuracy**: Always verify information from multiple sources
- **Timeliness**: Update immediately after major announcements
- **Community Input**: Encourage researchers to submit updates via PR

---

**Maintenance Responsibility**: Assign team member(s) for each vendor
**Review Cycle**: Set calendar reminders for monthly/quarterly reviews
**Documentation**: Keep this guide updated as sources change
