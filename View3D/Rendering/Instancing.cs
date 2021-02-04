using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Rendering
{
    public class Instancing
    {
        //Texture2D texture;
        Effect effect;

        VertexDeclaration instanceVertexDeclaration;

        VertexBuffer instanceBuffer;
        VertexBuffer geometryBuffer;
        IndexBuffer indexBuffer;

        VertexBufferBinding[] bindings;
        InstanceInfo[] instances;

        struct InstanceInfo
        {
            public Vector4 World;
            public float AtlasCoordinate;
        };

        Int32 instanceCount = 30;

        public void Initialize(GraphicsDevice device)
        {
            GenerateInstanceVertexDeclaration();
            GenerateGeometry(device);
            GenerateInstanceInformation(device, instanceCount);

            bindings = new VertexBufferBinding[2];
            bindings[0] = new VertexBufferBinding(geometryBuffer);
            bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
        }

        public void Load(ContentManager Content)
        {
            effect = Content.Load<Effect>("Shaders//InstancingShader");
            //texture = Content.Load<Texture2D>("default_256");
        }

        private void GenerateInstanceVertexDeclaration()
        {
            VertexElement[] instanceStreamElements = new VertexElement[2];

            instanceStreamElements[0] =
                    new VertexElement(0, VertexElementFormat.Vector4,
                        VertexElementUsage.Position, 1);

            instanceStreamElements[1] =
                new VertexElement(sizeof(float) * 4, VertexElementFormat.Single,
                    VertexElementUsage.Position, 2);

            instanceVertexDeclaration = new VertexDeclaration(instanceStreamElements);
        }

        //This creates a cube!
        public void GenerateGeometry(GraphicsDevice device)
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[24];

            #region filling vertices
            vertices[0].Position = new Vector3(-1, 1, -1);
            vertices[0].TextureCoordinate = new Vector2(0, 0);
            vertices[1].Position = new Vector3(1, 1, -1);
            vertices[1].TextureCoordinate = new Vector2(1, 0);
            vertices[2].Position = new Vector3(-1, 1, 1);
            vertices[2].TextureCoordinate = new Vector2(0, 1);
            vertices[3].Position = new Vector3(1, 1, 1);
            vertices[3].TextureCoordinate = new Vector2(1, 1);

            vertices[4].Position = new Vector3(-1, -1, 1);
            vertices[4].TextureCoordinate = new Vector2(0, 0);
            vertices[5].Position = new Vector3(1, -1, 1);
            vertices[5].TextureCoordinate = new Vector2(1, 0);
            vertices[6].Position = new Vector3(-1, -1, -1);
            vertices[6].TextureCoordinate = new Vector2(0, 1);
            vertices[7].Position = new Vector3(1, -1, -1);
            vertices[7].TextureCoordinate = new Vector2(1, 1);

            vertices[8].Position = new Vector3(-1, 1, -1);
            vertices[8].TextureCoordinate = new Vector2(0, 0);
            vertices[9].Position = new Vector3(-1, 1, 1);
            vertices[9].TextureCoordinate = new Vector2(1, 0);
            vertices[10].Position = new Vector3(-1, -1, -1);
            vertices[10].TextureCoordinate = new Vector2(0, 1);
            vertices[11].Position = new Vector3(-1, -1, 1);
            vertices[11].TextureCoordinate = new Vector2(1, 1);

            vertices[12].Position = new Vector3(-1, 1, 1);
            vertices[12].TextureCoordinate = new Vector2(0, 0);
            vertices[13].Position = new Vector3(1, 1, 1);
            vertices[13].TextureCoordinate = new Vector2(1, 0);
            vertices[14].Position = new Vector3(-1, -1, 1);
            vertices[14].TextureCoordinate = new Vector2(0, 1);
            vertices[15].Position = new Vector3(1, -1, 1);
            vertices[15].TextureCoordinate = new Vector2(1, 1);

            vertices[16].Position = new Vector3(1, 1, 1);
            vertices[16].TextureCoordinate = new Vector2(0, 0);
            vertices[17].Position = new Vector3(1, 1, -1);
            vertices[17].TextureCoordinate = new Vector2(1, 0);
            vertices[18].Position = new Vector3(1, -1, 1);
            vertices[18].TextureCoordinate = new Vector2(0, 1);
            vertices[19].Position = new Vector3(1, -1, -1);
            vertices[19].TextureCoordinate = new Vector2(1, 1);

            vertices[20].Position = new Vector3(1, 1, -1);
            vertices[20].TextureCoordinate = new Vector2(0, 0);
            vertices[21].Position = new Vector3(-1, 1, -1);
            vertices[21].TextureCoordinate = new Vector2(1, 0);
            vertices[22].Position = new Vector3(1, -1, -1);
            vertices[22].TextureCoordinate = new Vector2(0, 1);
            vertices[23].Position = new Vector3(-1, -1, -1);
            vertices[23].TextureCoordinate = new Vector2(1, 1);
            #endregion

            geometryBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration,
                                              24, BufferUsage.WriteOnly);
            geometryBuffer.SetData(vertices);

            #region filling indices

            int[] indices = new int[36];
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 1; indices[4] = 3; indices[5] = 2;

            indices[6] = 4; indices[7] = 5; indices[8] = 6;
            indices[9] = 5; indices[10] = 7; indices[11] = 6;

            indices[12] = 8; indices[13] = 9; indices[14] = 10;
            indices[15] = 9; indices[16] = 11; indices[17] = 10;

            indices[18] = 12; indices[19] = 13; indices[20] = 14;
            indices[21] = 13; indices[22] = 15; indices[23] = 14;

            indices[24] = 16; indices[25] = 17; indices[26] = 18;
            indices[27] = 17; indices[28] = 19; indices[29] = 18;

            indices[30] = 20; indices[31] = 21; indices[32] = 22;
            indices[33] = 21; indices[34] = 23; indices[35] = 22;

            #endregion

            indexBuffer = new IndexBuffer(device, typeof(int), 36, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        private void GenerateInstanceInformation(GraphicsDevice device, Int32 count)
        {
            instances = new InstanceInfo[count];
            Random rnd = new Random();

            for (int i = 0; i < count; i++)
            {
                //random position example
                instances[i].World = new Vector4(-rnd.Next(20),
                                                 -rnd.Next(20),
                                                 -rnd.Next(20), 1);

                instances[i].AtlasCoordinate = (float)rnd.NextDouble() * 2;
            }

            instanceBuffer = new VertexBuffer(device, instanceVertexDeclaration,
                                              count, BufferUsage.WriteOnly);
            instanceBuffer.SetData(instances);
        }

        //view and projection should come from your camera
        public void Draw(Matrix view, Matrix projection, GraphicsDevice device)
        {
            effect.CurrentTechnique = effect.Techniques["Instancing"];
            effect.Parameters["WVP"].SetValue(view * projection);
            //effect.Parameters["cubeTexture"].SetValue(texture);

            device.Indices = indexBuffer;

            effect.CurrentTechnique.Passes[0].Apply();

            device.SetVertexBuffers(bindings);
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 24, 0, 12, instanceCount);
        }
    }
}
