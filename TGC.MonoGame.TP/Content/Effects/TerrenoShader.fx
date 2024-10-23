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

float3 lightPosition = normalize(float3(20000, 10000, 0)); // Posición de la luz
float3 lightDirection = normalize(float3(-1, 0 , 1)); //Dirección de la luz
float ambientLight = 3.5;
float lightIntensity = 0.5;

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
    float3 Normal : NORMAL0;        // Normal del vértice
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : SV_Position;    // Posición final que necesita el rasterizador
    float2 TexCoord : TEXCOORD0;      // Coordenadas de textura
    float4 worldPosition : TEXCOORD1;    // POS EN LA MATRIZ DE MUNDO
    float3 WorldNormal : TEXCOORD2;     // Normal en el espacio del mundo
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


    output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));
    output.worldPosition = input.Position;

    output.TexCoord = input.Position.xz;

    return output;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float3 lightDirection = normalize(lightPosition - input.worldPosition.xyz);
    float3 viewDirection = normalize(CameraPosition - input.worldPosition.xyz);
    float3 halfVector = normalize(lightDirection + viewDirection);

    float3 Normal = normalize(input.WorldNormal);
    
    //float NdotL = saturate(0.4 + 0.7 * saturate(dot(Normal, lightDirection)));
    //float3 L = normalize(lightPosition - input.worldPosition.xyz); // Vector de luz
    float NdotH = saturate(0.4 + 0.7 * saturate(dot(Normal, halfVector)));
    float kd = saturate(0.4 + 0.7 * saturate(dot(Normal, lightDirection)));

    float4 diffuseColor = Diffuse.Sample(SamplerType, input.TexCoord*0.001);
    float4 normalColor = NormalTexture.Sample(SamplerType, input.TexCoord*0.001);

    float4 finalColor = (diffuseColor * 0.92 + normalColor * 0.08) * kd * 1.5;

    return float4(finalColor.rgb,  1.0);
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