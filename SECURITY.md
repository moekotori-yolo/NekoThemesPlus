# 安全政策

## 支持范围

| 版本 | 安全修复 |
| --- | --- |
| 最新 Release | 支持 |
| 更早版本 | 尽力而为 |

## 私密报告安全问题

请使用 GitHub 仓库的 **Security > Report a vulnerability**（Private vulnerability reporting）提交，不要创建公开 Issue。

报告请包含：

- 受影响的 NekoThemesPlus、Unity 和 Windows 版本。
- 复现步骤和预期/实际结果。
- 可能的影响范围。
- 日志、诊断报告或最小示例；请先移除用户名、项目路径、壁纸路径等隐私信息。

维护者会尽快确认收到报告。在修复公开前，请不要传播利用步骤。

## 范围说明

NekoThemesPlus 是 Editor-only 插件，但会使用 Unity 内部反射 API，并可选调用 Windows DWM API。反射兼容性故障、编辑器界面显示异常通常属于普通 Bug；能够执行非预期代码、访问未授权数据、破坏项目或绕过系统权限的问题属于安全问题。
