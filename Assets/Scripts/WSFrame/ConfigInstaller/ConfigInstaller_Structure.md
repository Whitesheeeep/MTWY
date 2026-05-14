# Config Installer 结构说明

`ConfigInstaller` 是框架配置注册入口。它不负责保存具体业务配置，也不负责创建业务对象，只负责在启动时执行一棵配置注册节点树。

当前实现采用组合模式，结构类似行为树：

```text
FrameworkConfigInstaller
  rootNode: FrameworkConfigRootNode
    child: AutoSingletonConfigRegistryModule
      child: TimeWheelManagerConfigProvider
```

`FrameworkConfigInstaller` 只持有一个 `rootNode`。根节点下面可以继续挂组合节点，也可以挂具体配置 Provider 节点。

## 设计目标

- `FrameworkConfigInstaller` 只作为场景入口，不绑定任何具体注册表。
- `ConfigRegisterNodeBase` 是所有配置注册节点的抽象基类。
- `CompositeConfigRegisterNode` 是组合节点的抽象基类，用于按顺序执行子节点。
- 具体模块的 Provider 放在对应模块目录，不集中放到 `ConfigInstaller`。
- AutoSingleton 的注册表属于 Singleton 模块，不混入通用节点层。

## 文件分层

```text
Assets/Scripts/WSFrame/ConfigInstaller
  Core
    IConfigRegisterNode.cs
    ConfigRegisterNodeBase.cs
    CompositeConfigRegisterNode.cs
    FrameworkConfigRootNode.cs
  Runtime
    FrameworkConfigInstaller.cs
  Assets
    FrameworkConfigInstaller.prefab
    FrameworkConfigRootNode.asset

Assets/Scripts/WSFrame/Singleton/Registry
  AutoSingletonConfigRegistry.cs
  AutoSingletonConfigRegistryModule.cs
  AutoSingletonConfigRegistryModule.asset

Assets/Scripts/WSFrame/Utilities/TimeWheel
  TimeWheelManagerConfigProvider.cs
  TimeWheelManagerConfigProvider.asset
```

## 核心类型

`IConfigRegisterNode` 是最小行为接口：

```csharp
public interface IConfigRegisterNode
{
    void Register();
}
```

`ConfigRegisterNodeBase` 是 Unity 可序列化节点基类：

```csharp
public abstract class ConfigRegisterNodeBase : ScriptableObject, IConfigRegisterNode
{
    public abstract void Register();
}
```

`CompositeConfigRegisterNode` 是抽象组合节点，内部持有 `children`：

```csharp
public abstract class CompositeConfigRegisterNode : ConfigRegisterNodeBase
{
    [SerializeField]
    private List<ConfigRegisterNodeBase> children;

    public override void Register()
    {
        foreach (var child in children)
        {
            child?.Register();
        }
    }
}
```

`FrameworkConfigRootNode` 是当前框架使用的具体根节点资产类型。

`FrameworkConfigInstaller` 是 Mono 入口，只调用一个根节点：

```csharp
public void RegisterAll()
{
    rootNode?.Register();
}
```

## AutoSingleton 接入

AutoSingleton 的配置注册由 `Singleton/Registry` 承担：

- `AutoSingletonConfigRegistry`：保存 `AutoSingleton -> Config` 映射。
- `AutoSingletonConfigRegistryModule`：继承 `CompositeConfigRegisterNode`，作为配置树中的一个组合节点。

这意味着 AutoSingleton 没有自己的特殊 Provider 基类。具体 Provider 直接继承通用的 `ConfigRegisterNodeBase`。

## Provider 位置

Provider 放在对应模块目录。例如 TimeWheel 的 Provider 放在：

```text
Assets/Scripts/WSFrame/Utilities/TimeWheel/TimeWheelManagerConfigProvider.cs
```

这样做是为了让配置定义、配置资产和模块代码靠近，避免 `ConfigInstaller` 目录变成所有模块配置的集中堆放点。

## 执行顺序

执行顺序完全由资产引用顺序决定：

```text
FrameworkConfigInstaller.Awake()
  -> FrameworkConfigRootNode.Register()
  -> AutoSingletonConfigRegistryModule.Register()
  -> TimeWheelManagerConfigProvider.Register()
```

组合节点会跳过 `null` 子节点，不会因为列表里有空引用而中断。
