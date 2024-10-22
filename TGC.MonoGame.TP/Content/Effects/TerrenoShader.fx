#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL vs_4_0_level_9_1
#endif

// Variables globales
float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraPosition;



// Texturas
Texture2D Diffuse;
//Texture2D HeightMapTexture;
Texture2D NormalTexture;

// Sampler para las texturas
SamplerState SamplerType
{
    Filter = Anisotropic;
    AddressU = Wrap;
    AddressV = Wrap;
};


// Entrada del vértice
struct VertexShaderInput
{
    float4 Position : POSITION0;   // La posición inicial del vértice
    float2 TexCoord : TEXCOORD0;  // Coordenadas de textura
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : SV_Position;    // Posición final que necesita el rasterizador
    float2 TexCoord : TEXCOORD0;      // Coordenadas de textura
    float4 worldPosition : TEXCOORD1;    // POS EN LA MATRIZ DE MUNDO
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    // Transformar la posición del vértice a espacio del mundo y luego a espacio de clip
    float4 worldPosition = mul(input.Position, World);
    // World space to View space
    float4 viewPosition = mul(worldPosition, View);	
	// View space to Projection space
    output.Position = mul(viewPosition, Projection);

    output.worldPosition = input.Position;

    output.TexCoord = input.Position.xz;

    return output;
}

float4 PS(VertexShaderOutput input) : COLOR
{

    float4 diffuseColor = Diffuse.Sample(SamplerType, input.TexCoord*0.001);
    float4 normalColor = NormalTexture.Sample(SamplerType, input.TexCoord);
    return float4(diffuseColor.rgb, 1.0);
    //float4 color = float4((input.worldPosition.y)*0.1,(input.worldPosition.y)*0.1,(input.worldPosition.y)*0.1, 1.0);
    //return float4(texCUBE(SamplerType, normalize(input.TextureCoordinate)).rgb,1);


}

// Técnica
technique TerrenoTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}