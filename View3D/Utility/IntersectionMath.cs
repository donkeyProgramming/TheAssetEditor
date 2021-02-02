using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Utility
{
    public static class IntersectionMath
    {
		public static bool MollerTrumboreIntersection(Ray r, Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, out float? distance)
		{
			//Source : https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
			const float EPSILON = 0.0000001f;
			Vector3 edge1, edge2, h, s, q;
			float a, f, u, v;
			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;
			h = Vector3.Cross(r.Direction, edge2);
			a = Vector3.Dot(edge1, h);
			if (a > -EPSILON && a < EPSILON)
			{
				distance = null;
				return false;    // This ray is parallel to this triangle.
			}
			f = 1.0f / a;
			s = r.Position - vertex0;
			u = f * Vector3.Dot(s, h);
			if (u < 0.0 || u > 1.0)
			{
				distance = null;
				return false;
			}
			q = Vector3.Cross(s, edge1);
			v = f * Vector3.Dot(r.Direction, q);
			if (v < 0.0 || u + v > 1.0)
			{
				distance = null;
				return false;
			}
			// At this stage we can compute t to find out where the intersection point is on the line.
			float t = f * Vector3.Dot(edge2, q);
			if (t > EPSILON) // ray intersection
			{
				distance = t;
				return true;
			}
			else // This means that there is a line intersection but not a ray intersection.
			{
				distance = null;
				return false;
			}
		}
	}
}
