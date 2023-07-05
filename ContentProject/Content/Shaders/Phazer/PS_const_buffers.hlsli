

#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Nml ) , normalize ( -input.Btgt ) );





struct point_light
{
    
    int is_active;
    float3 position;
		              
    float3 att;
    float range;
        		
    float4 color;

};
    
struct Light
{
    float3 lightDirection;

    float radiannce;

    float4 ambientLight;	
    float4 diffuseColor;
    float4 specularColor;
		
    float ambientFactor;
    float diffuseFactor;
    float specularFactor;
    float roughnessFactor;
   

    point_light oPointLight;
    
};

float getPointLightIntensity(in point_light oPL, in float3 world_pos)
{
    float3 lightToPixelVec = oPL.position - world_pos;
    
    float d = length(lightToPixelVec);
  
    
    // return falloff factor    
    return 10 / (d * d);
    
  //  return 1 / (oPL.att[0] + (oPL.att[1] * d) + (oPL.att[2] * (d * d)));
}


cbuffer LightBuffer //   : register (b0)
{
    float4x4 mRotEnv;
    Light light[2];
    
    int show_reflections;
    uint is_diffuse_linear;
    uint is_specular_linear;
    int has_alpha;
    
    float3 eyePosistion;
    float exposure;
    
    struct _debug_flags // variuous debug flags to switch stuff on and off for debugging
    {
        uint scale_by_one_over_pi;
        uint smoothness_channel; // 0 = Gloss.r, 1 = spec.a  2 = Gloss.R, 3 = gloss.G
        uint flagh3;
        uint flagh4;

    } debug_flags;
    
      
    float tone_mapping_brightness;
    float tone_mapping_burn;
    float pad1;
    float pad2;
	
    float4 color1;
    float4 color2;
	float4 color3;   
};



//struct PixelInputType
//{
//    float4 position : SV_POSITION;
//    float2 tex : TEXCOORD0;
//    float3 normal : NORMAL0;
//    float3 normal2 : NORMAL1;


//    float3 tangent : TANGENT;
//    float3 binormal : BINORMAL;

//    float3 viewDirection : TEXCOORD1;
//    float4 viewos : TEXCOORD2;
//    float4 norm : TEXCOORD3;
//    float3 eye : TEXCOORD4;
//    float3 worldPosition : TEXCOORD5;
    
//    float4 color1 : COLOR1;
    
    


    
    

//};



float3 normalSwizzle(in float3 ref)
{
    return float3(ref.x, ref.z, ref.y);
}


//float3 getTextureNormal(in Texture2D _texture, in sampler S, PixelInputType input)
//{

   
//    //PIXELDATA OUT;
//	// get normal and normal map.
//    const float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(-input.binormal));
       
//    float4 NormalTex = _texture.Sample(S, input.tex.xy).rgba;
    
//    float4 Np = 0;
//    Np.x = NormalTex.r * NormalTex.a;
//    Np.y = NormalTex.g;
//    Np = (Np * 2.0f) - 1.0f;
//    Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);
    
     
    
    
//    float3 N = normalSwizzle(Np.xyz);
//	//xform normal
//    float3 nN = normalize(mul(normalize(N), basis));
    
//    return nN;
	
    
//}
