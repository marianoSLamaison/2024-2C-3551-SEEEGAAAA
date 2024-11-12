#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL vs_4_0_level_9_1
#endif

// Variables globales
float4x4 World;//
float4x4 View;
float4x4 Projection;

float3 CameraPosition;//

// Texturas


texture BlueRockTexture;
texture MountainRockTexture;
// Sampler para las texturas
SamplerState SamplerType
{
    texture = (BlueRockTexture);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = Mirror;
    AddressV = Mirror;
};
sampler2D samplerTest
{
    texture = (BlueRockTexture);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = Mirror;
    AddressV = Mirror;
};

// Entrada del vértice
struct VertexShaderInput
{
    float4 Position : POSITION0;   // La posición inicial del vértice
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : POSITION0;    // Posición final que necesita el rasterizador
    float3 TextureCoordinate : TEXCOORD0;      // Coordenadas de textura
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    float4 VertexPosition = mul(input.Position, World);
    output.TextureCoordinate = VertexPosition.xyz - CameraPosition;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(samplerTest, input.TextureCoordinate.xy);
    //return float4(texCUBE(SamplerType, normalize(input.TextureCoordinate)).rgb,1);
    return color;
}

// Técnica
technique PlatformTechnique
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}