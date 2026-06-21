# 网上沟通交流系统

现代软件开发技术实践大作业 —— 基于 ASP.NET Core + SignalR + WPF + MVC 的跨端实时聊天系统。

## 项目结构

```
CommunicationSystem/
├── src/
│   ├── CommunicationSystem.Shared/    # 共享实体与 DTO
│   ├── CommunicationSystem.Api/       # Web API + SignalR 后端
│   ├── CommunicationSystem.Web/       # MVC Web 端（用户聊天 + 管理员）
│   └── CommunicationSystem.Desktop/   # WPF 桌面端（MVVM）
├── chat_db.sql                        # 数据库建表脚本
└── seed_admin.sql                     # 管理员初始化脚本
```

## 技术栈

| 层次 | 技术 |
|------|------|
| 后端 API | ASP.NET Core 9 Web API |
| 实时通信 | SignalR |
| ORM | Entity Framework Core + Pomelo MySQL |
| Web 端 | ASP.NET Core MVC + Bootstrap + SignalR JS |
| 桌面端 | WPF + MVVM (CommunityToolkit.Mvvm) |
| 数据库 | MySQL 8 |
| 密码加密 | BCrypt |

## 功能清单

### 管理员（Web 端）
- 审核用户注册申请（通过/拒绝）
- 禁止/解禁用户账号
- 查询与删除交流信息

### 一般用户（Web + WPF 双端）
- 注册申请、登录
- 好友搜索、添加、同意/拒绝、删除
- 群组创建与群聊
- 私聊/群聊实时消息（SignalR）
- 在线状态、正在输入提示
- 图片/文件传输
- 历史消息查询、单条删除、清空记录

## 环境要求

- .NET 9 SDK
- MySQL 8.0+
- Visual Studio 2022（推荐）或 VS Code

## 数据库配置

1. 创建数据库并导入表结构：

```bash
mysql -u root -p < chat_db.sql
mysql -u root -p chat_db < seed_admin.sql
```

2. 修改 API 连接字符串 `src/CommunicationSystem.Api/appsettings.Development.json`：

```json
"ConnectionStrings": {
  "Default": "Server=你的地址;Port=3306;Database=chat_db;User=root;Password=你的密码;CharSet=utf8mb4;"
}
```

## 启动方式

需要同时启动 **API** 和 **Web**（桌面端按需启动）。开三个终端：

```bash
# 终端 1 - 后端 API (http://localhost:5000)
cd src/CommunicationSystem.Api
dotnet run

# 终端 2 - Web 端 (http://localhost:5001)
cd src/CommunicationSystem.Web
dotnet run

# 终端 3 - WPF 桌面端
cd src/CommunicationSystem.Desktop
dotnet run
```

## 默认账号

| 角色 | 用户名 | 密码 | 登录端 |
|------|--------|------|--------|
| 管理员 | admin | admin123 | Web 端 |
| 一般用户 | 自行注册 | — | Web / WPF |

> 新注册用户需管理员在 Web 端「用户管理」中审批通过后方可登录。

## 答辩演示建议

1. **跨端联动**：Web 端登录用户 A，WPF 登录用户 B，互发消息实时显示
2. **好友流程**：搜索 → 申请 → 同意 → 私聊
3. **群聊**：创建群组 → 多人聊天
4. **文件传输**：发送图片/文件
5. **管理功能**：审批注册、禁用用户、删除违规消息
6. **断网重连**：断开网络后 SignalR 自动重连

## 端口说明

| 服务 | 地址 |
|------|------|
| API | http://localhost:5000 |
| Web | http://localhost:5001 |
| SignalR Hub | http://localhost:5000/hubs/chat |
