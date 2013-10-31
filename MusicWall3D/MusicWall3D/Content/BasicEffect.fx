matrix viewProj;
matrix view;
matrix projInv;

float  SpecularPower;
float3 SpecularColor;
float3 DiffuseColor;
float3 EmissiveColor;

float3 DirLight0Direction;
float3 DirLight0DiffuseColor;
float3 DirLight0SpecularColor;

float3 DirLight1Direction;
float3 DirLight1DiffuseColor;
float3 DirLight1SpecularColor;

float3 DirLight2Direction;
float3 DirLight2DiffuseColor;
float3 DirLight2SpecularColor;

float3 EyePosition;

float3 FogColor;
float FogStart;
float FogEnd;

struct VS_IN
{
	float3 pos : SV_Position;
	float3 normal : NORMAL;

	matrix world : WORLD;
	float4 color : COLOR;
};
//
//struct PS_IN
//{
//	float4 pos : SV_Position;
//	float3 posView : POSVIEW;
//	float4 normal : NORMAL;
//	float4 color : COLOR;
//};

//PS_IN BasicVS(VS_IN input)
//{
//	PS_IN output = (PS_IN)0;
//
//	output.pos = mul(float4(input.pos,1),input.world);
//	output.posView = mul(output.pos,view).xyz;
//	output.pos = mul(output.pos,viewProj);
//	output.normal = normalize(mul(mul(float4(input.normal,0),input.world),projInv));
//	output.color = input.color;
//	return output;
//}
//
//float4 BasicPS(PS_IN input) : SV_Target0
//{
//	float4 r = reflect(-lightView,input.normal);
//	float4 v = float4(normalize(-input.posView),0);
//	
//	 return float4((0.5 * input.color * saturate(dot(input.normal,lightView)) * lightColor
//		+ 0.7 * input.color * pow(saturate(dot(r,v)),16) * lightColor
//		+ 0.2 * input.color * lightColor).xyz,1);
//}

float ComputeFogFactor(float4 position,matrix world)
{
	matrix worldView = mul(world,view);
	float scale = 1.0 / (FogStart - FogEnd);
	return saturate(dot(position, float4(worldView._13 * scale, worldView._23 * scale, worldView._33 * scale, (worldView._43 + FogStart) * scale)));
}

void ApplyFog(inout float4 color, float fogFactor)
{
	color.rgb = lerp(color.rgb, FogColor * color.a, fogFactor);
}

void AddSpecular(inout float4 color, float3 specular)
{
	color.rgb += specular * color.a;
}

struct ColorPair
{
	float3 Diffuse;
	float3 Specular;
};

ColorPair ComputeLights(float3 eyeVector, float3 worldNormal, uniform int numLights)
{
	float3x3 lightDirections = 0;
	float3x3 lightDiffuse = 0;
	float3x3 lightSpecular = 0;
	float3x3 halfVectors = 0;
	
	[unroll]
	for (int i = 0; i < numLights; i++)
	{
		lightDirections[i] = float3x3(DirLight0Direction,     DirLight1Direction,     DirLight2Direction)    [i];
		lightDiffuse[i]    = float3x3(DirLight0DiffuseColor,  DirLight1DiffuseColor,  DirLight2DiffuseColor) [i];
		lightSpecular[i]   = float3x3(DirLight0SpecularColor, DirLight1SpecularColor, DirLight2SpecularColor)[i];
		
		halfVectors[i] = normalize(eyeVector - lightDirections[i]);
	}

	float3 dotL = mul(-lightDirections, worldNormal);
	float3 dotH = mul(halfVectors, worldNormal);
	
	float3 zeroL = step(0, dotL);

	float3 diffuse  = zeroL * dotL;
	float3 specular = pow(max(dotH, 0) * zeroL, SpecularPower);

	ColorPair result;
	
	result.Diffuse  = mul(diffuse,  lightDiffuse)  * DiffuseColor.rgb + EmissiveColor;
	result.Specular = mul(specular, lightSpecular) * SpecularColor;

	return result;
}

struct VSOutputPixelLighting
{
	float4 PositionWS : TEXCOORD0;
	float3 NormalWS   : TEXCOORD1;
	float4 Diffuse    : COLOR0;
	float4 PositionPS : SV_Position;
};

struct CommonVSOutputPixelLighting
{
	float4 Pos_ps;
	float3 Pos_ws;
	float3 Normal_ws;
	float  FogFactor;
};

CommonVSOutputPixelLighting ComputeCommonVSOutputPixelLighting(float4 position, float3 normal, matrix world)
{
	CommonVSOutputPixelLighting vout;
	
	vout.Pos_ps = mul(position, mul(world,viewProj));
	vout.Pos_ws = mul(position, mul(world,view)).xyz;
	vout.Normal_ws = normalize(mul(mul(float4(normal,0),  world), projInv).xyz);
	vout.FogFactor = ComputeFogFactor(position,world);
	
	return vout;
}

VSOutputPixelLighting BasicVS(VS_IN vin)
{
	VSOutputPixelLighting vout;
	
	CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(float4(vin.pos,1), vin.normal, vin.world);

	vout.PositionPS = cout.Pos_ps;
	vout.PositionWS = float4(cout.Pos_ws, cout.FogFactor);
	vout.NormalWS = cout.Normal_ws;
	vout.Diffuse = vin.color;
	
	return vout;
}

float4 BasicPS(VSOutputPixelLighting pin) : SV_Target0
{
	float4 color = pin.Diffuse;

	float3 eyeVector = normalize(EyePosition - pin.PositionWS.xyz);
	float3 worldNormal = normalize(pin.NormalWS);
	
	ColorPair lightResult = ComputeLights(eyeVector, worldNormal, 3);

	color.rgb *=  lightResult.Diffuse;
	
	AddSpecular(color, lightResult.Specular);
	ApplyFog(color, pin.PositionWS.w);
	
	return float4(color.xyz,pin.Diffuse.w);
}

RasterizerState rsCullBack {
	CullMode = Back;
};

DepthStencilState EnableDepth
{
	DepthEnable = TRUE;
	DepthWriteMask = ALL;
	DepthFunc = LESS_EQUAL;
};

BlendState NoBlending
{
	AlphaToCoverageEnable = FALSE;
	BlendEnable[0] = FALSE;
};

BlendState BSBlendOver 
{ 
	BlendEnable[0]    = TRUE; 
	SrcBlend[0]       = SRC_ALPHA; 
	SrcBlendAlpha[0]  = ONE; 
	DestBlend[0]      = INV_SRC_ALPHA; 
	DestBlendAlpha[0] = INV_SRC_ALPHA; 
}; 

technique11 Render
{
	pass Basic
	{
		SetVertexShader(CompileShader(vs_4_0, BasicVS()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, BasicPS()));

		SetDepthStencilState(EnableDepth, 0);
		SetBlendState(BSBlendOver, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
	}
}
