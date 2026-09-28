# 构建与发布指南

本文档面向希望从源码构建或自行发布 TypeMaster 的开发者。

---

## 1. 环境要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 10 / 11（64 位） |
| .NET SDK | 8.0（建议 8.0.4xx） |
| 磁盘空间 | 源码约 50 MB；构建产物约 500 MB；发布包约 175 MB |

> **Windows 是硬性要求**。项目使用 WPF（Windows 专有 UI 框架），
> 且音频与语音依赖 Windows 专有 API（NAudio `WaveOut`、SAPI）。
> 无法在 macOS / Linux 上构建或运行。

### 检查 SDK

```powershell
dotnet --list-sdks
```

---

## 2. 关于 global.json

项目根目录的 `global.json` 将 SDK 固定为 `8.0.425`：

```json
{
  "sdk": {
    "version": "8.0.425",
    "rollForward": "latestFeature"
  }
}
```

### 为什么需要它

如果机器上装了多个 SDK（例如同时有 .NET 8 / 9 / 10），
`dotnet build` 会选择**版本最高**的那个。

本项目曾在更高版本 SDK 下出现问题（NuGet 还原与构建行为不一致），
因此固定版本以保证结果可复现。

### 如果你没有 8.0.425

两种处理方式：

**方式一**：改成你本机已有的 8.x 版本

```powershell
dotnet --list-sdks          # 查看可用版本
# 编辑 global.json，把 version 改成实际存在的 8.x 版本号
```

**方式二**：直接删除 `global.json`
（此时会使用你机器上的默认 SDK，可能不是 8.x）

> `"rollForward": "latestFeature"` 的含义：允许在同一主次版本内
> 向上滚动到最新的功能带（例如 8.0.4xx 内可自动选更高的补丁版本），
> 但不会跨到 9.x / 10.x。

---

## 3. 构建

```powershell
git clone <你的仓库地址>
cd TypeMaster

dotnet build TypeMaster.sln -c Release
```

**预期结果**：

```
已成功生成。
    0 个警告
    0 个错误
```

> **0 警告 0 错误是本项目的验收基线**。若你的构建出现警告，
> 通常意味着 SDK 版本不对（检查 `global.json`）。

### 常用变体

```powershell
# 清理后重新构建
dotnet build TypeMaster.sln -c Release --no-incremental

# 详细输出（排查问题时用）
dotnet build TypeMaster.sln -c Release -v n

# 只构建某个项目
dotnet build src/TypeMaster.Services/TypeMaster.Services.csproj -c Release
```

---

## 4. 本地运行

```powershell
dotnet run --project src/TypeMaster.App/TypeMaster.App.csproj -c Release
```

---

## 5. 发布单文件 exe

### 命令

```powershell
dotnet publish src/TypeMaster.App/TypeMaster.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false `
  -o publish
```

产物：`publish\TypeMaster.exe`，约 **175 MB**。

### 参数说明

| 参数 | 作用 |
| --- | --- |
| `-c Release` | 发布配置 |
| `-r win-x64` | 目标运行时标识（64 位 Windows） |
| `--self-contained true` | 自包含，内含 .NET 运行时，目标机器无需安装任何运行时 |
| `-p:PublishSingleFile=true` | 打成单个 exe |
| **`-p:IncludeNativeLibrariesForSelfExtract=true`** | **打包原生库（关键，见下）** |
| `-p:DebugType=None -p:DebugSymbols=false` | 不生成调试符号，减小体积 |

### ⚠️ 为什么 `IncludeNativeLibrariesForSelfExtract` 不能省

`PublishSingleFile=true` **默认不打包原生库**，只把托管程序集打进 exe。

如果漏掉这个开关：

- 在 `publish` 目录里直接运行**看起来是正常的**（因为原生 DLL 就躺在旁边）
- 但把 exe **单独拷到别的目录**运行时会**直接崩溃**，报缺少原生库

加上这个开关后，以下原生库会一并打包进 exe（合计约 9.4 MB）：

- `wpfgfx_cor3.dll` — WPF 图形渲染
- `PresentationNative_cor3.dll` — WPF 原生支持
- `vcruntime140_cor3.dll` — VC++ 运行时
- `D3DCompiler_47_cor3.dll` — Direct3D 着色器编译
- `PenImc_cor3.dll` — 笔输入
- `e_sqlite3.dll` — SQLite 原生库

### 验证发布包

**必须做这一步**——只在 `publish` 目录里运行是测不出问题的。

```powershell
# 拷到空目录单独测试
$iso = Join-Path $env:TEMP 'TMTest'
New-Item -ItemType Directory -Force -Path $iso | Out-Null
Copy-Item publish\TypeMaster.exe $iso
Start-Process (Join-Path $iso 'TypeMaster.exe')
```

程序应正常启动。若闪退，检查是否漏了 `IncludeNativeLibrariesForSelfExtract`。

---

## 6. 打包 Release 附件

GitHub 仓库有**单文件 100 MB 限制**，而 exe 有 175 MB，
因此 exe **只能作为 Release 附件上传，不能提交进 git 仓库**（`dist/` 已在 `.gitignore` 中）。

```powershell
$ver = 'v1.0.928'
$out = "dist\TypeMaster-$ver-win-x64"
New-Item -ItemType Directory -Force -Path $out | Out-Null

Copy-Item publish\TypeMaster.exe            $out
Copy-Item LICENSE                           $out
Copy-Item docs\RELEASE-$ver.md              $out\发行说明.md
Copy-Item docs\使用说明.txt                 $out

Compress-Archive -Path "$out\*" -DestinationPath "dist\TypeMaster-$ver-win-x64.zip" -Force
```

---

## 7. 常见问题排查

### NuGet 还原报 `Value cannot be null. (Parameter 'path1')`

**原因**：`APPDATA` 或 `LOCALAPPDATA` 环境变量为空。

**处理**：显式设置环境变量后重试：

```powershell
$env:APPDATA    = 'C:\Users\<用户名>\AppData\Roaming'
$env:LOCALAPPDATA = 'C:\Users\<用户名>\AppData\Local'
dotnet build TypeMaster.sln -c Release
```

> 这种情形常见于从精简环境（如某些 CI、计划任务、服务账户）中调用 dotnet。

### 构建选了错误版本的 SDK

**现象**：出现莫名其妙的编译错误或警告。

**处理**：检查根目录 `global.json` 是否存在、`version` 是否为本机已安装的版本。

```powershell
dotnet --version            # 当前生效的 SDK
dotnet --list-sdks          # 全部已安装的 SDK
```

### publish 报 `MSB4018` / `CopyFile` 失败

**原因**：目标 exe 正在运行，文件被占用。

**处理**：先关闭程序再发布。

```powershell
Stop-Process -Name TypeMaster -Force -ErrorAction SilentlyContinue
Remove-Item publish\TypeMaster.exe -Force -ErrorAction SilentlyContinue
```

### 发布包能启动但界面异常

**原因**：`bin` / `obj` 中有残留的旧产物。

**处理**：

```powershell
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
dotnet build TypeMaster.sln -c Release
```

### 语音朗读没有声音

**原因**：系统缺少中文语音引擎。

**处理**：「设置 → 时间和语言 → 语音 → 管理语音」中添加中文语音包。
软件在系统缺少引擎时会自动跳过朗读而不会报错。

---

## 8. 项目结构速览

```
TypeMaster/
├─ global.json               # SDK 版本固定
├─ TypeMaster.sln
├─ src/
│  ├─ TypeMaster.Core        # 模型 / 枚举 / 接口（无外层依赖）
│  ├─ TypeMaster.Data        # EF Core + SQLite
│  ├─ TypeMaster.Services    # 业务逻辑（无 UI 依赖）
│  ├─ TypeMaster.ViewModels  # MVVM 视图模型
│  └─ TypeMaster.App         # WPF 入口 / 视图 / 资源
├─ docs/                     # 文档
├─ dist/                     # 发布包输出（git 忽略）
└─ publish/                  # 发布中间产物（git 忽略）
```

### 各层目标框架

| 项目 | TFM |
| --- | --- |
| TypeMaster.Core | `net8.0` |
| TypeMaster.Data | `net8.0` |
| TypeMaster.Services | `net8.0-windows` |
| TypeMaster.ViewModels | `net8.0-windows` |
| TypeMaster.App | `net8.0-windows` |

> `TypeMaster.Services` 之所以是 `net8.0-windows` 而非 `net8.0`：
> NAudio 的 `WaveOut` 与 `System.Speech` 都是 Windows 专有 API。
> 但服务层**不依赖 WPF**——淡入淡出用 `System.Threading.Timer` 而非 `DispatcherTimer`。

---

## 9. 贡献约定

- 提交前确保 `dotnet build TypeMaster.sln -c Release` **0 警告 0 错误**
- 源码文件保持 **UTF-8 无 BOM + LF** 换行
- 图标只用 MDIX 矢量 `PackIcon`，**不使用 emoji**
- 强调色使用纯色实心，不使用渐变
- WPF 通用行为用**附加属性**实现，不继承自定义基类
