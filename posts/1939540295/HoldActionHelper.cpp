// Fill out your copyright notice in the Description page of Project Settings.

#include "UI/Common/HoldActionHelper.h"
#include "CommonUserWidget.h"
#include "Input/CommonUIInputTypes.h"
#include "Input/UIActionBinding.h"
#include "Input/CommonUIInputSettings.h"
#include "CommonUITypes.h"
#include "CommonInputBaseTypes.h"
#include "ICommonInputModule.h"
#include "InputAction.h"
#include "Common/Log.h"

void UHoldActionHelper::Initialize(UCommonUserWidget* InOwnerWidget)
{
	check(InOwnerWidget);
	OwnerWidget = InOwnerWidget;
}

FUIActionBindingHandle UHoldActionHelper::RegisterHoldAction(
	const UInputAction* InInputAction,
	bool bInForceHold,
	bool bInConsumeInput,
	float InHoldTime,
	float InHoldRollbackTime,
	float InHoldTriggerThreshold)
{
	if (!InInputAction || !OwnerWidget.IsValid())
	{
		LOG_WARNING(this, "UHoldActionHelper::RegisterHoldAction - 无效的 InputAction 或 OwnerWidget");
		return FUIActionBindingHandle();
	}

	// 构造 FBindUIActionArgs，OnExecuteAction 绑定到 HandleHoldExecuted
	// 当 Hold 进度达到 1.0 时，CommonUI 内部会触发 OnExecuteAction
	FBindUIActionArgs BindArgs(
		InInputAction,
		FSimpleDelegate::CreateUObject(this, &ThisClass::HandleHoldExecuted)
	);

	BindArgs.bForceHold = bInForceHold;
	BindArgs.bConsumeInput = bInConsumeInput;
	BindArgs.InputMode = ECommonInputMode::Menu;

	// 绑定长按的三个回调
	BindArgs.OnHoldActionProgressed.BindUObject(this, &ThisClass::HandleHoldProgress);
	BindArgs.OnHoldActionPressed.BindUObject(this, &ThisClass::HandleHoldPressed);
	BindArgs.OnHoldActionReleased.BindUObject(this, &ThisClass::HandleHoldReleased);

	// 通过宿主 Widget 注册到 CommonUI ActionRouter
	FUIActionBindingHandle Handle = OwnerWidget->RegisterUIActionBinding(BindArgs);
	BindingHandles.Add(Handle);

	// 保存阈值配置
	if (InHoldTriggerThreshold > 0 && InHoldTime)
	{
		HoldTriggerThreshold = FMath::Clamp(InHoldTriggerThreshold / InHoldTime, 0.f, 1.f);
	}

	// 增强输入路径下，CommonUI 构造函数不会填充 HoldMappings，需要手动注入。
	// 由于 RegisterUIActionBinding 内部 AddBinding 时 HoldMappings 尚为空，
	// 需要先从 Collection 中移除，注入 HoldMappings 后再重新添加，
	// 确保 AddBinding 检查 HoldMappings.Num() 时能正确递增 HoldBindingsCount。
	if (bInForceHold)
	{
		OwnerWidget->RemoveActionBinding(Handle);
		InjectHoldMappingsForEnhancedInput(Handle, InHoldTime, InHoldRollbackTime);
		OwnerWidget->AddActionBinding(Handle);
	}
	return Handle;
}

FUIActionBindingHandle UHoldActionHelper::RegisterHoldActionFromEntry(const FHoldActionEntry& Entry)
{
	if (!Entry.InputAction || !OwnerWidget.IsValid())
	{
		LOG_WARNING(this, "UHoldActionHelper::RegisterHoldActionFromEntry - 无效的 Entry 配置");
		return FUIActionBindingHandle();
	}

	FBindUIActionArgs BindArgs(
		Entry.InputAction,
		FSimpleDelegate::CreateUObject(this, &ThisClass::HandleHoldExecuted)
	);

	BindArgs.bForceHold = Entry.bForceHold;
	BindArgs.bConsumeInput = Entry.bConsumeInput;
	BindArgs.bDisplayInActionBar = Entry.bDisplayInActionBar;
	BindArgs.InputMode = ECommonInputMode::Menu;

	BindArgs.OnHoldActionProgressed.BindUObject(this, &ThisClass::HandleHoldProgress);
	BindArgs.OnHoldActionPressed.BindUObject(this, &ThisClass::HandleHoldPressed);
	BindArgs.OnHoldActionReleased.BindUObject(this, &ThisClass::HandleHoldReleased);

	FUIActionBindingHandle Handle = OwnerWidget->RegisterUIActionBinding(BindArgs);
	BindingHandles.Add(Handle);

	// 增强输入路径下，CommonUI 构造函数不会填充 HoldMappings，需要手动注入。
	// 先从 Collection 移除再重新添加，确保 HoldBindingsCount 正确递增。
	if (Entry.bForceHold)
	{
		OwnerWidget->RemoveActionBinding(Handle);
		InjectHoldMappingsForEnhancedInput(Handle, Entry.HoldTime, Entry.HoldRollbackTime);
		OwnerWidget->AddActionBinding(Handle);
	}

	// 保存阈值配置
	HoldTriggerThreshold = FMath::Clamp(Entry.HoldTriggerThreshold, 0.f, 1.f);

	return Handle;
}

void UHoldActionHelper::RegisterHoldActions(const TArray<FHoldActionEntry>& Entries)
{
	for (const FHoldActionEntry& Entry : Entries)
	{
		RegisterHoldActionFromEntry(Entry);
	}
}

void UHoldActionHelper::UnregisterHoldAction(FUIActionBindingHandle Handle)
{
	if (OwnerWidget.IsValid())
	{
		OwnerWidget->RemoveActionBinding(Handle);
	}
	Handle.Unregister();
	BindingHandles.Remove(Handle);
}

void UHoldActionHelper::UnregisterAll()
{
	for (FUIActionBindingHandle& Handle : BindingHandles)
	{
		if (OwnerWidget.IsValid())
		{
			OwnerWidget->RemoveActionBinding(Handle);
		}
		Handle.Unregister();
	}
	BindingHandles.Empty();
}

void UHoldActionHelper::HandleHoldProgress(float Progress)
{
	const float EffectiveProgress = RemapProgressWithThreshold(Progress);

	// 检测阈值达成事件（仅触发一次）
	if (HoldTriggerThreshold > 0.f && !bThresholdReached && EffectiveProgress > 0.f)
	{
		bThresholdReached = true;
		OnHoldThresholdReached.Broadcast();
	}
	if (bThresholdReached)
	{
		OnHoldProgressChanged.Broadcast(EffectiveProgress);
	}

	// 原始进度达到 1.0 表示长按真正完成
	if (Progress >= 1.f)
	{
		bHoldProgressCompleted = true;
	}
}

void UHoldActionHelper::HandleHoldPressed()
{
	bThresholdReached = false;
	bHoldProgressCompleted = false;
	OnHoldStarted.Broadcast();
}

void UHoldActionHelper::HandleHoldReleased()
{
	// 重置阈值达成标记
	bThresholdReached = false;
	OnHoldCanceled.Broadcast();
	OnHoldProgressChanged.Broadcast(0.f);
}

void UHoldActionHelper::HandleHoldExecuted()
{
	// CommonUI 短按回退（GeneratePress）也会触发 OnExecuteAction，
	// 只有长按进度真正达到 1.0 时才广播完成事件
	if (!bHoldProgressCompleted)
	{
		return;
	}

	OnHoldCompleted.Broadcast();

	bThresholdReached = false;
	bHoldProgressCompleted = false;
}

float UHoldActionHelper::RemapProgressWithThreshold(float RawProgress) const
{
	// 无阈值时直接透传
	if (HoldTriggerThreshold <= 0.f)
	{
		return RawProgress;
	}

	// 原始进度未超过阈值，有效进度为 0
	if (RawProgress <= HoldTriggerThreshold)
	{
		return 0.f;
	}

	// 原始进度超过阈值后，将 [Threshold, 1.0] 线性映射到 [0.0, 1.0]
	return FMath::Clamp((RawProgress - HoldTriggerThreshold) / (1.f - HoldTriggerThreshold), 0.f, 1.f);
}

void UHoldActionHelper::InjectHoldMappingsForEnhancedInput(FUIActionBindingHandle Handle, float InHoldTime, float InHoldRollbackTime)
{
	TSharedPtr<FUIActionBinding> Binding = FUIActionBinding::FindBinding(Handle);
	if (!Binding.IsValid() || !Binding->InputAction.IsValid())
	{
		return;
	}

	// 获取增强输入系统中该 InputAction 绑定的所有按键
	TArray<FKey> ActionKeys;
	CommonUI::GetEnhancedInputActionKeys(
		Handle.GetBoundLocalPlayer(),
		Binding->InputAction.Get(),
		ActionKeys);

	if (ActionKeys.IsEmpty())
	{
		LOG_WARNING(this,
			"UHoldActionHelper::InjectHoldMappingsForEnhancedInput - InputAction '%s' 在当前 IMC 中没有绑定任何按键",
			*Binding->InputAction->GetName());
		return;
	}

	// 如果未指定自定义 HoldTime，从项目设置的 UCommonUIHoldData 获取默认值
	float ResolvedHoldTime = InHoldTime;
	float ResolvedRollbackTime = InHoldRollbackTime;

	if (ResolvedHoldTime <= 0.f)
	{
		TSubclassOf<UCommonUIHoldData> HoldDataClass = ICommonInputModule::GetSettings().GetDefaultHoldData();
		if (HoldDataClass)
		{
			const UCommonUIHoldData* HoldDataCDO = HoldDataClass.GetDefaultObject();
			// 默认使用键鼠的 HoldTime（各平台值通常相同，都是 0.75s）
			ResolvedHoldTime = HoldDataCDO->KeyboardAndMouse.HoldTime;
			ResolvedRollbackTime = HoldDataCDO->KeyboardAndMouse.HoldRollbackTime;
		}
		else
		{
			// 如果项目没有配置 HoldData，使用引擎默认值
			ResolvedHoldTime = 0.75f;
		}
	}

	// 将 NormalMappings 中匹配的按键转移到 HoldMappings，并注入新的 HoldMappings
	for (const FKey& Key : ActionKeys)
	{
		// 先移除 NormalMappings 中同一个 Key（如果有的话），避免既在 Normal 又在 Hold
		Binding->NormalMappings.RemoveAll([&Key](const FUIActionKeyMapping& Mapping)
		{
			return Mapping.Key == Key;
		});

		// 检查 HoldMappings 中是否已有该 Key（避免重复）
		const bool bAlreadyExists = Binding->HoldMappings.ContainsByPredicate(
			[&Key](const FUIActionKeyMapping& Mapping)
			{
				return Mapping.Key == Key;
			});

		if (!bAlreadyExists)
		{
			Binding->HoldMappings.Add(FUIActionKeyMapping(Key, ResolvedHoldTime, ResolvedRollbackTime));
			LOG_INFO(this,
				"InputAction '%s' add HoldMapping: Key=%s, HoldTime=%.2f, HoldRollbackTime=%.2f, HoldTriggerThreshold=%.2f",
				*Binding->InputAction->GetName(),
				*Key.ToString(),
				ResolvedHoldTime,
				ResolvedRollbackTime,
				HoldTriggerThreshold);
		}
	}
}
