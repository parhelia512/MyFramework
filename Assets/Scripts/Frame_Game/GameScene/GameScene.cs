﻿using UnityEngine;
using System;
using System.Collections.Generic;
using static UnityUtility;
using static FrameBase;
using static CSharpUtility;

// 用于实现一个逻辑场景,一个逻辑场景会包含多个流程,进入一个场景时上一个场景会被销毁
public abstract class GameScene : ComponentOwner
{
	protected Dictionary<Type, SceneProcedure> mSceneProcedureList = new(); // 场景的流程列表
	protected List<SceneProcedure> mLastProcedureList = new();				// 所进入过的所有流程
	protected SceneProcedure mCurProcedure;									// 当前流程
	protected GameObject mObject;											// 场景对应的GameObject
	protected Type mStartProcedure;											// 起始流程类型,进入场景时会默认进入该流程
	protected Type mTempStartProcedure;										// 仅使用一次的起始流程类型,设置后进入场景时会默认进入该流程,生效后就清除
	protected Type mExitProcedure;											// 场景的退出流程,退出场景进入其他场景时会先进入该流程,一般用作资源卸载
	protected const int MAX_LAST_PROCEDURE_COUNT = 8;						// mLastProcedureList列表的最大长度,当超过该长度时,会移除列表开始的元素
	protected string mTempStartIntent;										// 进入mTempStartProcedure时的参数
	// 进入场景时初始化
	public virtual void init()
	{
		// 创建场景对应的物体,并挂接到场景管理器下
		mObject = createGameObject(mName, mGameSceneManager.getObject());
		initComponents();
		// 创建出所有的场景流程
		createSceneProcedure();
		// 设置起始流程名
		assignStartExitProcedure();
		// 开始执行起始流程
		enterStartProcedure();
	}
	public override void resetProperty()
	{
		base.resetProperty();
		mSceneProcedureList.Clear();
		mLastProcedureList.Clear();
		mCurProcedure = null;
		mObject = null;
		mStartProcedure = null;
		mTempStartProcedure = null;
		mExitProcedure = null;
		mTempStartIntent = null;
	}
	public void enterStartProcedure()
	{
		changeProcedure(mTempStartProcedure ?? mStartProcedure, mTempStartIntent);
		mTempStartProcedure = null;
	}
	public virtual void willDestroy()
	{
		foreach (SceneProcedure item in mSceneProcedureList.Values)
		{
			item.willDestroy();
		}
	}
	public override void destroy()
	{
		base.destroy();
		// 销毁所有流程
		foreach (SceneProcedure item in mSceneProcedureList.Values)
		{
			item.destroy();
		}
		mSceneProcedureList.Clear();
		destroyUnityObject(ref mObject);
	}
	public override void update(float elapsedTime)
	{
		// 更新组件
		base.update(elapsedTime);
		// 更新当前流程
		mCurProcedure?.update(elapsedTime);
	}
	public override void lateUpdate(float elapsedTime)
	{
		base.lateUpdate(elapsedTime);
		mCurProcedure?.lateUpdate(elapsedTime);
	}
	// 退出场景
	public virtual void exit()
	{
		// 首先进入退出流程,然后再退出最后的流程
		changeProcedure(mExitProcedure, null);
		mCurProcedure?.exit(null, null);
		mCurProcedure = null;
		GC.Collect();
	}
	public abstract void assignStartExitProcedure();
	public virtual void createSceneProcedure() { }
	public bool changeProcedure(Type procedureType, string intent, bool addToLastList = true)
	{
		// 不能重复进入同一流程
		if (mCurProcedure != null && mCurProcedure.getType() == procedureType)
		{
			return false;
		}
		if (!mSceneProcedureList.TryGetValue(procedureType, out SceneProcedure targetProcedure))
		{
			logError("can not find scene procedure : " + procedureType);
			return false;
		}
		log("enter procedure:" + procedureType);
		// 将上一个流程记录到返回列表中
		if (mCurProcedure != null && addToLastList)
		{
			mLastProcedureList.Add(mCurProcedure);
			if (mLastProcedureList.Count > MAX_LAST_PROCEDURE_COUNT)
			{
				mLastProcedureList.RemoveAt(0);
			}
		}
		if (mCurProcedure == null || mCurProcedure.getType() != procedureType)
		{
			// 如果当前已经在一个流程中了,则要先退出当前流程,但是不要销毁流程
			// 需要找到共同的父节点,退到该父节点时则不再退出
			mCurProcedure?.exit(mCurProcedure.getSameParent(targetProcedure), targetProcedure);
			SceneProcedure lastProcedure = mCurProcedure;
			mCurProcedure = targetProcedure;
			mCurProcedure.init(lastProcedure, intent);
		}
		return true;
	}
	public SceneProcedure getProcedure(Type type) { return mSceneProcedureList.get(type); }
	public SceneProcedure getCurProcedure() { return mCurProcedure; }
	public void setTempStartProcedure(Type procedure, string intent)
	{
		mTempStartProcedure = procedure;
		mTempStartIntent = intent;
	}
	public T addProcedure<T>(Type parent = null) where T : SceneProcedure
	{
		return addProcedure(typeof(T), parent) as T;
	}
	public SceneProcedure addProcedure(Type type, Type parent = null)
	{
		var procedure = createInstance<SceneProcedure>(type);
		procedure.setGameScene(this);
		if (parent != null)
		{
			SceneProcedure parentProcedure = getProcedure(parent);
			if (parentProcedure == null)
			{
				logError("invalid parent procedure, procedure:" + procedure.getType());
			}
			parentProcedure.addChildProcedure(procedure);
		}
		return mSceneProcedureList.add(procedure.getType(), procedure);
	}
}