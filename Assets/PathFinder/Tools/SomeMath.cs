using UnityEngine;
using System.Collections;
using K_PathFinder.VectorInt ;
using System.Collections.Generic;
using System;
using System.Linq;

//TODO: TLP_Projec_DO_ME
namespace K_PathFinder {
    /// <summary>
    /// junkyard of math
    /// </summary>
    public static class SomeMath {
        public enum Axis {
            x,y,z
        }
        public enum Axises {
            xy, xz, yz
        }

        //const float SMALL_ENOUGH_FLOAT = 0.0001f;


        //if points in line have specific order then it help to know if ray look at ray or not
        public static float RotateLineRightAndReturnDot(float aX, float aY, float bX, float bY, float rayX, float rayY) {
            return (-(aY - bY) * rayX) + ((aX - bX) * rayY);
        }
        public static float RotateLineRightAndReturnDot(Vector2 A, Vector2 B, Vector2 ray) {
            return (-(A.y - B.y) * ray.x) + ((A.x - B.x) * ray.y);
        }
        public static float Sqr(float value) {
            return value * value;
        }
        public static int Sqr(int value) {
            return value * value;
        }
        
        public static int SqrDistance(int ax, int ay, int bx, int by) {
            return ((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay));
        }
        public static float SqrDistance(float ax, float ay, float bx, float by) {
            return ((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay));
        }
        public static float SqrDistance(float ax, float ay, float az, float bx, float by, float bz) {
            return ((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay)) + ((bz - az) * (bz - az));
        }
        public static float SqrDistance(Vector2 a, Vector2 b) {
            return ((b.x - a.x) * (b.x - a.x)) + ((b.y - a.y) * (b.y - a.y));
        }
        public static float SqrDistance(Vector3 a, Vector3 b) {
            return ((b.x - a.x) * (b.x - a.x)) + ((b.y - a.y) * (b.y - a.y)) + ((b.z - a.z) * (b.z - a.z));
        }
        public static float Distance(float ax, float ay, float az, float bx, float by, float bz) {
            return Mathf.Sqrt(SqrDistance(ax, ay, az, bx, by, bz));
        }
        public static float Distance(float ax, float ay, float bx, float by) {
            return Mathf.Sqrt(SqrDistance(ax, ay, bx, by));
        }
        public static float Distance(Vector3 a, Vector3 b) {
            return Mathf.Sqrt(SqrDistance(a, b));
        }
        public static float Distance(Vector2 a, Vector2 b) {
            return Mathf.Sqrt(SqrDistance(a, b));
        }

        public static float SqrMagnitude(float x, float y) {
            return (x * x) + (y * y);
        }
        public static float SqrMagnitude(Vector2 vector) {
            return (vector.x * vector.x) + (vector.y * vector.y);
        }
        public static float SqrMagnitude(float x, float y, float z) {
            return (x * x) + (y * y) + (z * z);
        }
        public static float SqrMagnitude(Vector3 vector) {
            return (vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z);
        }

        public static float Magnitude(float x, float y) {
            return Mathf.Sqrt(SqrMagnitude(x, y));
        }
        public static float Magnitude(float x, float y, float z) {
            return Mathf.Sqrt(SqrMagnitude(x, y, z));
        }
        public static float Magnitude(Vector2 vec) {
            return Mathf.Sqrt(SqrMagnitude(vec.x, vec.y));
        }
        public static float Magnitude(Vector3 vec) {
            return Mathf.Sqrt(SqrMagnitude(vec.x, vec.y, vec.z));
        }
        public static void Normalize(ref float x, ref float y) {
            float m = Magnitude(x, y);
            x = x / m;
            y = y / m;
        }
        public static void Normalize(ref float x, ref float y, ref float z) {
            float m = Magnitude(x, y);
            x = x / m;
            y = y / m;
            z = z / m;
        }

        public static float V2Cross(Vector2 left, Vector2 right) {
            return (left.y * right.x) - (left.x * right.y);
        }
        public static float V2Cross(float leftX, float leftY, float rightX, float rightY) {
            return (leftY * rightX) - (leftX * rightY);
        }
        public static float Dot(Vector2 A, Vector2 B) {
            return (A.x * B.x) + (A.y * B.y);
        }
        public static float Dot(float Ax, float Ay, float Bx, float By) {
            return (Ax * Bx) + (Ay * By);
        }
        public static float Dot(Vector3 A, Vector3 B) {
            return (A.x * B.x) + (A.y * B.y) + (A.z * B.z);
        }
        public static float Dot(float Ax, float Ay, float Az, float Bx, float By, float Bz) {
            return (Ax * Bx) + (Ay * By) + (Az * Bz);
        }

        public static Vector3 Cross(Vector3 A, Vector3 B) {
            return new Vector3(
                A.y * B.z - A.z * B.y,
                A.z * B.x - A.x * B.z,
                A.x * B.y - A.y * B.x);
        }


        public static float Min(float a, float b) {
            return a < b ? a : b;
        }
        public static float Max(float a, float b) {
            return a > b ? a : b;
        }


        public static float Min(float a, float b, float c) {
            a = a < b ? a : b;
            return a < c ? a : c;
        }
        public static float Max(float a, float b, float c) {
            a = a > b ? a : b;
            return a > c ? a : c;
        }

        public static int Min(int a, int b, int c) {
            a = a < b ? a : b;
            return a < c ? a : c;
        }
        public static int Max(int a, int b, int c) {
            a = a > b ? a : b;
            return a > c ? a : c;
        }

        public static int Difference(int a, int b) {
            a = a - b;
            if (a < 0)
                a *= -1;
            return a;
        }
        public static float Difference(float a, float b) {
            a = a - b;
            if (a < 0)
                a *= -1;
            return a;
        }

        public static int Clamp(int min, int max, int value) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
            //return value < min ? min : value > max ? max : value;
        }
        public static float Clamp(float min, float max, float value) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
            //return value < min ? min : value > max ? max : value;
        }

        //first inclussive and last exclussive
        public static bool InRangeArrayLike(int value, int min, int max) {
            return value < max && value >= max;
        }
        public static bool InRangeExclusive(int value, int min, int max) {
            return value > min && value < max;
        }
        public static bool InRangeExclusive(float value, float min, float max) {
            return value > min && value < max;
        }
        public static bool InRangeInclusive(int value, int min, int max) {
            return value >= min && value <= max;
        }
        public static bool InRangeInclusive(float value, float min, float max) {
            return value >= min && value <= max;
        }

        public static bool InRangeInclusive(float value1, float value2, float min, float max) {
            return (value1 >= min && value1 <= max) | (value2 >= min && value2 <= max);
        }

        public static Vector2 RotateRight(Vector2 vector) {
            return new Vector2(-vector.y, vector.x);
        }

        public static Vector3 TwoVertexNormal(Vector3 first, Vector3 second) {
            return (first.z * second.x) - (first.x * second.z) < 0 ?
                (first.normalized + second.normalized).normalized * -1 :
                (first.normalized + second.normalized).normalized;
        }

        public static float LinePointSideMathf(Vector2 a, Vector2 b, Vector2 point) {
            return Mathf.Sign((point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y));
        }
        public static float LinePointSideMath(Vector2 a, Vector2 b, Vector2 point) {
            return Math.Sign((point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y));
        }
        public static float LinePointSideMath(Vector2 a, Vector2 b, float pointX, float pointY) {
            return Math.Sign((pointX - b.x) * (a.y - b.y) - (a.x - b.x) * (pointY - b.y));
        }

        //public static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 po) {
        //    float s = a.y * c.x - a.x * c.y + (c.y - a.y) * po.x + (a.x - c.x) * po.y;
        //    float t = a.x * b.y - a.y * b.x + (a.y - b.y) * po.x + (b.x - a.x) * po.y;

        //    if ((s <= 0) != (t <= 0))
        //        return false;

        //    float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
        //    if (A < 0.0) {
        //        s = -s;
        //        t = -t;
        //        A = -A;
        //    }
        //    return s > 0 && t > 0 && (s + t) < A;
        //}

        //public static bool PointInTriangle(
        //    float Ax, float Ay,              // A
        //    float Bx, float By,              // B
        //    float Cx, float Cy,              // C
        //    float Px, float Py) {            // Point

        //    float s = Ay * Cx - Ax * Cy + (Cy - Ay) * Px + (Ax - Cx) * Py;
        //    float t = Ax * By - Ay * Bx + (Ay - By) * Px + (Bx - Ax) * Py;

        //    if ((s <= 0) != (t <= 0))
        //        return false;

        //    float a = -By * Cx + Ay * (Cx - Bx) + Ax * (By - Cy) + Bx * Cy;
        //    if (a < 0.0) {
        //        s = -s;
        //        t = -t;
        //        a = -a;
        //    }
        //    return s > 0 && t > 0 && (s + t) < a;
        //}



        public static bool PointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
            return PointInTriangle(A.x, A.y, B.x, B.y, C.x, C.y, P.x, P.y);
        }

        public static bool PointInTriangle(float Ax, float Ay, float Bx, float By, float Cx, float Cy, float Px, float Py) {
            float s1 = Cy - Ay;
            float s2 = Cx - Ax;
            float s3 = By - Ay;
            float s4 = Py - Ay;

            float w1 = (Ax * s1 + s4 * s2 - Px * s1) / (s3 * s2 - (Bx - Ax) * s1);
            float w2 = (s4 - w1 * s3) / s1;
            return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
        }
        //- is right 
        //+ is left
        public static float LineSide(Vector2 A, Vector2 B, Vector2 P) {
            return (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
        }
        //- is right 
        //+ is left
        public static float LineSide(float Ax, float Ay, float Bx, float By, float pointX, float pointY) {
            return (Bx - Ax) * (pointY - Ay) - (By - Ay) * (pointX - Ax);
        }

        public static bool PointInTriangleSimple(Vector3 A, Vector3 B, Vector3 C, float pointX, float pointZ) {//and little over
            return 
                (LineSide(A.x, A.z, B.x, B.z, pointX, pointZ) <= 0) == 
                (LineSide(B.x, B.z, C.x, C.z, pointX, pointZ) <= 0) == 
                (LineSide(C.x, C.z, A.x, A.z, pointX, pointZ) <= 0);
        }

        public static float CalculateHeight(Vector3 A, Vector3 B, Vector3 C, float x, float z) {
            float det = (B.z - C.z) * (A.x - C.x) + (C.x - B.x) * (A.z - C.z);
            float l1 = ((B.z - C.z) * (x - C.x) + (C.x - B.x) * (z - C.z)) / det;
            float l2 = ((C.z - A.z) * (x - C.x) + (A.x - C.x) * (z - C.z)) / det;
            float l3 = 1.0f - l1 - l2;
            return l1 * A.y + l2 * B.y + l3 * C.y;
        }

        public static List<Vector3> RasterizeTriangleShity(Vector3 A, Vector3 B, Vector3 C, float step) {
            List<Vector3> result = new List<Vector3>();
            for (int x = Mathf.RoundToInt(Math.Min(Math.Min(A.x, B.x), C.x) / step); x < Mathf.RoundToInt(Math.Max(Math.Max(A.x, B.x), C.x) / step); x++) {
                for (int z = Mathf.RoundToInt(Math.Min(Math.Min(A.z, B.z), C.z) / step); z < Mathf.RoundToInt(Math.Max(Math.Max(A.z, B.z), C.z) / step); z++) {
                    if (PointInTriangle(toV2(A), toV2(B), toV2(C), new Vector2(x * step, z * step)))
                        result.Add(new Vector3(x * step, CalculateHeight(A, B, C, x * step, z * step), z * step));
                }
            }
            return result;
        }

        private static Vector2 toV2(Vector3 pos) {
            return new Vector2(pos.x, pos.z);
        }



        public static Vector3 NearestPointOnLine(float ax, float ay, float az, float bx, float by, float bz, float pointx, float pointy, float pointz) {
            //direction of BA
            bx = bx - ax;
            by = by - ay;
            bz = bz - az;

            //magnitude of BA
            float ABmagnitude = Magnitude(bx, by, bz);

            if (ABmagnitude == 0f) //length of line are 0
                return new Vector3(ax, ay, az);            

            //normalized BA
            bx = bx / ABmagnitude;
            by = by / ABmagnitude;
            bz = bz / ABmagnitude;

            //clamp target length between 0 and magnitude of BA
            float mul = Dot(pointx - ax, pointy - ay, pointz - az, bx, by, bz);
            return new Vector3(ax + bx * mul, ay + by * mul, az + bz * mul);
        }
        public static Vector3 NearestPointOnLine(Vector3 A, Vector3 B, Vector3 point) {
            return NearestPointOnLine(A.x, A.y, A.z, B.x, B.y, B.z, point.x, point.y, point.z);
        }
        public static Vector2 NearestPointOnLine(float ax, float ay, float bx, float by, float pointx, float pointy) {
            //direction of BA
            bx = bx - ax;
            by = by - ay;

            //magnitude of BA
            float ABmagnitude = Magnitude(bx, by);

            if (ABmagnitude == 0f) {//length of line are 0
                return new Vector2(ax, ay);
            }

            //normalized BA
            bx = bx / ABmagnitude;
            by = by / ABmagnitude;

            //clamp target length between 0 and magnitude of BA
            float mul = Dot(pointx - ax, pointy - ay, bx, by);
            return new Vector2(ax + bx * mul, ay + by * mul);
        }
        public static Vector2 NearestPointOnLine(Vector2 A, Vector2 B, Vector2 point) {
            return NearestPointOnLine(A.x, A.y, B.x, B.y, point.x, point.y);
        }

        //Vector3
        public static Vector3 NearestPointOnSegment(float ax, float ay, float az, float bx, float by, float bz, float pointx, float pointy, float pointz) {
            //direction of BA
            bx = bx - ax;
            by = by - ay;
            bz = bz - az;

            //magnitude of BA
            float ABmagnitude = Magnitude(bx, by, bz);

            if (ABmagnitude == 0f) {//length of line are 0
                return new Vector3(ax, ay, az);
            }

            //normalized BA
            bx = bx / ABmagnitude;   
            by = by / ABmagnitude;
            bz = bz / ABmagnitude;

            //clamp target length between 0 and magnitude of BA
            float mul = Mathf.Clamp(Dot(pointx - ax, pointy - ay, pointz - az, bx, by, bz), 0, ABmagnitude);
            return new Vector3(ax + bx * mul, ay + by * mul, az + bz * mul);
        }
        public static Vector3 NearestPointOnSegment(Vector3 A, Vector3 B, Vector3 point) {
            return NearestPointOnSegment(A.x, A.y, A.z, B.x, B.y, B.z, point.x, point.y, point.z);
        }
        //Vector2
        public static Vector2 NearestPointOnSegment(float ax, float ay, float bx, float by, float pointx, float pointy) {
            //direction of BA
            bx = bx - ax;
            by = by - ay;   

            //magnitude of BA
            float ABmagnitude = Magnitude(bx, by);


            if (ABmagnitude == 0f) {//length of line are 0
                return new Vector2(ax, ay);
            }

            //normalized BA
            bx = bx / ABmagnitude;
            by = by / ABmagnitude;

            //clamp target length between 0 and magnitude of BA
            float mul = Mathf.Clamp(Dot(pointx - ax, pointy - ay, bx, by), 0, ABmagnitude);
            return new Vector2(ax + bx * mul, ay + by * mul);
        }
        public static Vector2 NearestPointOnSegment(Vector2 A, Vector2 B, Vector2 point) {
            return NearestPointOnSegment(A.x, A.y, B.x, B.y, point.x, point.y);
        }
                
        public static float TriangleArea(Vector2 a, Vector2 b, Vector2 c) {
            return Math.Abs(Vector3.Cross(b - a, c - a).z) * 0.5f;
        }

        #region not-a-k_math-still-good
        //public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

        //    Vector3 vector = linePoint2 - linePoint1;

        //    Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

        //    int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);
        //    Debug.Log(side);

        //    //The projected point is on the line segment
        //    if (side == 0) {

        //        return projectedPoint;
        //    }

        //    if (side == 1) {

        //        return linePoint1;
        //    }

        //    if (side == 2) {

        //        return linePoint2;
        //    }

        //    //output is invalid
        //    return Vector3.zero;
        //}
        //public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {

        //    //get vector from point on line to point in space
        //    Vector3 linePointToPoint = point - linePoint;

        //    float t = Vector3.Dot(linePointToPoint, lineVec);

        //    return linePoint + lineVec * t;
        //}
        //public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

        //    Vector3 lineVec = linePoint2 - linePoint1;
        //    Vector3 pointVec = point - linePoint1;

        //    float dot = Vector3.Dot(pointVec, lineVec);

        //    //point is on side of linePoint2, compared to linePoint1
        //    if (dot > 0) {

        //        //point is on the line segment
        //        if (pointVec.magnitude <= lineVec.magnitude) {

        //            return 0;
        //        }

        //        //point is not on the line segment and it is on the side of linePoint2
        //        else {

        //            return 2;
        //        }
        //    }

        //    //Point is not on side of linePoint2, compared to linePoint1.
        //    //Point is not on the line segment and it is on the side of linePoint1.
        //    else {

        //        return 1;
        //    }
        //}
        #endregion


        public static Vector3 MidPoint(params Vector3[] input) {
            Vector3 output = Vector3.zero;
            foreach (var item in input)
                output += item;

            return output / input.Length;
        }        
        public static Vector3 MidPoint(IEnumerable<Vector3> input) {
            Vector3 output = Vector3.zero;
            foreach (var item in input)
                output += item;

            return output / input.Count();
        }
        public static Vector2 MidPoint(params Vector2[] input) {
            Vector2 output = Vector2.zero;
            foreach (var item in input)
                output += item;

            return output / input.Length;
        }
        public static Vector2 MidPoint(IEnumerable<Vector2> input) {
            Vector2 output = Vector2.zero;
            foreach (var item in input)
                output += item;

            return output / input.Count();
        }

        public static List<Vector2> DouglasPeucker(List<Vector2> points, int startIndex, int lastIndex, float epsilon) {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i) {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon) {
                var res1 = DouglasPeucker(points, startIndex, index, epsilon);
                var res2 = DouglasPeucker(points, index, lastIndex, epsilon);

                var finalRes = new List<Vector2>();
                for (int i = 0; i < res1.Count - 1; ++i) {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i) {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else {
                return new List<Vector2>(new Vector2[] { points[startIndex], points[lastIndex] });
            }
        }

        public static float PointLineDistance(Vector2 point, Vector2 start, Vector2 end) {
            if (start == end) {
                return Vector2.Distance(point, start);
            }

            float n = Mathf.Abs((end.x - start.x) * (start.y - point.y) - (start.x - point.x) * (end.y - start.y));
            float d = Mathf.Sqrt((end.x - start.x) * (end.x - start.x) + (end.y - start.y) * (end.y - start.y));

            return n / d;
        }

        public static List<Vector3> DouglasPeucker(List<Vector3> points, int startIndex, int lastIndex, float epsilon) {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i) {
                float d = Vector3.Distance(NearestPointOnLine(points[startIndex], points[lastIndex], points[i]), points[i]);
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon) {
                var res1 = DouglasPeucker(points, startIndex, index, epsilon);
                var res2 = DouglasPeucker(points, index, lastIndex, epsilon);

                var finalRes = new List<Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i) {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i) {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else {
                return new List<Vector3>(new Vector3[] { points[startIndex], points[lastIndex] });
            }
        }



        #region projection
        public static Vector3 ClosestToLineTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f)
                Debug.LogError("Lines are paralel");

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;

            return lineA + lineVec1 * s;
        }

        public static bool ClosestToSegmentTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point, out Vector3 intersection) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f) {
                intersection = Vector3.zero;
                return false;
            }

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;

            intersection = lineA + lineVec1 * s;
            return s >= 0 & s <= 1f;
        }

        public static bool ClosestToSegmentTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point, bool clamp, out Vector3 intersection) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f) {
                intersection = Vector3.zero;
                return false;
            }

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;


            if (clamp) {
                s = Mathf.Clamp01(s);
                intersection = lineA + lineVec1 * s;
                return true;
            }
            else {
                intersection = lineA + lineVec1 * s;
                return s >= 0 & s <= 1f;
            }
        }

        public static bool TwoLinesProjectionByX(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, float maxDist, out Vector2 minus, out Vector2 plus) {
            minus = Vector2.zero;
            plus = Vector2.zero;

            if (a1.x == a2.x || b1.x == b2.x)//cause nono
                return false;

            //1.x < 2.x
            if (a1.x > a2.x) {
                Vector2 temp = a2;
                a2 = a1;
                a1 = temp;
            }

            if (b1.x > b2.x) {
                Vector2 temp = b2;
                b2 = b1;
                b1 = temp;
            }

            //if we dont overlap by X then no-no
            if ((TLP_InRange(a1.x, a2.x, b1.x) || TLP_InRange(a1.x, a2.x, b2.x) || TLP_InRange(b1.x, b2.x, a1.x) || TLP_InRange(b1.x, b2.x, a2.x)) == false)
                return false;


            Vector2? resultMinus = null, resultPlus = null;

            Vector2 point;
            if (TLP_InRange(a1.x, a2.x, b1.x) && TLP_Project(a1, a2, b1, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(a1.x, a2.x, b2.x) && TLP_Project(a1, a2, b2, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(b1.x, b2.x, a1.x) && TLP_Project(b1, b2, a1, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(b1.x, b2.x, a2.x) && TLP_Project(b1, b2, a2, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (resultMinus != null && resultMinus.Value != resultPlus.Value) {
                minus = resultMinus.Value;
                plus = resultPlus.Value;
                return true;
            }
            else
                return false;
        }

        private static bool TLP_InRange(float rangeStart, float rangeEnd, float value) {
            return value >= rangeStart && value <= rangeEnd;
        }
        private static bool TLP_Project(Vector2 left, Vector2 right, Vector2 projectPoint, float maxDist, out Vector2 point) {
            float d = right.x - left.x;
            float ppd = projectPoint.x - left.x;

            float t1 = ppd / d;
            float lineY = (right.y - left.y) * t1 + left.y;

            point = new Vector2(projectPoint.x, (lineY + projectPoint.y) * 0.5f);
            return Math.Abs(projectPoint.y - lineY) <= maxDist;
        }

        //private static bool TLP_Projec_DO_ME(Vector2 left, Vector2 right, Vector2 pp, Vector2 ppTarget, float maxDist, out Vector2 point) {
        //    Debuger.AddDot(left, Color.yellow);
        //    Debuger.AddDot(right, Color.yellow);
        //    Debuger.AddDot(ppTarget, Color.magenta);

        //    point = Vector2.zero;

        //    if (ppTarget.x >= left.x | ppTarget.x <= right.x) {
        //        float d = right.x - left.x;
        //        float ppd = ppTarget.x - left.x;

        //        float lineD = ppd / d;
        //        float lineY = (right.y - left.y) * lineD + left.y;

        //        point = new Vector2(ppTarget.x, (lineY + ppTarget.y) * 0.5f);
        //        Debuger.AddDot(point, Color.cyan);

        //        if (Mathf.Abs(lineY - ppTarget.y) < maxDist)
        //            return true;
        //    }

        //    //Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2

        //    Vector2 v1 = right - left;
        //    Vector2 v2 = ppTarget - pp;

        //    float denominator = (v1.y * v2.x - v1.x * v2.y);

        //    if (denominator == 0) 
        //        return false;

        //    float t1 = ((left.x - pp.x) * v2.y + (pp.y - left.y) * v2.x) / denominator;
        //    Vector2 intersection = new Vector2(left.x + v1.x * t1, left.y + v1.y * t1);

        //    Vector2 targetDir = t1 < 0.5f ? Vector2.right : Vector2.left;

        //    Debuger.AddRay(intersection, targetDir, Color.red);
        //    Debuger.AddDot(intersection, Color.red);

        //    float angle1 = Vector2.Angle(v1, targetDir);
        //    float angle2 = Vector2.Angle(v2, targetDir);

        //    Vector2 upper, lower;
        //    float angle;

        //    if (angle1 > angle2) {
        //        angle = angle1;
        //        upper = v1;
        //        lower = v2;
        //    }
        //    else {
        //        angle = angle2;
        //        upper = v2;
        //        lower = v1;
        //    }

        //    float ulAngle = Vector2.Angle(v1, v2);

        //    upper = upper.normalized;
        //    lower = lower.normalized;

        //    return false;

        //    //Vector2 intersection;
        //    //if(LineIntersection3(left, right, pp, ppTarget, out intersection)) {
        //    //    Debuger.AddDot(intersection, Color.red);


        //    //}

        //    //point = Vector2.zero;

        //    //Vector2 r = new Vector2(1, 0);

        //    //Debuger.AddRay(left, r, Color.red);

        //    //Vector2 v1 = right - left;
        //    //Vector2 v2 = ppTarget - left;

        //    //float angle1 = Vector2.Angle(v1, r);
        //    //float angle2 = Vector2.Angle(v2, r);

        //    //Vector2 upper, lower;
        //    //float angle;

        //    //if (angle1 > angle2) {
        //    //    angle = angle1;
        //    //    upper = v1;
        //    //    lower = v2;
        //    //}
        //    //else {
        //    //    angle = angle2;
        //    //    upper = v2;
        //    //    lower = v1;
        //    //}

        //    //float ulAngle = Vector2.Angle(v1, v2);

        //    //upper = upper.normalized;
        //    //lower = lower.normalized;


        //    //var c = (1d / Math.Sin(angle * Mathf.Deg2Rad));

        //    //var topAngle = Vector2.Angle(Vector2.up, upper);
        //    //var a = maxDist * Math.Sin(topAngle * Mathf.Deg2Rad);
        //    //var c2 = a / Math.Sin(ulAngle * Mathf.Deg2Rad);

        //    //Debuger.AddLabel(left, topAngle);
        //    //Debuger.AddDot(left + (lower * (float)c2), Color.blue);
        //    //Debuger.AddRay(left + (lower * (float)c2), Vector2.up, Color.red, maxDist);


        //}

        public static bool TwoLinesProjectionX(Vector2 lineA1, Vector2 lineA2, Vector2 lineB1, Vector2 lineB2, float maxDistance, out Vector2 intersectionA, out Vector2 intersectionB) {
            SortedList<float, Vector2> results = new SortedList<float, Vector2>();

            ProjectionHelper(lineA1, lineA2, lineB1, maxDistance, ref results);
            ProjectionHelper(lineA1, lineA2, lineB2, maxDistance, ref results);
            ProjectionHelper(lineB1, lineB2, lineA1, maxDistance, ref results);
            ProjectionHelper(lineB1, lineB2, lineA2, maxDistance, ref results);

            if (results.Count == 2) {
                intersectionA = results.First().Value;
                intersectionB = results.Last().Value;
                return true;
            }

            if (results.Count > 2) {
                intersectionA = results.First().Value;
                intersectionB = results.Last().Value;
                Debug.Log("wat");
                return false;
            }

            intersectionA = Vector2.zero;
            intersectionB = Vector2.zero;
            return false;
        }

        private static void ProjectionHelper(Vector2 lineA, Vector2 lineB, Vector2 point, float maxDistance, ref SortedList<float, Vector2> list) {
            Vector2 intersection;
            if (point.x == lineA.x) {
                intersection = new Vector2(point.x, (point.y + lineA.y) * 0.5f);
                goto ADD_TO_LIST;
            }

            if (point.x == lineB.x) {
                intersection = new Vector2(point.x, (point.y + lineB.y) * 0.5f);
                goto ADD_TO_LIST;
            }

            Vector2 lineDirVec = lineB - lineA;
            Vector2 pointDirVec = point - lineA;

            if (lineDirVec.x > 0f ? InRangeExclusive(pointDirVec.x, 0f, lineDirVec.x) : InRangeExclusive(pointDirVec.x, lineDirVec.x, 0f)) {
                float val = pointDirVec.x / lineDirVec.x;
                if (Math.Abs((lineDirVec.y * val) - pointDirVec.y) < maxDistance) {
                    intersection = new Vector2(lineDirVec.x * val, ((lineDirVec.y * val) + pointDirVec.y) * 0.5f) + lineA;
                    goto ADD_TO_LIST;
                }
                else
                    return;
            }
            else
                return;

            ADD_TO_LIST:
            {
                if (list.ContainsKey(intersection.x) == false)
                    list.Add(intersection.x, intersection);
            }
        }
        #endregion

        //public static bool ClosestToLineTopProjection(Vector2 point, Vector3 a, Vector3 b, float maxDifY, out Vector3 intersection) {
        //    Vector2 linePointA = new Vector2(a.x, a.z);
        //    Vector2 linePointB = new Vector2(b.x, b.z);

        //    linePointB = linePointA - linePointB;
        //    linePointB.Normalize();//this needs to be a unit vector
        //    Vector2 v = point - linePointA;
        //    var d = Vector2.Dot(v, linePointB);
        //    intersection = a + ((a - b).normalized * d);
        //    return true;
        //}





        private static bool RayIntersectXZ_for_troubleshooting(
            float rayOriginX, float rayOriginZ, float rayDirectionX, float rayDirectionZ,
            float lineA_x, float lineA_y, float lineA_z, float lineB_x, float lineB_y, float lineB_z,
            out Vector3 intersectRay, out Vector3 intersectLine, out float tVal, out float dot) {
            float lineDir_x = lineB_x - lineA_x;
            float lineDir_y = lineB_y - lineA_y;
            float lineDir_z = lineB_z - lineA_z;
            float denominator = (lineDir_z * rayDirectionX - lineDir_x * rayDirectionZ);

            //paralel
            if (denominator == 0) {
                intersectLine = new Vector3();
                intersectRay = new Vector3();
                tVal = 0;
                dot = 0;
                return false;
            }

            tVal = ((lineA_x - rayOriginX) * rayDirectionZ + (rayOriginZ - lineA_z) * rayDirectionX) / denominator;

            intersectLine = new Vector3(
                lineA_x + (lineDir_x * tVal),
                lineA_y + (lineDir_y * tVal),
                lineA_z + (lineDir_z * tVal));

            float tValClamped = SomeMath.Clamp(0f, 1f, tVal);

            intersectRay = new Vector3(
                lineA_x + (lineDir_x * tValClamped),
                lineA_y + (lineDir_y * tValClamped),
                lineA_z + (lineDir_z * tValClamped));

            dot =
                (rayDirectionX * (intersectLine.x - rayOriginX)) +
                (rayDirectionZ * (intersectLine.z - rayOriginZ));

            return tVal >= 0f && tVal <= 1f && dot > 0;
        }


        #region something intersect something
        #region line intersect line

        #endregion

        #region ray intersect segment
        public static bool RayIntersectSegment(
            float rayOrigin_x, float rayOrigin_y, 
            float rayDirection_x, float rayDirection_y,
            float segmentA_x, float segmentA_y, 
            float segmentB_x, float segmentB_y,
            out float intersect_x, 
            out float intersect_y) {
            float lineDir_x = segmentB_x - segmentA_x;
            float lineDir_y = segmentB_y - segmentA_y;
            float denominator = lineDir_y * rayDirection_x - lineDir_x * rayDirection_y;

            //paralel
            if (denominator == 0) {
                intersect_x = intersect_y = 0;
                return false;
            }

            float t = ((segmentA_x - rayOrigin_x) * rayDirection_y + (rayOrigin_y - segmentA_y) * rayDirection_x) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = segmentA_x + (lineDir_x * t);
                intersect_y = segmentA_y + (lineDir_y * t);

                float dot =
                    (rayDirection_x * (intersect_x - rayOrigin_x)) +
                    (rayDirection_y * (intersect_y - rayOrigin_y));

                if (dot > 0)
                    return true;
                else {
                    intersect_x = intersect_y = 0;
                    return false;
                }
            }
            else {
                intersect_x = intersect_y = 0;
                return false;
            }
        }

        public static bool RayIntersectSegment(Vector2 rayOrigin, Vector2 rayDirection, Vector2 segmentA, Vector2 segmentB, out Vector2 intersection) {
            float resultX, resultY;
            bool result = RayIntersectSegment(rayOrigin.x, rayOrigin.y, rayDirection.x, rayDirection.y, segmentA.x, segmentA.y, segmentB.x, segmentB.y, out resultX, out resultY);
            intersection = new Vector2(resultX, resultY);
            return result;
        }

        /// <summary>
        /// slightly simplified version for cases when ray starts from 0,0
        /// </summary>
        public static bool RayIntersectSegment(         
            float rayDirectionX, float rayDirectionY,
            float segmentA_x, float segmentA_y,
            float segmentB_x, float segmentB_y,
            out float intersect_x,
            out float intersect_y) {
            float lineDir_x = segmentB_x - segmentA_x;
            float lineDir_y = segmentB_y - segmentA_y;
            float denominator = lineDir_y * rayDirectionX - lineDir_x * rayDirectionY;

            //paralel
            if (denominator == 0) {
                intersect_x = intersect_y = 0;
                return false;
            }

            float t = (segmentA_x * rayDirectionY + -segmentA_y * rayDirectionX) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = segmentA_x + (lineDir_x * t);
                intersect_y = segmentA_y + (lineDir_y * t);

                float dot =
                    (rayDirectionX * intersect_x) +
                    (rayDirectionY * intersect_y);

                if (dot > 0)
                    return true;
                else {
                    intersect_x = intersect_y = 0;
                    return false;
                }
            }
            else {
                intersect_x = intersect_y = 0;
                return false;
            }
        }
        /// <summary>
        /// slightly simplified version for cases when ray starts from 0,0
        /// </summary>
        public static bool RayIntersectSegment(Vector2 rayDirection, Vector2 segmentA, Vector2 segmentB, out Vector2 intersection) {
            float resultX, resultY;
            bool result = RayIntersectSegment(rayDirection.x, rayDirection.y, segmentA.x, segmentA.y, segmentB.x, segmentB.y, out resultX, out resultY);
            intersection = new Vector2(resultX, resultY);
            return result;
        }
        #endregion

        #region line intersect segment
        public static bool LineIntersectSegment(
            float lineA_x, float lineA_y,
            float lineB_x, float lineB_y,
            float segmentA_x, float segmentA_y,
            float segmentB_x, float segmentB_y,
            out float intersect_x,
            out float intersect_y) {
            float lineDir_x = segmentB_x - segmentA_x;
            float lineDir_y = segmentB_y - segmentA_y;
            float denominator = lineDir_y * lineB_x - lineDir_x * lineB_y;

            //paralel
            if (denominator == 0) {
                intersect_x = intersect_y = 0;
                return false;
            }

            float t = ((segmentA_x - lineA_x) * lineB_y + (lineA_y - segmentA_y) * lineB_x) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = segmentA_x + (lineDir_x * t);
                intersect_y = segmentA_y + (lineDir_y * t);
                return true;
            }
            else {
                intersect_x = intersect_y = 0;
                return false;
            }
        }

        public static bool LineIntersectSegment(Vector2 rayOrigin, Vector2 rayDirection, Vector2 segmentA, Vector2 segmentB, out Vector2 intersection) {
            float resultX, resultY;
            bool result = LineIntersectSegment(rayOrigin.x, rayOrigin.y, rayDirection.x, rayDirection.y, segmentA.x, segmentA.y, segmentB.x, segmentB.y, out resultX, out resultY);
            intersection = new Vector2(resultX, resultY);
            return result;
        }

        /// <summary>
        /// slightly simplified version for cases when line starts from 0,0
        /// </summary>
        public static bool LineIntersectSegment(
            float line_x, float line_y,
            float segmentA_x, float segmentA_y,
            float segmentB_x, float segmentB_y,
            out float intersect_x,
            out float intersect_y) {
            float lineDir_x = segmentB_x - segmentA_x;
            float lineDir_y = segmentB_y - segmentA_y;
            float denominator = lineDir_y * line_x - lineDir_x * line_y;

            //paralel
            if (denominator == 0) {
                intersect_x = intersect_y = 0;
                return false;
            }

            float t = (segmentA_x * line_y + -segmentA_y * line_x) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = segmentA_x + (lineDir_x * t);
                intersect_y = segmentA_y + (lineDir_y * t);
                return true; 
            }
            else {
                intersect_x = intersect_y = 0;
                return false;
            }
        }
        /// <summary>
        /// slightly simplified version for cases when line starts from 0,0
        /// </summary>
        public static bool LineIntersectSegment(Vector2 line, Vector2 segmentA, Vector2 segmentB, out Vector2 intersection) {
            float resultX, resultY;
            bool result = LineIntersectSegment(line.x, line.y, segmentA.x, segmentA.y, segmentB.x, segmentB.y, out resultX, out resultY);
            intersection = new Vector2(resultX, resultY);
            return result;
        }
        #endregion

        #region ray intersect 3d segment with top projection
        public static bool RayIntersectXZ(Vector2 rayOrigin, Vector2 rayDirection, Vector2 segmentA, Vector2 segmentB, out Vector2 lineIntersection) {
            float intersectX, intersectY, intersectZ;
            bool result = RayIntersectXZ(rayOrigin.x, rayOrigin.y, rayDirection.x, rayDirection.y, segmentA.x, 0, segmentA.y, segmentB.x, 0, segmentB.y, out intersectX, out intersectY, out intersectZ);
            lineIntersection = result ? new Vector2(intersectX, intersectZ) : new Vector2();
            return result;
        }

        public static bool RayIntersectXZ(Vector3 rayOrigin, Vector3 rayDirection, Vector3 segmentA, Vector3 segmentB, out Vector3 lineIntersection) {
            float intersectX, intersectY, intersectZ;
            bool result = RayIntersectXZ(rayOrigin.x, rayOrigin.z, rayDirection.x, rayDirection.z, segmentA.x, segmentA.y, segmentA.z, segmentB.x, segmentB.y, segmentB.z, out intersectX, out intersectY, out intersectZ);
            lineIntersection = result ? new Vector3(intersectX, intersectY, intersectZ) : new Vector3();
            return result;
        }


        public static bool RayIntersectXZ(Vector2 rayOrigin, Vector2 rayDirection, Vector3 segmentA, Vector3 segmentB, out Vector3 lineIntersection) {
            float intersectX, intersectY, intersectZ;
            bool result = RayIntersectXZ(rayOrigin.x, rayOrigin.y, rayDirection.x, rayDirection.y, segmentA.x, segmentA.y, segmentA.z, segmentB.x, segmentB.y, segmentB.z, out intersectX, out intersectY, out intersectZ);
            lineIntersection = result ? new Vector3(intersectX, intersectY, intersectZ) : new Vector3();
            return result;
        }

        public static bool RayIntersectXZ(
            float rayOriginX, float rayOriginZ, float rayDirectionX, float rayDirectionZ,
            float segmentA_x, float segmentA_y, float segmentA_z, float segmentB_x, float segmentB_y, float segmentB_z,
            out float intersect_x, out float intersect_y, out float intersect_z) {
            float lineDir_x = segmentB_x - segmentA_x;
            float lineDir_y = segmentB_y - segmentA_y;
            float lineDir_z = segmentB_z - segmentA_z;
            float denominator = (lineDir_z * rayDirectionX - lineDir_x * rayDirectionZ);

            //paralel
            if (denominator == 0) {
                intersect_x = intersect_y = intersect_z = 0;
                return false;
            }

            float t = ((segmentA_x - rayOriginX) * rayDirectionZ + (rayOriginZ - segmentA_z) * rayDirectionX) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = segmentA_x + (lineDir_x * t);
                intersect_y = segmentA_y + (lineDir_y * t);
                intersect_z = segmentA_z + (lineDir_z * t);

                float dot =
                    (rayDirectionX * (intersect_x - rayOriginX)) +
                    (rayDirectionZ * (intersect_z - rayOriginZ));

                if (dot > 0)
                    return true;
                else {
                    intersect_x = intersect_y = intersect_z = 0;
                    return false;
                }
            }
            else {
                intersect_x = intersect_y = intersect_z = 0;
                return false;
            }
        }

        public static bool RayIntersectXZ(float rayOriginX, float rayOriginZ, float rayDirectionX, float rayDirectionZ, Graphs.CellContentData segment, out float intersect_x, out float intersect_y, out float intersect_z) {
            return RayIntersectXZ(rayOriginX, rayOriginZ, rayDirectionX, rayDirectionZ, segment.xLeft, segment.yLeft, segment.zLeft, segment.xRight, segment.yRight, segment.zRight, out intersect_x, out intersect_y, out intersect_z);
        }
        #endregion
        #endregion


        public static bool ClampedRayIntersectXZ(
        Vector3 rayOrigin, Vector3 rayDirection,
        Vector3 lineA, Vector3 lineB,
        out Vector3 lineIntersection) {

            Vector3 lineDirection = lineB - lineA;
            float denominator = (lineDirection.z * rayDirection.x - lineDirection.x * rayDirection.z);

            //lines are paralel
            if (denominator == 0) {
                lineIntersection = Vector3.zero;
                return false;
            }

            float t1 = ((lineA.x - rayOrigin.x) * rayDirection.z + (rayOrigin.z - lineA.z) * rayDirection.x) / denominator;
            bool result = t1 < 0f || t1 > 1f;
            t1 = Mathf.Clamp01(t1);


            lineIntersection = lineA + (lineDirection * t1);

            //float dot =
            //    (rayDirection.x * (lineIntersection.x - rayOrigin.x)) +
            //    (rayDirection.z * (lineIntersection.z - rayOrigin.z));

            return result;
        }

        public static bool LineIntersectXZ(Vector3 mainLineA, Vector3 mainLineB, Vector3 leadingLineA, Vector3 leadingLineB, out Vector3 lineIntersection) {
            Vector3 mainLineDirection = mainLineB - mainLineA;
            Vector3 leadingLineDirection = leadingLineB - leadingLineA;
            float denominator = (mainLineDirection.z * leadingLineDirection.x - mainLineDirection.x * leadingLineDirection.z);

            //paralel
            if (denominator == 0) {
                lineIntersection = Vector3.zero;
                return false;
            }

            float t = ((mainLineA.x - leadingLineA.x) * leadingLineDirection.z + (leadingLineA.z - mainLineA.z) * leadingLineDirection.x) / denominator;

            if (t >= 0f && t <= 1f) {
                lineIntersection = mainLineA + (mainLineDirection * t);
                float dot = (leadingLineDirection.x * (lineIntersection.x - leadingLineA.x)) + (leadingLineDirection.z * (lineIntersection.z - leadingLineA.z));
                if (dot >= 0 &
                    Vector2.Distance(new Vector2(leadingLineA.x, leadingLineA.z), new Vector2(leadingLineB.x, leadingLineB.z)) >=
                    Vector2.Distance(new Vector2(leadingLineA.x, leadingLineA.z), new Vector2(lineIntersection.x, lineIntersection.z)))
                    return true;
                else {
                    lineIntersection = Vector3.zero;
                    return false;
                }
            }
            else {
                lineIntersection = Vector3.zero;
                return false;
            }
        }

        public static bool LineLineIntersectXZ(Vector3 mainLineA, Vector3 mainLineB, Vector3 leadingLineA, Vector3 leadingLineB, out Vector3 lineIntersection) {
            Vector3 mainLineDirection = mainLineB - mainLineA;
            Vector3 leadingLineDirection = leadingLineB - leadingLineA;
            float denominator = (mainLineDirection.z * leadingLineDirection.x - mainLineDirection.x * leadingLineDirection.z);

            //paralel
            if (denominator == 0) {
                lineIntersection = Vector3.zero;
                return false;
            }

            float t = ((mainLineA.x - leadingLineA.x) * leadingLineDirection.z + (leadingLineA.z - mainLineA.z) * leadingLineDirection.x) / denominator;
            lineIntersection = mainLineA + (mainLineDirection * t);
            return true;
        }

        public static bool LineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
            float ix, iy;
            bool result = LineIntersection(a1.x, a1.y, a2.x, a2.y, b1.x, b1.y, b2.x, b2.y, out ix, out iy);
            intersection = new Vector2(ix, iy);
            return result;
        }

        public static bool LineIntersection(
            float line_1_A_x, float line_1_A_y, //line 1 A
            float line_1_B_x, float line_1_B_y, //line 1 B
            float line_2_A_x, float line_2_A_y, //line 2 A
            float line_2_B_x, float line_2_B_y, //line 2 B
            out float intersectionX, 
            out float intersectionY) {

            float line_1_dir_x = line_1_B_x - line_1_A_x;
            float line_a_dir_y = line_1_B_y - line_1_A_y;

            float line_2_dir_x = line_2_B_x - line_2_A_x;
            float line_2_dir_y = line_2_B_y - line_2_A_y;            

            float d = line_a_dir_y * line_2_dir_x - line_1_dir_x * line_2_dir_y;

            if (d == 0) {
                intersectionX = intersectionY = 0f;
                return false;
            }

            float m = ((line_1_A_x - line_2_A_x) * line_2_dir_y + (line_2_A_y - line_1_A_y) * line_2_dir_x) / d;

            intersectionX = line_1_A_x + line_1_dir_x * m;
            intersectionY = line_1_A_y + line_a_dir_y * m;
            return true;
        }



        public static bool SegmentLineIntersection(Vector2 segment1, Vector2 segment2, Vector2 line1, Vector2 line2, out Vector2 intersection) {
            float ix, iy;
            bool result = SegmentLineIntersection(segment1.x, segment1.y, segment2.x, segment2.y, line1.x, line1.y, line2.x, line2.y, out ix, out iy);
            intersection = new Vector2(ix, iy);
            return result;
        }

        public static bool SegmentLineIntersection(
            float segment1X, float segment1Y,//segment 1
            float segment2X, float segment2Y,//segment 2
            float line1X, float line1Y,      //ray position
            float line2X, float line2Y,      //ray direction
            out float intersectionX, 
            out float intersectionY) {

            float segmentDirX = segment2X - segment1X;
            float segmentDirY = segment2Y - segment1Y;

            float lineDirX = line2X - line1X;
            float lineDirY = line2Y - line1Y;

            float d = (segmentDirY * lineDirX - segmentDirX * lineDirY);

            if (d == 0) {
                intersectionX = intersectionY = 0f;
                return false;
            }

            float t = ((segment1X - line1X) * lineDirY + (line1Y - segment1Y) * lineDirX) / d;

            // Find the point of intersection.
            if (t >= 0f && t <= 1f) {
                intersectionX = segment1X + segmentDirX * t;
                intersectionY = segment1Y + segmentDirY * t;
                return true;
            }
            else {
                intersectionX = intersectionY = 0f;
                return false;
            }
        }

        /**
         * <summary>Computes the signed distance from a line connecting the
         * specified points to a specified point.</summary>
         *
         * <returns>Positive when the point c lies to the left of the line ab.
         * </returns>
         *
         * <param name="a">The first point on the line.</param>
         * <param name="b">The second point on the line.</param>
         * <param name="c">The point to which the signed distance is to be
         * calculated.</param>
         */
        public static float LeftOf(Vector2 a, Vector2 b, Vector2 c) {
            return V2Cross(b - a, a - c);
        }

        public static Bounds FitBounds(Bounds A, Bounds B) {
            Vector3 max = Vector3.Min(A.max, B.max);
            Vector3 min = Vector3.Max(A.min, B.min);
            Vector3 size = max - min;
            return new Bounds(min + (size * 0.5f), size);
        }

        public static Bounds GetBounds(params Vector3[] vectors) {
            if (vectors.Length == 0)
                return new Bounds();

            float minX, maxX, minY, maxY, minZ, maxZ;
            minX = maxX = vectors[0].x;
            minY = maxY = vectors[0].y;
            minZ = maxZ = vectors[0].z;
            for (int i = 1; i < vectors.Length; i++) {
                Vector3 vector = vectors[i];
                minX = Math.Min(minX, vector.x);
                maxX = Math.Max(maxX, vector.x);
                minY = Math.Min(minY, vector.y);
                maxY = Math.Max(maxY, vector.y);
                minZ = Math.Min(minZ, vector.z);
                maxZ = Math.Max(maxZ, vector.z);
            }

            Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            return new Bounds(new Vector3(minX + (size.x * 0.5f), minY + (size.y * 0.5f), minZ + (size.z * 0.5f)), size);
        }

        public static Bounds2D GetBounds2D(params Vector2[] vectors) {
            if (vectors.Length == 0)
                return new Bounds2D();

            float minX, maxX, minY, maxY;
            minX = maxX = vectors[0].x;
            minY = maxY = vectors[0].y;
            for (int i = 1; i < vectors.Length; i++) {
                Vector3 vector = vectors[i];
                minX = Math.Min(minX, vector.x);
                maxX = Math.Max(maxX, vector.x);
                minY = Math.Min(minY, vector.y);
                maxY = Math.Max(maxY, vector.y);
            }

            return new Bounds2D(minX, minY, maxX, maxY);
        }

        public static Vector2[] DrawCircle(int value) {
            Vector2[] result = new Vector2[value];
            for (int i = 0; i < value; ++i) {
                result[i] = new Vector3(
                    (float)Math.Cos(i * 2.0f * Math.PI / value),
                    (float)Math.Sin(i * 2.0f * Math.PI / value));
            }
            return result;
        }

        public static Vector2[] DrawCircle(int value, float radius) {
            Vector2[] result = new Vector2[value];
            for (int i = 0; i < value; ++i) {
                result[i] = new Vector3(
                    (float)Math.Cos(i * 2.0f * Math.PI / value) * radius,
                    (float)Math.Sin(i * 2.0f * Math.PI / value) * radius);
            }
            return result;
        }

        public static Vector3[] DrawCircle(int value, Vector3 position, float radius) {
            Vector3[] result = new Vector3[value];
            for (int i = 0; i < value; ++i) {
                result[i] = new Vector3(
                    (float)Math.Cos(i * 2.0f * Math.PI / value) * radius + position.x,
                    position.y,
                    (float)Math.Sin(i * 2.0f * Math.PI / value) * radius + position.z);
            }
            return result;
        }

        public static Vector3[] DrawCircle(Axises axises, int count, Vector3 position, float radius = 1f) {
            Vector3[] result = new Vector3[count];
        
            for (int i = 0; i < count; ++i) {
                Vector3 vector;
                float v1 = (float)Math.Cos(i * 2.0f * Math.PI / count) * radius;
                float v2 = (float)Math.Sin(i * 2.0f * Math.PI / count) * radius;
                switch (axises) {
                    case Axises.xy:
                        vector = new Vector3(v1 + position.x, v2 + position.y, position.z);
                        break;
                    case Axises.xz:
                        vector = new Vector3(v1 + position.x, position.y, v2 + position.z);
                        break;
                    case Axises.yz:
                        vector = new Vector3(position.x, v1 + position.y, v2 + position.z);
                        break;
                    default:
                        vector = position;
                        break;
                }

                result[i] = vector;
            }
            return result;
        }


        public static Vector2 GetTargetVector(float angle, float length) {
            return new Vector2(
                (float)Math.Cos(angle * Math.PI / 180) * length,
                (float)Math.Sin(angle * Math.PI / 180) * length);
        }


        public static Vector3 ClipLineToPlaneX(Vector3 linePoint, Vector3 lineVectorNormalized, float x) {
            return linePoint + (((x - linePoint.x) / lineVectorNormalized.x) * lineVectorNormalized);
        }
        public static Vector3 ClipLineToPlaneY(Vector3 linePoint, Vector3 lineVectorNormalized, float y) {
            return linePoint + (((y - linePoint.y) / lineVectorNormalized.y) * lineVectorNormalized);
        }
        public static Vector3 ClipLineToPlaneZ(Vector3 linePoint, Vector3 lineVectorNormalized, float z) {
            return linePoint + (((z - linePoint.z) / lineVectorNormalized.z) * lineVectorNormalized);
        }

        public static Vector2 ClipLineToPlaneX(Vector2 linePoint, Vector2 lineVectorNormalized, float x) {
            return linePoint + (((x - linePoint.x) / lineVectorNormalized.x) * lineVectorNormalized);
        }
        public static Vector2 ClipLineToPlaneY(Vector2 linePoint, Vector2 lineVectorNormalized, float y) {
            return linePoint + (((y - linePoint.y) / lineVectorNormalized.y) * lineVectorNormalized);
        }


        public static Vector2 ToVector2(Vector3 vector) {
            return new Vector2(vector.x, vector.z);
        }

        public static Bounds GetCombinedBounds(Bounds[] input) {
            float boundsMinX, boundsMinY, boundsMinZ, boundsMaxX, boundsMaxY, boundsMaxZ;
            Bounds firstBounds = input[0];
            Vector3 center = firstBounds.center;
            Vector3 extends = firstBounds.extents;

            boundsMinX = center.x - extends.x;
            boundsMinY = center.y - extends.y;
            boundsMinZ = center.z - extends.z;
            boundsMaxX = center.x + extends.x;
            boundsMaxY = center.y + extends.y;
            boundsMaxZ = center.z + extends.z;

            for (int i = 1; i < input.Length; i++) {
                Bounds bounds = input[i];
                Vector3 bCenter = bounds.center;
                Vector3 bExtents = bounds.extents;
                boundsMinX = Min(bCenter.x - bExtents.x, boundsMinX);
                boundsMinY = Min(bCenter.y - bExtents.y, boundsMinY);
                boundsMinZ = Min(bCenter.z - bExtents.z, boundsMinZ);
                boundsMaxX = Max(bCenter.x + bExtents.x, boundsMaxX);
                boundsMaxY = Max(bCenter.y + bExtents.y, boundsMaxY);
                boundsMaxZ = Max(bCenter.z + bExtents.z, boundsMaxZ);
            }

            return new Bounds(
                new Vector3((boundsMinX + boundsMaxX) * 0.5f, (boundsMinY + boundsMaxY) * 0.5f, (boundsMinZ + boundsMaxZ) * 0.5f),
                new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, boundsMaxZ - boundsMinZ));

        }
    }
}