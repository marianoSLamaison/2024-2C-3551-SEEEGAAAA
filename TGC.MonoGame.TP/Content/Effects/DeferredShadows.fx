
///////////COSAS BASICAS////////////
uniform float4x4 World;
uniform float4x4 View;
uniform float4x4 Projection;

uniform float3 ambientColor;
uniform float3 diffuseColor;
uniform float3 specularColor;
uniform float KAmbient;
uniform float KDiffuse;
uniform float KSpecular;

///////////TEXTURAS BASICAS////////
uniform texture2D baseTexture;
uniform texture2D metallicTexture;  // Textura metálica
uniform texture2D AOTexture;
uniform texture2D roughnessTexture;


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



///////////RENDER TARGETS//////////
uniform texture2D position;
uniform texture2D normal;
uniform texture2D albedo;
uniform texture2D especular;
   
  sampler2D positionSampler : register(s0)   
{
    Texture = <position>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D normalSampler : register(s1)
{
    Texture = (normal);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D albedoSampler : register(s2)
{
    Texture = (albedo);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D especularSampler :register(s3)
{
    Texture = (especular);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

/*
//NOTA: Todas las luces tienen el mismo color, y la misma intensidad
struct Luz 
{//NOTA: se presume la direccion esta normalizada
    float3 posicion;
    float3 direccion;//direccion en la que mira la luz
    float  projeccionVorde; //Porcentaje de projeccion de un vector que se encuentra en el borde del cono
    float  intensidad;//que tan fuerte queremos que sea
    float3 color;//color de la luz
};
*/


#define MAX_LIGHTS 40
uniform float3 posicionesLuces[MAX_LIGHTS];
uniform float3 direcciones[MAX_LIGHTS];
uniform float  projeccionVorde[MAX_LIGHTS];
uniform float3 colores[MAX_LIGHTS];
uniform int numero_luces;//cuantas luces hay ( siempre hay al menos 1 )

struct VSinput
{
    float4 localPos : POSITION0;
    float4 normal : NORMAL0;
    float2 textPos : TEXCOORD0;
};

struct VSoutput
{
    float4 finalPos : SV_Position;
    float4 normal : TEXCOORD0;
    float2 textCoord : TEXCOORD1;
    float4 viewPos : TEXCOORD2;
};

struct Light_VSinput
{
    //como va a dibujarse todo en base a un fullscreen cuad, no hay necesidad de sacar toda la informacion
    float4 posicion : POSITION0;
    float2 texcoord : TEXCOORD0;
};

struct Light_VSoutput
{
    float4 position  : SV_Position;
    float2 textCoord : TEXCOORD0;
};

struct PSoutput
{
    float4 position : SV_Target0;
    float4 normal : SV_Target1;
    float4 albedo : SV_Target2;
    float4 especular : SV_Target3;
};


const float maxDist = 1000.0, minDist = -1000.0;


VSoutput VS(in VSinput input)
{
    VSoutput output = (VSoutput)0;
    float4x4 worldView = mul(World, View);
    float4 worldPos = mul(input.localPos, World);
    float4 viewPos = mul(worldPos, View);
    float4 projPos = mul(viewPos, Projection);
    output.textCoord = input.textPos;
    output.finalPos = projPos;
    output.viewPos = viewPos;
    output.normal = normalize(float4(mul(input.normal.xyz, (float3x3)worldView), 0.0));
    return output;
}
//carga todo al buffer objetivo
PSoutput GBuffer_PS(in VSoutput input)
{
    
    PSoutput output = (PSoutput)0;
    output.position = input.viewPos;
    output.normal = input.normal * 0.5 + 0.5;//remapeamos por el tema de como guardan los colores
    output.albedo = tex2D(textureSampler, input.textCoord);
    output.especular = float4(tex2D(metallicSampler, input.textCoord).r * specularColor * KSpecular, 1.0);
    return output;
}

Light_VSoutput LightPass_VS(in Light_VSinput input)
{
    //como es un fullScreenCuad, una de sus coordenadas esta anulada todo el tiempo
    Light_VSoutput output = (Light_VSoutput)0;
    output.position = input.posicion;
    output.textCoord = input.texcoord;
    return output;
}

float4 LightPass_PS(in Light_VSoutput input) : COLOR0
{
    //recojemos los valores de el Buffer
    float4 viewPos    = tex2D(positionSampler, input.textCoord);
    float4 normalV    = tex2D(normalSampler, input.textCoord);
    normalV           = (normalV - 0.5) * 2.0;
    float4 albedoV    = tex2D(albedoSampler, input.textCoord);
    float4 especularV = tex2D(especularSampler, input.textCoord);

    //////////////////////////
    float4 direccionALuz;
    float projeccion;
    float3 finalColor = (float3)0;
    //Luz nuestraLuz;

    ////////////////Valores de coloracion
    float4 ambient;
    float4 diffuse;
    float4 especulativa;
    float  AO;
    float  projectionLigthOnNormal;
    float4 vectorRefleccion;
    float3 currentLigthPos;
    float3 currentLigthDir;
    for ( int i=0; i<numero_luces; i++)
    {
        //sacamos la direccion acia el punto en cuestion
        //nuestraLuz = luces[i];
        currentLigthPos = posicionesLuces[i];
        currentLigthPos = mul(float4(currentLigthPos, 0), View);//para trasladarla
        currentLigthDir = direcciones[i];
        currentLigthDir = mul(currentLigthDir, (float3x3)View);//para rotarlo a donde deba
        direccionALuz = float4(posicionesLuces[i] - viewPos.xyz, 0.0);
        projeccion = dot(direccionALuz.xyz, direcciones[i]);
        ///AJAJAJAJJAJAAJJ 
        if ( (projeccion* projeccion) / dot(direccionALuz, direccionALuz) > projeccionVorde[i] )
        {
            //normalizamos los datos
            float3 normal = normalize(normalV.xyz);
            float3 lightDir = normalize(currentLigthPos - viewPos.xyz);
            float3 viewDir = normalize(-viewPos.xyz);  // View direction in view space

            // calculamos la luz
            float3 ambient = ambientColor * KAmbient;
            float3 diffuse = max(dot(normal, lightDir), 0.0) * diffuseColor * KDiffuse * 0.15 + colores[i] * 0.15 + albedoV.xyz * 0.7;
            float3 specular = pow(max(dot(reflect(-lightDir, normal), viewDir), 0.0), 0.3) * specularColor * KSpecular;

            finalColor = ambient + diffuse + specular * 0.5;

        }

    }

    return    float4(finalColor, 1.0);
}
//shader diferido
technique DeferredShading
{
    pass pass0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 GBuffer_PS();
    }
    /*
    pass pass1
    {
        VertexShader = compile vs_3_0 LightPass_VS();//este renderiza a un fullScreenCuad
        PixelShader = compile ps_3_0 LightPass_PS();
    }
    */
}
//Monogame decidio que su Draw dibujara CADA pass de mi tecnica a si que tengo que hacer esto
technique Lighting
{
    pass pass0
    {
        VertexShader = compile vs_3_0 LightPass_VS();//este renderiza a un fullScreenCuad
        PixelShader = compile ps_3_0 LightPass_PS();
    }
}

technique Bloom
{//solo un pass por que es nada mas para agregar el bloom
    pass pass0
    {

    }
}