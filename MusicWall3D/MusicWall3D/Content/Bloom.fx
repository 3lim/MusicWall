Texture2D<float4> Texture;

SamplerState PointSampler;
float BrightPassThreshold;

float4 PSBrightPassFilter(float2 tex : TEXCOORD) : SV_TARGET
{
	float3 color = Texture.Sample(PointSampler, tex).rgb;

	// Calculate perceptive luminance
	float luminance = dot(color, float3(0.299, 0.587, 0.114));  

	// Apply threshold
	color *= max(luminance - BrightPassThreshold, 0.0f) / luminance;
	
	return float4(color, 1.0);
}

// Blur5x5

float2 TextureTexelSize;
SamplerState LinearSampler;

float4 Blur5x5Core(float2 tex, float2 texScale)
{
	static const float offsets[2] = { 0, 1.217873 };
	static const float weights[2] = { 0.3745258, 0.3127371 };

	float3 value = 0;

	value += Texture.Sample(LinearSampler, tex - texScale * offsets[1]).rgb * weights[1];
	value += Texture.Sample(LinearSampler, tex).rgb * weights[0];
	value += Texture.Sample(LinearSampler, tex + texScale * offsets[1]).rgb * weights[1];

	return float4(value, 1.0);
}

// Horizontal gaussian blur
float4 PSBlur5x5H(float2 tex : TEXCOORD) : SV_Target
{
	return Blur5x5Core(tex, float2(1, 0) * TextureTexelSize);
}

// Vertical gaussian blur
float4 PSBlur5x5V(float2 tex : TEXCOORD) : SV_Target
{
	return Blur5x5Core(tex, float2(0, 1) * TextureTexelSize);
}

technique BrightPassTechnique
{
	pass 
	{
		Profile = 9.1;
		PixelShader = PSBrightPassFilter;
	}
}

technique BlurPassTechnique
{
	pass 
	{
		// Horizontal pass
		Profile = 9.1;
		PixelShader = PSBlur5x5H;
	}
	pass 
	{
		// Vertical pass
		Profile = 9.1;
		PixelShader = PSBlur5x5V;
	}
}
