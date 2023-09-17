﻿using System;
using static UnityUtility;
using static FrameUtility;
using static FrameBase;
using static CSharpUtility;
using static MathUtility;
using static BinaryUtility;
using static FrameDefine;

// Frame层默认的UDP连接封装类,应用层可根据实际需求仿照此类封装自己的UDP连接类
public class NetConnectUDPFrame : NetConnectUDP
{
	protected EncryptPacket mEncryptPacket;
	protected DecryptPacket mDecryptPacket;
	protected SerializerBitWrite mBitWriter;
	protected long mToken;						// 用于服务器识别客户端的唯一凭证,一般是当前角色的ID
	public NetConnectUDPFrame()
	{
		mBitWriter = new SerializerBitWrite();
	}
	public override void resetProperty()
	{
		base.resetProperty();
		mEncryptPacket = null;
		mDecryptPacket = null;
	}
	public void setToken(long token) { mToken = token; }
	public void setEncrypt(EncryptPacket encrypt, DecryptPacket decrypt)
	{
		mEncryptPacket = encrypt;
		mDecryptPacket = decrypt;
	}
	public override void sendNetPacket(NetPacket packet)
	{
		if (!isMainThread())
		{
			mSocketFactory.destroyPacket(packet);
			logError("只能在主线程发送消息");
			return;
		}
		if (mSocket == null)
		{
			mSocketFactory.destroyPacket(packet);
			return;
		}
		var netPacket = packet as NetPacketFrame;
		if (netPacket.isDestroy())
		{
			logError("消息对象已经被销毁,数据无效");
			return;
		}

		mBitWriter.clear();
		netPacket.write(mBitWriter, out ulong fieldFlag);
		int realPacketSize = mBitWriter.getByteCount();
		byte[] packetBodyData = mBitWriter.getBuffer();
		// 序列化完以后立即销毁消息包
		mSocketFactory.destroyPacket(netPacket);

		if (realPacketSize < 0)
		{
			logError("消息序列化失败!");
			return;
		}

		// 加密包体
		if (packetBodyData != null)
		{
			mEncryptPacket?.Invoke(packetBodyData, 0, realPacketSize, 0);
		}

		using (new ClassScope<SerializerBitWrite>(out var writer))
		{
			writer.write(realPacketSize);
			writer.write(generateCRC16(realPacketSize));
			writer.write(netPacket.getPacketType());
			writer.write(mToken);
			// 写入一位用于获取是否需要使用标记位
			writer.write(fieldFlag != FULL_FIELD_FLAG);
			if (fieldFlag != FULL_FIELD_FLAG)
			{
				writer.write(fieldFlag);
			}
			if (packetBodyData != null)
			{
				writer.writeBuffer(packetBodyData, realPacketSize);
			}
			writer.write(generateCRC16(writer.getBuffer(), writer.getByteCount()));
			int curByteCount = writer.getByteCount();
			ARRAY_THREAD(out byte[] packetData, getGreaterPow2(curByteCount));
			memcpy(packetData, writer.getBuffer(), 0, 0, curByteCount);
			// 添加到写缓冲中
			mOutputBuffer.add(new OutputDataInfo(packetData, curByteCount));
		}
	}
	//------------------------------------------------------------------------------------------------------------------------------
	protected override PARSE_RESULT preParsePacket(byte[] buffer, int size, ref int bitIndex, out byte[] outPacket, out ushort packetType, out int packetSize, out ulong fieldFlag)
	{
		outPacket = null;
		packetType = 0;
		packetSize = 0;
		fieldFlag = FULL_FIELD_FLAG;
		// 可能还没有接收完全,等待下次接收
		if (size == 0)
		{
			return PARSE_RESULT.NOT_ENOUGH;
		}
		using (new ClassThreadScope<SerializerBitRead>(out var reader))
		{
			reader.init(buffer, size, bitIndex);
			if (!reader.read(out packetSize))
			{
				return PARSE_RESULT.NOT_ENOUGH;
			}
			if (!reader.read(out ushort packetSizeCRC))
			{
				return PARSE_RESULT.NOT_ENOUGH;
			}
			if (generateCRC16(packetSize) != packetSizeCRC)
			{
				return PARSE_RESULT.ERROR;
			}
			if (!reader.read(out packetType))
			{
				return PARSE_RESULT.NOT_ENOUGH;
			}
			if (!reader.read(out bool useFlag))
			{
				return PARSE_RESULT.NOT_ENOUGH;
			}
			if (useFlag && !reader.read(out fieldFlag))
			{
				return PARSE_RESULT.NOT_ENOUGH;
			}
			if (packetSize > 0)
			{
				ARRAY_THREAD(out outPacket, getGreaterPow2(packetSize));
				if (!reader.readBuffer(outPacket, packetSize))
				{
					UN_ARRAY_THREAD(ref outPacket);
					return PARSE_RESULT.NOT_ENOUGH;
				}
			}
			reader.skipToByteEnd();
			// 需要在读取crc之前就计算crc
			ushort generatedCRC = generateCRC16(reader.getBuffer(), reader.getBufferSize());
			if (!reader.read(out ushort readCrc))
			{
				if (outPacket != null)
				{
					UN_ARRAY_THREAD(ref outPacket);
				}
				return PARSE_RESULT.NOT_ENOUGH;
			}
			// 客户端接收到的必须是SC类型的
			if (!(packetType > SC_GAME_MIN && packetType < SC_GAME_MAX) && 
				!(packetType > SC_GAME_CORE_MIN && packetType < SC_GAME_CORE_MAX))
			{
				if (outPacket != null)
				{
					UN_ARRAY_THREAD(ref outPacket);
				}
				logError("包类型错误:" + packetType);
				debugHistoryPacket();
				mInputBuffer.clear();
				return PARSE_RESULT.ERROR;
			}

			if (generatedCRC != readCrc)
			{
				logError("crc校验失败:" + packetType + ",解析出的crc:" + readCrc + ",计算出的crc:" + generatedCRC);
				if (outPacket != null)
				{
					UN_ARRAY_THREAD(ref outPacket);
				}
				return PARSE_RESULT.ERROR;
			}
			bitIndex = reader.getBitIndex();
		}
		return PARSE_RESULT.SUCCESS;
	}
	// 解析包体数据
	protected override NetPacket parsePacket(ushort packetType, byte[] buffer, int size, ulong fieldFlag)
	{
		// 创建对应的消息包,并设置数据,然后放入列表中等待解析
		var packetReply = mSocketFactory.createSocketPacket(packetType) as NetPacketFrame;
		packetReply.setConnect(this);

		// 解密包体
		if (buffer != null && size > 0)
		{
			mDecryptPacket?.Invoke(buffer, 0, size, 0);
			int readDataCount = 0;
			using (new ClassScope<SerializerBitRead>(out var reader))
			{
				reader.init(buffer, size);
				if (!packetReply.read(reader, fieldFlag))
				{
					logError("解析失败:" + packetReply.getPacketType());
				}
				readDataCount = reader.getReadByteCount();
			}
			if (readDataCount != size)
			{
				logError("接收字节数与解析后消息包字节数不一致:" + packetType + ",接收:" + size + ", 解析:" + readDataCount + ", type:" + packetType);
				mSocketFactory.destroyPacket(packetReply);
				return null;
			}
		}
		return packetReply;
	}
}