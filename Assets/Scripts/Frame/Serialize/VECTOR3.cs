﻿using System;
using UnityEngine;

// 自定义的对Vector3的封装,提供类似于Vector3指针的功能,可用于序列化
public class VECTOR3 : SerializableBit
{
	public Vector3 mValue;      // 值
	public override void resetProperty()
	{
		base.resetProperty();
		mValue = Vector3.zero;
	}
	public void set(Vector3 value) { mValue = value; }
	public override bool read(SerializerBitRead reader)
	{
		return reader.read(out mValue);
	}
	public override void write(SerializerBitWrite writer)
	{
		writer.write(mValue);
	}
}