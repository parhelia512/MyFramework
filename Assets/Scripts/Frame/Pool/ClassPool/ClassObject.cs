﻿using System;

// 可使用对象池进行创建和销毁的对象
// 继承WidgetUtility是为了在调用工具函数时方便,把一些完全独立的工具函数类串起来继承,所有继承自ClassObject的类都可以直接访问四大工具类中的函数
public class ClassObject : FrameUtility
{
	protected long mAssignID;   // 重新分配时的ID,每次分配都会设置一个新的唯一执行ID
	protected bool mDestroy;    // 当前对象是否已经被回收
	public ClassObject()
	{
		mDestroy = true;
	}
	public virtual void resetProperty()
	{
		mAssignID = 0;
		mDestroy = true;
	}
	public virtual void setDestroy(bool isDestroy) { mDestroy = isDestroy; }
	public virtual bool isDestroy() { return mDestroy; }
	public virtual void setAssignID(long assignID) { mAssignID = assignID; }
	public virtual long getAssignID() { return mAssignID; }
}