
///////////COSAS BASICAS////////////
uniform float4x4 World;
uniform float4x4 View;
uniform float4x4 Projection;
uniform float4x4 LightViewProjection;//Esta es desde la pespectiva de la luz

uniform float3 ambientColor;
uniform float3 diffuseColor;
uniform float3 specularColor;
uniform float KLuzAmbiental;
uniform float KLuzDifusa;
uniform float KSpecular;
uniform float brillantes;//esto es particular de cada objeto que alla
uniform float2 screenDims;

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
uniform texture2D shadowMap;
   
sampler2D positionSampler = sampler_state   
{
    Texture = <position>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D normalSampler = sampler_state
{
    Texture = (normal);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D albedoSampler = sampler_state
{
    Texture = (albedo);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D especularSampler = sampler_state
{
    Texture = (especular);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D shadowMapSampler = sampler_state
{
    Texture = <shadowMap>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

/*
//NOTA: Todas las luces tienen el mismo color, y la misma intensidad
//A mono no le gustan las structs al parecer a si que toca tener 5 arrays
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
    float4 finalPos   : SV_Position;
    float4 normal     : TEXCOORD0;
    float2 textCoord  : TEXCOORD1;
    float4 position   : TEXCOORD2;
};

struct PSoutput
{
    float4 position  : SV_Target0;
    float4 normal    : SV_Target1;
    float4 albedo    : SV_Target2;
    float4 especular : SV_Target3;
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

struct Depth_VSinput
{
    float4 position : POSITION0;
};

struct Depth_VSoutput
{
    float4 position : SV_Position;
    float4 posEspacioPantalla  : TEXCOORD0;//la posicion en espacio projeccion
};

struct Depth_PSoutput
{
    float4 Profundidad : SV_Target0;
};

Depth_VSoutput DepthPass_VS(in Depth_VSinput input)
{
    Depth_VSoutput output = (Depth_VSoutput)0;
    float4 worldPos = mul(input.position, World);
    float4 viewPos = mul(worldPos, View);
    output.position = mul(worldPos, LightViewProjection);
    output.posEspacioPantalla = output.position;
    return output;
}

Depth_PSoutput DepthPass_PS(in Depth_VSoutput input)
{//escribimos la profundidad en el target
//NOTA esta es la profundidad con respecto de la camara normal
//no de la luz
    Depth_PSoutput output = (Depth_PSoutput)0;
    float Profundidad = input.posEspacioPantalla.z / input.posEspacioPantalla.w;
    output.Profundidad = float4(Profundidad, Profundidad, Profundidad, 1.0);
    return output;
}

VSoutput VS(in VSinput input)
{
    VSoutput output = (VSoutput)0;
    float4x4 worldView = mul(World, View);
    float4 worldPos = mul(input.localPos, World);
    float4 viewPos = mul(worldPos, View);
    float4 projPos = mul(viewPos, Projection);

    output.textCoord = input.textPos;
    output.finalPos = projPos;
    output.position = worldPos;//lo guardamos en mundo por que lo necesitamos luego para el light pass
    output.normal = normalize(float4(mul(input.normal.xyz, (float3x3)worldView), 0.0));

    return output;
}
//carga todo al buffer objetivo
PSoutput GBuffer_PS(in VSoutput input)
{
    PSoutput output = (PSoutput)0;
    output.position = input.position;
    output.normal = input.normal;//remapeamos por el tema de como guardan los colores
    output.albedo = tex2D(textureSampler, input.textCoord) * (1 - KLuzDifusa ) + float4(diffuseColor,0) * KLuzDifusa;
    output.albedo.w = 1;
    output.especular.r = tex2D(metallicSampler, input.textCoord).r  + brillantes;//todo lo que afecte el brillo de la cosa
    output.especular.w = 1;
    return output;
}

Light_VSoutput LightPass_VS(in Light_VSinput input)
{
    //como es un fullScreenCuad, una de sus coordenadas esta anulada todo el tiempo
    Light_VSoutput output = (Light_VSoutput)0;
    output.position  = input.posicion;
    output.textCoord = input.texcoord;
    return output;
}

float4 LightPass_PS(in Light_VSoutput input) : COLOR0 
{ 
    //recojemos los valores de el Buffer 
    float4 worlPos = tex2D(positionSampler, input.textCoord);
    float4 viewPos = mul(worlPos, View);//para pasarla a espacio de mundo 
    float4 normalV = tex2D(normalSampler, input.textCoord); 
    float4 albedoV = tex2D(albedoSampler, input.textCoord); 
    float  especularV = tex2D(especularSampler, input.textCoord).r;
    float  epsiloDinamico = 0;


    //la posicion en el espacio projectado de la luz
    float3 posicionLuz = mul(worlPos, LightViewProjection).xyz / mul(worlPos, LightViewProjection).w;
    //sacamos las coordenadas para leer y luego las normalizamos para no leer valores negativos
    float2 coordenadasShadowMap = posicionLuz.xy * 0.5 + 0.5;
    coordenadasShadowMap.y = 1 - coordenadasShadowMap.y;
    //lo ultimo es para arreglar el tema de que las coordenadas en espacio si tienen sentido
    float profundidadEnShadowMap = tex2D(shadowMapSampler, coordenadasShadowMap).r;

    ////////////////////////// 
    //Luz nuestraLuz; ////////////////Valores de coloracion 
    float4 direccionALuz; 
    float projeccion; 
    float projLuzEnNormal;
    float3 LuzAmbiental; 
    float3 LuzDifusa;
    float3 LuzEspeculativa;
    float4 direccionView;
    float4 reflexion;
    float3 currentLigthPos;
    float3 currentLigthDir;
    float3 finalColor = (float3)0; 
    float2 unitScreen = 1 / screenDims;//para poder iterar el shadowmap
    float  oscuridad;

    for ( int i=0; i<numero_luces; i++) 
    { 
        //para tenerlas mas a mano
        currentLigthPos = posicionesLuces[i]; 
        currentLigthPos = (float3)mul(float4(currentLigthPos, 0), View);
        currentLigthDir = direcciones[i]; 
        currentLigthDir = mul(currentLigthDir, (float3x3)View);//Nota: Los multiplicamos por 3x3 view para rotarlos nada mas no nos interesa re escalar un normal
        direccionALuz = float4(normalize(currentLigthPos - viewPos.xyz),0);
        direccionView = float4(normalize(-currentLigthPos), 0.0);// direccion a la camara ( recuerda estamos en espacio de view)

        projLuzEnNormal = dot(normalV, direccionALuz);
        LuzAmbiental = ambientColor * 0.6 + colores[i] * 0.4;//los que llega desde el LuzAmbientale ( es una simplificacion )
        LuzDifusa = projLuzEnNormal * (albedoV.xyz*0.9);//lus que es reflejada en la superficie del objeto, no necesariamente llega directo al ojo
        reflexion = 2.0 * normalV * projLuzEnNormal - direccionALuz;
        reflexion = normalize(reflexion);
        LuzEspeculativa = specularColor * pow(abs(dot(reflexion, direccionView)), especularV);//la luz LuzEspeculativa;que es reflejada directamente hasta la camara
        direccionALuz = normalize(float4(currentLigthPos - viewPos.xyz, 0.0)); //apunta desde este punto hasta la luz
        
        projeccion = dot(-direccionALuz.xyz, currentLigthDir);//lo ponemos en negativo, por que si no estaria apuntando desde el objeto hasta la luz
        //lo sumamos por que se supone debe dar mas vueltas que solo 1
        finalColor += LuzDifusa + KSpecular * saturate(LuzEspeculativa) + KLuzAmbiental * LuzAmbiental;
        //finalColor = float3(1,1,1);
        finalColor *= step(projeccionVorde[i], projeccion);//si es mayor que lo esperado, no lo afecta, caso contrario, lo vuelve 0
        //si algo no esta mirando a la luz es por que no deberia estar en sombra probablemente
        //los valores los saque de los samples de tgc para no andar tanteando a mano
        epsiloDinamico = max( 0.00004 * ( 1 - projLuzEnNormal ), 0.00003);
        
        oscuridad = 1.0;
        for (int x=-1; x<1; x++)
        {
            for ( int y=-1; y<1; y++)
            {
                profundidadEnShadowMap = tex2D(shadowMapSampler, coordenadasShadowMap + float2(x, y) * unitScreen).r + epsiloDinamico;
                oscuridad -= 1.0/9.0 * step(posicionLuz.z, profundidadEnShadowMap);
            }
        }

        finalColor *= 1 - oscuridad;
        //finalColor *= step(posicionLuz.z, profundidadEnShadowMap + epsiloDinamico);//si esta en sombra, solo le ponemos 0-
        
        //finalColor = float3((profundidadEnShadowMap) ,0,0);
    }
    //return normalV;
    return float4(finalColor,1.0);
    //return shadowDist;
}
        ///AJAJAJAJJAJAAJJ 
        /*
        if ( (projeccion* projeccion) / dot(direccionALuz, direccionALuz) > projeccionVorde[i] ) 
        { 
            //normalizamos los datos 
            float3 normal = normalize(normalV.xyz);
            float3 viewDir = normalize(-viewPos.xyz);
            // View direction in view space // calculamos la luz 
            float3 LuzAmbiental
     = LuzAmbiental
    Color * KLuzAmbiental
    ; 
            float3 LuzDifusa = max(dot(normal, direccionALuz.xyz), 0.0) * LuzDifusaColor * KLuzDifusa * 0.15 + colores[i] * 0.15 + albedoV.xyz * 0.7; 
            float3 specular LuzEspeculativa;= pow(max(dot(reflect(-direccionALuz.xyz, normal), viewLuzEspeculativa;Dir), 0.0), 0.3) *LuzEspeculativa; specularColor * KSpecular; 
            finalColor = LuzAmbiental
     + LuzDifusa + specular * 0.5; 
        } LuzEspeculativa;
    } 
        */


//Me di  cuenta que puedo calcular las sombras directamente en la pasado de geometria
//Esta cosa va a ser enorme
//shader diferido
technique DeferredShading
{
    pass pass0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 GBuffer_PS();
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
        PixelShader  = compile ps_3_0 LightPass_PS();
    }
}

//tenemos que hacer una pasada por shadows antes por que 
//esto no soporta mas de 4 render targets a la vez 
technique EffectsPass
{
    pass pass0
    {
        VertexShader = compile vs_3_0 DepthPass_VS();
        PixelShader  = compile ps_3_0 DepthPass_PS();
    }
}