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

float3 ambientColor = float3(0.0, 0.0, 1.0); // Light's Ambient Color
float3 diffuseColor = float3(0.1, 0.1, 0.6); // Light's Diffuse Color
float3 specularColor = float3(0.0,0.0,0.0); // Light's Specular Color

float KAmbient = 0.1f;
float KDiffuse = 0.7f;
float KSpecular = 0.4f;
float shininess = 4.0f;

float3 lightPosition; // Posición de la luz
float3 lightDirection; //Dirección de la luz
float ambientLight = 1.2;
float lightIntensity = 1.2;
float lightRadius = 1000.0f;    // Ajusta el radio de la luz
float lightFalloff = 0.01f;     // Controla la rapidez de la atenuación
float3 CameraPosition;

float3 lightBoxColor = float3(1.0, 1.0, 1.0);
float3 conoColor = float3(1.0, 1.0, 0.0);

// Texturas
Texture2D Diffuse;

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
    float4 ambientColor = float4(ambientLight, ambientLight, ambientLight, 1.0);
    float4 diffuseColor = Diffuse.Sample(SamplerType, input.TexCoord*0.001);
    float4 normalColor = NormalTexture.Sample(SamplerType, input.TexCoord*0.001);

    float distance = -length(lightPosition - input.worldPosition.xyz);   //Distancia hacia la luz.
    float attenuation = saturate(1.0 - (lightRadius/distance));          //Atenuación según distancia
    
    float3 lightDirection = normalize(lightPosition - input.worldPosition.xyz);
    float3 viewDirection = normalize(CameraPosition - input.worldPosition.xyz);
    float3 halfVector = normalize(lightDirection + viewDirection);
    float3 normal = normalize(input.WorldNormal);

    float NdotL = saturate(0.4 + 0.7 * saturate(dot(normal, lightDirection)));
    float3 diffuseLight = KDiffuse * diffuseColor.rgb * NdotL;
    
    float NdotH = saturate(0.4 + 0.7 * saturate(dot(normal, halfVector)));
    //float3 specularLight = KSpecular * specularColor * pow(saturate(NdotH), shininess);

    float kd = saturate(0.4 + 0.7 * saturate(dot(normal, lightDirection)));
    
    float4 texelColor = (diffuseColor * 0.92 + normalColor * 0.08);

    float4 finalColor = (diffuseColor * 0.92 + normalColor * 0.08) * kd * 1.5 * attenuation;
    return float4(finalColor.rgb,  1.0);
    //float4 finalColor = float4(saturate(ambientColor * KAmbient + diffuseLight) * texelColor.rgb /*+ specularLight*/, texelColor.a);
    //return finalColor;

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