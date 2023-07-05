
#include "vs_const_buffers.hlsli"
#include "ps_vs_structs.hlsli"
#include "common_functions.hlsli"


PixelInputType main(in VertexInputType input) // main is the default function name
{
    PixelInputType output;
    float3 worldPosition;

	// Change the position vector to be 4 units for proper matrix calculations.
	//float4x4 m = mWorld;

	//m[3][3] = 0.5;
	// Calculate the position of the vertex against the world, view, and projection matrices.
    //float4 pos = input.position;
	
		
    //float3x3 _view1 = (float3x3) View;
    //float4x4 _view2 = float4x4(View);
	
    //pos.x -= translate.x*4;
    //pos.y += translate.y*4;
    
    
    //input.position.w = 1.0f;
    
    float4 _V1 = input.position;
    
    float4 _N1 = float4(input.normal, 0);        
    float4 _T1 = float4(input.tangent, 0);    
    float4 _B1 = float4(input.binormal, 0);

    
    
    float4x4 W = mWorld;
   
    ////float4 _V = pos;
    
    float4x4 PM[4];
    PM[0] = mul(inverse[input.BoneIndices.x], tranforms[input.BoneIndices.x]);
    PM[1] = mul(inverse[input.BoneIndices.y], tranforms[input.BoneIndices.y]);
    PM[2] = mul(inverse[input.BoneIndices.z], tranforms[input.BoneIndices.z]);
    PM[3] = mul(inverse[input.BoneIndices.w], tranforms[input.BoneIndices.w]);

    
    
    
    uint IDs[4];
    
    IDs[0] = input.BoneIndices.x;
    IDs[1] = input.BoneIndices.y;
    IDs[2] = input.BoneIndices.z;
    IDs[3] = input.BoneIndices.w;
     
    
    float4 _V2 = 0;
    float4 _N2 = 0;
    float4 _N2_rot = 0;
    float4 _T2 = 0;
    float4 _B2 = 0;   
    
    
    output.rig_colors[0] = 0;
    output.rig_colors[1] = 0;
    output.rig_colors[2] = 0;
    output.rig_colors[3] = 0;
       
        
    
            if (static_model != 1 || is_weapon == 1)
            {
      
                for (int i = 0; i < 4; i++)
                {
            
               //     if (false)
               //     if (is_weapon == 1)
               //     {
               //// _N1.x *= -1;
               // //_N1.y *= -1;
                
               // //swap(_N1.x, _N1.y);
               // //_T1.x *= -1;
               // //_T1.y *= -1;
                
               // //_B1.x *= -1;
               // //_B1.y *= -1;
                
               // //if (false)
               //     //if (i == 0)     4           //(IDs[i] != IDs[0]) || i == 0)
               //     {
                        
                        
                        
               //             _V2 += input.Weights[i] * mul(_V1, mul(mWeapon, PM[i]));
                    
                    
               //     //        _N2_rot.xyz += input.Weights[i] * mul(_N1_rot.xyz, (float3x3) mul(mWeapon, PM[i])); // only use rotation part (3x3) of matrices
               //             _N2.xyz += input.Weights[i] * mul(_N1.xyz, (float3x3) mul(mWeapon, PM[i])); // only use rotation part (3x3) of matrices
               //             _T2.xyz += input.Weights[i] * mul(_T1.xyz, (float3x3) mul(mWeapon, PM[i]));
               //             _B2.xyz += input.Weights[i] * mul(_B1.xyz, (float3x3) mul(mWeapon, PM[i]));
                    
               //             output.rig_colors[i][i] += input.Weights[i];
               //             output.rig_colors[i][i] = saturate(output.rig_colors[i][i]);
                    
               //     //_V2 = _V1;
               //     //_N2 = _N1;
               //     //_T2 = _T1;
               //     //_B2 = _B1;
               //         }
               //     }
               //     else
                    {
            
                        if ((IDs[i] != IDs[0]) || i == 0)
                        {
                            _V2 += input.Weights[i] * mul(_V1, PM[i]);
                            //_N2_rot.xyz += input.Weights[i] * mul(_N1_rot.xyz, (float3x3) PM[i]); // only use rotation part (3x3) of matrices
                            _N2.xyz += input.Weights[i] * mul(_N1.xyz, (float3x3) PM[i]); // only use rotation part (3x3) of matrices
                            _T2.xyz += input.Weights[i] * mul(_T1.xyz, (float3x3) PM[i]);
                            _B2.xyz += input.Weights[i] * mul(_B1.xyz, (float3x3) PM[i]);
                
                    
                            
                            output.rig_colors[i].r += input.Weights[i];
                            output.rig_colors[i].r = saturate(output.rig_colors[i].r);
                    
                        }
                    }
                }
        
                output.position = _V2;
                output.normal = _N2.xyz;
                output.tangent = _T2.xyz;
                output.binormal = _B2.xyz;
                output.normal2 = _N2_rot.xyz;
    
            }
            else
            {
        
                output.position = input.position;
                output.normal = input.normal;
                output.tangent = input.tangent;
                output.binormal = input.binormal;
                output.normal2 = input.normal;
            }
    
               
            output.position = mul(output.position, rot_y);
            output.position = mul(output.position, rot_x);
            output.position = mul(output.position, W);
    
            _V2 = output.position;
            worldPosition = output.position.xyz;
        
                
            output.normal2 = float4(mul(input.normal, (float3x3) mRotEnv), 0);
    
            output.position = mul(output.position, View);
            output.position = mul(output.position, Projection);
        //output.position = mul(input.position, rot_y);
        //output.position = mul(output.position, rot_x);
        //output.position = mul(output.position, W);
    
        //_V2 = output.position;
        //worldPosition = output.position;
        
        //output.position = mul(output.position, View);
        //output.position = mul(output.position, Projection);
        
        
        //output.normal = input.normal;
        //output.tangent = input.tangent;
        //output.binormal = input.binormal;

        
        
    
	
            output.color1 = 1;

	// Store the texture coordinates for the pixel shader.
            output.tex.x = input.tex.x;
            output.tex.y = 1.0f - input.tex.y;

    //output.tex.xy = input.tex.xy;
    //output.tex.zw = input.TexCoord1.xy;
	
    
    
            output.norm = float4(output.normal, 0);
     	
            output.viewPos = mul(output.position, W) - float4(cameraPosition, 1);
    
	//output.viewPos = mul(output.viewPos, W);
    
	
    //output.TexCoord.w += 1;
	
    
	// Calculate the normal vector against the world matrix only.
    //output.normal = (float3) mul(float4(_N2), W);

    
    
    //-----------------------------------------------------------------------------
    //          Normal
    //-----------------------------------------------------------------------------                
    //if (is_weapon)
    //{
    //    output.normal = mul(output.normal, (float3x3) mWeapon);
    //    output.normal = normalize(output.normal);
    //}    
    
            float3x3 m = (float3x3) mul((float3x3) rot_y, (float3x3) rot_x);
    
            m = mul(m, (float3x3) W);
    
            output.normal = mul(output.normal, (float3x3) rot_y);
            output.normal = normalize(output.normal);
    
            output.normal = mul(output.normal, (float3x3) rot_x);
            output.normal = normalize(output.normal);

            output.normal = mul(output.normal, (float3x3) W);
            output.normal = normalize(output.normal);
	
    
            output.normal2 = mul(output.normal2, (float3x3) rot_y);
            output.normal2 = normalize(output.normal2);
    
            output.normal2 = mul(output.normal2, (float3x3) rot_x);
            output.normal2 = normalize(output.normal2);

            output.normal2 = mul(output.normal2, (float3x3) W);
            output.normal2 = normalize(output.normal2);
	
    
    //-----------------------------------------------------------------------------
    //          Tangent
    //-----------------------------------------------------------------------------        
    //if (is_weapon)
    //{
    //    output.tangent = mul(output.tangent, (float3x3) mWeapon);
    //    output.tangent = normalize(output.tangent);
    //}
    
            output.tangent = mul(output.tangent, (float3x3) rot_y);
            output.tangent = normalize(output.tangent);
    
            output.tangent = mul(output.tangent, (float3x3) rot_x);
            output.tangent = normalize(output.tangent);
    
            output.tangent = mul(output.tangent, (float3x3) W);
            output.tangent = normalize(output.tangent);
                                                
    
  
    //-----------------------------------------------------------------------------
    //          Binormal
    //-----------------------------------------------------------------------------        
    
    //if (is_weapon)
    //{
    //    output.binormal = mul(output.binormal, (float3x3) mWeapon);
    //    output.binormal = normalize(output.binormal);
    //}
    
    
            output.binormal = mul(output.binormal, (float3x3) rot_y);
            output.binormal = normalize(output.binormal);
    
            output.binormal = mul(output.binormal, (float3x3) rot_x);
            output.binormal = normalize(output.binormal);
    
            output.binormal = mul(output.binormal, (float3x3) W);
            output.binormal = normalize(output.binormal);
                                              
    //output.color = color;
    
    // Calculate the position of the vertex in the world.
    //worldPosition = mul(_V2, W).xyz;
            float3 camW = mul(float4(cameraPosition, 1), W).xyz;
            camW = cameraPosition.xyz;
    // Determine the viewing direction based on the position of the camera and the position of the vertex in the world.
            output.worldPosition = worldPosition;
                                                    
    
   
	    
    //output.viewDirection = -normalize(cameraPosition - worldPosition.xyz);
    
            float3 vCam = cameraLookAt;
	
    //vCam = mul(float4(vCam,1), W).xyz;
            output.eye = camW;
    //output.eye = float3(vCam - input.position.xyz);
    // Determine the viewing direction based on the position of the camera and the position of the vertex in the world.
    //output.viewDirection = camW.xyz - worldPosition.xyz;
	
    // Normalize the viewing direction vector.
    //output.viewDirection = -normalize(output.viewDirection);
            output.eye = cameraPosition.xyz;
            output.viewDirection = camW - _V2.xyz;
            output.viewPos.xyz = worldPosition;
    
            output.viewDirection.xyz = normalize(
	(float3) ViewI[3] - worldPosition);
    
    //output.viewDirection.xyz = normalize(
    //cameraPosition.xyz   -worldPosition);
    
    // Normalize the viewing direction vector.
            output.viewDirection = normalize(output.viewDirection);

    

    
    
            return output;

        }
//PixelInputType main(in VertexInputType input) // main is the default function name
//{
//    PixelInputType output;
//    float3 worldPosition;

//	// Change the position vector to be 4 units for proper matrix calculations.
//	//float4x4 m = mWorld;

//	//m[3][3] = 0.5;
//	// Calculate the position of the vertex against the world, view, and projection matrices.
//    //float4 pos = input.position;
	
		
//    //float3x3 _view1 = (float3x3) View;
//    //float4x4 _view2 = float4x4(View);
	
//    //pos.x -= translate.x*4;
//    //pos.y += translate.y*4;
    
    
//    //input.position.w = 1.0f;
    
//    float4 pos = input.position;
    
//    float4 _N1 = float4(input.normal, 0);
//    float4 _T1 = float4(input.tangent, 0);
//    float4 _B1 = float4(input.binormal, 0);

    
//    float4x4 W = mWorld;
   

//    ////float4 _V = pos;
    
//    float4x4 PM[4];
//    PM[0] = mul(inverse[input.BoneIndices.x], tranforms[input.BoneIndices.x]);
//    PM[1] = mul(inverse[input.BoneIndices.y], tranforms[input.BoneIndices.y]);
//    PM[2] = mul(inverse[input.BoneIndices.z], tranforms[input.BoneIndices.z]);
//    PM[3] = mul(inverse[input.BoneIndices.w], tranforms[input.BoneIndices.w]);

//    uint IDs[4];
    
//    IDs[0] = input.BoneIndices.x;
//    IDs[1] = input.BoneIndices.y;
//    IDs[2] = input.BoneIndices.z;
//    IDs[3] = input.BoneIndices.w;
    
    
//    float4 _V2 = 0;
//    float4 _N2 = 0;
//    float4 _T2 = 0;
//    float4 _B2 = 0;     
    
//    //if (is_weapon == 1)
//    //    pos = mul(pos, mWeapon);
    
//    for (int i = 0; i < 4; i++)
//    {
            
//        if ((IDs[i] != IDs[0]) || i == 0)
//        {
            
//            if (is_weapon == 1)
//            {
//                // roate 180 degfrees ?
//                pos.xyz = weapon(pos.xyz);
//                //_N1.xyz = weapon(_N1.xyz);
//                //_T1.xyz = weapon(_T1.xyz);
//                //_B1.xyz = weapon(_B1.xyz);
//                //_N2 += input.Weights[i] * mul(_N1, PM[i]);
//                //_T2 += input.Weights[i] * mul(_T1, PM[i]);
//                //_B2 += input.Weights[i] * mul(_B1, PM[i]);
            

//                //_N2 += input.Weights[i] * mul(_N1, mul(mWeapon, PM[i]));
//                //_T2 += input.Weights[i] * mul(_T1, mul(mWeapon, PM[i]));
//                //_B2 += input.Weights[i] * mul(_B1, mul(mWeapon, PM[i]));
            
//            }
            
//            {
             
//                _N2 += input.Weights[i] * mul(_N1, PM[i]);
//                _T2 += input.Weights[i] * mul(_T1, PM[i]);
//                _B2 += input.Weights[i] * mul(_B1, PM[i]);
                
//            }
            
//            _V2 += input.Weights[i] * mul(pos, PM[i]);
//        }
//    }
        
        
        
//    //    float4 _V1 = input.position;
//    //float4 _V2 = _V1 +
//    //    input.Weights[0] * mul(_V1, PM[0])+    
//    //    input.Weights[1] * mul(_V1, PM[1])+
//    //    input.Weights[2] * mul(_V1, PM[2])+
//    //    input.Weights[3] * mul(_V1, PM[3]);
    
//    //_V2 -= _V1;
        
        
//        //float4 _N2 =
//        //input.Weights[0] * mul(_N1, PM[0]) +
//        //input.Weights[1] * mul(_N1, PM[1]) +
//        //input.Weights[2] * mul(_N1, PM[2]) +
//        //input.Weights[3] * mul(_N1, PM[3]);
    
       
//        //float4 _T2 =
//        //input.Weights[0] * mul(_T1, PM[0]) +
//        //input.Weights[1] * mul(_T1, PM[1]) +
//        //input.Weights[2] * mul(_T1, PM[2]) +
//        //input.Weights[3] * mul(_T1, PM[3]);

       
//        //float4 _B2 =
//        //input.Weights[0] * mul(_B1, PM[0]) +
//        //input.Weights[1] * mul(_B1, PM[1]) +
//        //input.Weights[2] * mul(_B1, PM[2]) +
//        //input.Weights[3] * mul(_B1, PM[3]);
   
        
    
//        output.position = _V2;
//        output.normal = _N2.xyz;
//        output.tangent = _T2.xyz;
//        output.binormal = _B2.xyz;

        
   
//    //if (0)
//    ////else
//    //{
//    //    //if (is_weapon == 1)        
//    //    //    output.position = mul(input.position, mWeapon);
        
//    //    //output.position = mul(output.position, rot_y);
//    //    //output.position = mul(output.position, rot_x);
//    //    //output.position = mul(output.position, W);
    
//    //    //_V2 = output.position;
        
//    //    //output.position = mul(output.position, View);
//    //    //output.position = mul(output.position, Projection);
        
        
//    //    output.normal = input.normal;
//    //    output.tangent = input.tangent;
//    //    output.binormal = input.binormal;

        
        
//    //}
   
//    //if (is_weapon == 1)
//    //{
    
        

               
//    //}
    
    
   
//    output.position = mul(output.position, rot_y);
//    output.position = mul(output.position, rot_x);
//    output.position = mul(output.position, W);
    
//    output.worldPosition = output.position;
    
//    output.position = mul(output.position, View);
//    output.position = mul(output.position, Projection);
                                                                                                                          
	
//    output.color1 = 1;

//	// Store the texture coordinates for the pixel shader.
//    output.tex.x = input.tex.x;
//    output.tex.y = 1.0f - input.tex.y;

//    //output.tex.xy = input.tex.xy;
//    //output.tex.zw = input.TexCoord1.xy;
	
    
    
//    output.norm = float4(output.normal, 0);
     	
//    output.viewPos = mul(output.position, W) - float4(cameraPosition, 1);
    
//    //-----------------------------------------------------------------------------
//    //          Normal
//    //-----------------------------------------------------------------------------                
//    //float4x4 mWMP = mul(W, rot_x );
//    //mWMP = mul(mWMP, rot_y);
    
//    //if (is_weapon)
//    //    mWMP = mul(mWMP, mWeapon);
    
//    //output.normal = (float3) mul(float4(output.normal, 0), mWMP);
//    //output.normal = normalize(output.normal);
    
//    output.normal = (float3) mul(float4(output.normal, 0), rot_y);
//    output.normal = normalize(output.normal);
//    output.normal = (float3) mul(float4(output.normal, 0), rot_x);
//    output.normal = normalize(output.normal);

//    output.normal = (float3) mul(float4(output.normal, 0), W);
//    output.normal = normalize(output.normal);
	
//    //-----------------------------------------------------------------------------
//    //          Tangent
//    //-----------------------------------------------------------------------------        
    
   
    
//    //output.tangent = (float3) mul(float4(output.tangent, 0), mWMP);
//    //output.tangent = normalize(output.tangent);
    
    
//    output.tangent = (float3) mul(float4(output.tangent, 0), rot_y);
//    output.tangent = normalize(output.tangent);
//    output.tangent = (float3) mul(float4(output.tangent, 0), rot_x);
//    output.tangent = normalize(output.tangent);
    
    
    
//    output.tangent = (float3) mul(float4(output.tangent, 0), W);
//    output.tangent = normalize(output.tangent);
                                                     
//    //-----------------------------------------------------------------------------
//    //          Binormal
//    //-----------------------------------------------------------------------------            
//    //output.binormal = (float3) mul(float4(output.binormal, 0), mWMP);
//    //output.binormal = normalize(output.binormal);
    
//    output.binormal = (float3) mul(float4(output.binormal, 0), rot_y);
//    output.binormal = normalize(output.binormal);
//    output.binormal = (float3) mul(float4(output.binormal, 0), rot_x);
//    output.binormal = normalize(output.binormal);
    
//    output.binormal = (float3) mul(float4(output.binormal, 0), W);
//    output.binormal = normalize(output.binormal);
                                                                 
//    output.color1 = color;
    
//    // Calculate the position of the vertex in the world.
//    worldPosition = mul(_V2, W).xyz;
    
//    float3 camW = cameraPosition.xyz;
//    // Determine the viewing direction based on the position of the camera and the position of the vertex in the world.
   
                                                    
    
//    output.viewDirection.xyz = normalize(
//	(float3) ViewI[3] - worldPosition);
	    
//    //output.viewDirection = -normalize(cameraPosition - worldPosition.xyz);
    
//    float3 vCam = cameraLookAt;
	
//    //vCam = mul(float4(vCam,1), W).xyz;
//    output.eye = camW;
//    //output.eye = float3(vCam - input.position.xyz);
//    // Determine the viewing direction based on the position of the camera and the position of the vertex in the world.
//    //output.viewDirection = camW.xyz - worldPosition.xyz;
	
//    // Normalize the viewing direction vector.
//    //output.viewDirection = -normalize(output.viewDirection);

//    output.viewDirection = float4(camW - worldPosition, 1).xyz;
//    output.viewPos.xyz = worldPosition;
    
//    // Normalize the viewing direction vector.
//    output.viewDirection = normalize(output.viewDirection);

    

    
    
//    return output;

//}