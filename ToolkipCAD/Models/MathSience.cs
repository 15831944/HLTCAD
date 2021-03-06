﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MxDrawXLib;

namespace ToolkipCAD.Models
{
    public class MathSience
    {
        // 点到直线的距离
        public static double pointToLineDistance(MxDrawPoint pt1,MxDrawPoint pt2, double x3, double y3)
        {
            double normalLength = Math.Sqrt((pt2.x - pt1.x) * (pt2.x - pt1.x) + (pt2.y - pt1.y) * (pt2.y - pt1.y));
            return Math.Abs((x3 - pt1.x) * (pt2.y - pt1.y) - (y3 - pt1.y) * (pt2.x - pt1.x)) / normalLength;
        }
        public static double DistanceForPointToABLine(double x, double y, MxDrawPoint pt1, MxDrawPoint pt2)//所在点到AB线段的垂线长度
        {
            double reVal = 0d;
            bool retData = false;

            double cross = (pt2.x - pt1.x) * (x - pt1.x) + (pt2.y - pt1.y) * (y - pt1.y);
            if (cross <= 0)
            {
                reVal = (float)Math.Sqrt((x - pt1.x) * (x - pt1.x) + (y - pt1.y) * (y - pt1.y));
                retData = true;
            }

            double d2 = (pt2.x - pt1.x) * (pt2.x - pt1.x) + (pt2.y - pt1.y) * (pt2.y - pt1.y);
            if (cross >= d2)
            {
                reVal = (double)Math.Sqrt((x - pt2.x) * (x - pt2.x) + (y - pt2.y) * (y - pt2.y));
                retData = true;
            }

            if (!retData)
            {
                double r = cross / d2;
                double px = pt1.x + (pt2.x - pt1.x) * r;
                double py = pt1.y + (pt2.y - pt1.y) * r;
                reVal = (double)Math.Sqrt((x - px) * (x - px) + (py - y) * (py - y));
            }

            return reVal;

        }
        public static double PointToSegDist(double x, double y, MxDrawPoint pt1, MxDrawPoint pt2)
        {

            double cross = (pt2.x - pt1.x) * (x - pt1.x) + (pt2.y - pt1.y) * (y - pt1.y);
            if (cross <= 0) return Math.Sqrt((x - pt1.x) * (x - pt1.x) + (y - pt1.y) * (y - pt1.y));
            double d2 = (pt2.x - pt1.x) * (pt2.x - pt1.x) + (pt2.y - pt1.y) * (pt2.y - pt1.y);
            if (cross >= d2) return Math.Sqrt((x - pt2.x) * (x - pt2.x) + (y - pt2.y) * (y - pt2.y));
            double r = cross / d2;
            double px = pt1.x + (pt2.x - pt1.x) * r;
            double py = pt1.y + (pt2.y - pt1.y) * r;
            return Math.Sqrt((x - px) * (x - px) + (py - pt1.y) * (py - pt1.y));
        }
        //点与线的距离
        public static double Distance_point_line(MxDrawPoint l1,MxDrawPoint l2, MxDrawPoint p1)
        {
            double result = 0;
            double A = -(l1.y - l2.y);
            double B = (l1.x - l2.x);
            double C = (l1.y * l2.x - l2.y * l1.x);
            if (A == 0 && B == 0)
            {
                double detax = (l1.x - p1.x);
                double detay = (l1.y - p1.y);
                result = Math.Sqrt(Math.Pow(detax, 2) + Math.Pow(detay, 2));
            }
            else
            {
                result = Math.Abs(A * p1.x + B * p1.y + C) / Math.Sqrt(Math.Pow(A, 2) + Math.Pow(B, 2));
            }
            return result;
        }
        //判断线是否垂直
        public static bool vertical(MxDrawPoint p1,MxDrawPoint p2, MxDrawPoint l1,MxDrawPoint l2)
        {
            if (((p2.y - p1.y) * (l2.y - l1.y) + (p2.x - p1.x) * (l2.x - l1.x)) <= 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //判断线是否平行
        public static bool parallel(MxDrawPoint p1, MxDrawPoint p2, MxDrawPoint l1, MxDrawPoint l2)
        {
            if (Math.Abs((p2.y - p1.y) * (l2.x - l1.x) - (l2.y - l1.y) * (p2.x - p1.x)) <= 1)
                return true;
            else
                return false;
        }
        //两条线的交点
        public static PointF point_intersection(MxDrawPoint p1, MxDrawPoint p2, MxDrawPoint l1, MxDrawPoint l2)
        {
            //p1.x = Math.Round(p1.x,6);
            //p1.y = Math.Round(p1.y, 6);
            //p2.x = Math.Round(p2.x, 6);
            //p2.y = Math.Round(p2.y, 6);
            //l1.x = Math.Round(l1.x, 6);
            //l1.x = Math.Round(l1.y, 6);
            //l2.x = Math.Round(l2.x, 6);
            //l2.x = Math.Round(l2.y, 6);

            double A = -(p1.y - p2.y);
            double B = (p1.x - p2.x);
            double C = (p1.y * p2.x - p2.y * p1.x);

            double A1 = -(l1.y - l2.y);
            double B1 = (l1.x - l2.x);
            double C1 = (l1.y * l2.x - l2.y * l1.x);

            double D = A * B1 - A1 * B;
            if (D != 0)
            {
                double x = (B * C1 - B1 * C) / D;
                double y = (A1 * C - A * C1) / D;

                return new PointF((float)x,(float)y);
            }
            else
            {
                return new PointF(0, 0);
            }
        }
        //判断点是否在直线上
        public static bool GetPointIsInLine(PointF pf, MxDrawPoint p1, MxDrawPoint p2, double range=0)
        {
            //range 判断的的误差，不需要误差则赋值0
            //点在线段首尾两端之外则return false
            double cross = (p2.x - p1.x) * (pf.X - p1.x) + (p2.y - p1.y) * (pf.Y - p1.y);
            if (cross <= 0) return false;
            double d2 = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);
            if (cross >= d2) return false;
            double r = cross / d2;
            double px = p1.x + (p2.x - p1.x) * r;
            double py = p1.y + (p2.y - p1.y) * r;
            //判断距离是否小于误差
            return Math.Sqrt((pf.X - px) * (pf.X - px) + (py - pf.Y) * (py - pf.Y)) <= range;
        }
        //获得线的向量角度
        public static double GetLineK(MxDrawPoint p1,MxDrawPoint p2)
        {
            double k = Math.PI / 2;
            if (p1.x != p2.x)
            {
                double dy = p2.y - p1.y;
                double dx = p2.x - p1.x;
                k = dy / dx;
                k = Math.Atan(k);
                if (dx < 0) k = k + Math.PI;
                if (k < 0) k = k + Math.PI * 2;
            }
            else
            {
                if (p2.y < p1.y) k = Math.PI * 3 / 2;
            }
            return k;
        }
        //求直线夹角
        public static double GetAngle(MxDrawPoint pt,MxDrawPoint p1,MxDrawPoint p2)
        {
            double cosfi = 0, fi = 0, norm = 0;
            double dsx = p1.x - pt.x;
            double dsy = p1.y - pt.y;
            double dex = p2.x - pt.x;
            double dey = p2.y - pt.y;

            cosfi = dsx * dex + dsy * dey;
            norm = (dsx * dsx + dsy * dsy) * (dex * dex + dey * dey);
            cosfi /= Math.Sqrt(norm);

            if (cosfi >= 1.0) return 0;
            if (cosfi <= -1.0) return Math.PI;
            fi = Math.Acos(cosfi);

            if (180 * fi / Math.PI < 180)
            {
                return 180 * fi / Math.PI;
            }
            else
            {
                return 360 - 180 * fi / Math.PI;
            }

        }
        public static double GetAngle2(MxDrawPoint p1, MxDrawPoint p2)
        {
            return Math.Atan2((p2.y - p1.y), (p2.x - p1.x)) * 180 / Math.PI;
        }
        //求两点距离
        public static double GetDistance(double A_x, double A_y, double B_x, double B_y)
        {
            double x = Math.Abs(B_x - A_x);
            double y = Math.Abs(B_y - A_y);
            return Math.Sqrt(x * x + y * y);
        }


    }
}
