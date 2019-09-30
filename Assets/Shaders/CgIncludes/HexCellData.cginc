#ifndef HEX_CELL_DATA_CG_INCLUDE
#define HEX_CELL_DATA_CG_INCLUDE
sampler2D _HexCellData;							// 单元格数据纹理
float4 _HexCellData_TexelSize;					// 纹理大小
 
 // 得到一个六边形格子的数据（a是地形类型）
 // v.texcoord2记录着顶点相邻的三个格子的索引， index表示是这三个格子的第几个
float4 GetCellData (appdata_full v, int index) {
    float2 uv;

    //uv.x = v.texcoord2.x * _HexCellData_TexelSize.x;   
    //float row = floor(uv.x);
    //uv.x -= row;
	
	// 用六边形格子下标乘地图宽的倒数，得到格子位于第几行
	float row = floor(v.texcoord2[index] * _HexCellData_TexelSize.x);
	// 格子位于第几列
	float col = v.texcoord2[index] % _HexCellData_TexelSize.z;

	uv.x = (col + 0.5) * _HexCellData_TexelSize.x;										// u, +0.5是为了取到一个像素的中心而不是边缘，避免取错颜色
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;										// v
	
    float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));								// 没有mipmap所以z和w填0
	data.w *= 255;																		// 0~1转换成0~255
	return data;
}

// cellDataCoordinates.x 表示格子在第几列， cellDataCoordinates.y 表示格子在第几行
float4 GetCellData (float2 cellDataCoordinates) {
    float2 uv = cellDataCoordinates + 0.5;												// +0.5是为了取到一个像素的中心而不是边缘，避免取错颜色
    uv.x *= _HexCellData_TexelSize.x;
    uv.y *= _HexCellData_TexelSize.y;
    return tex2Dlod(_HexCellData, float4(uv, 0, 0));
}
#endif