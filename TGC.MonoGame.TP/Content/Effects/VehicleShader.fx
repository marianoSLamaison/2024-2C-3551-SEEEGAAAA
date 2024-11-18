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

sampler2D shadowMapSampler = sampler_state
{
	Texture = <shadowMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
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
    float2 TextCoords : TEXCOORD0;
    float4 WorldPosition : TEXCOORD1;
    float4 Normal : TEXCOORD2;
    float4 LigthViewPosition : TEXCOORD3;
    float4 DirectionToLigth : TEXCOORD4;
    float4 DirectionToCamera : TEXCOORD5;
};
//no me gustan los nombres, pero tendra que servir por ahora
struct DepthPassVertexShaderInput
{
	float4 Position : POSITION0;
};

struct DepthPassVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Projection : TEXCOORD1;
};

DepthPassVertexShaderOutput DepthPassVS(in DepthPassVertexShaderInput input)
{
	DepthPassVertexShaderOutput output;
	float4 worldPosition = mul(input.Position, World);
    // World space to View space
    float4 viewPosition = mul(worldPosition, View);	
	// View space to Projection space
    output.Position = mul(viewPosition, Projection);

	output.Projection = mul(worldPosition, LightViewProjection);
    
	return output;
}
//para calcular la posicion de todos los pixeles
float4 DepthPassPS(in DepthPassVertexShaderOutput input) : COLOR
{
    //la profundida se guarda en el valor z de la coordenada
    float3 depth = (float3)(input.Projection.z / input.Projection.w);
    return float4(depth, 1.0);
}

//Se encarga de rotar un vector normal de manera que mire a donde deberia 
//(mantenga su magnitud y su misma direccion relativa con el modelo)
float4 LocaliceNormal(in float4 vNormal ) : TEXCOORD0
{
    float4 ret = (float4)0;
    //el vector normal deberia quedar con igual modulo, y una direccion equivalente
    float3x3 RotationMatrix = float3x3(
                                        World[0][0], World[0][1], World[0][2],
                                        World[1][0], World[1][1], World[1][2],
                                        World[2][0], World[2][1], World[2][2] 
                                        );
    ret.xyz = mul(vNormal.xyz, RotationMatrix);
    ret = normalize(ret);
    return ret;
}

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 worldP = mul(input.Position, World);
    float4 viewP = mul(worldP, View);
    float4 projP = mul(viewP, Projection); 
    
    output.Position = projP;
    output.Normal = LocaliceNormal(input.Normal);
    output.WorldPosition = worldP;
    output.TextCoords = input.TextureCoordinates;
    output.DirectionToLigth = float4(normalize(lightPosition - worldP.xyz), 0.0);
    output.DirectionToCamera = float4(normalize(CameraPosition - worldP.xyz), 0.0);
    //projectamos la posicion de el auto en la luz
    output.LigthViewPosition = mul(worldP, LightViewProjection);
    
    return output;
}
/*
El procedimiento consiste en primero bajarle el valor de brillo ( de saturarlo ) al auto si esta muy iluminado
para que no se vea totalmente blanco, y luego acercarlo al cero en base a que tan cerca esta del 
(0.5, 0.5, 0.5) dado que hay es donde puede darse por este metodo que el auto casi no cambie
a pesar de deber cambiar de color
*/
float3 DeWitifier(float3 color) : COLOR0
{
    /*No preguntes. Estaba muy confundido
    static const float sq3 = 1.732;
    //vector unitario que apunta al blanco
    static const float3 dirToWhite = float3(0.577350269,0.577350269,0.577350269);
    float minCoord = min( min(color.r,color.b), color.g);
    //punto donde toca un plano y sigue estando en los valores positivos
    float3 ground = float3(1.0,1.0,1.0) * -minCoord + color;
    float ParecidoConBlanco = 1.0 - dot(dirToWhite, color) / sq3;
    float3 newSaturatedColor = lerp(ground, color, ParecidoConBlanco);
    //terminar esto

    //color = lerp(dot(dirToWhite, newSaturatedColor) * dirToWhite, newSaturatedColor,  dot(dirToWhite, newSaturatedColor));
    //Resultado : 
        //El color funciona, pero lo puntos superiores, siguen viendose blancos dado su sercania original
        //se agregara un factor para "Arquear" la curba a medida se acerca al centro
    color = newSaturatedColor;
    float3 ortogonalIncrement = newSaturatedColor - dot(dirToWhite, newSaturatedColor) * dirToWhite;
    float distToMidle = abs(0.5 - dot(dirToWhite, newSaturatedColor));
    color += ortogonalIncrement * (2.0 - 8.0 *distToMidle * distToMidle); 
    color = saturate(color);
    //resultado :
        //Color demaciado grisaceo los colores son todos aproximados al blanco cuando se encojen
        //independientemente de si es necesario o no
    //color *= ParecidoConBlanco;
    //Nota los colores deben de converger a 0.0 suavemente
    */

    const float sq3 = 1.732;
    const float3 dirToWhite = float3(0.577350269,0.577350269,0.577350269);
    //usamos margenes, por que los reflejos son casi puro blanco, pero queremos dejarlos
    const float maxMargenReduccion = 0.85;
    const float minMargenReduccion = 0.68;

    float3 ret = color;
    float ParecidoConBlanco = abs(dot(dirToWhite, color) / sq3);
    ret *= 1 - 0.35 * step( minMargenReduccion, ParecidoConBlanco) * step(ParecidoConBlanco, maxMargenReduccion);
    //1 dia entero con lo anterior para salir con esta cosa y que funcione mejor :D
    return ret;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    ////////////////////////////////////////////
    //obtencion de otros valores
    float4 textColor = tex2D(textureSampler, input.TextCoords);
    float metalicidad = tex2D(metallicSampler, input.TextCoords).r;
    float AO = tex2D(AOSampler, input.TextCoords).r;
    ///////////////////////////////////////////
    float4 finalColor = (float4)0;
    float projectionLigthOnNormal = dot(input.Normal, input.DirectionToLigth);
    //float4 viewVector = normalize(float4(CameraPosition,0) - input.WorldPosition);
    float4 vectorRefleccion = normalize(2 * input.Normal * projectionLigthOnNormal - input.DirectionToLigth);

    //separo estos 3 por que si no el calculo queda demaciado grande
    float3 ambient = KAmbient * ambientColor *  AO;
    float3 diffuse = KDiffuse * diffuseColor * projectionLigthOnNormal * 0.15 + textColor.xyz * 0.85;
    float3 specular = KSpecular * specularColor * metalicidad * dot(vectorRefleccion, input.DirectionToCamera);
    float3 colorBase = 
                        ambient + 
                        diffuse + 
                        specular;
    //se se para en alguna partes, eso lo hace verse gris a si que le bajamos un poco
    //la blacura para arreglarlo
    colorBase = DeWitifier(colorBase);
    
    //////////////////////////////////////////////
    //Seccion del calculo de sombras
    //sacamos la data del shadowMap
    //por formato la ligthView guarda la distancia actual en el ultimo valor
    //y el shadowmap guarda en su primer componente
    //float datosShadowMap = tex2D(shadowMapSampler, input.LigthViewPosition.xy).r;
    //si el objeto no esta en la sombra, se le da un valor de 1 que no altera su color, caso contrario si si lo esta
    //esta en la luz hasta que se demuestre lo contrario
    float datosShadowMap;
    float valorLuminico = 1.0;
    const float2 texelSize = 1.0 / shadowMapSize;
    //repasamos si los texceles aledaños estan tambien en sombra o no, para poder dar un mejor sombreado
    for ( float x = -1.0; x<1.0; x++)
        for ( float y = -1.0; y<1.0; y++)
        {
            datosShadowMap = tex2D(shadowMapSampler, input.LigthViewPosition.xy + float2(x, y) * texelSize);
            valorLuminico -= (1.0/9.0) * step(datosShadowMap.r, input.LigthViewPosition.z);
        }
    //colorBase *= 1.0 - (int)(input.LigthViewPosition.z < datosShadowMap) * 0.5; 
    colorBase *= valorLuminico;
    finalColor = float4(colorBase, 1.0);
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
