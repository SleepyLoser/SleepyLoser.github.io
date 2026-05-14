// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "Input/UIActionBindingHandle.h"
#include "HoldActionHelper.generated.h"

class UInputAction;
class UCommonUserWidget;
enum class ECommonInputMode : uint8;

/**
 * 长按事件回调代理
 */
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnHoldProgressChanged, float, Progress);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnHoldStarted);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnHoldCompleted);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnHoldCanceled);

/** 长按触发阈值达成时触发（即原始进度首次超过阈值，正式开始计入有效进度） */
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnHoldThresholdReached);

/**
 * FHoldActionEntry
 * 
 * 单条长按动作的配置，描述一个 InputAction 对应的长按行为
 */
USTRUCT(BlueprintType)
struct FHoldActionEntry
{
	GENERATED_BODY()

	/** 增强输入动作（需要在 IMC 中配置了按键映射） */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action")
	TObjectPtr<const UInputAction> InputAction = nullptr;

	/** 是否强制以长按模式处理（即使 InputAction 本身没有 Hold 映射） */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action")
	bool bForceHold = true;

	/** 是否在 ActionBar 中显示 */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action")
	bool bDisplayInActionBar = false;

	/** 是否消费输入（阻止事件继续传递） */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action")
	bool bConsumeInput = true;

	/**
	 * 自定义长按时间（秒），0 表示使用项目设置中 UCommonUIHoldData 的默认值（默认 0.75s）。
	 * 仅在 bForceHold = true 时生效。
	 */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action", meta = (EditCondition = "bForceHold", ClampMin = "0.0", UIMin = "0.0"))
	float HoldTime = 0.f;

	/**
	 * 长按回退时间（秒），中途松开后进度从当前值回退到 0 所需的时间。
	 * 0 表示立即归零。仅在 bForceHold = true 时生效。
	 */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action", meta = (EditCondition = "bForceHold", ClampMin = "0.0", UIMin = "0.0"))
	float HoldRollbackTime = 0.f;

	/**
	 * 长按触发阈值（0.0 ~ 1.0），原始 Hold 进度超过此阈值后才算正式开始长按。
	 * 阈值之前的按住时间不会产生有效进度，用于过滤误触和短按。
	 * 0 表示不使用阈值（按下即开始计入进度）。
	 */
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Hold Action", meta = (EditCondition = "bForceHold", ClampMin = "0.0", ClampMax = "1.0", UIMin = "0.0", UIMax = "1.0"))
	float HoldTriggerThreshold = 0.f;
};

/**
 * UHoldActionHelper
 * 
 * 通用长按动作辅助对象，封装 CommonUI 的 Hold Action 注册/注销逻辑。
 * 
 * 使用方式：
 * 1. 在任意 UCommonUserWidget 子类中声明一个 UPROPERTY 成员
 * 2. 调用 Initialize() 传入宿主 Widget
 * 3. 调用 RegisterHoldAction() 注册长按动作
 * 4. 绑定 OnHoldProgressChanged / OnHoldCompleted 等代理
 * 5. 在 Widget 销毁或 Deactivate 时调用 UnregisterAll()
 *
 * 示例：
 * @code
 *   // .h
 *   UPROPERTY()
 *   TObjectPtr<UDemoHoldActionHelper> HoldHelper;
 * 
 *   UPROPERTY(EditDefaultsOnly)
 *   TObjectPtr<UInputAction> IA_Interact;
 *
 *   // .cpp - NativeOnActivated / NativeConstruct
 *   HoldHelper = NewObject<UHoldActionHelper>(this);
 *   HoldHelper->Initialize(this);
 *   HoldHelper->OnHoldProgressChanged.AddDynamic(this, &ThisClass::OnHoldProgress);
 *   HoldHelper->OnHoldCompleted.AddDynamic(this, &ThisClass::OnHoldComplete);
 *   HoldHelper->RegisterHoldAction(IA_Interact);
 *
 *   // .cpp - NativeOnDeactivated / NativeDestruct
 *   HoldHelper->UnregisterAll();
 * @endcode
 */
UCLASS(BlueprintType)
class API UHoldActionHelper : public UObject
{
	GENERATED_BODY()

public:

	/**
	 * 初始化辅助对象，绑定到宿主 Widget
	 * @param InOwnerWidget  拥有此辅助对象的 Widget，必须是 UCommonUserWidget 的子类
	 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	void Initialize(UCommonUserWidget* InOwnerWidget);

	/**
	 * 使用 InputAction 注册一个长按动作
	 * @param InInputAction  增强输入动作
	 * @param bInForceHold   是否强制以长按模式处理
	 * @param bInConsumeInput 是否消费输入
	 * @param InHoldTime      自定义长按时间（秒），0 表示使用全局默认值
	 * @param InHoldRollbackTime 长按回退时间（秒），0 表示立即归零
	 * @param InHoldTriggerThreshold 长按触发阈值（0.0~1.0），超过此阈值后才开始计入有效进度，0 表示不使用
	 * @return 绑定句柄，可用于后续单独注销
	 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	FUIActionBindingHandle RegisterHoldAction(
		const UInputAction* InInputAction,
		bool bInForceHold = true,
		bool bInConsumeInput = true,
		float InHoldTime = 1.3f,
		float InHoldRollbackTime = 0.f,
		float InHoldTriggerThreshold = 0.3f);

	/**
	 * 使用预配置的 Entry 结构注册长按动作
	 * @param Entry  长按动作配置
	 * @return 绑定句柄
	 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	FUIActionBindingHandle RegisterHoldActionFromEntry(const FHoldActionEntry& Entry);

	/**
	 * 批量注册长按动作（适合在蓝图中配置数组后一次性注册）
	 * @param Entries  长按动作配置数组
	 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	void RegisterHoldActions(const TArray<FHoldActionEntry>& Entries);

	/**
	 * 注销指定的长按动作
	 * @param Handle  之前注册时返回的绑定句柄
	 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	void UnregisterHoldAction(FUIActionBindingHandle Handle);

	/** 注销所有已注册的长按动作 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	void UnregisterAll();

	/** 获取当前注册的所有绑定句柄 */
	UFUNCTION(BlueprintCallable, Category = "Hold Action")
	const TArray<FUIActionBindingHandle>& GetBindingHandles() const { return BindingHandles; }

public:

	// ---- 事件代理 ----

	/** 长按进度变化（每帧触发），Progress 范围 0.0 ~ 1.0 */
	UPROPERTY(BlueprintAssignable, Category = "Hold Action|Events")
	FOnHoldProgressChanged OnHoldProgressChanged;

	/** 长按开始（按下瞬间） */
	UPROPERTY(BlueprintAssignable, Category = "Hold Action|Events")
	FOnHoldStarted OnHoldStarted;

	/** 长按完成（进度达到 1.0） */
	UPROPERTY(BlueprintAssignable, Category = "Hold Action|Events")
	FOnHoldCompleted OnHoldCompleted;

	/** 长按取消（中途松开） */
	UPROPERTY(BlueprintAssignable, Category = "Hold Action|Events")
	FOnHoldCanceled OnHoldCanceled;

	/** 长按触发阈值达成（原始进度首次超过阈值，正式开始计入有效进度） */
	UPROPERTY(BlueprintAssignable, Category = "Hold Action|Events")
	FOnHoldThresholdReached OnHoldThresholdReached;

protected:

	/** 内部：长按进度回调，对原始进度进行阈值重映射后再广播 */
	void HandleHoldProgress(float Progress);

	/** 内部：长按开始回调 */
	void HandleHoldPressed();

	/** 内部：长按释放回调 */
	void HandleHoldReleased();

	/** 内部：长按完成（OnExecuteAction）回调 */
	void HandleHoldExecuted();

	/**
	 * 将原始进度按阈值重映射为有效进度。
	 * 当原始进度 <= 阈值时，有效进度为 0；
	 * 当原始进度 > 阈值时，有效进度从 0 线性增长到 1.0。
	 * @param RawProgress  CommonUI 原始进度 (0.0 ~ 1.0)
	 * @return 重映射后的有效进度 (0.0 ~ 1.0)
	 */
	float RemapProgressWithThreshold(float RawProgress) const;

	/**
	 * 内部：为增强输入的 Binding 注入 HoldMappings。
	 * 
	 * 原因：CommonUI 的 FUIActionBinding 构造函数在增强输入分支中不会填充 HoldMappings，
	 * 导致 bForceHold 对增强输入无效。此方法在注册后手动获取 InputAction 的按键映射，
	 * 构建 FUIActionKeyMapping 并注入到 Binding->HoldMappings 中，补全 Hold 逻辑。
	 */
	void InjectHoldMappingsForEnhancedInput(FUIActionBindingHandle Handle, float InHoldTime, float InHoldRollbackTime);

private:

	/** 宿主 Widget */
	UPROPERTY()
	TWeakObjectPtr<UCommonUserWidget> OwnerWidget;

	/** 所有已注册的绑定句柄 */
	UPROPERTY()
	TArray<FUIActionBindingHandle> BindingHandles;

	/** 长按触发阈值（0.0 ~ 1.0），原始进度超过此阈值后才开始计入有效进度 */
	float HoldTriggerThreshold = 0.f;

	/** 标记阈值是否已经达成（用于在本次按住过程中只触发一次 OnHoldThresholdReached） */
	bool bThresholdReached = false;

	/** 标记长按进度是否真正达到 1.0（用于过滤短按回退触发的 OnExecuteAction） */
	bool bHoldProgressCompleted = false;
};
