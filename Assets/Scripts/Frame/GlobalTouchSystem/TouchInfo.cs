﻿using UnityEngine;
using System.Collections.Generic;
using static MathUtility;
using static FrameUtility;
using static FrameBase;

// 存放一个触点的悬停和按下的物体
public class TouchInfo : ClassObject
{
	protected HashSet<IMouseEventCollect> mHoverList;	// 触点当前悬停的物体列表
	protected SafeList<IMouseEventCollect> mPressList;	// 保存鼠标按下时所选中的所有物体,需要给这些窗口发送鼠标移动的消息
	protected TouchPoint mTouch;						// 触点信息
	public TouchInfo()
	{
		mHoverList = new HashSet<IMouseEventCollect>();
		mPressList = new SafeList<IMouseEventCollect>();
	}
	public void init(TouchPoint touch)
	{
		mTouch = touch;
	}
	public override void resetProperty()
	{
		base.resetProperty();
		mHoverList.Clear();
		mPressList.clear();
		mTouch = null;
	}
	public void update(float elapsedTime)
	{
		if (isVectorZero(mTouch.getMoveDelta()))
		{
			return;
		}

		int touchID = mTouch.getTouchID();
		Vector3 curPos = mTouch.getCurPosition();
		// 通知触点移动,只通知触点按下时的窗口列表
		foreach (var obj in mPressList.startForeach())
		{
			if (isVectorZero(mTouch.getMoveDelta()))
			{
				obj.onMouseStay(curPos, mTouch.getTouchID());
			}
			else
			{
				obj.onMouseMove(curPos, mTouch.getMoveDelta(), elapsedTime, mTouch.getTouchID());
			}
		}
		mPressList.endForeach();

		using (new HashSetScope<IMouseEventCollect>(out var newList))
		{
			mGlobalTouchSystem.getAllHoverWindow(newList, curPos);
			// 触点是否刚进入了某个窗口,只有触点移动时才检测
			foreach (var item in newList)
			{
				// 不过也许此时悬停窗口已经不接收输入事件了或者碰撞盒子被禁用了,需要判断一下
				if (!mHoverList.Contains(item) && item.isActive() && item.isHandleInput())
				{
					item.onMouseEnter(curPos, touchID);
				}
			}
			// 触点是否刚离开了某个窗口,只有触点移动时才检测
			foreach (var item in mHoverList)
			{
				// 不过也许此时悬停窗口已经不接收输入事件了或者碰撞盒子被禁用了,需要判断一下
				if (!newList.Contains(item) && item.isActive() && item.isHandleInput())
				{
					item.onMouseLeave(curPos, touchID);
				}
			}
			mHoverList.Clear();
			foreach (var item in newList)
			{
				mHoverList.Add(item);
			}
		}
	}
	public void touchPress()
	{
		mGlobalTouchSystem.getAllHoverWindow(mHoverList, mTouch.getCurPosition());
		foreach(var item in mHoverList)
		{
			mPressList.add(item);
		}
	}
	public void clearPressList() { mPressList.clear(); }
	public SafeList<IMouseEventCollect> getPressList() { return mPressList; }
	public TouchPoint getTouch() { return mTouch; }
	public void removeObject(IMouseEventCollect obj)
	{
		mPressList.remove(obj);
		mHoverList.Remove(obj);
	}	
}