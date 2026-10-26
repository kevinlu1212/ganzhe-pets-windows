# 素材、来源与许可

本项目是独立的、非官方的 Windows 移植实现，与上游作者及头像/声音涉及的人物没有官方关联。
本仓库的 Windows 程序代码采用 MIT 许可，见 [LICENSE](LICENSE)。

## 上游项目

| 角色 | 上游仓库 | 固定提交 |
| --- | --- | --- |
| 太对了 | [Ganzhe2028/SunPet](https://github.com/Ganzhe2028/SunPet) | `9b68d0911552d8c880a46ca72980d3f9af9743ee` |
| 薄肌 | [Ganzhe2028/ShaolanPet](https://github.com/Ganzhe2028/ShaolanPet) | `7ce7e2b1ed49c4da66d19d5aac419794af0d32db` |
| 好事 | [Ganzhe2028/FengGe-Desktop-Pet](https://github.com/Ganzhe2028/FengGe-Desktop-Pet) | `d3336debfafc37963ec5612cb27793e250228b9b` |

交互和接话顺序参考上述项目；Windows 端使用 C# / WPF 实现，没有修改或打包 macOS 二进制。
保留的上游 MIT 全文、原始素材说明及第三方许可见 [licenses/](licenses/)。

## 头像与声音

上游明确声明：`Assets/face.png` 和 `Assets/Clips/` 的版权归原权利人，**不在 MIT 许可范围内**，用途说明为个人学习交流、玩梗、不商用。
此说明不代表本项目取得了素材再许可，也不授予使用者额外的素材使用权。

本仓库及 Release 不打包原始头像、音频或字幕清单；首次准备素材时，由脚本直接下载上游公开的固定提交。
`upstream.lock.json` 保存来源、提交与每个所需文件的 SHA-256；下载后逐一核对，拒绝缺失或内容变化的文件。
素材仅保存到使用者本机的 `assets/`，默认被 Git 忽略。安装后运行不访问网络。

素材相关问题请同时查看对应上游的 `NOTICE.md`；本仓库涉及的内容问题可通过本仓库 Issue 联系维护者。
