# 三人桌宠 · Windows

把「太对了」「薄肌」「这是个好事」放到 Windows 桌面上。点击头像播原声，三个人还能互相接话。

基于 [SunPet](https://github.com/Ganzhe2028/SunPet)、[ShaolanPet](https://github.com/Ganzhe2028/ShaolanPet) 和 [FengGe-Desktop-Pet](https://github.com/Ganzhe2028/FengGe-Desktop-Pet) 的交互与素材制作的 **非官方 Windows 移植版**，使用 C# / WPF 实现。

## 下载与安装

1. 从 [Releases](https://github.com/kevinlu1212/ganzhe-pets-windows/releases/latest) 下载 `GanzhePets-Windows-1.0.0.zip`，完整解压。
2. 双击 `Install.cmd`，等待首次从 GitHub 下载并校验原始素材。
3. 双击桌面「三人桌宠」，同时启动三个独立浮窗。

安装面向当前 Windows 用户，无需管理员权限。首次准备素材需要连接 GitHub，运行时不联网，也不开机自启。
安装器使用 Windows PowerShell；如果电脑的脚本执行策略或组织策略禁止运行本地脚本，请先按电脑的管理要求处理，安装器不会更改执行策略。

环境：Windows 10 / 11、.NET Framework 4.8、可用的音频播放设备及 Windows 媒体组件。已在 Windows 10 x64 实机验证；Windows 11 尚未做实机验证。Windows N/KN 缺失媒体组件时，可能无法解码 M4A。

## 怎么玩

| 操作 | 效果 |
| --- | --- |
| 点击头像 | 随机说一句，每轮不重复 |
| 按住头像拖动 | 移动浮窗，退出时保存位置 |
| 右键头像 | 选语录、固定一句、调音量/大小、切换置顶或联动 |
| 聚焦后按空格 / Enter | 播放一句 |
| 再次启动 | 找回已运行的浮窗，避免重复启动 |
| 右键 → 退出全部桌宠 | 关闭程序 |

- 太对了：28 句；薄肌：37 句；好事：11 句，共 76 段原声。
- 薄肌默认随机使用上游推荐的 8 句；右键「全部语录参与随机」可启用 37 句。
- 互相接话：**薄肌 → 太对了 → 好事 → 太对了**，第四句后停止。
- 单独点击好事：好事 → 太对了；单独点击太对了：只说一句。
- 连续点击会取消上一条接话链，避免越接越多。

每个角色可以独立关闭；要重新显示关闭的角色，先退出全部再启动。
相比 macOS 原版，此实现按音频结束顺序接话，不包含机身摇动触发、压音重叠或 macOS 专属集成。

## 卸载与数据

在 Windows「应用和功能 / 已安装的应用」里卸载「三人桌宠（Windows 本地移植版）」。卸载器删除安装文件和快捷方式，保留个人设置。

- 程序：`%LOCALAPPDATA%\Programs\GanzhePets`
- 设置及本机诊断：`%LOCALAPPDATA%\GanzhePets`

## 从源码构建

无需安装 .NET SDK 或 Visual Studio。Windows 自带的 .NET Framework C# 编译器即可构建。

```powershell
git clone https://github.com/kevinlu1212/ganzhe-pets-windows.git
cd ganzhe-pets-windows
powershell -NoProfile -File .\scripts\build.ps1
.\build\package\GanzhePets.exe
```

第一次构建会从锁定的上游提交下载头像、音频、字幕清单，并校验全部 82 个文件。后续构建优先使用已校验的本地素材。
若 GitHub 下载触发匿名 API 频率限制，等待限制恢复后重试；下载失败不会改为执行远端脚本。

测试与打包：

```powershell
# 静音完整播放 76 段音频，并验证四步联动；需要可用的 Windows 媒体组件。
powershell -NoProfile -File .\scripts\test.ps1

# 生成不含素材的 Release ZIP，以及 SHA-256 校验文件。
powershell -NoProfile -File .\scripts\package.ps1 -Version 1.0.0
```

实机验证记录见 [docs/verification.md](docs/verification.md)。CI 验证源码编译、素材完整性和发布包内容；完整媒体播放在 Windows 桌面会话中验证。

## 项目结构

```text
src/Pets.cs             Windows 浮窗、播放、设置、联动与自检入口
scripts/                准备素材、构建、安装、卸载、验证与打包
upstream.lock.json      上游固定提交与素材 SHA-256
licenses/               上游原始许可和素材权利说明
docs/                   架构与实机验证记录
```

代码：MIT。头像、声音及其权利独立于代码许可；仓库及 Release 不包含这些原始素材。来源、固定提交及用途说明见 [NOTICE.md](NOTICE.md)。
