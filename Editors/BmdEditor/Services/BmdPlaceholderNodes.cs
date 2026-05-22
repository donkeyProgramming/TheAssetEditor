using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Transforms;
using GameWorld.Core.SceneNodes;

namespace Editors.BmdEditor.Services
{
    // Custom node class for VFX placeholders that renders a visible cube
    public class VfxPlaceholderNode(string name = "VFX_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Purple;
        public Color SelectedNodeColour { get; set; } = Color.Magenta;
        public float Scale { get; set; } = 0.5f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a small cube as the placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new VfxPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not VfxPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            base.CopyInto(target);
        }
    }

    // Custom node class for CSC placeholders that renders a visible cube
    public class CscPlaceholderNode(string name = "CSC_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Orange;
        public Color SelectedNodeColour { get; set; } = Color.DarkOrange;
        public float Scale { get; set; } = 0.4f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a small cube as the placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new CscPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not CscPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            base.CopyInto(target);
        }
    }

    // Custom node class for Light Probe placeholders that renders two spheres (inner and outer)
    public class LightProbePlaceholderNode(string name = "LightProbe_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Cyan;
        public Color SelectedNodeColour { get; set; } = Color.DarkCyan;
        public float OuterRadius { get; set; } = 1.0f;
        public float InnerRadius { get; set; } = 0.5f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                // Render outer sphere
                var outerTransform = Matrix.CreateScale(OuterRadius) * ModelMatrix * parentWorld;
                var outerSphereLines = PointLightSphereNode.CreateWireframeSphere(outerTransform, NodeColour, 16);
                renderEngine.AddRenderLines(outerSphereLines);
                
                // Render inner sphere
                var innerTransform = Matrix.CreateScale(InnerRadius) * ModelMatrix * parentWorld;
                var innerSphereLines = PointLightSphereNode.CreateWireframeSphere(innerTransform, SelectedNodeColour, 16);
                renderEngine.AddRenderLines(innerSphereLines);
            }
        }

        public override ISceneNode CreateCopyInstance() => new LightProbePlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not LightProbePlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.OuterRadius = OuterRadius;
            typedTarget.InnerRadius = InnerRadius;
            base.CopyInto(target);
        }
    }
    
    // Custom node class for Building Projectile Emitter placeholders that renders a visible cube
    public class BuildingProjectileEmitterPlaceholderNode(string name = "BuildingProjectileEmitter_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Red;
        public Color SelectedNodeColour { get; set; } = Color.DarkRed;
        public float Scale { get; set; } = 0.35f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a small cube as the placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new BuildingProjectileEmitterPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not BuildingProjectileEmitterPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            base.CopyInto(target);
        }
    }

    // Custom node class for Battlefield Building placeholders that renders a visible cube
    public class BattlefieldBuildingPlaceholderNode(string name = "BattlefieldBuilding_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Brown;
        public Color SelectedNodeColour { get; set; } = Color.SaddleBrown;
        public float Scale { get; set; } = 0.6f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a cube as the placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new BattlefieldBuildingPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not BattlefieldBuildingPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            base.CopyInto(target);
        }
    }

    // Custom node class for GoOutline placeholders that renders connected vertices as lines
    public class GoOutlineNode(string name = "GoOutline") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Yellow;
        public Color SelectedNodeColour { get; set; } = Color.Gold;
        public List<RmvVector2> VertexList { get; set; } = [];

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && VertexList.Count >= 2)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;

                // Create lines connecting all vertices in order, including closing the loop
                var lineVertices = new List<VertexPositionColor>();

                for (var i = 0; i < VertexList.Count; i++)
                {
                    var currentVertex = VertexList[i];
                    var nextVertex = VertexList[(i + 1) % VertexList.Count]; // Wrap around to connect last to first

                    var startPos = new Vector3(currentVertex.X, 0, currentVertex.Y);
                    var endPos = new Vector3(nextVertex.X, 0, nextVertex.Y);

                    var worldStart = Vector3.Transform(startPos, worldTransform);
                    var worldEnd = Vector3.Transform(endPos, worldTransform);

                    // Add two vertices for each line segment
                    lineVertices.Add(new VertexPositionColor(worldStart, drawColour));
                    lineVertices.Add(new VertexPositionColor(worldEnd, drawColour));
                }

                if (lineVertices.Count > 0)
                {
                    renderEngine.AddRenderLines([.. lineVertices]);
                }
            }
        }

        public override ISceneNode CreateCopyInstance() => new GoOutlineNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not GoOutlineNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.VertexList = [.. VertexList];
            base.CopyInto(target);
        }
    }

    // Custom node class for NonTerrainOutline placeholders that renders connected vertices as lines
    public class NonTerrainOutlineNode(string name = "NonTerrainOutline") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Cyan;
        public Color SelectedNodeColour { get; set; } = Color.DarkCyan;
        public List<RmvVector2> VertexList { get; set; } = [];

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && VertexList.Count >= 2)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;

                // Create lines connecting all vertices in order, including closing the loop
                var lineVertices = new List<VertexPositionColor>();

                for (var i = 0; i < VertexList.Count; i++)
                {
                    var currentVertex = VertexList[i];
                    var nextVertex = VertexList[(i + 1) % VertexList.Count]; // Wrap around to connect last to first

                    var startPos = new Vector3(currentVertex.X, 0, currentVertex.Y);
                    var endPos = new Vector3(nextVertex.X, 0, nextVertex.Y);

                    var worldStart = Vector3.Transform(startPos, worldTransform);
                    var worldEnd = Vector3.Transform(endPos, worldTransform);

                    // Add two vertices for each line segment
                    lineVertices.Add(new VertexPositionColor(worldStart, drawColour));
                    lineVertices.Add(new VertexPositionColor(worldEnd, drawColour));
                }

                if (lineVertices.Count > 0)
                {
                    renderEngine.AddRenderLines([.. lineVertices]);
                }
            }
        }

        public override ISceneNode CreateCopyInstance() => new NonTerrainOutlineNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not NonTerrainOutlineNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.VertexList = [.. VertexList];
            base.CopyInto(target);
        }
    }

    // Custom node class for Boundary placeholders that renders connected vertices as lines
    public class BoundaryNode(string name = "Boundary") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Magenta;
        public Color SelectedNodeColour { get; set; } = Color.Purple;
        public List<RmvVector2> PointList { get; set; } = [];

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && PointList.Count >= 2)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;
                
                // Create lines connecting all points in order, including closing the loop
                var lineVertices = new List<VertexPositionColor>();
                
                for (var i = 0; i < PointList.Count; i++)
                {
                    var currentPoint = PointList[i];
                    var nextPoint = PointList[(i + 1) % PointList.Count]; // Wrap around to connect last to first
                    
                    var startPos = new Vector3(currentPoint.X, 0, currentPoint.Y);
                    var endPos = new Vector3(nextPoint.X, 0, nextPoint.Y);
                    
                    var worldStart = Vector3.Transform(startPos, worldTransform);
                    var worldEnd = Vector3.Transform(endPos, worldTransform);
                    
                    // Add two vertices for each line segment
                    lineVertices.Add(new VertexPositionColor(worldStart, drawColour));
                    lineVertices.Add(new VertexPositionColor(worldEnd, drawColour));
                }
                
                if (lineVertices.Count > 0)
                {
                    renderEngine.AddRenderLines([.. lineVertices]);
                }
            }
        }

        public override ISceneNode CreateCopyInstance() => new BoundaryNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not BoundaryNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.PointList = [.. PointList];
            base.CopyInto(target);
        }
    }
    
    // Custom node class for Point Light spheres that renders a sphere with radius
    public class PointLightSphereNode(string name = "PointLight_Sphere") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Yellow;
        public Color SelectedNodeColour { get; set; } = Color.Orange;
        public float Radius { get; set; } = 1.0f;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Radius) * ModelMatrix * parentWorld;
                
                // Render a wireframe sphere as the point light indicator
                var sphereLines = CreateWireframeSphere(worldTransform, drawColour, 16);
                renderEngine.AddRenderLines(sphereLines);
            }
        }

        public override ISceneNode CreateCopyInstance() => new PointLightSphereNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not PointLightSphereNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Radius = Radius;
            base.CopyInto(target);
        }

        public static VertexPositionColor[] CreateWireframeSphere(Matrix transform, Color color, int segments)
        {
            var lines = new List<VertexPositionColor>();
            
            // Create latitude lines (horizontal circles)
            for (var lat = 0; lat <= segments / 2; lat++)
            {
                var theta = MathF.PI * lat / (segments / 2);
                var sinTheta = MathF.Sin(theta);
                var cosTheta = MathF.Cos(theta);
                
                Vector3? prevPoint = null;
                
                for (var lon = 0; lon <= segments; lon++)  // Use <= to close the circle
                {
                    var phi = 2 * MathF.PI * lon / segments;
                    var sinPhi = MathF.Sin(phi);
                    var cosPhi = MathF.Cos(phi);
                    
                    var x = sinTheta * cosPhi;
                    var y = cosTheta;
                    var z = sinTheta * sinPhi;
                    
                    var point = new Vector3(x, y, z);
                    point = Vector3.Transform(point, transform);
                    
                    if (prevPoint.HasValue)
                    {
                        lines.Add(new VertexPositionColor(prevPoint.Value, color));
                        lines.Add(new VertexPositionColor(point, color));
                    }
                    
                    prevPoint = point;
                }
            }
            
            // Create longitude lines (vertical lines from pole to pole)
            for (var lon = 0; lon < segments; lon++)
            {
                var phi = 2 * MathF.PI * lon / segments;
                var sinPhi = MathF.Sin(phi);
                var cosPhi = MathF.Cos(phi);
                
                Vector3? prevPoint = null;
                
                for (var lat = 0; lat <= segments / 2; lat++)
                {
                    var theta = MathF.PI * lat / (segments / 2);
                    var sinTheta = MathF.Sin(theta);
                    var cosTheta = MathF.Cos(theta);
                    
                    var x = sinTheta * cosPhi;
                    var y = cosTheta;
                    var z = sinTheta * sinPhi;
                    
                    var point = new Vector3(x, y, z);
                    point = Vector3.Transform(point, transform);
                    
                    if (prevPoint.HasValue)
                    {
                        lines.Add(new VertexPositionColor(prevPoint.Value, color));
                        lines.Add(new VertexPositionColor(point, color));
                    }
                    
                    prevPoint = point;
                }
            }
            
            return [.. lines];
        }
    }

    // Custom node class for Spot Light cones that renders a cone with direction
    public class SpotLightConeNode(string name = "SpotLight_Cone") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.LightBlue;
        public Color SelectedNodeColour { get; set; } = Color.Blue;
        public float Length { get; set; } = 2.0f;
        public float InnerAngle { get; set; } = 0.5f;
        public float OuterAngle { get; set; } = 1.0f;
        public RmvVector3 Position { get; set; } = new();
        public Quaternion Quaternion { get; set; } = Quaternion.Identity;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                // Create transform from position and quaternion rotation
                var positionVector = new Vector3(Position.X, Position.Y, Position.Z);
                var rotationMatrix = Matrix.CreateFromQuaternion(Quaternion);
                var translationMatrix = Matrix.CreateTranslation(positionVector);
                var localTransform = rotationMatrix * translationMatrix;
                var worldTransform = localTransform * parentWorld;
                
                // Render outer cone (larger, more transparent)
                var outerConeLines = CreateWireframeCone(worldTransform, NodeColour, Length, OuterAngle, 16);
                renderEngine.AddRenderLines(outerConeLines);
                
                // Render inner cone (smaller, more visible)
                var innerConeLines = CreateWireframeCone(worldTransform, SelectedNodeColour, Length, InnerAngle, 16);
                renderEngine.AddRenderLines(innerConeLines);
            }
        }

        public override ISceneNode CreateCopyInstance() => new SpotLightConeNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not SpotLightConeNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Length = Length;
            typedTarget.InnerAngle = InnerAngle;
            typedTarget.OuterAngle = OuterAngle;
            typedTarget.Position = Position;
            typedTarget.Quaternion = Quaternion;
            base.CopyInto(target);
        }

        private static VertexPositionColor[] CreateWireframeCone(Matrix transform, Color color, float length, float angle, int segments)
        {
            var lines = new List<VertexPositionColor>();
            // Use half the angle since cone geometry uses half-angle from center axis
            var radius = length * MathF.Tan(angle * 0.5f);
            
            // Create cone tip at origin (will be transformed to spotlight position)
            var tip = new Vector3(0, 0, 0);
            tip = Vector3.Transform(tip, transform);
            
            // Create base circle points extending along positive X axis (X as forward)
            var basePoints = new Vector3[segments];
            for (var i = 0; i < segments; i++)
            {
                var theta = 2 * MathF.PI * i / segments;
                basePoints[i] = new Vector3(length, radius * MathF.Cos(theta), radius * MathF.Sin(theta));
                basePoints[i] = Vector3.Transform(basePoints[i], transform);
            }
            
            // Create lines from tip to base
            for (var i = 0; i < segments; i++)
            {
                lines.Add(new VertexPositionColor(tip, color));
                lines.Add(new VertexPositionColor(basePoints[i], color));
            }
            
            // Create base circle lines
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                lines.Add(new VertexPositionColor(basePoints[i], color));
                lines.Add(new VertexPositionColor(basePoints[next], color));
            }
            
            return [.. lines];
        }
    }

    // Custom node class for Terrain Hole edges that renders triangle edges only
    public class TerrainHoleEdgesNode(string name = "TerrainHole_Edges") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Red;
        public Color SelectedNodeColour { get; set; } = Color.DarkRed;
        public RmvVector3 FirstVert { get; set; } = new();
        public RmvVector3 SecondVert { get; set; } = new();
        public RmvVector3 ThirdVert { get; set; } = new();

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;
                
                // Create triangle edges (3 lines forming a triangle, no filled faces)
                var v1 = new Vector3(FirstVert.X, FirstVert.Y, FirstVert.Z);
                var v2 = new Vector3(SecondVert.X, SecondVert.Y, SecondVert.Z);
                var v3 = new Vector3(ThirdVert.X, ThirdVert.Y, ThirdVert.Z);
                
                // Transform vertices to world space
                v1 = Vector3.Transform(v1, worldTransform);
                v2 = Vector3.Transform(v2, worldTransform);
                v3 = Vector3.Transform(v3, worldTransform);
                
                // Draw triangle edges
                renderEngine.AddRenderLines([
                    new VertexPositionColor(v1, drawColour),
                    new VertexPositionColor(v2, drawColour),
                    new VertexPositionColor(v2, drawColour),
                    new VertexPositionColor(v3, drawColour),
                    new VertexPositionColor(v3, drawColour),
                    new VertexPositionColor(v1, drawColour)
                ]);
            }
        }

        public override ISceneNode CreateCopyInstance() => new TerrainHoleEdgesNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not TerrainHoleEdgesNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.FirstVert = FirstVert;
            typedTarget.SecondVert = SecondVert;
            typedTarget.ThirdVert = ThirdVert;
            base.CopyInto(target);
        }
    }

    // Custom node class for PolyMesh triangles that renders filled triangles with materials
    public class PolyMeshTrianglesNode(string name = "PolyMesh_Triangles") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Green;
        public Color SelectedNodeColour { get; set; } = Color.DarkGreen;
        public RmvVector3[] Vertices { get; set; } = [];
        public ushort[] Triangles { get; set; } = [];
        public string MaterialString { get; set; } = string.Empty;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && Vertices.Length > 0 && Triangles.Length >= 3)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;
                
                // Convert vertices to world space
                var worldVertices = new Vector3[Vertices.Length];
                for (var i = 0; i < Vertices.Length; i++)
                {
                    var vertex = new Vector3(Vertices[i].X, Vertices[i].Y, Vertices[i].Z);
                    worldVertices[i] = Vector3.Transform(vertex, worldTransform);
                }
                
                // Create triangle edges (for now, render as wireframe - filled triangles would require proper mesh rendering)
                var lineVertices = new List<VertexPositionColor>();
                
                for (var i = 0; i < Triangles.Length; i += 3)
                {
                    if (i + 2 < Triangles.Length)
                    {
                        var idx1 = Triangles[i];
                        var idx2 = Triangles[i + 1];
                        var idx3 = Triangles[i + 2];
                        
                        if (idx1 < worldVertices.Length && idx2 < worldVertices.Length && idx3 < worldVertices.Length)
                        {
                            // Add triangle edges
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx1], drawColour));
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx2], drawColour));
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx2], drawColour));
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx3], drawColour));
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx3], drawColour));
                            lineVertices.Add(new VertexPositionColor(worldVertices[idx1], drawColour));
                        }
                    }
                }
                
                if (lineVertices.Count > 0)
                {
                    renderEngine.AddRenderLines([.. lineVertices]);
                }
            }
        }

        public override ISceneNode CreateCopyInstance() => new PolyMeshTrianglesNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not PolyMeshTrianglesNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Vertices = Vertices;
            typedTarget.Triangles = Triangles;
            typedTarget.MaterialString = MaterialString;
            base.CopyInto(target);
        }
    }

    // Custom node class for BMD Info placeholders that represents recursive BMD file references
    public class BmdInfoPlaceholderNode(string name = "BMD_Info_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.White;
        public Color SelectedNodeColour { get; set; } = Color.Gray;
        public float Scale { get; set; } = 0.8f;
        public string ReferencedBmdPath { get; set; } = string.Empty;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a larger cube as the BMD reference placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new BmdInfoPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not BmdInfoPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            typedTarget.ReferencedBmdPath = ReferencedBmdPath;
            base.CopyInto(target);
        }
    }

    // Custom node class for Prop placeholders that renders a visible cube when RMV2 loading fails
    public class PropPlaceholderNode(string name = "Prop_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Red;
        public Color SelectedNodeColour { get; set; } = Color.DarkRed;
        public float Scale { get; set; } = 0.5f;
        public string FailedModelPath { get; set; } = string.Empty;

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible)
            {
                var drawColour = NodeColour;
                var worldTransform = Matrix.CreateScale(Scale) * ModelMatrix * parentWorld;
                
                // Render a cube as the prop placeholder
                renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(worldTransform, drawColour));
            }
        }

        public override ISceneNode CreateCopyInstance() => new PropPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not PropPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            typedTarget.FailedModelPath = FailedModelPath;
            base.CopyInto(target);
        }
    }

    // Custom node class for Sound placeholders that renders cubes at coordinates with optional connecting lines
    public class SoundPlaceholderNode(string name = "Sound_Placeholder") : GroupNode(name), IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Lime;
        public Color SelectedNodeColour { get; set; } = Color.Green;
        public float Scale { get; set; } = 0.3f;
        public string SoundType { get; set; } = string.Empty;
        public RmvVector3[] CoordList { get; set; } = [];

        public void Render(GameWorld.Core.Components.Rendering.RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && CoordList.Length > 0)
            {
                var drawColour = NodeColour;
                var worldTransform = ModelMatrix * parentWorld;

                // Render cubes at all coordinates
                foreach (var coord in CoordList)
                {
                    var position = new Vector3(coord.X, coord.Y, coord.Z);
                    var cubeTransform = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(position) * worldTransform;
                    renderEngine.AddRenderLines(GameWorld.Core.Rendering.LineHelper.CreateCube(cubeTransform, drawColour));
                }

                // For LINE_LIST, also draw connecting lines between vertices
                if (SoundType == "SST_LINE_LIST" && CoordList.Length >= 2)
                {
                    var lineVertices = new List<VertexPositionColor>();
                    
                    for (var i = 0; i < CoordList.Length - 1; i++)
                    {
                        var currentVertex = CoordList[i];
                        var nextVertex = CoordList[i + 1];
                        
                        var startPos = new Vector3(currentVertex.X, currentVertex.Y, currentVertex.Z);
                        var endPos = new Vector3(nextVertex.X, nextVertex.Y, nextVertex.Z);
                        
                        var worldStart = Vector3.Transform(startPos, worldTransform);
                        var worldEnd = Vector3.Transform(endPos, worldTransform);
                        
                        // Add two vertices for each line segment
                        lineVertices.Add(new VertexPositionColor(worldStart, drawColour));
                        lineVertices.Add(new VertexPositionColor(worldEnd, drawColour));
                    }
                    
                    if (lineVertices.Count > 0)
                    {
                        renderEngine.AddRenderLines([.. lineVertices]);
                    }
                }
            }
        }

        public override ISceneNode CreateCopyInstance() => new SoundPlaceholderNode();

        public override void CopyInto(ISceneNode target)
        {
            if (target is not SoundPlaceholderNode typedTarget)
                return;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.Scale = Scale;
            typedTarget.SoundType = SoundType;
            typedTarget.CoordList = CoordList;
            base.CopyInto(target);
        }
    }
}
