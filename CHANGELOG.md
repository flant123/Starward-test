# Changelog

本文件记录本项目（fork：`flant123/Starward-test`）在**上游 [Scighost/Starward](https://github.com/Scighost/Starward) 基础**上的全部改动。

上游基线：`9cc6f1ce`（`fix: "MaxWidth" should be changed to "Width". (#1919)`）

---

## [Unreleased] — 2026-08-20

### Added

#### 活动日历（Activity Calendar）— 核心新功能
为每个游戏提供活动日历，数据来自米哈游官方公告 API（`getAnnList`）：

- **数据层**（`Starward.Core/GameNotice`）
  - 新增 `GameAnnouncement` 模型：活动标题/横幅/起止时间/标签，服务器时区 → 本地时间转换，HTML 清理
  - 新增 `AnnListData` / `AnnContentData` 响应模型
  - `GameNoticeClient` 新增：
    - `GetAnnListAsync` — 公告列表
    - `GetActivityListAsync` — 活动列表（按游戏适配活动类型：原神 type 1、绝区零 type 4、崩坏3 type 8000020、星穹铁道取全部并过滤长期系统公告）
    - `GetAnnContentAsync` — 活动详情 HTML（用于提取资源图）
- **UI 层**（`Starward/Features/ActivityCalendar`，全新目录）
  - `ActivityCalendarWindow` — 活动日历窗口
  - `TimelineGanttLayout` — 自定义甘特图布局（`VirtualizingLayout`）
  - `ActivityCalendarItem` — 条目模型（状态/剩余时间/资源图/祈愿识别）
  - 赛博工业风 + 科幻游戏终端风格：深黑金属背景、扫描线、网格轨道、四角螺丝、橙红强调色
  - 顶部 6 周连续机械时间轴：周标签、金属分隔线、**每日刻度**（42 个日刻度 + 周边界加长刻度）
  - **红色当前日期定位线**贯穿活动区，底部圆形终点，打开时平滑滑入
  - 甘特活动条：**按开始时间定位、按持续时间定宽**，与时间轴坐标系严格对齐
  - 状态视觉：进行中（橙红强调+黄色文字）、已结束（暗灰+半透明+完成勾选）、未开启（深灰低亮度）
  - 活动封面图（80×46，完整显示）+ 短标题（只显示「」/‘’内容防截断）
  - **资源图片**：从活动详情 HTML 提取（最多 3 张，金色描边图标槽，排除主横幅）
  - **祈愿/调频活动置顶**（标题含 祈愿/调频/跃迁/补给）
  - 动效：卡片错峰滑入、新活动红色闪烁、按钮悬停发光旋转、定位线动画
  - 点击活动条打开官方详情；悬停 Tooltip 显示完整信息
- **入口**：启动器公告面板新增活动日历按钮（`GameBannerAndPost`）

#### 绝区零养成指南（Character Guide）
- 新增 `CharacterGuideWindow`：WebView2 嵌入官方养成指南网页（`act.mihoyo.com/zzz/gt/character-builder-h`）
- 入口位于启动器公告面板中**活动日历图标旁边**（仅绝区零国服/国际服/B站显示）

### 本地化（Starward.Language）
- 新增 15 个字符串（中/英）：活动日历、第 N 周、剩余/开启天数、今日结束/开启、养成指南、加载失败等

---

## 提交历史（相对上游基线）

| 提交 | 说明 |
|---|---|
| `d74cc45c` | 添加活动日历（数据层 + 基础 UI + 入口） |
| `09a5acd3` | UI 重构为赛博工业风控制台界面（时间轴/定位线/渐变卡片/动效） |
| `37e6ff4e` | 优化为科幻游戏活动终端风格（连续轨道/日刻度/任务条/奖励槽） |
| `860307c9` | 改为甘特图时间轴布局（位置=开始时间，长度=持续时间） |
| `2d050753` | 甘特条加回活动图 + 资源图展示 + 祈愿活动置顶 |
| `e99fb212` | 图片加宽完整显示、条目加大、坐标系对齐、短标题防截断 |
| `fefbf0a9` | 绝区零新增官方养成指南入口 |
| `4a726d52` | 养成指南入口移到活动日历图标旁边 |

---

## 已知限制

- **资源图片**：仅原神的 `getAnnContent` 返回带图文的详情；绝区零/崩坏3/星铁的该接口对所有公告返回同一份通用文本，无法提取资源图（自动降级不显示）
- **崩坏3 国际服**（`bh3_global`）：公告 API 端点当前返回 404（上游同款代码同样如此），该区服活动日历会显示加载失败提示
- 其他语言（除中英文）的新字符串暂回退到英文，待 Crowdin 同步
- 养成指南仅绝区零有官方网页，其他游戏不显示入口
