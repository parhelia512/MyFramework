﻿using System;
using System.Collections.Generic;

// 场景流程
public abstract class SceneProcedure : DelayCmdWatcher
{
	protected Dictionary<Type, SceneProcedure> mChildProcedureList = new(); // 子流程列表
	protected SceneProcedure mParentProcedure;	// 父流程
	protected SceneProcedure mPrepareNext;      // 准备退出到的流程
	protected GameScene mGameScene;             // 流程所属的场景
	protected string mPrepareIntent;			// 传递参数用
	protected bool mInited;                     // 是否已经初始化,子节点在初始化时需要先确保父节点已经初始化
	public override void resetProperty()
	{
		base.resetProperty();
		mChildProcedureList.Clear();
		mParentProcedure = null;
		mPrepareNext = null;
		mGameScene = null;
		mPrepareIntent = null;
		mInited = false;
	}
	// 销毁场景时会调用流程的销毁
	public virtual void willDestroy() { }
	public override void destroy() { base.destroy(); }
	public void setGameScene(GameScene gameScene) { mGameScene = gameScene; }
	// 由GameScene调用
	// 进入流程
	public void init(SceneProcedure lastProcedure, string intent)
	{
		// 如果父节点还没有初始化,则先初始化父节点
		if (mParentProcedure != null && !mParentProcedure.mInited)
		{
			mParentProcedure.init(lastProcedure, intent);
			// 退出父节点自身而进入子节点
			mParentProcedure.onExitToChild(this);
			mParentProcedure.onExitSelf();
		}
		// 再初始化自己,如果是从子节点返回到父节点,则需要调用另外一个初始化函数
		if (lastProcedure != null && lastProcedure.isThisOrParent(getType()))
		{
			onInitFromChild(lastProcedure, intent);
		}
		else
		{
			onInit(lastProcedure, intent);
		}
		mInited = true;
	}
	// 更新流程
	public void update(float elapsedTime)
	{
		// 先更新父节点
		mParentProcedure?.update(elapsedTime);
		// 再更新自己
		onUpdate(elapsedTime);
	}
	public void lateUpdate(float elapsedTime)
	{
		mParentProcedure?.lateUpdate(elapsedTime);
		onLateUpdate(elapsedTime);
	}
	// 退出流程
	public void exit(SceneProcedure exitTo, SceneProcedure nextPro)
	{
		// 中断自己所有未执行的命令
		interruptAllCommand();
		// 当停止目标为自己时,则不再退出,此时需要判断当前将要进入的流程是否为当前流程的子流程
		// 如果是,则需要调用onExitToChild,执行退出当前并且进入子流程的操作
		// 如果不是则不需要调用,不需要执行任何退出操作
		if (this == exitTo)
		{
			if (nextPro != null && nextPro.isThisOrParent(getType()))
			{
				onExitToChild(nextPro);
				onExitSelf();
			}
			return;
		}
		// 先退出自己
		onExit(nextPro);
		onExitSelf();
		mInited = false;
		// 再退出父节点
		mParentProcedure?.exit(exitTo, nextPro);
		mPrepareNext = null;
		mPrepareIntent = null;
	}
	public void keyProcess(float elapsedTime)
	{
		// 先处理父节点按键响应
		mParentProcedure?.keyProcess(elapsedTime);
		// 然后再处理自己的按键响应
		onKeyProcess(elapsedTime);
	}
	public void getParentList(List<SceneProcedure> parentList)
	{
		// 由于父节点列表中需要包含自己,所以先加入自己
		parentList.Add(this);
		// 再加入父节点的所有父节点
		mParentProcedure?.getParentList(parentList);
	}
	// 获得自己和otherProcedure的共同的父节点
	public SceneProcedure getSameParent(SceneProcedure otherProcedure)
	{
		// 获得两个流程的父节点列表
		SceneProcedure sameParent = null;
		using var a = new ListScope2<SceneProcedure>(out var tempList0, out var tempList1);
		getParentList(tempList0);
		otherProcedure.getParentList(tempList1);
		// 从前往后判断,找到第一个相同的父节点
		int count0 = tempList0.Count;
		for (int i = 0; i < count0; ++i)
		{
			SceneProcedure thisParent = tempList0[i];
			foreach (SceneProcedure item in tempList1)
			{
				if (thisParent == item)
				{
					sameParent = thisParent;
					i = count0;
					break;
				}
			}
		}
		return sameParent;
	}
	public bool isThisOrParent(Type type)
	{
		// 判断是否是自己的类型
		if (getType() == type)
		{
			return true;
		}
		// 判断是否为父节点的类型
		if (mParentProcedure != null)
		{
			return mParentProcedure.isThisOrParent(type);
		}
		// 没有父节点,返回false
		return false;
	}
	public Type getType() { return GetType(); }
	public SceneProcedure getParent(Type type)
	{
		// 没有父节点,返回null
		if (mParentProcedure == null)
		{
			return null;
		}
		// 有父节点,则判断类型是否匹配,匹配则返回父节点
		if (mParentProcedure.getType() == type)
		{
			return mParentProcedure;
		}
		// 不匹配,则继续向上查找
		return mParentProcedure.getParent(type);
	}
	public bool addChildProcedure(SceneProcedure child)
	{
		if (child == null || !mChildProcedureList.TryAdd(child.getType(), child))
		{
			return false;
		}
		child.setParent(this);
		return true;
	}
	//------------------------------------------------------------------------------------------------------------------------------
	// 从自己的子流程进入当前流程
	protected virtual void onInitFromChild(SceneProcedure lastProcedure, string intent) { }
	// 在进入流程时调用
	// 在onInit中如果要跳转流程,必须使用延迟命令进行跳转
	protected abstract void onInit(SceneProcedure lastProcedure, string intent);
	// 更新流程时调用
	protected virtual void onUpdate(float elapsedTime) { }
	protected virtual void onLateUpdate(float elapsedTime) { }
	// 更新流程时调用
	protected virtual void onKeyProcess(float elapsedTime) { }
	// 退出当前流程,进入的不是自己的子流程时调用
	protected abstract void onExit(SceneProcedure nextProcedure);
	// 退出当前流程,进入自己的子流程时调用
	protected virtual void onExitToChild(SceneProcedure nextProcedure) { }
	// 退出当前流程进入其他任何流程时调用
	protected virtual void onExitSelf() { }
	protected bool setParent(SceneProcedure parent)
	{
		if (mParentProcedure != null)
		{
			return false;
		}
		mParentProcedure = parent;
		return true;
	}
}