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
    private static Entry[] Catalog =>
    [
        // ── 文件 / 搜索 ──
        new() { Name = "filesystem", Category = L.Pick("文件", "Files"), Description = L.Pick("文件系统读写/遍历（安全受限目录）", "Filesystem read/write/traversal (restricted safe directories)"), Args = ["-y", "@modelcontextprotocol/server-filesystem", "."] },
        new() { Name = "everything", Category = L.Pick("文件", "Files"), Description = L.Pick("Everything 桌面文件搜索（Windows）", "Everything desktop file search (Windows)"), Args = ["-y", "@modelcontextprotocol/server-everything"] },
        new() { Name = "fetch", Category = L.Pick("文件", "Files"), Description = L.Pick("网页抓取并转 Markdown", "Fetch web pages and convert them to Markdown"), Args = ["-y", "@modelcontextprotocol/server-fetch"] },
        new() { Name = "pdf", Category = L.Pick("文件", "Files"), Description = L.Pick("PDF 读取/分页提取/标注（支持本地与 arxiv 等来源）", "Read PDFs, extract by page and annotate (local files and sources such as arxiv)"), Args = ["-y", "--silent", "--registry=https://registry.npmjs.org/", "@modelcontextprotocol/server-pdf", "--stdio"] },

        // ── 版本控制 ──
        new() { Name = "git", Category = L.Pick("版本控制", "Version control"), Description = L.Pick("Git 仓库操作（status/log/diff/commit）", "Git repository operations (status/log/diff/commit)"), Args = ["-y", "@modelcontextprotocol/server-git"] },
        new() { Name = "github", Category = L.Pick("版本控制", "Version control"), Description = L.Pick("GitHub 仓库/PR/Issue（需 GITHUB_TOKEN）", "GitHub repositories, PRs and issues (requires GITHUB_TOKEN)"), Args = ["-y", "@modelcontextprotocol/server-github"], Env = new() { ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "${GITHUB_TOKEN}" } },
        new() { Name = "gitlab", Category = L.Pick("版本控制", "Version control"), Description = L.Pick("GitLab 项目/文件/Issue/MR（需 GITLAB_TOKEN）", "GitLab projects, files, issues and MRs (requires GITLAB_TOKEN)"), Args = ["-y", "@modelcontextprotocol/server-gitlab"], Env = new() { ["GITLAB_PERSONAL_ACCESS_TOKEN"] = "${GITLAB_TOKEN}", ["GITLAB_API_URL"] = "${GITLAB_API_URL}" } },

        // ── 浏览器 ──
        new() { Name = "puppeteer", Category = L.Pick("浏览器", "Browsers"), Description = L.Pick("无头浏览器自动化（截图/导航/点击）", "Headless browser automation (screenshots/navigation/clicks)"), Args = ["-y", "@modelcontextprotocol/server-puppeteer"] },
        new() { Name = "playwright", Category = L.Pick("浏览器", "Browsers"), Description = L.Pick("Playwright 浏览器自动化（微软出品）", "Playwright browser automation (by Microsoft)"), Args = ["-y", "@playwright/mcp@latest"] },
        new() { Name = "browserbase", Category = L.Pick("浏览器", "Browsers"), Description = L.Pick("Browserbase 云端浏览器自动化（需 API Key/Project）", "Browserbase cloud browser automation (requires API Key/Project)"), Args = ["-y", "@browserbasehq/mcp"], Env = new() { ["BROWSERBASE_API_KEY"] = "${BROWSERBASE_API_KEY}", ["BROWSERBASE_PROJECT_ID"] = "${BROWSERBASE_PROJECT_ID}" } },

        // ── 搜索 ──
        new() { Name = "brave-search", Category = L.Pick("搜索", "Search"), Description = L.Pick("Brave 网页搜索（需 BRAVE_API_KEY）", "Brave web search (requires BRAVE_API_KEY)"), Args = ["-y", "@modelcontextprotocol/server-brave-search"], Env = new() { ["BRAVE_API_KEY"] = "${BRAVE_API_KEY}" } },
        new() { Name = "firecrawl", Category = L.Pick("搜索", "Search"), Description = L.Pick("Firecrawl 网页抓取+搜索（需 FIRECRAWL_API_KEY）", "Firecrawl web scraping and search (requires FIRECRAWL_API_KEY)"), Args = ["-y", "firecrawl-mcp"], Env = new() { ["FIRECRAWL_API_KEY"] = "${FIRECRAWL_API_KEY}" } },
        new() { Name = "tavily", Category = L.Pick("搜索", "Search"), Description = L.Pick("Tavily 实时网页搜索（需 TAVILY_API_KEY）", "Tavily real-time web search (requires TAVILY_API_KEY)"), Args = ["-y", "tavily-mcp"], Env = new() { ["TAVILY_API_KEY"] = "${TAVILY_API_KEY}" } },
        new() { Name = "exa", Category = L.Pick("搜索", "Search"), Description = L.Pick("Exa 语义搜索（需 EXA_API_KEY）", "Exa semantic search (requires EXA_API_KEY)"), Args = ["-y", "@exa/mcp-server"], Env = new() { ["EXA_API_KEY"] = "${EXA_API_KEY}" } },
        new() { Name = "perplexity", Category = L.Pick("搜索", "Search"), Description = L.Pick("Perplexity 实时搜索/深度研究（需 PERPLEXITY_API_KEY）", "Perplexity real-time search and deep research (requires PERPLEXITY_API_KEY)"), Args = ["-y", "@perplexity-ai/mcp-server"], Env = new() { ["PERPLEXITY_API_KEY"] = "${PERPLEXITY_API_KEY}" } },
        new() { Name = "duckduckgo", Category = L.Pick("搜索", "Search"), Description = L.Pick("DuckDuckGo 网页搜索（Python/uvx，无需 key）", "DuckDuckGo web search (Python/uvx, no key needed)"), Command = "uvx", Args = ["duckduckgo-mcp-server"] },
        new() { Name = "aws-kb-retrieval", Category = L.Pick("搜索", "Search"), Description = L.Pick("AWS Bedrock Knowledge Base RAG（需 AWS 凭证）", "AWS Bedrock Knowledge Base RAG (requires AWS credentials)"), Args = ["-y", "@modelcontextprotocol/server-aws-kb-retrieval"], Env = new() { ["AWS_ACCESS_KEY_ID"] = "${AWS_ACCESS_KEY_ID}", ["AWS_SECRET_ACCESS_KEY"] = "${AWS_SECRET_ACCESS_KEY}", ["AWS_REGION"] = "${AWS_REGION}" } },
        new() { Name = "serper", Category = L.Pick("搜索", "Search"), Description = L.Pick("Serper Google 搜索 API（需 SERPER_API_KEY）", "Serper Google search API (requires SERPER_API_KEY)"), Args = ["-y", "mcp-server-serper"], Env = new() { ["SERPER_API_KEY"] = "${SERPER_API_KEY}" } },
        new() { Name = "baidu", Category = L.Pick("搜索", "Search"), Description = L.Pick("百度中文搜索（免费，无需 key）", "Baidu Chinese search (free, no key needed)"), Args = ["-y", "baidu-search-mcp"] },
        new() { Name = "searxng", Category = L.Pick("搜索", "Search"), Description = L.Pick("SearXNG 自托管元搜索（需 SEARXNG_SERVER_URL，可接国内实例）", "SearXNG self-hosted meta search (requires SEARXNG_SERVER_URL; a local instance works)"), Args = ["-y", "searxng-mcp"], Env = new() { ["SEARXNG_SERVER_URL"] = "${SEARXNG_SERVER_URL}" } },

        // ── 数据库 ──
        new() { Name = "sqlite", Category = L.Pick("数据库", "Databases"), Description = L.Pick("SQLite 数据库查询", "SQLite database queries"), Args = ["-y", "@modelcontextprotocol/server-sqlite", "data.db"] },
        new() { Name = "postgres", Category = L.Pick("数据库", "Databases"), Description = L.Pick("PostgreSQL 查询（改连接串）", "PostgreSQL queries (edit the connection string)"), Args = ["-y", "@modelcontextprotocol/server-postgres", "postgresql://localhost:5432/postgres"] },
        new() { Name = "mongodb", Category = L.Pick("数据库", "Databases"), Description = L.Pick("MongoDB Atlas 查询（需 MONGODB_URI）", "MongoDB Atlas queries (requires MONGODB_URI)"), Args = ["-y", "@mongodb/mcp"], Env = new() { ["MONGODB_URI"] = "${MONGODB_URI}" } },
        new() { Name = "neo4j", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Neo4j 图数据库查询（需 NEO4J_URI）", "Neo4j graph database queries (requires NEO4J_URI)"), Args = ["-y", "@neo4j/mcp-server"], Env = new() { ["NEO4J_URI"] = "${NEO4J_URI}", ["NEO4J_USERNAME"] = "${NEO4J_USERNAME}", ["NEO4J_PASSWORD"] = "${NEO4J_PASSWORD}" } },
        new() { Name = "mysql", Category = L.Pick("数据库", "Databases"), Description = L.Pick("MySQL 查询（默认只读，需 MYSQL_HOST/PORT/USER/PASS/DB）", "MySQL queries (read-only by default; requires MYSQL_HOST/PORT/USER/PASS/DB)"), Args = ["-y", "@benborla29/mcp-server-mysql"], Env = new() { ["MYSQL_HOST"] = "${MYSQL_HOST}", ["MYSQL_PORT"] = "${MYSQL_PORT}", ["MYSQL_USER"] = "${MYSQL_USER}", ["MYSQL_PASS"] = "${MYSQL_PASS}", ["MYSQL_DB"] = "${MYSQL_DB}" } },
        new() { Name = "redis", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Redis key-value 查询（默认 localhost:6379）", "Redis key-value queries (defaults to localhost:6379)"), Args = ["-y", "@modelcontextprotocol/server-redis", "redis://localhost:6379"] },
        new() { Name = "chroma", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Chroma 向量数据库（RAG 检索，默认本地 :8000）", "Chroma vector database (RAG retrieval; defaults to local :8000)"), Args = ["-y", "chromadb-mcp"] },
        new() { Name = "qdrant", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Qdrant 向量数据库（需 QDRANT_URL/API_KEY）", "Qdrant vector database (requires QDRANT_URL/API_KEY)"), Args = ["-y", "mcp-server-qdrant"], Env = new() { ["QDRANT_URL"] = "${QDRANT_URL}", ["QDRANT_API_KEY"] = "${QDRANT_API_KEY}" } },
        new() { Name = "elasticsearch", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Elasticsearch 全文检索（需 ES_URL/ES_API_KEY）", "Elasticsearch full-text search (requires ES_URL/ES_API_KEY)"), Args = ["-y", "@elastic/mcp-server-elasticsearch"], Env = new() { ["ES_URL"] = "${ES_URL}", ["ES_API_KEY"] = "${ES_API_KEY}" } },
        new() { Name = "weaviate", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Weaviate 向量数据库（需 WEAVIATE_URL/API_KEY）", "Weaviate vector database (requires WEAVIATE_URL/API_KEY)"), Args = ["-y", "mcp-server-weaviate"], Env = new() { ["WEAVIATE_URL"] = "${WEAVIATE_URL}", ["WEAVIATE_API_KEY"] = "${WEAVIATE_API_KEY}" } },
        new() { Name = "snowflake", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Snowflake 数据仓库（需 SNOWFLAKE_ACCOUNT/USER/PASSWORD）", "Snowflake data warehouse (requires SNOWFLAKE_ACCOUNT/USER/PASSWORD)"), Args = ["-y", "snowflake-mcp"], Env = new() { ["SNOWFLAKE_ACCOUNT"] = "${SNOWFLAKE_ACCOUNT}", ["SNOWFLAKE_USER"] = "${SNOWFLAKE_USER}", ["SNOWFLAKE_PASSWORD"] = "${SNOWFLAKE_PASSWORD}" } },
        new() { Name = "duckdb", Category = L.Pick("数据库", "Databases"), Description = L.Pick("DuckDB 嵌入式分析数据库（本地文件）", "DuckDB embedded analytics database (local file)"), Args = ["-y", "duckdb-mcp"] },
        new() { Name = "clickhouse", Category = L.Pick("数据库", "Databases"), Description = L.Pick("ClickHouse 列式分析库（需 CLICKHOUSE_URL）", "ClickHouse columnar analytics database (requires CLICKHOUSE_URL)"), Args = ["-y", "clickhouse-mcp"], Env = new() { ["CLICKHOUSE_URL"] = "${CLICKHOUSE_URL}" } },
        new() { Name = "typesense", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Typesense 搜索引擎（需 TYPESENSE_HOST/API_KEY）", "Typesense search engine (requires TYPESENSE_HOST/API_KEY)"), Args = ["-y", "typesense-mcp"], Env = new() { ["TYPESENSE_HOST"] = "${TYPESENSE_HOST}", ["TYPESENSE_API_KEY"] = "${TYPESENSE_API_KEY}" } },
        new() { Name = "pinecone", Category = L.Pick("数据库", "Databases"), Description = L.Pick("Pinecone 向量数据库（需 PINECONE_API_KEY）", "Pinecone vector database (requires PINECONE_API_KEY)"), Args = ["-y", "@pinecone-database/mcp"], Env = new() { ["PINECONE_API_KEY"] = "${PINECONE_API_KEY}" } },

        // ── 云平台 ──
        new() { Name = "aws", Category = L.Pick("云", "Cloud"), Description = L.Pick("AWS 云服务（EC2/S3/Lambda 等，需 AWS 凭证）", "AWS cloud services (EC2/S3/Lambda, etc.; requires AWS credentials)"), Args = ["-y", "aws-mcp"], Env = new() { ["AWS_ACCESS_KEY_ID"] = "${AWS_ACCESS_KEY_ID}", ["AWS_SECRET_ACCESS_KEY"] = "${AWS_SECRET_ACCESS_KEY}", ["AWS_REGION"] = "${AWS_REGION}" } },
        new() { Name = "google-cloud", Category = L.Pick("云", "Cloud"), Description = L.Pick("Google Cloud 服务（需项目 + 凭证）", "Google Cloud services (requires a project and credentials)"), Args = ["-y", "google-cloud-mcp"], Env = new() { ["GOOGLE_CLOUD_PROJECT"] = "${GOOGLE_CLOUD_PROJECT}", ["GOOGLE_APPLICATION_CREDENTIALS"] = "${GOOGLE_APPLICATION_CREDENTIALS}" } },
        new() { Name = "firebase", Category = L.Pick("云", "Cloud"), Description = L.Pick("Firebase 数据库/认证/存储（需项目 + 凭证）", "Firebase database, auth and storage (requires a project and credentials)"), Args = ["-y", "firebase-mcp"], Env = new() { ["FIREBASE_PROJECT_ID"] = "${FIREBASE_PROJECT_ID}", ["GOOGLE_APPLICATION_CREDENTIALS"] = "${GOOGLE_APPLICATION_CREDENTIALS}" } },
        new() { Name = "digitalocean", Category = L.Pick("云", "Cloud"), Description = L.Pick("DigitalOcean 云主机/对象存储（需 DIGITALOCEAN_TOKEN）", "DigitalOcean droplets and object storage (requires DIGITALOCEAN_TOKEN)"), Args = ["-y", "@digitalocean/mcp"], Env = new() { ["DIGITALOCEAN_TOKEN"] = "${DIGITALOCEAN_TOKEN}" } },

        // ── 记忆 / 思考 ──
        new() { Name = "memory", Category = L.Pick("记忆", "Memory"), Description = L.Pick("知识图谱持久记忆", "Persistent knowledge-graph memory"), Args = ["-y", "@modelcontextprotocol/server-memory"] },
        new() { Name = "sequential-thinking", Category = L.Pick("记忆", "Memory"), Description = L.Pick("多步顺序思考（复杂推理）", "Multi-step sequential thinking (complex reasoning)"), Args = ["-y", "@modelcontextprotocol/server-sequential-thinking"] },

        // ── 开发工具 ──
        new() { Name = "context7", Category = L.Pick("开发", "Development"), Description = L.Pick("最新库/框架文档查询", "Look up current library and framework documentation"), Args = ["-y", "@upstash/context7-mcp"] },
        new() { Name = "docker", Category = L.Pick("开发", "Development"), Description = L.Pick("Docker 容器/镜像管理", "Docker container and image management"), Args = ["-y", "@docker/mcp"] },
        new() { Name = "sentry", Category = L.Pick("开发", "Development"), Description = L.Pick("Sentry 错误追踪（需 SENTRY_TOKEN）", "Sentry error tracking (requires SENTRY_TOKEN)"), Args = ["-y", "@sentry/mcp@latest"], Env = new() { ["SENTRY_TOKEN"] = "${SENTRY_TOKEN}" } },
        new() { Name = "figma", Category = L.Pick("开发", "Development"), Description = L.Pick("Figma 设计文件/组件/样式读取（需 FIGMA_ACCESS_TOKEN）", "Read Figma design files, components and styles (requires FIGMA_ACCESS_TOKEN)"), Args = ["-y", "@figma/mcp-server"], Env = new() { ["FIGMA_ACCESS_TOKEN"] = "${FIGMA_ACCESS_TOKEN}" } },
        new() { Name = "chrome-devtools", Category = L.Pick("开发", "Development"), Description = L.Pick("Chrome DevTools 浏览器调试（性能/网络/控制台）", "Chrome DevTools browser debugging (performance/network/console)"), Args = ["-y", "chrome-devtools-mcp@latest"] },
        new() { Name = "e2b", Category = L.Pick("开发", "Development"), Description = L.Pick("E2B 云沙箱执行代码（隔离容器，需 E2B_API_KEY）", "Run code in the E2B cloud sandbox (isolated container; requires E2B_API_KEY)"), Args = ["-y", "@e2b/mcp-server"], Env = new() { ["E2B_API_KEY"] = "${E2B_API_KEY}" } },
        new() { Name = "blender", Category = L.Pick("开发", "Development"), Description = L.Pick("Blender 3D 建模/场景/渲染（需本地 Blender 运行并开启插件）", "Blender 3D modelling, scenes and rendering (requires a local Blender running with the add-on enabled)"), Args = ["-y", "blender-mcp"] },
        new() { Name = "kubernetes", Category = L.Pick("开发", "Development"), Description = L.Pick("Kubernetes 集群管理（pod/部署/日志，用本地 kubeconfig）", "Kubernetes cluster management (pods/deployments/logs, using the local kubeconfig)"), Args = ["-y", "mcp-server-kubernetes"] },
        new() { Name = "screenshotone", Category = L.Pick("开发", "Development"), Description = L.Pick("ScreenshotOne 网页截图（需 SCREENSHOTONE_ACCESS_KEY）", "ScreenshotOne web page screenshots (requires SCREENSHOTONE_ACCESS_KEY)"), Args = ["-y", "@screenshotone/mcp"], Env = new() { ["SCREENSHOTONE_ACCESS_KEY"] = "${SCREENSHOTONE_ACCESS_KEY}" } },
        new() { Name = "midscene", Category = L.Pick("开发", "Development"), Description = L.Pick("Midscene AI UI 自动化测试（需 OPENAI_API_KEY）", "Midscene AI UI test automation (requires OPENAI_API_KEY)"), Args = ["-y", "@midscene/mcp"], Env = new() { ["OPENAI_API_KEY"] = "${OPENAI_API_KEY}" } },
        new() { Name = "magic", Category = L.Pick("开发", "Development"), Description = L.Pick("Magic AI 前端组件生成（需 TWENTY_FIRST_API_KEY）", "Magic AI front-end component generation (requires TWENTY_FIRST_API_KEY)"), Args = ["-y", "@21st-dev/magic"], Env = new() { ["TWENTY_FIRST_API_KEY"] = "${TWENTY_FIRST_API_KEY}" } },
        new() { Name = "composio", Category = L.Pick("开发", "Development"), Description = L.Pick("Composio 工具集成平台（需 COMPOSIO_API_KEY）", "Composio tool integration platform (requires COMPOSIO_API_KEY)"), Args = ["-y", "@composio/mcp"], Env = new() { ["COMPOSIO_API_KEY"] = "${COMPOSIO_API_KEY}" } },
        new() { Name = "openrouter", Category = L.Pick("开发", "Development"), Description = L.Pick("OpenRouter 多模型路由（需 OPENROUTER_API_KEY）", "OpenRouter multi-model routing (requires OPENROUTER_API_KEY)"), Args = ["-y", "@openrouter/mcp"], Env = new() { ["OPENROUTER_API_KEY"] = "${OPENROUTER_API_KEY}" } },
        new() { Name = "posthog", Category = L.Pick("开发", "Development"), Description = L.Pick("PostHog 产品分析（需 POSTHOG_API_KEY）", "PostHog product analytics (requires POSTHOG_API_KEY)"), Args = ["-y", "@posthog/mcp"], Env = new() { ["POSTHOG_API_KEY"] = "${POSTHOG_API_KEY}", ["POSTHOG_HOST"] = "${POSTHOG_HOST}" } },

        // ── 协作 / 办公 ──
        new() { Name = "notion", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Notion 页面/数据库读写（需 NOTION_TOKEN）", "Read and write Notion pages and databases (requires NOTION_TOKEN)"), Args = ["-y", "@notionhq/notion-mcp-server"], Env = new() { ["NOTION_TOKEN"] = "${NOTION_TOKEN}" } },
        new() { Name = "linear", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Linear 项目/Issue 管理（需 LINEAR_API_KEY）", "Linear project and issue management (requires LINEAR_API_KEY)"), Args = ["-y", "@linear/mcp"], Env = new() { ["LINEAR_API_KEY"] = "${LINEAR_API_KEY}" } },
        new() { Name = "atlassian", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Atlassian Jira/Confluence（需 ATLASSIAN_API_KEY）", "Atlassian Jira/Confluence (requires ATLASSIAN_API_KEY)"), Args = ["-y", "@atlassian/atlassian-mcp"], Env = new() { ["ATLASSIAN_API_KEY"] = "${ATLASSIAN_API_KEY}" } },
        new() { Name = "gdrive", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Google Drive 文件搜索/读取（首次需 OAuth auth）", "Google Drive file search and reading (first run needs OAuth auth)"), Args = ["-y", "@modelcontextprotocol/server-gdrive"], Env = new() { ["GDRIVE_OAUTH_PATH"] = "${GDRIVE_OAUTH_PATH}", ["GDRIVE_CREDENTIALS_PATH"] = "${GDRIVE_CREDENTIALS_PATH}" } },
        new() { Name = "trello", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Trello 看板/卡片/列表（需 TRELLO_API_KEY/TOKEN）", "Trello boards, cards and lists (requires TRELLO_API_KEY/TOKEN)"), Args = ["-y", "mcp-server-trello"], Env = new() { ["TRELLO_API_KEY"] = "${TRELLO_API_KEY}", ["TRELLO_TOKEN"] = "${TRELLO_TOKEN}" } },
        new() { Name = "clickup", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("ClickUp 任务/列表/文档（需 CLICKUP_API_KEY）", "ClickUp tasks, lists and docs (requires CLICKUP_API_KEY)"), Args = ["-y", "mcp-server-clickup"], Env = new() { ["CLICKUP_API_KEY"] = "${CLICKUP_API_KEY}" } },
        new() { Name = "gmail", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Gmail 邮件读写/搜索（需 OAuth token）", "Read, write and search Gmail (requires an OAuth token)"), Args = ["-y", "gmail-mcp"], Env = new() { ["GMAIL_OAUTH_TOKEN"] = "${GMAIL_OAUTH_TOKEN}" } },
        new() { Name = "google-calendar", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Google Calendar 日程/会议（需 OAuth token）", "Google Calendar events and meetings (requires an OAuth token)"), Args = ["-y", "google-calendar-mcp"], Env = new() { ["GOOGLE_OAUTH_TOKEN"] = "${GOOGLE_OAUTH_TOKEN}" } },
        new() { Name = "shopify", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Shopify 商品/订单/客户（需 SHOPIFY_ACCESS_TOKEN）", "Shopify products, orders and customers (requires SHOPIFY_ACCESS_TOKEN)"), Args = ["-y", "shopify-mcp"], Env = new() { ["SHOPIFY_SHOP_URL"] = "${SHOPIFY_SHOP_URL}", ["SHOPIFY_ACCESS_TOKEN"] = "${SHOPIFY_ACCESS_TOKEN}" } },
        new() { Name = "hubspot", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("HubSpot CRM 客户/线索（需 HUBSPOT_API_KEY）", "HubSpot CRM contacts and leads (requires HUBSPOT_API_KEY)"), Args = ["-y", "hubspot-mcp"], Env = new() { ["HUBSPOT_API_KEY"] = "${HUBSPOT_API_KEY}" } },
        new() { Name = "salesforce", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Salesforce CRM 客户/机会（需实例 + token）", "Salesforce CRM contacts and opportunities (requires an instance and token)"), Args = ["-y", "@salesforce/mcp"], Env = new() { ["SALESFORCE_INSTANCE_URL"] = "${SALESFORCE_INSTANCE_URL}", ["SALESFORCE_ACCESS_TOKEN"] = "${SALESFORCE_ACCESS_TOKEN}" } },
        new() { Name = "zendesk", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Zendesk 工单/客服（需子域 + email + token）", "Zendesk tickets and support (requires a subdomain, email and token)"), Args = ["-y", "zendesk-mcp"], Env = new() { ["ZENDESK_SUBDOMAIN"] = "${ZENDESK_SUBDOMAIN}", ["ZENDESK_EMAIL"] = "${ZENDESK_EMAIL}", ["ZENDESK_API_TOKEN"] = "${ZENDESK_API_TOKEN}" } },
        new() { Name = "mailchimp", Category = L.Pick("协作", "Collaboration"), Description = L.Pick("Mailchimp 邮件营销/订阅者（需 MAILCHIMP_API_KEY）", "Mailchimp email marketing and subscribers (requires MAILCHIMP_API_KEY)"), Args = ["-y", "mailchimp-mcp"], Env = new() { ["MAILCHIMP_API_KEY"] = "${MAILCHIMP_API_KEY}" } },

        // ── 通讯 ──
        new() { Name = "discord", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("Discord 消息/频道/机器人（需 DISCORD_TOKEN）", "Discord messages, channels and bots (requires DISCORD_TOKEN)"), Args = ["-y", "discord-mcp"], Env = new() { ["DISCORD_TOKEN"] = "${DISCORD_TOKEN}" } },
        new() { Name = "telegram", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("Telegram 消息/群组/机器人（需 TELEGRAM_BOT_TOKEN）", "Telegram messages, groups and bots (requires TELEGRAM_BOT_TOKEN)"), Args = ["-y", "telegram-mcp"], Env = new() { ["TELEGRAM_BOT_TOKEN"] = "${TELEGRAM_BOT_TOKEN}" } },
        new() { Name = "whatsapp", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("WhatsApp 消息（需 WHATSAPP_API_TOKEN）", "WhatsApp messages (requires WHATSAPP_API_TOKEN)"), Args = ["-y", "whatsapp-mcp"], Env = new() { ["WHATSAPP_API_TOKEN"] = "${WHATSAPP_API_TOKEN}" } },
        new() { Name = "twilio", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("Twilio 短信/语音/验证码（需 SID + Auth Token）", "Twilio SMS, voice and verification codes (requires SID and Auth Token)"), Args = ["-y", "twilio-mcp"], Env = new() { ["TWILIO_ACCOUNT_SID"] = "${TWILIO_ACCOUNT_SID}", ["TWILIO_AUTH_TOKEN"] = "${TWILIO_AUTH_TOKEN}" } },
        new() { Name = "weixin", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("微信收发消息（扫码登录即用，无需公众号）", "Send and receive WeChat messages (works right after QR login; no official account needed)"), Args = ["-y", "weixin-mcp"] },
        new() { Name = "qq", Category = L.Pick("通讯", "Messaging"), Description = L.Pick("QQ 群消息发送（需 QQ_API_URL + QQ_TOKEN，走 HTTP API）", "Send QQ group messages (requires QQ_API_URL and QQ_TOKEN; uses the HTTP API)"), Command = "uvx", Args = ["qq-mcp"], Env = new() { ["QQ_API_URL"] = "${QQ_API_URL}", ["QQ_TOKEN"] = "${QQ_TOKEN}", ["QQ_GROUP_ID"] = "${QQ_GROUP_ID}" } },

        // ── 云 / 服务 ──
        new() { Name = "time", Category = L.Pick("服务", "Services"), Description = L.Pick("时间/时区转换", "Time and time-zone conversion"), Args = ["-y", "@modelcontextprotocol/server-time"] },
        new() { Name = "slack", Category = L.Pick("服务", "Services"), Description = L.Pick("Slack 消息/频道（需 SLACK_BOT_TOKEN）", "Slack messages and channels (requires SLACK_BOT_TOKEN)"), Args = ["-y", "@modelcontextprotocol/server-slack"], Env = new() { ["SLACK_BOT_TOKEN"] = "${SLACK_BOT_TOKEN}" } },
        new() { Name = "google-maps", Category = L.Pick("服务", "Services"), Description = L.Pick("Google Maps 地理/路线（需 API key）", "Google Maps geocoding and directions (requires an API key)"), Args = ["-y", "@modelcontextprotocol/server-google-maps"], Env = new() { ["GOOGLE_MAPS_API_KEY"] = "${GOOGLE_MAPS_API_KEY}" } },
        new() { Name = "stripe", Category = L.Pick("服务", "Services"), Description = L.Pick("Stripe 支付/账单查询（需 STRIPE_SECRET_KEY）", "Stripe payments and billing queries (requires STRIPE_SECRET_KEY)"), Args = ["-y", "@stripe/mcp-server"], Env = new() { ["STRIPE_SECRET_KEY"] = "${STRIPE_SECRET_KEY}" } },
        new() { Name = "supabase", Category = L.Pick("服务", "Services"), Description = L.Pick("Supabase 数据库/认证（需 SUPABASE_ACCESS_TOKEN）", "Supabase database and auth (requires SUPABASE_ACCESS_TOKEN)"), Args = ["-y", "@supabase/mcp-server-supabase"], Env = new() { ["SUPABASE_ACCESS_TOKEN"] = "${SUPABASE_ACCESS_TOKEN}" } },
        new() { Name = "cloudflare", Category = L.Pick("服务", "Services"), Description = L.Pick("Cloudflare Workers/KV（需 CLOUDFLARE_API_TOKEN）", "Cloudflare Workers and KV (requires CLOUDFLARE_API_TOKEN)"), Args = ["-y", "@cloudflare/mcp-server-cloudflare"], Env = new() { ["CLOUDFLARE_API_TOKEN"] = "${CLOUDFLARE_API_TOKEN}", ["CLOUDFLARE_ACCOUNT_ID"] = "${CLOUDFLARE_ACCOUNT_ID}" } },
        new() { Name = "resend", Category = L.Pick("服务", "Services"), Description = L.Pick("Resend 邮件发送（需 RESEND_API_KEY）", "Send email with Resend (requires RESEND_API_KEY)"), Args = ["-y", "resend-mcp"], Env = new() { ["RESEND_API_KEY"] = "${RESEND_API_KEY}" } },
        new() { Name = "weather", Category = L.Pick("服务", "Services"), Description = L.Pick("天气/预报查询（免费，无需 key）", "Weather and forecast lookup (free, no key needed)"), Args = ["-y", "mcp-server-weather"] },
        new() { Name = "spotify", Category = L.Pick("服务", "Services"), Description = L.Pick("Spotify 音乐/歌单/播放（需 CLIENT_ID/SECRET）", "Spotify music, playlists and playback (requires CLIENT_ID/SECRET)"), Args = ["-y", "spotify-mcp"], Env = new() { ["SPOTIFY_CLIENT_ID"] = "${SPOTIFY_CLIENT_ID}", ["SPOTIFY_CLIENT_SECRET"] = "${SPOTIFY_CLIENT_SECRET}" } },
        new() { Name = "zapier", Category = L.Pick("服务", "Services"), Description = L.Pick("Zapier 自动化工作流（需 ZAPIER_API_KEY）", "Zapier automation workflows (requires ZAPIER_API_KEY)"), Args = ["-y", "zapier-mcp"], Env = new() { ["ZAPIER_API_KEY"] = "${ZAPIER_API_KEY}" } },
        new() { Name = "n8n", Category = L.Pick("服务", "Services"), Description = L.Pick("n8n 自动化工作流（需 N8N_API_KEY/HOST）", "n8n automation workflows (requires N8N_API_KEY/HOST)"), Args = ["-y", "n8n-mcp"], Env = new() { ["N8N_API_KEY"] = "${N8N_API_KEY}", ["N8N_HOST"] = "${N8N_HOST}" } },
        new() { Name = "datadog", Category = L.Pick("服务", "Services"), Description = L.Pick("Datadog 监控/日志/指标（需 DD_API_KEY/APP_KEY）", "Datadog monitoring, logs and metrics (requires DD_API_KEY/APP_KEY)"), Args = ["-y", "datadog-mcp"], Env = new() { ["DD_API_KEY"] = "${DD_API_KEY}", ["DD_APP_KEY"] = "${DD_APP_KEY}", ["DD_SITE"] = "${DD_SITE}" } },

        // ── 部署 ──
        new() { Name = "netlify", Category = L.Pick("部署", "Deployment"), Description = L.Pick("Netlify 站点部署/环境变量/域名（需 NETLIFY_AUTH_TOKEN）", "Netlify site deploys, environment variables and domains (requires NETLIFY_AUTH_TOKEN)"), Args = ["-y", "@netlify/mcp"], Env = new() { ["NETLIFY_AUTH_TOKEN"] = "${NETLIFY_AUTH_TOKEN}" } },
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
