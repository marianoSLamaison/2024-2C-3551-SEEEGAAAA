// Variables globales
float4x4 World;
float4x4 View;
float4x4 Projection;

float3 ambientColor;
float3 diffuseColor;
float3 specularColor;
float KAmbient;
float KDiffuse;
float KSpecular;
float shininess;
float3 lightPosition;

float4x4 LightViewProjection;
float2 shadowMapSize;

static const float modulatedEpsilon = 0.00001;
static const float maxEpsilon = 0.000005;


texture shadowMap;
sampler2D shadowMapSampler =
sampler_state
{
	Texture = <shadowMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

float3 CameraPosition;

texture2D baseTexture;
texture2D metallicTexture;  // Textura metálica
texture2D AOTexture;
texture2D roughnessTexture;
texture2D normalTexture;

sampler2D textureSampler = sampler_state
{
    Texture = (baseTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D metallicSampler = sampler_state
{
    Texture = (metallicTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D AOSampler = sampler_state
{
    Texture = (AOTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D normalSampler = sampler_state
{
	Texture = (normalTexture);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

// Entrada del vértice
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL;
    float2 TextureCoordinates : TEXCOORD0;
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float2 TextureCoordinates : TEXCOORD0;
    float4 WorldPosition : TEXCOORD3;
    float4 Normal : TEXCOORD2;
    float4 LightSpacePosition : TEXCOORD1;
};

struct DepthPassVertexShaderInput
{
	float4 Position : POSITION0;
};

struct DepthPassVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 ScreenSpacePosition : TEXCOORD1;
};

DepthPassVertexShaderOutput DepthPassVS(in DepthPassVertexShaderInput input)
{
	DepthPassVertexShaderOutput output;
	float4 worldPosition = mul(input.Position, World);
    // World space to View space
    float4 viewPosition = mul(worldPosition, View);	
	// View space to Projection space
    output.Position = mul(viewPosition, Projection);

	output.ScreenSpacePosition = output.Position;
	return output;
}

float4 DepthPassPS(in DepthPassVertexShaderOutput input) : COLOR
{
    float depth = input.ScreenSpacePosition.z / input.ScreenSpacePosition.w;
    return float4(depth, depth, depth, 1.0);
}


float3 getNormalFromMap(float2 textureCoordinates, float3 worldPosition, float3 worldNormal)
{
	float3 tangentNormal = tex2D(normalSampler, textureCoordinates).xyz * 2.0 - 1.0;

	float3 Q1 = ddx(worldPosition);
	float3 Q2 = ddy(worldPosition);
	float2 st1 = ddx(textureCoordinates);
	float2 st2 = ddy(textureCoordinates);

	worldNormal = normalize(worldNormal.xyz);
	float3 T = normalize(Q1 * st2.y - Q2 * st1.y);
	float3 B = -normalize(cross(worldNormal, T));
	float3x3 TBN = float3x3(T, B, worldNormal);

	return normalize(mul(tangentNormal, TBN));
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.WorldPosition = worldPosition;
    output.Normal = normalize(mul(float4(input.Normal.xyz, 0.0), World));
    output.TextureCoordinates = input.TextureCoordinates;

    output.LightSpacePosition = mul(output.WorldPosition, LightViewProjection);

    return output;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float3 lightSpacePosition = input.LightSpacePosition.xyz / input.LightSpacePosition.w;
    float2 shadowMapTextureCoordinates = 0.5 * lightSpacePosition.xy + float2(0.5, 0.5);
    shadowMapTextureCoordinates.y = 1.0f - shadowMapTextureCoordinates.y;

    float3 lightDirection = normalize(lightPosition - input.WorldPosition.xyz);
    float3 viewDirection = normalize(CameraPosition - input.WorldPosition.xyz);
    float3 halfVector = normalize(lightDirection + viewDirection);

    float3 Normal = getNormalFromMap(input.TextureCoordinates, input.WorldPosition.xyz, input.Normal.xyz);

    float inclinationBias = max(modulatedEpsilon * (1.0 - dot(Normal, lightDirection)), maxEpsilon);
    float shadowMapDepth = tex2D(shadowMapSampler, shadowMapTextureCoordinates).r + inclinationBias;

    // Compare the shadowmap with the REAL depth of this fragment
	// in light space
    float notInShadow = 0.0;
    float2 texelSize = 1.0 / shadowMapSize;
    for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            float pcfDepth = tex2D(shadowMapSampler, shadowMapTextureCoordinates + float2(x, y) * texelSize).r + inclinationBias;
            notInShadow += step(lightSpacePosition.z, pcfDepth) / 9.0;
        }

    // Texturas
    float4 texelColor = tex2D(textureSampler, input.TextureCoordinates);
    float metallic = tex2D(metallicSampler, input.TextureCoordinates).r;
    float AO = tex2D(AOSampler, input.TextureCoordinates).r;

    // Cálculo de luz difusa y especular ajustada por metallic
    float NdotL = saturate(dot(Normal, lightDirection));
    float3 diffuseLight = KDiffuse * diffuseColor * NdotL;

    // Reflexión especular con efecto de metallic
    float NdotH = dot(Normal, halfVector);
    float3 specularLight = metallic * KSpecular * specularColor * pow(saturate(NdotH), shininess);

    // Color final
    float4 finalColor = float4(saturate(ambientColor * KAmbient * AO + diffuseLight) * texelColor.rgb + specularLight, texelColor.a);
    finalColor.rgb *= 0.5 + 0.5 * notInShadow;

    return finalColor;
}

// Técnica
technique AutoTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}

technique DepthPass{
    pass Pass0
    {
        VertexShader = compile vs_3_0 DepthPassVS();
        PixelShader = compile ps_3_0 DepthPassPS();
    }
}
