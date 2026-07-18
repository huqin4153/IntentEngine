# 注意 
bge-small-zh-v1.5.onnx 文件 92MB，超过 GitHub 推荐的单文件大小上限，已分卷压缩为 3 个 RAR 文件存放在 Resources/ 目录下。使用前需要解压合并
# Important
bge-small-zh-v1.5.onnx is 92MB and exceeds GitHub's recommended file size. It is split into 3 RAR volumes (bge-small-zh-v1.5.part1.rar ~ part3.rar) in the Resources/ directory. Extract them before running

# Intent Engine — 自然语言查询系统

## 这个工具解决什么问题

**一线运维**每天要查数据——业务状态、变更记录、回传日志……每次都要找**二线开发**跑 SQL。
二线开发被打断、等回复、重复劳动。

这个工具让**一线运维自己查**——用自然语言搜，不用写 SQL，不用找开发。

```
运维输入: "查一下这个单子的变更记录"
  → Embedding 自动匹配意图
    → 执行预配 SQL
      → 展示结果
```

**核心理念：AI 只做路由，不做生成。** Embedding 自动适配用户说的话到最匹配的功能。SQL 是预先配好的，执行结果 100% 确定，不会编造数据。

---

> 本项目由 **Claude Code + DeepSeek v4** 完成。后续功能完善、Bug 修复、新数据源适配等可持续使用 Claude Code 进行。

---

## 功能概述

| 功能 | 说明 |
|------|------|
| **自然语言搜索** | 输入"查一下数据修改记录"，系统自动匹配对应功能 |
| **语义匹配** | 基于 BGE-small-zh embedding，不依赖关键词硬匹配 |
| **多步骤编排** | 支持 COUNT 判断、条件跳转（goto）、错误提示、表格展示 |
| **多数据源** | 支持 Oracle、SQLite、SqlServer、MySQL |
| **配置即开发** | 新增查询不需要改代码，导入 JSON 即可 |
| **本地运行** | 不上传数据，不依赖外部 API，无需 GPU |

## 工作原理

```
用户输入 "查一下这个单子的变更记录"
  → Embedding 匹配最相似的意图
    → 命中 "变更记录查询" 意图
      → 弹出参数：单号（必填）
        → 用户填写单号，确认
          → 执行预配的 SQL
            → 展示结果表格
```

**核心设计：AI 只做路由，不做生成。** Embedding 只决定匹配哪个意图。SQL 是预先配置的，执行结果 100% 确定。

---

## 快速开始

### 环境要求

- Windows Server / Windows 10+
- IIS + .NET Framework 4.5
- Oracle Data Access 客户端（如需查询 Oracle）
- Visual C++ Redistributable（如需 ONNX 模型）

### 部署文件

| 文件 | 说明 | 目标位置 |
|------|------|----------|
| `bin/IntentEngine.dll` | 编译后的程序集 | `/app/bin/` |
| `Default.aspx` | 主页面 | `/app/` |
| `Login.aspx` | 登录页面 | `/app/` |
| `Captcha.aspx` | 验证码 | `/app/` |
| `Web.config` | 配置文件 | `/app/` |
| `Static/js/config.js` | 前端配置管理 | `/app/Static/js/` |
| `Static/js/flow.js` | 前端执行流 | `/app/Static/js/` |
| `Static/js/intent.js` | 前端搜索匹配 | `/app/Static/js/` |
| `Static/css/app.css` | 样式 | `/app/Static/css/` |
| `onnxruntime.dll` | ONNX 原生库 | `/app/` 和 `/app/bin/` |
| `Resources/` | 模型文件 | `/app/Resources/` |

### 初始化

```bash
# 1. 停止网站，删除旧数据库（首次启动自动创建）
del /app/Data/IntentEngine.db

# 2. 启动网站
# 3. 访问 http://your-server/app/
# 4. 登录（默认账号：admin / admin123）
```

### 导入演示数据

系统启动后没有任何功能，需要导入配置：

**方法一：导入 JSON 文件**
1. 登录后点击右上角 **配置管理**
2. 点击顶部 **导入** 按钮
3. 打开 `Data/demo_system_query.json`，复制全部内容
4. 粘贴到导入对话框，点击 **导入**
5. 导入成功后点击 **重建向量**

**方法二：手动新增**
1. 在配置管理左侧点击 **+ 新增**
2. 填写意图名称、描述、关键词、分类
3. 添加 Function → 添加参数 → 添加步骤（SQL → show_table → end）
4. 保存后重建向量

> **版本状态**：当前为可运行版本，可满足一线运维的基本查询需求。
>
> **配置生成**：本项目内的功能 JSON 由项目自身配合 Claude Code 生成。其他 AI 生成的内容导入时可能需要手动调整——目前界面的"SQL转配置"功能不太好用，建议直接参考现有 JSON 格式手动编写。

### 验证

导入演示数据后，在搜索框输入：

```
查看系统配置
系统统计概览
查一下意图列表
```

系统应该返回匹配的结果。

---

## 配置说明

### Web.config

```xml
<appSettings>
  <!-- 设为 true 会在搜索结果中显示 SQL 诊断信息 -->
  <add key="DebugSql" value="false" />
  
  <!-- 语义匹配阈值，0-1 之间。越低越容易匹配 -->
  <add key="MatchThreshold" value="0.65" />
</appSettings>
```

### 数据库连接

```xml
<connectionStrings>
  <!-- Config = 系统 SQLite 数据库（默认） -->
  <add name="DefaultConfigDb" connectionString="..." providerName="System.Data.SQLite" />
  
  <!-- BusinessDB = 业务 Oracle 数据库 -->
  <add name="BusinessDB" connectionString="..." providerName="Oracle" />
</connectionStrings>
```

---

## Embedding 模型切换

系统内置两种 Embedding 引擎。

### ONNX 模型（推荐）

文件依赖：

| 文件 | 说明 |
|------|------|
| `Resources/bge-small-zh-v1.5.onnx` | BGE 语义模型（384 维） |
| `Resources/vocab.txt` | 分词器词表 |
| `bin/onnxruntime.dll` | ONNX Runtime 原生库 |
| `bin/Microsoft.ML.OnnxRuntime.dll` | .NET 管理包装器 |

代码入口：`Services/OnnxEmbeddingService.cs`

需要安装 Visual C++ Redistributable。

### CharEmbedding（纯 C# 无依赖）

代码入口：`Services/CharEmbeddingService.cs`

无需模型文件，无需 VC++ 运行库。精度低于 ONNX 但兼容性最好。

### 切换方式

在 `App_Start/IocConfig.cs` 中：

```csharp
// 优先加载 ONNX，失败则回退到 CharEmbedding
var onnx = new OnnxEmbeddingService();
onnx.Load(modelPath, vocabPath);
if (onnx.IsReady) { embedding = onnx; }

if (embedding == null) embedding = new CharEmbeddingService();
```

如果想强制使用 CharEmbedding，删除或重命名 `Resources/bge-small-zh-v1.5.onnx` 即可。

### 升级 Embedding 模型

ONNX 模型的升级路径：

```
1. 下载新的 ONNX 模型文件（如 bge-large-zh 或其他 embedding 模型）
2. 替换 Resources/bge-small-zh-v1.5.onnx
3. 修改 OnnxEmbeddingService.cs 中的模型参数：
```

需要修改 `Services/OnnxEmbeddingService.cs` 中的常量：

```csharp
// 根据新模型的输出维度修改
private const int HIDDEN_SIZE = 384;  // BGE-small-zh 是 384 维
                                    // BGE-large-zh 是 1024 维
                                    // 其他模型请查阅对应文档
```

如果替换为其他 embedding 模型，可能需要调整：

| 需要修改的地方 | 说明 |
|------|------|
| `HIDDEN_SIZE` | 模型输出向量维度（BGE-small=384, BGE-large=1024） |
| `SEQUENCE_LENGTH` | 输入文本最大长度（默认 128） |
| `Tokenizer.cs` | 如果新模型使用不同的分词方式 |
| 输入输出张量名称 | `input_ids`, `attention_mask`, `token_type_ids` 等 |

---

## 为什么是 .NET Framework 4.5

本项目基于 .NET Framework 4.5 构建，原因：

- **目标环境限制**：多数企业生产服务器仍在使用 Windows Server 2012 R2 / 2016，自带 .NET Framework 4.5/4.6，无需额外安装运行时
- **兼容性优先**：IIS 8+ 原生支持，不需要安装 ASP.NET Core 运行时或托管捆绑包

如果需要升级到 .NET 6/8：

```
1. 修改 .csproj 中的 TargetFrameworkVersion
2. 替换 Microsoft.ML.OnnxRuntime 管理包为对应版本
3. 处理 System.Memory、System.Buffers 等依赖的版本冲突
4. 注意 Oracle Data Access 驱动的版本兼容性
```

升级是可行的，但带来的依赖问题需要自行处理。

---

## 为什么 Embedding 不支持中英文混合

当前使用的 BGE-small-zh 模型是**中文专用**模型。它在中文本语语义匹配上表现好，但无法正确处理英文输入。

如果需要中英文混合支持：

| 方案 | 说明 |
|------|------|
| 替换为 BGE-base-en（英文） | 需要下载新模型，修改 HIDDEN_SIZE |
| 替换为 multilingual 模型 | 如 BGE-m3、LaBSE 等多语言模型 |
| 部署两个模型实例 | 根据输入语言动态切换 |
| 使用 OpenAI Embedding API | 牺牲本地化，换取多语言能力 |

嵌入模型的选择取决于业务数据使用的语言，跟本系统架构无关。系统只需要 embedding 输出一个向量进行相似度计算，不限制具体使用什么模型或语言。

---

## 模拟排查逻辑示例

多步骤编排的核心能力是**模拟人工排查流程**：

```
场景：排查一笔异常交易

Step-0: SQL 查询交易基本信息
   ├─ 无记录 → show_error "未找到该交易" → end
   └─ 有记录 → 继续

Step-1: SQL 查询交易状态变更日志
   ├─ 无日志 → show_text "该交易无变更记录"
   └─ 有日志 → show_table 展示变更历史

Step-2: SQL 对比上下游数据
   ├─ 一致 → show_text "上下游数据一致，无需处理"
   └─ 不一致 → show_table 展示差异明细

Step-3: SQL 查询操作人信息
Step-4: end
```

每个步骤独立判断，有数据就展示，没数据就跳过或报错。这模拟了一个运维人员"先查基本信息→再查变更记录→再比对数据→最后查操作人"的完整排查思路。全部在 JSON 中配置，不需要写代码。

---

## 新增查询（配置流程）

1. 点击 **配置管理**
2. 左侧 **+ 新增** 创建意图（名称、描述、关键词、分类）
3. 点击意图名称进入详情
4. **+ 新增 Function** 创建功能（选择数据源）
5. **+ 新增参数** 定义用户输入字段
6. **+ 新增 Step** 编写 SQL 和展示方式
7. 保存后点击 **重建向量**

### 步骤类型

| 类型 | 作用 |
|------|------|
| `sql` | 执行 SQL，支持 @参数 和 $变量 |
| `show_table` | 以表格展示查询结果 |
| `show_text` | 展示文本信息 |
| `show_error` | 展示错误提示 |
| `end` | 结束流程 |

### 条件分支

sql 步骤支持预期检查：

```
SELECT COUNT(*) FROM table WHERE ... → gt 0?
  ├─ 通过（有数据）→ 继续下一步
  └─ 失败（无数据）→ goto 指定步骤 / show_error / stop
```

---

## 项目文件结构

```
IntentEngine/
├── App_Start/          IoC 容器、WebAPI 配置
│   ├── IocConfig.cs    依赖注册
│   └── WebApiConfig.cs WebAPI 路由
├── Controllers/        REST API 接口
│   ├── AuthController.cs      登录认证
│   ├── IntentController.cs    意图匹配
│   ├── FlowController.cs      功能执行
│   └── ConfigController.cs    配置管理
├── DataExecutors/      数据源执行器
│   ├── OracleDataExecutor.cs  Oracle 查询
│   ├── SqliteDataExecutor.cs  SQLite 查询
│   ├── SqlServerDataExecutor.cs
│   └── MySqlDataExecutor.cs
├── Models/             数据模型
│   ├── Intent.cs        意图
│   ├── Function.cs      功能
│   ├── FlowStep.cs      步骤
│   └── FunctionParameter.cs  参数
├── Repositories/       数据访问层
├── Services/           核心逻辑
│   ├── DefaultIntentMatcher.cs   匹配引擎
│   ├── DefaultFlowEngine.cs      执行引擎
│   ├── TemplateEngine.cs         SQL 模板
│   ├── OnnxEmbeddingService.cs   ONNX 嵌入
│   └── CharEmbeddingService.cs   C# 嵌入
├── Static/             前端资源
│   ├── js/config.js     配置管理界面
│   ├── js/flow.js       执行界面
│   └── js/intent.js     搜索界面
└── Data/               数据文件
    ├── IntentEngine.db  系统数据库
    └── demo_system_query.json  演示配置
```

---

## 输入输出示例

### 搜索

```
输入: "查一下系统配了哪些功能"
输出: 意图 "系统配置查询" → 功能列表 → 表格展示

输入: "数据修改记录"
输出: 意图 "系统配置查询" → 参数填写 → 查 QueryLog 表

输入: "帮我看一下意图列表"
输出: 意图 "系统配置查询" → 意图列表查询 → 表格展示
```

### API

```bash
# 搜索匹配
POST /api/intent/match
{"text": "查一下修改记录"}

# 执行功能
POST /api/flow/execute
{"functionId": 1, "params": {"@SampleId": "20260101001"}}

# 导入配置
POST /api/config/import
{"json": "[{...意图配置...}]"}

# 导出配置
POST /api/config/export

# 重建向量
POST /api/config/rebuildVectors
```

---

## 可持续开发方向

当前系统已满足基础查询需求，以下方向可以进一步增强：

### 权限管理
- 目前所有用户共用同一账号，无权限隔离
- 可扩展方向：**角色区分**（管理员 / 操作员 / 只读）
  - 管理员：完整 CRUD 配置权限
  - 操作员：只能执行查询，不能修改配置
  - 只读：只能看结果，不能进入配置管理
- 实现方式：`Web.config` 中配置多账号角色，`AuthFilterAttribute` 校验接口权限

### 多轮对话
- 当前每次搜索独立，无上下文
- 可扩展：保留历史命中意图和参数，支持"上一条结果的基础上再查"

### 查询日志分析
- `QueryLog` 表已有查询记录
- 可扩展：统计高频查询、匹配失败率、耗时趋势，辅助优化配置

### 数据源完善
- 当前 Oracle / SQLite 已完善
- **SqlServer / MySQL 为半成品**，未在实际环境中验证
- 如需生产使用，或需要扩展 PostgreSQL、MongoDB、REST API 等数据源
- 建议继续使用 **Claude Code** 完善对应的 DataExecutor 实现和数据源注册

### 配置版本管理
- 当前配置修改即生效，无法回滚
- 可扩展：导入时保存历史版本，支持一键回退

---

## 常见问题

### Q: 搜索不到结果？
A: 检查是否重建了向量。修改意图后需要重建向量才能生效。

### Q: 匹配度太低？
A: 降低 `Web.config` 中 `MatchThreshold` 的值（默认 0.65）。或者在意图中添加更精准的关键词。

### Q: 提示 Oracle 连接失败？
A: 检查 `Web.config` 中 `BusinessDB` 的连接字符串。确认服务器安装了 Oracle Data Access 客户端。

### Q: 如何备份配置？
A: 配置管理 → 导出，保存返回的 JSON 文件。导入时直接粘贴即可恢复。


bge-small-zh-v1.5.onnx 文件 92MB，超过 GitHub 推荐的单文件大小上限，已分卷压缩为 3 个 RAR 文件存放在 Resources/ 目录下。使用前需要解压合并：

rar x bge-small-zh-v1.5.part1.rar
或直接从 HuggingFace 下载 ONNX 格式模型：
https://huggingface.co/BAAI/bge-small-zh-v1.5/tree/main/onnx

---

# Intent Engine — Natural Language Query System
...


---

# Intent Engine — Natural Language Query System

## What Problem Does This Solve

**Front-line operators** need to check data every day — business status, change logs, return records. Every time they have to ask **developers** to run SQL.
Developers get interrupted, wait, repeat.

This tool lets **operators search themselves** — in natural language, no SQL needed, no developers needed.

```
Operator: "check the change log for this order"
  → Embedding auto-matches the intent
    → Executes pre-configured SQL
      → Shows results
```

**Core idea: AI for routing, not for generation.** Embedding routes user input to the best-matching function. SQL is pre-configured, results are 100% deterministic.

---

> Built with **Claude Code + DeepSeek v4**. Future feature development, bug fixes, and new data source adapters can continue using Claude Code.

---

## Features

| Feature | Description |
|---------|-------------|
| **Natural Language Search** | Type "check data change log", system auto-matches functions |
| **Semantic Matching** | BGE-small-zh embedding, no keyword dependency |
| **Multi-step Workflow** | COUNT checks, conditional jumps (goto), errors, table display |
| **Multi Data Source** | Oracle, SQLite, SqlServer, MySQL |
| **Config as Code** | New queries require no code changes, import JSON |
| **Local Runtime** | No data upload, no external API, no GPU needed |

## How It Works

```
User: "check the change log for this order"
  → Embedding matches the best intent
    → Hits "Change Log Query" intent
      → Prompts for parameter: Order ID (required)
        → User fills Order ID, confirms
          → Executes pre-configured SQL
            → Shows result table
```

**Core design: AI for routing only, not for generation.** Embedding only decides which intent to match. SQL is pre-configured, results are 100% deterministic.

---

## Quick Start

### Requirements

- Windows Server / Windows 10+
- IIS + .NET Framework 4.5
- Oracle Data Access client (for Oracle queries)
- Visual C++ Redistributable (for ONNX model)

### Deploy Files

| File | Description | Target |
|------|-------------|--------|
| `bin/IntentEngine.dll` | Compiled assembly | `/app/bin/` |
| `Default.aspx` | Main page | `/app/` |
| `Login.aspx` | Login page | `/app/` |
| `Captcha.aspx` | CAPTCHA | `/app/` |
| `Web.config` | Configuration | `/app/` |
| `Static/js/config.js` | Config UI | `/app/Static/js/` |
| `Static/js/flow.js` | Execution UI | `/app/Static/js/` |
| `Static/js/intent.js` | Search UI | `/app/Static/js/` |
| `Static/css/app.css` | Styles | `/app/Static/css/` |
| `onnxruntime.dll` | ONNX native library | `/app/` and `/app/bin/` |
| `Resources/` | Model files | `/app/Resources/` |

### Initialize

```bash
del /app/Data/IntentEngine.db
# Start website, visit http://your-server/app/
# Login: admin / admin123
```

### Import Demo Data

1. Import `Data/demo_system_query_en.json` via Config → Import
2. Click **Rebuild Vectors**

### Verify

Search: "system config", "system overview", "list intents"

---

## Embedding Model

| Engine | File | Note |
|--------|------|------|
| ONNX (BGE-small-zh) | `Services/OnnxEmbeddingService.cs` | Requires VC++ redist |
| CharEmbedding | `Services/CharEmbeddingService.cs` | Pure C#, no deps |

Switch in `App_Start/IocConfig.cs`. To upgrade embedding, replace the ONNX model and update `HIDDEN_SIZE` in `OnnxEmbeddingService.cs`.

---

## Why .NET 4.5

Target environment constraint. Most enterprise servers have .NET 4.5/4.6 pre-installed. Upgrading is possible but dependency issues must be handled manually.

## Why Chinese-Only UI

The BGE-small-zh model is Chinese-specific. English input would produce poor matching. To support English/multilingual, replace the embedding model and translate UI strings — use **Claude Code** for this.

---

## Project Structure

```
IntentEngine/
├── App_Start/          IoC container, WebAPI config
├── Controllers/        REST API endpoints
├── DataExecutors/      Oracle, SQLite, SqlServer, MySQL
├── Models/             Data models
├── Repositories/       Database access layer
├── Services/           Core logic (matching, flow engine, embedding)
├── Static/             Frontend (JS, CSS)
└── Data/               SQLite DB, demo JSON configs
```

## API

```bash
POST /api/intent/match     {"text": "..."}
POST /api/flow/execute     {"functionId": 1, "params": {...}}
POST /api/config/import    {"json": "[...]"}
POST /api/config/export
POST /api/config/rebuildVectors
```

---

## Future Development

- **Access control**: Role-based (Admin / Operator / Read-only)
- **Multi-turn**: Context-aware follow-up queries
- **Log analysis**: Statistics on query patterns
- **Alerts**: DingTalk/WeCom/email integration
- **Data sources**: SqlServer/MySQL incomplete — use Claude Code to finish
- **Config versioning**: History and rollback

## FAQ

**No results?** Rebuild vectors after config changes.
**Low match rate?** Lower `MatchThreshold` in Web.config.
**Oracle error?** Check connection string and Oracle client installation.
**Other languages?** Use Claude Code to translate UI and switch embedding model.

bge-small-zh-v1.5.onnx is 92MB and exceeds GitHub's recommended file size. It is split into 3 RAR volumes (bge-small-zh-v1.5.part1.rar ~ part3.rar) in the Resources/ directory. Extract them before running:

# Combine and extract
rar x bge-small-zh-v1.5.part1.rar
Or download the model directly from BAAI/bge-small-zh-v1.5 on HuggingFace.
