#ifndef HEX_CELL_DATA_CG_INCLUDE
#define HEX_CELL_DATA_CG_INCLUDE
sampler2D _HexCellData;							// ��Ԫ����������
float4 _HexCellData_TexelSize;					// �����С
 
 // �õ�һ�������θ��ӵ����ݣ�a�ǵ������ͣ�
 // v.texcoord2��¼�Ŷ������ڵ��������ӵ������� index��ʾ�����������ӵĵڼ���
float4 GetCellData (appdata_full v, int index) {
    float2 uv;

    //uv.x = v.texcoord2.x * _HexCellData_TexelSize.x;   
    //float row = floor(uv.x);
    //uv.x -= row;
	
	// �������θ����±�˵�ͼ��ĵ������õ�����λ�ڵڼ���
	float row = floor(v.texcoord2[index] * _HexCellData_TexelSize.x);
	// ����λ�ڵڼ���
	float col = v.texcoord2[index] % _HexCellData_TexelSize.z;

	uv.x = (col + 0.5) * _HexCellData_TexelSize.x;										// u, +0.5��Ϊ��ȡ��һ�����ص����Ķ����Ǳ�Ե������ȡ����ɫ
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;										// v
	
    float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));								// û��mipmap����z��w��0
	data.w *= 255;																		// 0~1ת����0~255
	return data;
}

// cellDataCoordinates.x ��ʾ�����ڵڼ��У� cellDataCoordinates.y ��ʾ�����ڵڼ���
float4 GetCellData (float2 cellDataCoordinates) {
    float2 uv = cellDataCoordinates + 0.5;												// +0.5��Ϊ��ȡ��һ�����ص����Ķ����Ǳ�Ե������ȡ����ɫ
    uv.x *= _HexCellData_TexelSize.x;
    uv.y *= _HexCellData_TexelSize.y;
    return tex2Dlod(_HexCellData, float4(uv, 0, 0));
}
#endif