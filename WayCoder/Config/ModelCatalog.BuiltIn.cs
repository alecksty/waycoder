using System.Text;
using WayCoder.Infra;
namespace WayCoder;

public static partial class ModelCatalog
{
    public static readonly ModelInfo[] BuiltIn =
    [
        // OpenAI
        new("gpt-5.5", "GPT-5.5", "OpenAI", "openai", "O", "Flagship", 1_050_000, 5, 30, "https://api.openai.com", "Top reasoning + code + multimodal"),
        new("gpt-5.4", "GPT-5.4", "OpenAI", "openai", "O", "Flagship", 1_050_000, 2.5, 15, "https://api.openai.com", "Cost-effective flagship"),
        new("gpt-5.4-mini", "GPT-5.4 Mini", "OpenAI", "openai", "O", "Light", 400_000, 0.75, 4.5, "https://api.openai.com", "Small model daily tasks"),
        new("gpt-5.4-nano", "GPT-5.4 Nano", "OpenAI", "openai", "O", "Light", 400_000, 0.2, 1.25, "https://api.openai.com", "Tiny model"),
        new("o4-mini", "o4 Mini", "OpenAI", "openai", "O", "Reasoning", 200_000, 1.1, 4.4, "https://api.openai.com", "Reasoning specialist"),
        new("gpt-4.1", "GPT-4.1", "OpenAI", "openai", "O", "Flagship", 1_000_000, 2, 8, "https://api.openai.com", "Ultra-long context"),
        new("gpt-4.1-mini", "GPT-4.1 Mini", "OpenAI", "openai", "O", "Light", 1_000_000, 0.4, 1.6, "https://api.openai.com", "Ultra-long context light"),
        new("gpt-4.1-nano", "GPT-4.1 Nano", "OpenAI", "openai", "O", "Light", 1_000_000, 0.1, 0.4, "https://api.openai.com", "Ultra-long context tiny"),
        new("gpt-4o", "GPT-4o", "OpenAI", "openai", "O", "Flagship", 128_000, 2.5, 10, "https://api.openai.com", "Multimodal flagship (old)"),
        new("gpt-4o-mini", "GPT-4o Mini", "OpenAI", "openai", "O", "Light", 128_000, 0.15, 0.6, "https://api.openai.com", "Multimodal light (old)"),

        // Anthropic
        new("claude-opus-5", "Claude Opus 5", "Anthropic", "anthropic", "A", "Flagship", 200_000, 5, 25, "https://api.anthropic.com", "Best code intelligence"),
        new("claude-sonnet-5", "Claude Sonnet 5", "Anthropic", "anthropic", "A", "Flagship", 1_000_000, 2, 10, "https://api.anthropic.com", "High-performance code"),
        new("claude-haiku-4-5", "Claude Haiku 4.5", "Anthropic", "anthropic", "A", "Light", 200_000, 1, 5, "https://api.anthropic.com", "Fast and light"),
        new("claude-opus-4-6", "Claude Opus 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 5, 25, "https://api.anthropic.com", "Best code (old)"),
        new("claude-sonnet-4-6", "Claude Sonnet 4.6", "Anthropic", "anthropic", "A", "Flagship", 200_000, 3, 15, "https://api.anthropic.com", "High-perf code (old)"),

        // DeepSeek
        new("deepseek-v4-pro", "DeepSeek V4 Pro", "DeepSeek", "deepseek", "D", "Flagship", 1_048_576, 0.435, 0.87, "https://api.deepseek.com", "Flagship deep reasoning", ReasoningEffortAllowed: "low,medium,high"),
        new("deepseek-v4-flash", "DeepSeek V4 Flash", "DeepSeek", "deepseek", "D", "Light", 1_048_576, 0.14, 0.28, "https://api.deepseek.com", "Fast and cost-effective", ReasoningEffortAllowed: "low,medium,high"),
        new("deepseek-chat", "DeepSeek V3 (old)", "DeepSeek", "deepseek", "D", "Flagship", 64_000, 0.27, 1.10, "https://api.deepseek.com", "V3 legacy"),
        new("deepseek-reasoner", "DeepSeek R1", "DeepSeek", "deepseek", "D", "Reasoning", 64_000, 0.55, 2.19, "https://api.deepseek.com", "Deep reasoning"),

        // Google
        new("gemini-2.5-pro", "Gemini 2.5 Pro", "Google", "google", "G", "Flagship", 1_000_000, 1.25, 10, "https://generativelanguage.googleapis.com", "Ultra-long context"),
        new("gemini-2.5-flash", "Gemini 2.5 Flash", "Google", "google", "G", "Light", 1_000_000, 0.30, 2.50, "https://generativelanguage.googleapis.com", "Ultra-long light"),
        new("gemini-2.0-flash", "Gemini 2.0 Flash", "Google", "google", "G", "Light", 1_000_000, 0.10, 0.4, "https://generativelanguage.googleapis.com", "Ultra-fast light"),

        // Alibaba Qwen
        new("qwen3-max", "Qwen3 Max", "Alibaba", "qwen", "Q", "Flagship", 128_000, 0.78, 3.9, "https://dashscope.aliyuncs.com/compatible-mode/v1", "Alibaba flagship"),
        new("qwen3-plus", "Qwen3 Plus", "Alibaba", "qwen", "Q", "Light", 128_000, 0.26, 0.78, null, "Alibaba cost-effective"),
        new("qwen-max", "Qwen Max", "Alibaba", "qwen", "Q", "Flagship", 32_000, 0.78, 3.9, null, "Alibaba old flagship"),
        new("qwen-plus", "Qwen Plus", "Alibaba", "qwen", "Q", "Light", 131_072, 0.26, 0.78, null, "Alibaba old light"),
        new("qwen-turbo", "Qwen Turbo", "Alibaba", "qwen", "Q", "Light", 1_000_000, 0.05, 0.15, null, "Alibaba ultra-fast"),

        // Moonshot Kimi
        new("kimi-k2.5", "Kimi K2.5", "Moonshot", "moonshot", "M", "Flagship", 262_144, 0.45, 2.25, "https://api.moonshot.cn", "Chinese flagship"),

        // Zhipu GLM
        new("glm-4-plus", "GLM-4 Plus", "Zhipu", "zhipu", "Z", "Flagship", 128_000, 0.47, 0.54, "https://open.bigmodel.cn/api/paas/v4", "Chinese flagship", ReasoningEffortAllowed: "low,medium,high"),
        new("glm-4-flash", "GLM-4 Flash", "Zhipu", "zhipu", "Z", "Light", 128_000, 0.07, 0.14, null, "Chinese cost-effective", ReasoningEffortAllowed: "low,medium,high"),

        // ByteDance Doubao
        new("doubao-pro-1.5", "Doubao Pro 1.5", "ByteDance", "bytedance", "B", "Flagship", 128_000, 0.87, 2.6, "https://ark.cn-beijing.volces.com/api/v3", "Doubao flagship"),
        new("doubao-lite-1.5", "Doubao Lite 1.5", "ByteDance", "bytedance", "B", "Light", 128_000, 0.087, 0.26, null, "Doubao light"),

        // 01.AI Yi
        new("yi-large", "Yi Large", "01.AI", "01ai", "Y", "Flagship", 32_000, 0.5, 1.5, "https://api.lingyiwanwu.com", "Chinese flagship"),

        // xAI Grok
        new("grok-3", "Grok 3", "xAI", "xai", "X", "Flagship", 128_000, 3, 15, "https://api.x.ai", "xAI flagship"),

        // Mistral
        new("mistral-large", "Mistral Large", "Mistral", "mistral", "Mi", "Flagship", 128_000, 2, 6, "https://api.mistral.ai", "European flagship"),
        new("mistral-small", "Mistral Small", "Mistral", "mistral", "Mi", "Light", 32_000, 0.2, 0.6, null, "European light"),
        new("codestral", "Codestral", "Mistral", "mistral", "Mi", "Code", 256_000, 0.3, 0.9, null, "Code specialist"),

        // Meta Llama (via OpenRouter / Groq / Together)
        new("llama-4-maverick", "Llama 4 Maverick", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source flagship"),
        new("llama-4-scout", "Llama 4 Scout", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source light"),
        new("llama-3.1-405b", "Llama 3.1 405B", "Meta", "meta", "Ll", "OpenSource", 128_000, 0, 0, null, "Open-source giant"),

        // SiliconFlow (Chinese proxy)
        new("Pro/deepseek-ai/DeepSeek-V3", "DeepSeek V3 (SiliconFlow)", "SiliconFlow", "siliconflow", "S", "Flagship", 64_000, 0, 0, "https://api.siliconflow.cn", "SiliconFlow proxy"),
        new("Pro/Qwen/Qwen3-235B-A22B", "Qwen3 235B (SiliconFlow)", "SiliconFlow", "siliconflow", "S", "Flagship", 128_000, 0, 0, null, "SiliconFlow proxy"),

        // AIHubMix 聚合网关（官网 aihubmix.com 常被墙，默认走 api.inferera.com）
        new("deepseek-v4-pro", "DeepSeek V4 Pro", "AIHubMix", "aihubmix", "Ai", "Flagship", 1_000_000, 0.464, 0.928, "https://api.inferera.com/v1", "DeepSeek flagship via AIHubMix"),
        new("deepseek-v4-flash", "DeepSeek V4 Flash", "AIHubMix", "aihubmix", "Ai", "Light", 1_000_000, 0.154, 0.308, "https://api.inferera.com/v1", "DeepSeek light via AIHubMix"),
        new("coding-kimi-k3", "Coding Kimi K3", "AIHubMix", "aihubmix", "Ai", "Code", 1_048_576, 0.44, 1.61333, "https://api.inferera.com/v1", "Kimi coding via AIHubMix"),
        new("coding-minimax-m3-free", "Coding MiniMax M3 (free)", "AIHubMix", "aihubmix", "Ai", "Light", 204_800, 0, 0, "https://api.inferera.com/v1", "免费代码模型 via AIHubMix"),
        new("glm-5.2", "GLM 5.2", "AIHubMix", "aihubmix", "Ai", "Flagship", 1_000_000, 1.1268, 3.9438, "https://api.inferera.com/v1", "GLM flagship via AIHubMix"),
        new("gemini-2.5-flash", "Gemini 2.5 Flash", "AIHubMix", "aihubmix", "Ai", "Light", 1_048_576, 0.3, 2.499, "https://api.inferera.com/v1", "Gemini flash via AIHubMix"),

        // OpenRouter 聚合网关（模型 id 走 org/model 格式）
        new("openrouter/free", "Free (Auto Router)", "OpenRouter", "openrouter", "Or", "Light", 128_000, 0, 0, "https://openrouter.ai/api/v1", "OpenRouter 自动免费路由"),
        new("cohere/north-mini-code:free", "North Mini Code (free)", "OpenRouter", "openrouter", "Or", "Code", 128_000, 0, 0, "https://openrouter.ai/api/v1", "免费代码模型"),
        new("deepseek/deepseek-chat-v3-0324", "DeepSeek V3", "OpenRouter", "openrouter", "Or", "Flagship", 64_000, 0.25, 1.0, "https://openrouter.ai/api/v1", "DeepSeek V3 via OpenRouter"),
        new("google/gemini-2.5-flash", "Gemini 2.5 Flash", "OpenRouter", "openrouter", "Or", "Light", 1_048_576, 0.3, 2.5, "https://openrouter.ai/api/v1", "Gemini flash via OpenRouter"),
        new("anthropic/claude-sonnet-4-5", "Claude Sonnet 4.5", "OpenRouter", "openrouter", "Or", "Flagship", 200_000, 3, 15, "https://openrouter.ai/api/v1", "Claude Sonnet via OpenRouter"),

        // 2026 新模型（models.dev 2026-08 数据）
        new("gpt-5.5-pro", "GPT-5.5 Pro", "OpenAI", "openai", "O", "Flagship", 1_050_000, 30, 180, "https://api.openai.com", "Top-tier reasoning"),
        new("gpt-5.6", "GPT-5.6", "OpenAI", "openai", "O", "Flagship", 1_050_000, 4, 20, "https://api.openai.com", "Latest balanced flagship"),
        new("o3-pro", "o3 Pro", "OpenAI", "openai", "O", "Reasoning", 200_000, 20, 80, "https://api.openai.com", "Deep reasoning specialist"),
        new("gpt-5.3-codex", "GPT-5.3 Codex", "OpenAI", "openai", "O", "Code", 400_000, 1.75, 14, "https://api.openai.com", "Coding specialist"),
        new("claude-fable-5", "Claude Fable 5", "Anthropic", "anthropic", "A", "Flagship", 1_000_000, 10, 50, "https://api.anthropic.com", "Latest frontier model"),
        new("claude-opus-4-8", "Claude Opus 4.8", "Anthropic", "anthropic", "A", "Flagship", 1_000_000, 5, 25, "https://api.anthropic.com", "Opus lineage (old)"),
        new("deepseek-v4-flash-vision-exp", "DeepSeek V4 Flash Vision", "DeepSeek", "deepseek", "D", "Vision", 1_000_000, 0.14, 0.28, "https://api.deepseek.com", "Vision experimental"),
        new("kimi-k3", "Kimi K3", "Moonshot", "moonshot", "M", "Flagship", 1_048_576, 3, 15, "https://api.moonshot.cn", "Latest flagship"),
        new("kimi-k2.7-code", "Kimi K2.7 Code", "Moonshot", "moonshot", "M", "Code", 262_144, 0.95, 4, "https://api.moonshot.cn", "Coding specialist"),
        new("kimi-k2.6", "Kimi K2.6", "Moonshot", "moonshot", "M", "Flagship", 262_144, 0.95, 4, "https://api.moonshot.cn", "Newer flagship"),
        new("grok-4.6", "Grok 4.6", "xAI", "xai", "X", "Flagship", 500_000, 2, 6, "https://api.x.ai", "Latest Grok"),
        new("gemini-3.1-pro", "Gemini 3.1 Pro", "Google", "google", "G", "Flagship", 1_048_576, 2, 12, "https://generativelanguage.googleapis.com", "Latest flagship"),
        new("gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite", "Google", "google", "G", "Light", 1_048_576, 0.1, 0.4, null, "Ultra-cheap"),
        new("glm-5.3", "GLM-5.3", "Zhipu", "zhipu", "Z", "Flagship", 1_000_000, 1.4, 4.4, "https://open.bigmodel.cn/api/paas/v4", "Latest flagship"),
        new("glm-5", "GLM-5", "Zhipu", "zhipu", "Z", "Flagship", 204_800, 1, 3.2, null, "Flagship"),
        new("glm-5.3-flash", "GLM-5.3 Flash", "Zhipu", "zhipu", "Z", "Light", 1_000_000, 0.075, 0.25, null, "Cost-effective"),
        new("glm-4.7", "GLM-4.7", "Zhipu", "zhipu", "Z", "Flagship", 204_800, 0.6, 2.2, null, "Previous flagship"),
        new("qwen3.7-max", "Qwen3.7 Max", "Alibaba", "qwen", "Q", "Flagship", 1_000_000, 2.5, 7.5, "https://dashscope.aliyuncs.com/compatible-mode/v1", "Latest flagship"),

        // Local / Ollama / LM Studio / vLLM (no API key needed, default base URL http://localhost:11434)
        new("qwen2.5-coder:latest", "Qwen2.5 Coder (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, "http://localhost:11434", "Ollama local code model"),
        new("qwen2.5-coder:3b", "Qwen2.5 Coder 3B (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama small code model"),
        new("qwen2.5-coder:7b", "Qwen2.5 Coder 7B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama mid code model"),
        new("qwen3:8b", "Qwen3 8B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama general model"),
        new("codellama:latest", "CodeLlama (Ollama)", "Local", "local", "L", "Local", 16_000, 0, 0, null, "Ollama local code model"),
        new("deepseek-coder-v2:latest", "DeepSeek Coder V2 (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama local code model"),
        new("deepseek-r1:8b", "DeepSeek R1 8B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama reasoning model"),
        new("deepseek-r1:14b", "DeepSeek R1 14B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama reasoning model"),
        new("llama3.2:3b", "Llama 3.2 3B (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama tiny fast model"),
        new("llama3.1:latest", "Llama 3.1 (Ollama)", "Local", "local", "L", "Local", 128_000, 0, 0, null, "Ollama local model"),
        new("phi4:latest", "Phi-4 (Ollama)", "Local", "local", "L", "Local", 16_000, 0, 0, null, "Ollama local model"),
        new("mistral:latest", "Mistral (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama local model"),
        new("gemma3:latest", "Gemma 3 (Ollama)", "Local", "local", "L", "Local", 32_000, 0, 0, null, "Ollama local model"),
        new("local-model", "Local Model (Custom)", "Local", "local", "L", "Local", 0, 0, 0, "http://localhost:11434", "Any Ollama/LM Studio/vLLM model"),

        // Custom
        new("custom", "Custom Model", "Custom", "custom", "*", "Custom", 0, 0, 0, null, "Enter model ID and API endpoint"),
    ];
}
