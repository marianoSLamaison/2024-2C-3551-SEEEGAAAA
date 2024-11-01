#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Custom Effects - https://docs.monogame.net/articles/content/custom_effects.html
// High-level shader language (HLSL) - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl
// Programming guide for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-pguide
// Reference for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reference
// HLSL Semantics - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 DiffuseColor;

float3 ambientColor; // Light's Ambient Color
float3 diffuseColor; // Light's Diffuse Color
float3 specularColor; // Light's Specular Color
float KAmbient; 
float KDiffuse; 
float KSpecular;
float shininess; 
float3 lightPosition;

float4x4 InverseTransposeWorld;

float3 CameraPosition;

float Time = 0;

// Entrada del vértice
struct VertexShaderInput
{
	float4 Position : POSITION0;
    float4 Normal : NORMAL;
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : SV_Position;    // Posición final que necesita el rasterizador
    float4 localPosition : TEXCOORD1;    // POS EN LA MATRIZ DE MUNDO
    float4 WorldPosition : TEXCOORD3;
    float4 Normal : TEXCOORD2; 
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


    //output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));
    output.localPosition = input.Position;
    output.WorldPosition = worldPosition;
    output.Normal = normalize(input.Normal);
	
    return output;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float3 lightDirection = normalize(lightPosition - input.WorldPosition.xyz);
    float3 viewDirection = normalize(CameraPosition - input.WorldPosition.xyz);
    float3 halfVector = normalize(lightDirection + viewDirection);

    // Get the texture texel
    // Calculate the diffuse light with a minimum light
    float NdotL = saturate(dot(input.Normal.xyz, lightDirection));
    float3 minLight = float3(0.05, 0.05, 0.05); // Luz mínima para zonas oscuras
    float3 diffuseLight = KDiffuse * diffuseColor * NdotL + minLight;

    // Calculate the specular light
    float NdotH = dot(input.Normal.xyz, halfVector);
    float3 specularLight = sign(NdotL) * KSpecular * specularColor * pow(saturate(NdotH), shininess);

    // Final calculation
    float4 finalColor = float4(saturate(ambientColor * KAmbient + diffuseLight) * DiffuseColor.rgb + specularLight, 1);
	//float4 finalColor = input.Normal;

    return finalColor;
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
