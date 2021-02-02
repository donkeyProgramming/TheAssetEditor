

sampler SampleType : register(s0);
sampler spBRDF_Sampler : register(s1);
sampler s_normal : register(s2);
sampler s_mask : register(s3);



TextureCube tex_cube_specular : register(t0);
TextureCube tex_cube_specular_blurry : register(t1);
TextureCube tex_cube_diffuse : register(t2);
TextureCube tex_cube_DUMMY : register(t3);

Texture2D eq_tex_cube_specular_blur1 : register(t4);
Texture2D specularBRDF_LUT : register(t5);

Texture2D shaderTextures[5] : register(t6);
Texture2D ScreenSpace : register(t11);

