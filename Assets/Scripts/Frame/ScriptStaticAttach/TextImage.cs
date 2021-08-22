﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class TextImage : Text
{
	protected List<myUGUIImage> mImageList;
	protected List<UIVertex> mVertexStream;
	protected CreateImage mCreateImage;
	protected DestroyImage mDestroyImage;
	// 图片的最后一个顶点的索引
	protected List<int> mImagesQuadIndex;
	// 正则取出所需要的属性
	protected static Regex mRegex = new Regex(@"<quad width=(\d*\.?\d+%?) sprite=(.+?)/>", RegexOptions.Singleline);
	public TextImage()
	{
		mImageList = new List<myUGUIImage>();
		mVertexStream = new List<UIVertex>();
		mImagesQuadIndex = new List<int>();
	}
	public void setCreateImage(CreateImage callback) { mCreateImage = callback; }
	public void setDestroyImage(DestroyImage callback) { mDestroyImage = callback; }
	// 此函数由UGUI自动调用
	public override void SetVerticesDirty()
	{
		base.SetVerticesDirty();
		if (mCreateImage == null || mDestroyImage == null)
		{
			return;
		}

		// 销毁现有的所有Image
		int curImageCount = mImageList.Count;
		for(int i = 0; i < curImageCount; ++i)
		{
			mDestroyImage(mImageList[i]);
		}
		mImageList.Clear();

		mImagesQuadIndex.Clear();
		int ignoreCount = 0;
		MatchCollection result = mRegex.Matches(text);
		int matchCount = result.Count;
		for(int i = 0; i < matchCount; ++i)
		{
			Match match = result[i];
			mImagesQuadIndex.Add(match.Index - ignoreCount);
			ignoreCount += match.Length - 1;
			myUGUIImage image = mCreateImage();
			image.setSpriteName(match.Groups[2].Value);
			float scale = float.Parse(match.Groups[1].Value);
			Vector2 spriteSize = image.getSpriteSize();
			image.setWindowSize(new Vector2(fontSize * scale, fontSize * scale * (spriteSize.y / spriteSize.x)));
			mImageList.Add(image);
		}
	}
	//------------------------------------------------------------------------------------------------------------------------------
	// 此函数由UGUI自动调用
	protected override void OnPopulateMesh(VertexHelper toFill)
	{
		base.OnPopulateMesh(toFill);
		mVertexStream.Clear();
		toFill.GetUIVertexStream(mVertexStream);
		for (int i = 0; i < mImagesQuadIndex.Count; ++i)
		{
			int quadIndex = mImagesQuadIndex[i];
			// 在获得的mVertexStream中每个四边形有2个三角形,共6个顶点
			int vertexIndex = quadIndex * 6;
			if (vertexIndex >= mVertexStream.Count)
			{
				break;
			}
			myUGUIImage image = mImageList[i];
			Vector3 pos0 = mVertexStream[vertexIndex].position;
			Vector3 pos1 = mVertexStream[vertexIndex + 1].position;
			Vector3 pos2 = mVertexStream[vertexIndex + 2].position;

			// 此处的width其实就等于fontSize * scale,scale是富文本标签中的width值
			float width = pos1.x - pos0.x;
			// y坐标需要调整,不做调整会有一定的向上偏移量,不知道这个偏移量是怎么产生的
			Vector3 pos = (pos2 + pos0) * 0.5f;
			pos.y -= (int)(width * 0.16f);
			image.setPosition(pos);
			
			// 抹掉左下角的不需要渲染的部分,也就是将其顶点合并,这里的下标仅仅是顶点的下标,不考虑索引
			for(int j = 1; j < 4; ++j)
			{
				toFill.SetUIVertex(mVertexStream[vertexIndex], quadIndex * 4 + j);
			}
		}
	}
}