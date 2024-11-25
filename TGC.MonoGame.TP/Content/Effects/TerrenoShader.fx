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

float3 ambientColor; // color del ambiente
float3 diffuseColor; // color difuso de la luz
float3 specularColor; // color especulativo de la luz
float KAmbient; 
float KDiffuse; 
float KSpecular;
float shininess; 
float3 lightPosition;

float4x4 LightViewProjection;
float2 shadowMapSize;

static const float modulatedEpsilon = 0.00001;
static const float maxEpsilon = 0.000005;

float4x4 InverseTransposeWorld;

float3 CameraPosition;

texture2D baseTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (baseTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture shadowMap;//dpnde seran cargadas nuestras sombras
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



// Entrada del vértice
struct VertexShaderInput
{
	float4 Position : POSITION0;//posicion de vertice
    float4 Normal : NORMAL;//normal de vertice
    float2 TextureCoordinates : TEXCOORD0;//coordenadas de textura
};

// Salida del vértice
struct VertexShaderOutput
{
    float4 Position : SV_Position;//posicion del vertice transformada ( la que se usara para el raster)
    float2 TextureCoordinates : TEXCOORD0;//coordenadas de textura
    float4 localPosition : TEXCOORD1;    // POS EN LA MATRIZ DE MUNDO (¿? Sera la de abajo o se dio un problema aqui)
    float4 Normal : TEXCOORD2;//normal del vector
    float4 WorldPosition : TEXCOORD3;
    float4 LightSpacePosition : TEXCOORD4;//possicion de la luz
    float4 ligthDirection : TEXCOORD5;
    float4 cameraDirection : TEXCOORD6;
    float4 ligthCameraMedium : TEXCOORD7;
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
    //cargamos el como se ve un objeto en pantalla
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
    //devolvemos a coordenadas normales todo
    //esto antes estaba como 
    float depth = input.ScreenSpacePosition.z / input.ScreenSpacePosition.w;
    //float3 depth = input.ScreenSpacePosition.xyz / input.ScreenSpacePosition.w;
    return float4(depth, depth, depth, 1.0);
    //return float4(depth, 1.0);
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    // Transformar la posición del vértice a espacio del mundo y luego a espacio de clip
    //sacamos como se veria nuestra imagen en todos las vistas y lo guardamos
    float4 worldPosition = mul(input.Position, World);
    // World space to View space
    float4 viewPosition = mul(worldPosition, View);	
	// View space to Projection space
    output.Position = mul(viewPosition, Projection);

    output.localPosition = input.Position;
    output.WorldPosition = worldPosition;

    //sacamos como se veria nuestro objeto desde la perspectiva de la luz
    output.LightSpacePosition = mul(output.WorldPosition, LightViewProjection);
    output.Normal = input.Normal;
    output.TextureCoordinates = input.TextureCoordinates;
	
    output.ligthDirection = normalize(float4(lightPosition, 0.0) - output.WorldPosition);
    output.cameraDirection = normalize(float4(CameraPosition, 0.0) - output.WorldPosition);
    output.ligthCameraMedium = normalize(output.ligthDirection + output.cameraDirection);

    return output;
}

float4 PS(VertexShaderOutput input) : COLOR
{   
    //la posicion de la luz convertida a coordenadas normales
    float3 lightSpacePosition = input.LightSpacePosition.xyz / input.LightSpacePosition.w;
    //coordenadas remapeadas para tener el centro en 0.5, 0.5
    float2 shadowMapTextureCoordinates = 0.5 * lightSpacePosition.xy + float2(0.5, 0.5);
    //invertimos las coordenadas en la region y
    shadowMapTextureCoordinates.y = 1.0f - shadowMapTextureCoordinates.y;

    //float3 lightDirection = normalize(lightPosition - input.WorldPosition.xyz);
    //float3 viewDirection = normalize(CameraPosition - input.WorldPosition.xyz);
    //float3 halfVector = normalize(lightDirection + viewDirection);

    //normales
    float3 normal = normalize(input.Normal.rgb);

    //sesgo de inclinacion

    //float inclinationBias = max(dynamicEpsilon * (1.0 - dot(normal, input.ligthDirection.xyz)), maxEpsilon);




    //prfundidad del shadowMap
    float shadowMapDepth = tex2D(shadowMapSampler, shadowMapTextureCoordinates).x + 0.0005;
	
    // Get the texture texel
    float4 texelColor = tex2D(textureSampler, input.TextureCoordinates * 0.001);
    //ilumina nuestro texcel
    texelColor.rgb = lerp(texelColor.rgb, float3(1, 1, 1), 0.1);

    // Calculate the diffuse light with a minimum light
    //
    float NdotL = saturate(dot(input.Normal.xyz, input.ligthDirection.xyz));
    float3 minLight = float3(0.05, 0.05, 0.05); // Luz mínima para zonas oscuras
    float3 diffuseLight = KDiffuse * diffuseColor * NdotL + minLight;

    // Compare the shadowmap with the REAL depth of this fragment
	// in light space
    //antes era 0.0 
    float notInShadow = 1.0;
    //
    float2 texelSize = 1.0 / shadowMapSize;
    const float epsilon = 0.09;
    for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            float pcfDepth = tex2D(shadowMapSampler, shadowMapTextureCoordinates + float2(x, y) * texelSize).r + 0.055555;

            notInShadow -= step(lightSpacePosition.z, pcfDepth + epsilon) / 9.0;
            //Preguntar a nacho despues

       
            notInShadow += step(lightSpacePosition.z, pcfDepth) / 9.0;

        }

    // Calculate the specular light
    float NdotH = dot(input.Normal.xyz, input.ligthCameraMedium.xyz);
    float3 specularLight = sign(NdotL) * KSpecular * specularColor * pow(saturate(NdotH), shininess);

    // Final calculation
    float4 finalColor = float4(saturate(ambientColor * KAmbient + diffuseLight) * texelColor.rgb + specularLight, texelColor.a);
    finalColor.rgb *= 0.5 + 0.5 * notInShadow;
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

technique DepthPass{
    pass Pass0
    {
        VertexShader = compile vs_3_0 DepthPassVS();
        PixelShader = compile ps_3_0 DepthPassPS();
    }
}