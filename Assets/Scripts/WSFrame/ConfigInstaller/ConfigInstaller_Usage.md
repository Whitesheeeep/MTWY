# Config Installer 使用与扩展

本文用 TimeWheel 作为示例，说明如何通过 `ConfigInstaller` 在启动时注册模块配置。

## TimeWheel 使用流程

TimeWheel 的配置 Provider 位于：

```text
Assets/Scripts/WSFrame/Utilities/TimeWheel/TimeWheelManagerConfigProvider.asset
```

它对应的代码是：

```csharp
public sealed class TimeWheelManagerConfigProvider : ConfigRegisterNodeBase
{
    [SerializeField]
    private TimeWheelConfig config = new TimeWheelConfig();

    public override void Register()
    {
        AutoSingletonConfigRegistry.Register<TimeWheelManager, TimeWheelConfig>(
            config.CreateRuntimeCopy());
    }
}
```

这个 Provider 做一件事：把 `TimeWheelConfig` 注册给 `TimeWheelManager`。

## 资产连接方式

当前推荐连接方式：

```text
FrameworkConfigInstaller.prefab
  rootNode -> FrameworkConfigRootNode.asset

FrameworkConfigRootNode.asset
  children -> AutoSingletonConfigRegistryModule.asset

AutoSingletonConfigRegistryModule.asset
  children -> TimeWheelManagerConfigProvider.asset
```

启动时，`FrameworkConfigInstaller` 会从根节点开始执行整棵配置树。

## 运行时链路

TimeWheel 配置注册链路如下：

```text
FrameworkConfigInstaller.Awake()
  -> rootNode.Register()
  -> AutoSingletonConfigRegistryModule.Register()
  -> TimeWheelManagerConfigProvider.Register()
  -> AutoSingletonConfigRegistry.Register<TimeWheelManager, TimeWheelConfig>()
```

之后第一次访问：

```csharp
TimeWheelManager.Instance
```

`TimeWheelManager` 会通过 `AutoConfigSingletonMonoBase` 从 `AutoSingletonConfigRegistry` 读取注册过的 `TimeWheelConfig`。如果没有注册，则使用 `CreateDefaultConfig()` 提供的默认配置。

## 配置 TimeWheel

在 `TimeWheelManagerConfigProvider.asset` 中编辑嵌套的 `TimeWheelConfig`：

- `tickSeconds`：基础 tick 间隔。
- `slotCounts`：时间轮层级和每层槽位数。
- `maxCatchUpTicksPerFrame`：单帧最多补多少个 tick。

示例配置：

```text
tickSeconds = 0.1
slotCounts = [256, 64, 64]
maxCatchUpTicksPerFrame = 100
```

业务代码正常使用 `TimeWheelManager`：

```csharp
TimeWheelManager.Instance.Schedule(1f, OnTimeout);
```

也可以直接创建独立 scheduler：

```csharp
var scheduler = new TimeWheelScheduler(
    new TimeWheelConfig(0.1f, new List<int> { 256, 64, 64 }, 100));
```

## 新增 AutoSingleton 配置 Provider

新增一个 AutoSingleton 配置 Provider 时，直接继承 `ConfigRegisterNodeBase`：

```csharp
using UnityEngine;
using WS_Modules.ConfigInstaller;
using WS_Modules.Singleton;

[CreateAssetMenu(fileName = "MyManagerConfigProvider", menuName = "WSFrame/AutoConfig/MyManager")]
public sealed class MyManagerConfigProvider : ConfigRegisterNodeBase
{
    [SerializeField]
    private MyManagerConfig config = new MyManagerConfig();

    public override void Register()
    {
        AutoSingletonConfigRegistry.Register<MyManager, MyManagerConfig>(
            config.CreateRuntimeCopy());
    }
}
```

然后把该 Provider 资产拖到 `AutoSingletonConfigRegistryModule.asset` 的 `children` 列表中。

## 新增其他 Registry 模块

如果未来有其他注册表，不需要改 `FrameworkConfigInstaller`。

可以新增一个专用组合节点：

```csharp
using UnityEngine;
using WS_Modules.ConfigInstaller;

[CreateAssetMenu(fileName = "OtherRegistryModule", menuName = "WSFrame/ConfigRegister/OtherRegistryModule")]
public sealed class OtherRegistryModule : CompositeConfigRegisterNode
{
}
```

再把该模块资产挂到 `FrameworkConfigRootNode.asset` 的 `children` 中。

如果某个 Provider 不属于 AutoSingleton，也可以直接继承 `ConfigRegisterNodeBase`，在 `Register()` 中写入自己的目标注册表。

## 注意事项

- `FrameworkConfigInstaller` 只持有一个 `rootNode`。
- `rootNode` 通常使用 `FrameworkConfigRootNode.asset`。
- `CompositeConfigRegisterNode` 和 `ConfigRegisterNodeBase` 是抽象基类，不直接创建资产。
- 具体资产类型必须是非抽象类，例如 `FrameworkConfigRootNode`、`AutoSingletonConfigRegistryModule`、`TimeWheelManagerConfigProvider`。
- Provider 应放在对应模块目录中，而不是放到 `ConfigInstaller` 目录。
- AutoSingleton 配置必须在对应 Singleton 第一次 `Instance` 访问前注册。
