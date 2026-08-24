namespace WayCoder.Tools;

/// <summary>
/// 内置 MCP 服务器目录 —— 精选社区常用 MCP 服务器（对标 Claude Code 800+ 服务器的精选子集），
/// 供 <c>/mcp list</c> 浏览、<c>/mcp add &lt;name&gt;</c> 一键写入 .waycoder/mcp_servers.json。
///
/// 设计约束：AOT 零反射、零网络依赖 —— 纯静态数据表，只在用户显式 add 时生成配置。
/// 全部采用 stdio 传输（npx / uvx / docker 任意 command，最通用、跨平台），需要 API key 的服务器用 ${VAR} 环境变量占位，
/// 用户先 export 对应环境变量再 add 即可直接可用（连接时经 ExpandEnvVars 展开）。
/// </summary>
public static class McpCatalog
{
    /// <summary>单条目录项：服务器名 + 描述 + 分类 + stdio 启动命令/参数/环境变量。</summary>
    public sealed class Entry
    {
        public string Name = "";
        public string Description = "";
        public string Category = "";
        public string Command = "npx";
        public List<string> Args = [];
        public Dictionary<string, string> Env = [];
    }

    /// <summary>内置目录（静态数据表，按分类分组排序）。</summary>
    private static readonly Entry[] Catalog =
    [
        // ── 文件 / 搜索 ──
        new() { Name = "filesystem", Category = "文件", Description = "文件系统读写/遍历（安全受限目录）", Args = ["-y", "@modelcontextprotocol/server-filesystem", "."] },
        new() { Name = "everything", Category = "文件", Description = "Everything 桌面文件搜索（Windows）", Args = ["-y", "@modelcontextprotocol/server-everything"] },
        new() { Name = "fetch", Category = "文件", Description = "网页抓取并转 Markdown", Args = ["-y", "@modelcontextprotocol/server-fetch"] },
        new() { Name = "pdf", Category = "文件", Description = "PDF 读取/分页提取/标注（支持本地与 arxiv 等来源）", Args = ["-y", "--silent", "--registry=https://registry.npmjs.org/", "@modelcontextprotocol/server-pdf", "--stdio"] },

        // ── 版本控制 ──
        new() { Name = "git", Category = "版本控制", Description = "Git 仓库操作（status/log/diff/commit）", Args = ["-y", "@modelcontextprotocol/server-git"] },
        new() { Name = "github", Category = "版本控制", Description = "GitHub 仓库/PR/Issue（需 GITHUB_TOKEN）", Args = ["-y", "@modelcontextprotocol/server-github"], Env = new() { ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "${GITHUB_TOKEN}" } },
        new() { Name = "gitlab", Category = "版本控制", Description = "GitLab 项目/文件/Issue/MR（需 GITLAB_TOKEN）", Args = ["-y", "@modelcontextprotocol/server-gitlab"], Env = new() { ["GITLAB_PERSONAL_ACCESS_TOKEN"] = "${GITLAB_TOKEN}", ["GITLAB_API_URL"] = "${GITLAB_API_URL}" } },

        // ── 浏览器 ──
        new() { Name = "puppeteer", Category = "浏览器", Description = "无头浏览器自动化（截图/导航/点击）", Args = ["-y", "@modelcontextprotocol/server-puppeteer"] },
        new() { Name = "playwright", Category = "浏览器", Description = "Playwright 浏览器自动化（微软出品）", Args = ["-y", "@playwright/mcp@latest"] },

        // ── 搜索 ──
        new() { Name = "brave-search", Category = "搜索", Description = "Brave 网页搜索（需 BRAVE_API_KEY）", Args = ["-y", "@modelcontextprotocol/server-brave-search"], Env = new() { ["BRAVE_API_KEY"] = "${BRAVE_API_KEY}" } },
        new() { Name = "firecrawl", Category = "搜索", Description = "Firecrawl 网页抓取+搜索（需 FIRECRAWL_API_KEY）", Args = ["-y", "firecrawl-mcp"], Env = new() { ["FIRECRAWL_API_KEY"] = "${FIRECRAWL_API_KEY}" } },
        new() { Name = "tavily", Category = "搜索", Description = "Tavily 实时网页搜索（需 TAVILY_API_KEY）", Args = ["-y", "tavily-mcp"], Env = new() { ["TAVILY_API_KEY"] = "${TAVILY_API_KEY}" } },
        new() { Name = "exa", Category = "搜索", Description = "Exa 语义搜索（需 EXA_API_KEY）", Args = ["-y", "@exa/mcp-server"], Env = new() { ["EXA_API_KEY"] = "${EXA_API_KEY}" } },
        new() { Name = "perplexity", Category = "搜索", Description = "Perplexity 实时搜索/深度研究（需 PERPLEXITY_API_KEY）", Args = ["-y", "@perplexity-ai/mcp-server"], Env = new() { ["PERPLEXITY_API_KEY"] = "${PERPLEXITY_API_KEY}" } },
        new() { Name = "duckduckgo", Category = "搜索", Description = "DuckDuckGo 网页搜索（Python/uvx，无需 key）", Command = "uvx", Args = ["duckduckgo-mcp-server"] },
        new() { Name = "aws-kb-retrieval", Category = "搜索", Description = "AWS Bedrock Knowledge Base RAG（需 AWS 凭证）", Args = ["-y", "@modelcontextprotocol/server-aws-kb-retrieval"], Env = new() { ["AWS_ACCESS_KEY_ID"] = "${AWS_ACCESS_KEY_ID}", ["AWS_SECRET_ACCESS_KEY"] = "${AWS_SECRET_ACCESS_KEY}", ["AWS_REGION"] = "${AWS_REGION}" } },

        // ── 数据库 ──
        new() { Name = "sqlite", Category = "数据库", Description = "SQLite 数据库查询", Args = ["-y", "@modelcontextprotocol/server-sqlite", "data.db"] },
        new() { Name = "postgres", Category = "数据库", Description = "PostgreSQL 查询（改连接串）", Args = ["-y", "@modelcontextprotocol/server-postgres", "postgresql://localhost:5432/postgres"] },
        new() { Name = "mongodb", Category = "数据库", Description = "MongoDB Atlas 查询（需 MONGODB_URI）", Args = ["-y", "@mongodb/mcp"], Env = new() { ["MONGODB_URI"] = "${MONGODB_URI}" } },
        new() { Name = "neo4j", Category = "数据库", Description = "Neo4j 图数据库查询（需 NEO4J_URI）", Args = ["-y", "@neo4j/mcp-server"], Env = new() { ["NEO4J_URI"] = "${NEO4J_URI}", ["NEO4J_USERNAME"] = "${NEO4J_USERNAME}", ["NEO4J_PASSWORD"] = "${NEO4J_PASSWORD}" } },
        new() { Name = "mysql", Category = "数据库", Description = "MySQL 查询（默认只读，需 MYSQL_HOST/PORT/USER/PASS/DB）", Args = ["-y", "@benborla29/mcp-server-mysql"], Env = new() { ["MYSQL_HOST"] = "${MYSQL_HOST}", ["MYSQL_PORT"] = "${MYSQL_PORT}", ["MYSQL_USER"] = "${MYSQL_USER}", ["MYSQL_PASS"] = "${MYSQL_PASS}", ["MYSQL_DB"] = "${MYSQL_DB}" } },
        new() { Name = "redis", Category = "数据库", Description = "Redis key-value 查询（默认 localhost:6379）", Args = ["-y", "@modelcontextprotocol/server-redis", "redis://localhost:6379"] },

        // ── 记忆 / 思考 ──
        new() { Name = "memory", Category = "记忆", Description = "知识图谱持久记忆", Args = ["-y", "@modelcontextprotocol/server-memory"] },
        new() { Name = "sequential-thinking", Category = "记忆", Description = "多步顺序思考（复杂推理）", Args = ["-y", "@modelcontextprotocol/server-sequential-thinking"] },

        // ── 开发工具 ──
        new() { Name = "context7", Category = "开发", Description = "最新库/框架文档查询", Args = ["-y", "@upstash/context7-mcp"] },
        new() { Name = "docker", Category = "开发", Description = "Docker 容器/镜像管理", Args = ["-y", "@docker/mcp"] },
        new() { Name = "sentry", Category = "开发", Description = "Sentry 错误追踪（需 SENTRY_TOKEN）", Args = ["-y", "@sentry/mcp@latest"], Env = new() { ["SENTRY_TOKEN"] = "${SENTRY_TOKEN}" } },
        new() { Name = "figma", Category = "开发", Description = "Figma 设计文件/组件/样式读取（需 FIGMA_ACCESS_TOKEN）", Args = ["-y", "@figma/mcp-server"], Env = new() { ["FIGMA_ACCESS_TOKEN"] = "${FIGMA_ACCESS_TOKEN}" } },
        new() { Name = "chrome-devtools", Category = "开发", Description = "Chrome DevTools 浏览器调试（性能/网络/控制台）", Args = ["-y", "chrome-devtools-mcp@latest"] },

        // ── 协作 / 办公 ──
        new() { Name = "notion", Category = "协作", Description = "Notion 页面/数据库读写（需 NOTION_TOKEN）", Args = ["-y", "@notionhq/notion-mcp-server"], Env = new() { ["NOTION_TOKEN"] = "${NOTION_TOKEN}" } },
        new() { Name = "linear", Category = "协作", Description = "Linear 项目/Issue 管理（需 LINEAR_API_KEY）", Args = ["-y", "@linear/mcp"], Env = new() { ["LINEAR_API_KEY"] = "${LINEAR_API_KEY}" } },
        new() { Name = "atlassian", Category = "协作", Description = "Atlassian Jira/Confluence（需 ATLASSIAN_API_KEY）", Args = ["-y", "@atlassian/atlassian-mcp"], Env = new() { ["ATLASSIAN_API_KEY"] = "${ATLASSIAN_API_KEY}" } },
        new() { Name = "gdrive", Category = "协作", Description = "Google Drive 文件搜索/读取（首次需 OAuth auth）", Args = ["-y", "@modelcontextprotocol/server-gdrive"], Env = new() { ["GDRIVE_OAUTH_PATH"] = "${GDRIVE_OAUTH_PATH}", ["GDRIVE_CREDENTIALS_PATH"] = "${GDRIVE_CREDENTIALS_PATH}" } },

        // ── 云 / 服务 ──
        new() { Name = "time", Category = "服务", Description = "时间/时区转换", Args = ["-y", "@modelcontextprotocol/server-time"] },
        new() { Name = "slack", Category = "服务", Description = "Slack 消息/频道（需 SLACK_BOT_TOKEN）", Args = ["-y", "@modelcontextprotocol/server-slack"], Env = new() { ["SLACK_BOT_TOKEN"] = "${SLACK_BOT_TOKEN}" } },
        new() { Name = "google-maps", Category = "服务", Description = "Google Maps 地理/路线（需 API key）", Args = ["-y", "@modelcontextprotocol/server-google-maps"], Env = new() { ["GOOGLE_MAPS_API_KEY"] = "${GOOGLE_MAPS_API_KEY}" } },
        new() { Name = "stripe", Category = "服务", Description = "Stripe 支付/账单查询（需 STRIPE_SECRET_KEY）", Args = ["-y", "@stripe/mcp-server"], Env = new() { ["STRIPE_SECRET_KEY"] = "${STRIPE_SECRET_KEY}" } },
        new() { Name = "supabase", Category = "服务", Description = "Supabase 数据库/认证（需 SUPABASE_ACCESS_TOKEN）", Args = ["-y", "@supabase/mcp-server-supabase"], Env = new() { ["SUPABASE_ACCESS_TOKEN"] = "${SUPABASE_ACCESS_TOKEN}" } },
        new() { Name = "cloudflare", Category = "服务", Description = "Cloudflare Workers/KV（需 CLOUDFLARE_API_TOKEN）", Args = ["-y", "@cloudflare/mcp-server-cloudflare"], Env = new() { ["CLOUDFLARE_API_TOKEN"] = "${CLOUDFLARE_API_TOKEN}", ["CLOUDFLARE_ACCOUNT_ID"] = "${CLOUDFLARE_ACCOUNT_ID}" } },

        // ── 部署 ──
        new() { Name = "netlify", Category = "部署", Description = "Netlify 站点部署/环境变量/域名（需 NETLIFY_AUTH_TOKEN）", Args = ["-y", "@netlify/mcp"], Env = new() { ["NETLIFY_AUTH_TOKEN"] = "${NETLIFY_AUTH_TOKEN}" } },
    ];

    /// <summary>全部目录项（快照）。</summary>
    public static IReadOnlyList<Entry> All => Catalog;

    /// <summary>按名称精确查找（忽略大小写），未找到返回 null。</summary>
    public static Entry? Find(string name)
    {
        foreach (var e in Catalog)
            if (e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return e;
        return null;
    }

    /// <summary>按关键词模糊匹配（名称或描述包含关键词，忽略大小写），空关键词返回全部。</summary>
    public static List<Entry> Search(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<Entry>(Catalog);
        var kw = keyword.Trim();
        var result = new List<Entry>();
        foreach (var e in Catalog)
            if (e.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || e.Category.Contains(kw, StringComparison.OrdinalIgnoreCase))
                result.Add(e);
        return result;
    }

    /// <summary>把目录项转成 mcp_servers.json 的服务器节点（stdio 传输）。</summary>
    public static JNode ToServerNode(Entry e)
    {
        var args = JNode.Array();
        foreach (var a in e.Args) args.Add(a);

        var node = JNode.Object()
            .Set("name", e.Name)
            .Set("command", e.Command)
            .Set("args", args);

        if (e.Env.Count > 0)
        {
            var env = JNode.Object();
            foreach (var kv in e.Env) env.Set(kv.Key, kv.Value);
            node.Set("env", env);
        }
        return node;
    }
}
