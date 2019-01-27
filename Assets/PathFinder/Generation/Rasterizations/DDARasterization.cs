using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Graphs {
    public static class DDARasterization {
        public delegate void DelegateDDA1(int x, int y);
        public delegate bool DelegateDDA2(int x, int y);//if true then stop
        public delegate bool DelegateDDA3(int x, int y, RaycastAllocatedData rad);//special case for raycasting

        /// <summary>
        /// DONT FORGET TO SCALE ALL SO ONE PIXEL IS 1f
        /// </summary>
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, RaycastAllocatedData rad, DelegateDDA3 del) {
            //UnityEngine.Debug.LogFormat("x0: {0}, y0: {1}, x1: {2}, y1: {3}", x0float, y0float, x1float, y1float);

            int x0int = (int)x0float;
            int y0int = (int)y0float;
            int x1int = (int)x1float;
            int y1int = (int)y1float;

            if (x0int == x1int) {
                if (y0int == y1int) {//inside one pixel
                    del(x0int, y0int, rad);
                    return;
                }
                //along x axis
                if (y0int < y1int) {
                    for (int y = y0int; y < y1int + 1; y++) {
                        if (del(x0int, y, rad))
                            return;
                    }
                }
                else {
                    for (int y = y0int; y > y1int - 1; y--) {
                        if (del(x0int, y, rad))
                            return;
                    }
                }
                return;
            }

            if (y0int == y1int) {//along y axis
                if (x0int < x1int) {
                    for (int x = x0int; x < x1int + 1; x++) {
                        if (del(x, y0int, rad))
                            return;
                    }
                }
                else {
                    for (int x = x0int; x > x1int - 1; x--) {
                        if (del(x, y0int, rad))
                            return;
                    }
                }
                return;
            }

            //some funky DDA based line rasterization
            if (SomeMath.InRangeInclusive((x1float - x0float) / (y1float - y0float), -1, 1)) {   //y
                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0y, t1y;

                if (x0float < x1float) {
                    t0y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int + 1, t1y, Color.green, 10f, "t1");
                }
                else {
                    t0y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int + 1, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int, t1y, Color.green, 10f, "t1");
                }

                int xDelta = Math.Sign(x1int - x0int);
                float yDelta = t1y - t0y;
                float curYfloat = t0y;

                if (y1float < y0float) {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int > (int)curYfloat - 1; y0int--) {
                            if (del(x0int, y0int, rad))
                                return;

                            if (y0int == y1int) {
                                del(x1int, y1int, rad);
                                return;
                            }
                        }
                        y0int = (int)curYfloat;
                        x0int += xDelta;

                    }
                }
                else {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int < (int)curYfloat + 1; y0int++) {
                            if (del(x0int, y0int, rad))
                                return;

                            if (y0int == y1int) {
                                if (x0int != x1int)
                                    del(x1int, y1int, rad);
                                return;
                            }
                        }

                        y0int = (int)curYfloat;
                        x0int += xDelta;
                    }
                }
            }
            else {   //x

                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0x, t1x;

                if (y0float < y1float) {
                    t0x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t1x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t0x, y0int, Color.red, 10f, "t0");
                    //DrawCross(t1x, y0int + 1, Color.green, 10f, "t1");
                }
                else {
                    t1x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t0x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t1x, y0int, Color.green, 10f, "t1");
                    //DrawCross(t0x, y0int + 1, Color.red, 10f, "t0");
                }

                int yDelta = Math.Sign(y1int - y0int);
                float xDelta = t1x - t0x;
                float curXfloat = t0x;
                if (x1float < x0float) {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int > (int)curXfloat - 1; x0int--) {
                            if (del(x0int, y0int, rad))
                                return;

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int, rad);
                                return;
                            }
                        }

                        x0int = (int)curXfloat;
                        y0int += yDelta;
                    }
                }
                else {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int < (int)curXfloat + 1; x0int++) {
                            if (del(x0int, y0int, rad))
                                return;

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int, rad);
                                return;
                            }
                        }

                        x0int = (int)curXfloat;
                        y0int += yDelta;
                    }
                }
            }
        }
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, float pixelSize, RaycastAllocatedData rad, DelegateDDA3 del) {
            DrawLine(x0float / pixelSize, y0float / pixelSize, x1float / pixelSize, y1float / pixelSize, rad, del);
        }

        /// <summary>
        /// DONT FORGET TO SCALE ALL SO ONE PIXEL IS 1f
        /// </summary>
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, DelegateDDA2 del) {
            //UnityEngine.Debug.LogFormat("x0: {0}, y0: {1}, x1: {2}, y1: {3}", x0float, y0float, x1float, y1float);

            int x0int = (int)x0float;
            int y0int = (int)y0float;
            int x1int = (int)x1float;
            int y1int = (int)y1float;

            if (x0int == x1int) {
                if (y0int == y1int) {//inside one pixel
                    del(x0int, y0int);
                    return;
                }
                //along x axis
                if (y0int < y1int) {
                    for (int y = y0int; y < y1int + 1; y++) {
                        if (del(x0int, y))
                            return;
                    }
                }
                else {
                    for (int y = y0int; y > y1int - 1; y--) {
                        if (del(x0int, y))
                            return;
                    }
                }
                return;
            }

            if (y0int == y1int) {//along y axis
                if (x0int < x1int) {
                    for (int x = x0int; x < x1int + 1; x++) {
                        if (del(x, y0int))
                            return;
                    }
                }
                else {
                    for (int x = x0int; x > x1int - 1; x--) {
                        if (del(x, y0int))
                            return;
                    }
                }
                return;
            }

            //some funky DDA based line rasterization
            if (SomeMath.InRangeInclusive((x1float - x0float) / (y1float - y0float), -1, 1)) {   //y
                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0y, t1y;

                if (x0float < x1float) {
                    t0y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int + 1, t1y, Color.green, 10f, "t1");
                }
                else {
                    t0y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int + 1, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int, t1y, Color.green, 10f, "t1");
                }

                int xDelta = Math.Sign(x1int - x0int);
                float yDelta = t1y - t0y;
                float curYfloat = t0y;

                if (y1float < y0float) {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int > (int)curYfloat - 1; y0int--) {
                            if (del(x0int, y0int))
                                return;

                            if (y0int == y1int) {
                                del(x1int, y1int);
                                return;
                            }
                        }
                        y0int = (int)curYfloat;
                        x0int += xDelta;

                    }
                }
                else {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int < (int)curYfloat + 1; y0int++) {
                            if (del(x0int, y0int))
                                return;

                            if (y0int == y1int) {
                                if (x0int != x1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        y0int = (int)curYfloat;
                        x0int += xDelta;
                    }
                }
            }
            else {   //x

                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0x, t1x;

                if (y0float < y1float) {
                    t0x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t1x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t0x, y0int, Color.red, 10f, "t0");
                    //DrawCross(t1x, y0int + 1, Color.green, 10f, "t1");
                }
                else {
                    t1x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t0x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t1x, y0int, Color.green, 10f, "t1");
                    //DrawCross(t0x, y0int + 1, Color.red, 10f, "t0");
                }

                int yDelta = Math.Sign(y1int - y0int);
                float xDelta = t1x - t0x;
                float curXfloat = t0x;
                if (x1float < x0float) {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int > (int)curXfloat - 1; x0int--) {
                            if (del(x0int, y0int))
                                return;

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        x0int = (int)curXfloat;
                        y0int += yDelta;
                    }
                }
                else {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int < (int)curXfloat + 1; x0int++) {
                            if (del(x0int, y0int))
                                return;

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        x0int = (int)curXfloat;
                        y0int += yDelta;
                    }
                }
            }
        }
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, float pixelSize, DelegateDDA2 del) {
            DrawLine(x0float / pixelSize, y0float / pixelSize, x1float / pixelSize, y1float / pixelSize, del);
        }

        /// <summary>
        /// DONT FORGET TO SCALE ALL SO ONE PIXEL IS 1f
        /// </summary>
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, DelegateDDA1 del) {
            //Debug.LogFormat("x0: {0}, y0: {1}, x1: {2}, y1: {3}", x0float, y0float, x1float, y1float);

            int x0int = (int)x0float;
            int y0int = (int)y0float;
            int x1int = (int)x1float;
            int y1int = (int)y1float;

            if (x0int == x1int) {
                if(y0int == y1int) {//inside one pixel
                    del(x0int, y0int);
                    return;
                }
                //along x axis
                if (y0int < y1int) {
                    for (int y = y0int; y < y1int + 1; y++) {
                        del(x0int, y);
                    }
                }
                else {
                    for (int y = y1int; y < y0int + 1; y++) {
                        del(x0int, y);
                    }
                }
                return;
            }

            if (y0int == y1int) {//along y axis
                if (x0int < x1int) {
                    for (int x = x0int; x < x1int + 1; x++) {
                        del(x, y0int);
                    }
                }
                else {
                    for (int x = x1int; x < x0int + 1; x++) {
                        del(x, y0int);
                    }
                }
                return;
            }

            //some funky DDA based line rasterization
            if (SomeMath.InRangeInclusive((x1float - x0float) / (y1float - y0float), -1, 1)) {   //y
                if (y1float < y0float) {
                    float temp;
                    temp = x0float;
                    x0float = x1float;
                    x1float = temp;
                    temp = y0float;
                    y0float = y1float;
                    y1float = temp;

                    x0int = (int)x0float;
                    y0int = (int)y0float;
                    x1int = (int)x1float;
                    y1int = (int)y1float;
                }
                //Handles.Label(new Vector2(x0float, y0float) * PIXEL_SIZE, "0");
                //Handles.Label(new Vector2(x1float, y1float) * PIXEL_SIZE, "1");

                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0y, t1y;

                if (x0float < x1float) {
                    t0y = IntersectionY((float)x0int, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY((float)(x0int + 1), x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int + 1, t1y, Color.green, 10f, "t1");
                }
                else {
                    t0y = IntersectionY((float)(x0int + 1), x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY((float)x0int, x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(x0int + 1, t0y, Color.red, 10f, "t0");
                    //DrawCross(x0int, t1y, Color.green, 10f, "t1");              
                }

                int xDelta = Math.Sign((int)(x1int - x0int));
                float yDelta = t1y - t0y;
                float curYfloat = t0y;

                //int curXint = x0int;
                //int curYint = y0int;
                //now its it values
                while (true) {
                    curYfloat += yDelta;
                    //int targetYint = (int)curYfloat;
                    //DrawLineDebug(x0int, y0int, x0int, (int)curYfloat, Color.magenta);      

                    for (; y0int < (int)curYfloat + 1; y0int++) {
                        del(x0int, y0int);
                        //grid[x0int][y0int] = true;

                        if (y0int == y1int) {
                            del(x1int, y1int);
                            //grid[x1int][y1int] = true;
                            return;
                        }
                    }

                    y0int = (int)curYfloat;
                    x0int += xDelta;
                    //DrawCross((curXint + 1), targetYfloat, 10f);
                }
            }
            else {   //x
                if (x1float < x0float) {
                    float temp;
                    temp = x0float;
                    x0float = x1float;
                    x1float = temp;
                    temp = y0float;
                    y0float = y1float;
                    y1float = temp;

                    x0int = (int)x0float;
                    y0int = (int)y0float;
                    x1int = (int)x1float;
                    y1int = (int)y1float;
                }
                //Handles.Label(new Vector2(x0float, y0float) * PIXEL_SIZE, "0");
                //Handles.Label(new Vector2(x1float, y1float) * PIXEL_SIZE, "1");

                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0x, t1x;

                if (y0float < y1float) {
                    t0x = IntersectionX((float)y0int, x0float, y0float, lineDirX, lineDirY);
                    t1x = IntersectionX((float)(y0int + 1), x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t0x, y0int, Color.red, 10f, "t0");
                    //DrawCross(t1x, y0int + 1, Color.green, 10f, "t1");
                }
                else {
                    t1x = IntersectionX((float)y0int, x0float, y0float, lineDirX, lineDirY);
                    t0x = IntersectionX((float)(y0int + 1), x0float, y0float, lineDirX, lineDirY);
                    //DrawCross(t1x, y0int, Color.green, 10f, "t1");
                    //DrawCross(t0x, y0int + 1, Color.red, 10f, "t0");
                }

                int yDelta = Math.Sign((int)(y1int - y0int));
                float xDelta = t1x - t0x;
                float curXfloat = t0x;
                //int curXint = x0int;
                //int curYint = y0int;
                //now its it values                

                while (true) {
                    curXfloat += xDelta;
                    //int targetXint = (int)curXfloat;
                    //DrawLineDebug(x0int, y0int, (int)curXfloat, y0int, Color.magenta);

                    for (; x0int < (int)curXfloat + 1; x0int++) {
                        del(x0int, y0int);

                        if (x0int == x1int) {
                            del(x1int, y1int);
                            return;
                        }
                    }

                    x0int = (int)curXfloat;
                    y0int += yDelta;
                    //DrawCross(targetXfloat, (curYint + 1), 10f);
                }
            }
        }
        public static void DrawLine(float x0float, float y0float, float x1float, float y1float, float pixelSize, DelegateDDA1 del) {
            DrawLine(x0float / pixelSize, y0float / pixelSize, x1float / pixelSize, y1float / pixelSize, del);
        }

        /// <summary>
        /// DONT FORGET TO SCALE ALL SO ONE PIXEL IS 1f
        /// </summary>
        public static void DrawLineFixedMinusValues(float x0float, float y0float, float x1float, float y1float, DelegateDDA1 del) {
            //UnityEngine.Debug.LogFormat("x0: {0}, y0: {1}, x1: {2}, y1: {3}", x0float, y0float, x1float, y1float);

            int x0int = ToInt(x0float);
            int y0int = ToInt(y0float);
            int x1int = ToInt(x1float);
            int y1int = ToInt(y1float);

            if (x0int == x1int) {
                if (y0int == y1int) {//inside one pixel
                    del(x0int, y0int);
                    return;
                }
                //along x axis
                if (y0int < y1int) {
                    for (int y = y0int; y < y1int + 1; y++) {
                        del(x0int, y);
                    }
                }
                else {
                    for (int y = y0int; y > y1int - 1; y--) {
                        del(x0int, y);
                    }
                }
                return;
            }

            if (y0int == y1int) {//along y axis
                if (x0int < x1int) {
                    for (int x = x0int; x < x1int + 1; x++) {
                        del(x, y0int);
                    }
                }
                else {
                    for (int x = x0int; x > x1int - 1; x--) {
                        del(x, y0int);
                    }
                }
                return;
            }

            //some funky DDA based line rasterization
            if (SomeMath.InRangeInclusive((x1float - x0float) / (y1float - y0float), -1, 1)) {   //y
                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0y, t1y;

                if (x0float < x1float) {
                    t0y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawPoint(x0int, t0y, Color.red, "t0");
                    //DrawPoint(x0int + 1, t1y, Color.green, "t1");  
                }
                else {
                    t0y = IntersectionY(x0int + 1, x0float, y0float, lineDirX, lineDirY);
                    t1y = IntersectionY(x0int, x0float, y0float, lineDirX, lineDirY);
                    //DrawPoint(x0int + 1, t0y, Color.red, "t0");
                    //DrawPoint(x0int, t1y, Color.green, "t1");
                }

                int xDelta = Math.Sign(x1int - x0int);
                float yDelta = t1y - t0y;
                float curYfloat = t0y;

                if (y1float < y0float) {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int > ToInt(curYfloat - 1); y0int--) {
                            del(x0int, y0int);

                            if (y0int == y1int) {
                                del(x1int, y1int);
                                return;
                            }
                        }
                        y0int = ToInt(curYfloat);
                        x0int += xDelta;
                        //DrawPoint(x0int, curYfloat, Color.red, "y");
                    }
                }
                else {
                    while (true) {
                        curYfloat += yDelta;

                        for (; y0int < ToInt(curYfloat + 1); y0int++) {
                            del(x0int, y0int);

                            if (y0int == y1int) {
                                if (x0int != x1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        y0int = ToInt(curYfloat);
                        x0int += xDelta;
                        //DrawPoint(x0int, curYfloat, Color.red, "y");
                    }
                }
            }
            else {   //x
                float lineDirX = x1float - x0float;
                float lineDirY = y1float - y0float;
                float t0x, t1x;

                if (y0float < y1float) {
                    t0x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t1x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawPoint(t0x, y0int, Color.red, "t0");
                    //DrawPoint(t1x, y0int + 1, Color.green, "t1");
                }
                else {
                    t1x = IntersectionX(y0int, x0float, y0float, lineDirX, lineDirY);
                    t0x = IntersectionX(y0int + 1, x0float, y0float, lineDirX, lineDirY);
                    //DrawPoint(t1x, y0int, Color.green, "t1");
                    //DrawPoint(t0x, y0int + 1, Color.red, "t0");
                }

                int yDelta = Math.Sign(y1int - y0int);
                float xDelta = t1x - t0x;
                float curXfloat = t0x;
                if (x1float < x0float) {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int > ToInt(curXfloat - 1); x0int--) {
                            del(x0int, y0int);

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        x0int = ToInt(curXfloat);
                        y0int += yDelta;
                        //DrawPoint(curXfloat, y0int, Color.red, "x");
                    }
                }
                else {
                    while (true) {
                        curXfloat += xDelta;

                        for (; x0int < ToInt(curXfloat + 1); x0int++) {
                            del(x0int, y0int);

                            if (x0int == x1int) {
                                if (y0int != y1int)
                                    del(x1int, y1int);
                                return;
                            }
                        }

                        x0int = ToInt(curXfloat);
                        y0int += yDelta;
                        //DrawPoint(curXfloat, y0int, Color.red, "x");
                    }
                }
            }
        }
        public static void DrawLineFixedMinusValues(float x0float, float y0float, float x1float, float y1float, float pixelSize, DelegateDDA1 del) {
            DrawLineFixedMinusValues(x0float / pixelSize, y0float / pixelSize, x1float / pixelSize, y1float / pixelSize, del);
        }

        //return X
        private static float IntersectionX(float y, float lineX1, float lineY1, float lineDirX, float lineDirY) {
            return (-lineX1 * lineDirY + (lineY1 - y) * lineDirX) / -lineDirY;
        }
        //return Y
        private static float IntersectionY(float x, float lineX1, float lineY1, float lineDirX, float lineDirY) {
            return ((x - lineX1) * lineDirY + lineY1 * lineDirX) / lineDirX;
        }
        ////return line
        //private static bool Intersection(Vector2 gridA, Vector2 gridB, Vector2 leadingA, Vector2 leadingB, out Vector2 intersection) {
        //    Vector2 gridDir = gridB - gridA; //main
        //    Vector2 leadingDir = leadingB - leadingA; //leading

        //    float denominator = (gridDir.y * leadingDir.x - gridDir.x * leadingDir.y);

        //    //paralel
        //    if (denominator == 0) {
        //        intersection = Vector3.zero;
        //        return false;
        //    }

        //    float t = ((gridA.x - leadingA.x) * leadingDir.y + (leadingA.y - gridA.y) * leadingDir.x) / denominator;

        //    if (t >= 0f && t <= 1f) {
        //        intersection = gridA + (gridDir * t);
        //        return true;
        //    }
        //    else {
        //        intersection = Vector3.zero;
        //        return false;
        //    }
        //}


        /// <summary>
        /// result of rasterization is in tempMap[x * gridSize + y] 
        /// </summary>
        public static void Rasterize(bool[] tempMap, int gridSize, float pixelSize, float ax, float ay, float bx, float by, float cx, float cy, float gridX, float gridY) {
            Rasterize(tempMap, gridSize, pixelSize, ax - gridX, ay - gridY, bx - gridX, by - gridY, cx - gridX, cy - gridY);
        }

        /// <summary>
        /// result of rasterization is in tempMap[x * gridSize + y] 
        /// </summary>
        public static void Rasterize(bool[] tempMap, int gridSize, float pixelSize, float ax, float ay, float bx, float by, float cx, float cy) {
            Rasterize(tempMap, gridSize, ax / pixelSize, ay / pixelSize, bx / pixelSize, by / pixelSize, cx / pixelSize, cy / pixelSize);
        }

        /// <summary>
        /// result of rasterization is in tempMap[x * gridSize + y] 
        /// </summary>
        public static void Rasterize(bool[] tempMap, int gridSize, float ax, float ay, float bx, float by, float cx, float cy) {
            float maxSize = gridSize;
            if ((ax < 0 & bx < 0 & cx < 0) | (ax > maxSize & bx > maxSize & cx > maxSize) | //outside by X
               (ay < 0 & by < 0 & cy < 0) | (ay > maxSize & by > maxSize & cy > maxSize)) { //outside by Y           
                return;
            }

            //fixing if all positions inide one pixel
            if (ax > 0 & ax < maxSize & ay > 0 & ay < maxSize)
                tempMap[(int)ax * gridSize + (int)ay] = true;
            if (bx > 0 & bx < maxSize & by > 0 & by < maxSize)
                tempMap[(int)bx * gridSize + (int)by] = true;
            if (cx > 0 & cx < maxSize & cy > 0 & cy < maxSize)
                tempMap[(int)cx * gridSize + (int)cy] = true;

            DelegateDDA1 del = (x, y) => {
                tempMap[Mathf.Clamp(x, 0, gridSize - 1) * gridSize + Mathf.Clamp(y, 0, gridSize - 1)] = true;
            };

            DrawLine(ax, ay, bx, by, del);
            DrawLine(bx, by, cx, cy, del);
            DrawLine(cx, cy, ax, ay, del);

            for (int x = 0; x < gridSize; x++) {
                bool exist = true;
                int first = 0;
                int last = 0;

                for (int y = 0; y < gridSize; y++) {
                    if (tempMap[x * gridSize + y]) {
                        first = y;
                        exist = true;
                        break;
                    }
                }

                for (int y = gridSize - 1; y != 0; y--) {
                    if (tempMap[x * gridSize + y]) {
                        last = y;
                        break;
                    }
                }

                if (exist) {
                    if (first == last)
                        continue;

                    for (int y = first; y < last; y++) {
                        tempMap[x * gridSize + y] = true;
                    }
                }
            }
        }

        private static int ToInt(float f) {
            return f < 0f ? (int)f - 1 : (int)f;
        }

        //private static void MakeLine(int gridSize, float pixelSize, bool[][] grid, Vector2 A, Vector2 B) {
        //    float maxSize = gridSize * pixelSize;
        //    //x
        //    for (int x = 1; x < gridSize; x++) {
        //        float curx = x * pixelSize;
        //        Vector2 gridA = new Vector2(curx, 0);
        //        Vector2 gridB = new Vector2(curx, pixelSize * gridSize);

        //        Vector2 intersection;
        //        if (Intersection(A, B, gridA, gridB, out intersection)) {
        //            if (intersection.y < 0)
        //                grid[x][0] = true;
        //            if (intersection.y > maxSize)
        //                grid[x][gridSize - 1] = true;
        //            else {
        //                int y = Mathf.Min((int)(intersection.y / pixelSize), gridSize - 1);
        //                grid[x][y] = true;
        //                grid[x - 1][y] = true;
        //            }
        //        }
        //    }

        //    //y
        //    for (int y = 1; y < gridSize; y++) {
        //        float curY = y * pixelSize;
        //        Vector2 gridA = new Vector2(0, curY);
        //        Vector2 gridB = new Vector2(pixelSize * gridSize, curY);

        //        Vector2 intersection;
        //        if (Intersection(A, B, gridA, gridB, out intersection)) {
        //            if (intersection.x < 0)
        //                grid[0][y] = true;
        //            if (intersection.x > maxSize)
        //                grid[gridSize - 1][y] = true;
        //            else {
        //                int x = Mathf.Min((int)(intersection.x / pixelSize), gridSize - 1);
        //                grid[x][y] = true;
        //                grid[x][y - 1] = true;
        //            }
        //        }
        //    }
        //}


    }
}