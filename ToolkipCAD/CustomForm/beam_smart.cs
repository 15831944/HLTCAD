﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MxDrawXLib;
using Newtonsoft.Json;
using ToolkipCAD.Models;

namespace ToolkipCAD
{
    public partial class beam_smart : Form
    {
        //与主窗体交互的委托
        public delegate object TransForm(Object param);
        public event TransForm transf;
        public Beam_XRrecord beam = new Beam_XRrecord();
        public beam_smart()
        {
            InitializeComponent();
        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void beam_smart_Load(object sender, EventArgs e)
        {
            select_range.SelectedIndex = 0;
            Line_Get.SelectedIndex = 0;
            wiff_Get.SelectedIndex = 0;
            Msg_Get.SelectedIndex = 0;
            if (this.Tag != null)
            {
                dynamic tag = this.Tag;
                List<Drawing_Manage> ComboSource = ((List<Drawing_Manage>)tag.list).Where(x => x.type == Drawing_type.配置).ToList();
                combox_peizhi.DataSource = ComboSource;
                combox_peizhi.ValueMember = "id";
                combox_peizhi.DisplayMember = "name";
                if (tag.json != "null")
                {
                    beam = JsonConvert.DeserializeObject<Beam_XRrecord>(tag.json); //tag.json as Beam_XRrecord;
                    if (beam.pto != null) { select_range.Text = "显示"; transf("Range"); }
                    //if (beam.side_lines != null) Line_Get.Text = "显示";
                    //if (beam.dim_texts != null) wiff_Get.Text = "显示";
                    //if (beam.seat_lines != null) Msg_Get.Text = "显示";
                    combox_Hnt.Text = beam.Concrete_type;
                    combox_Lzj.Text = beam.Rebar_type;
                    combox_Lgj.Text = beam.Strup_type;
                    combox_kzdj.Text = beam.earth_type;
                    Drawing_Manage Dlist = ComboSource.Find(x => x.id == beam.Drawing_Manage_id);
                    combox_peizhi.Text = Dlist == null ? "" : Dlist.name;
                    if (beam.overmm != null)
                    {
                        check_gj.Checked = true;
                        over_box.Text = beam.overmm;
                    }
                    if (beam.Rebar_overmm != null) combox_Gjcy.Text = beam.Rebar_overmm;
                }
            }
        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            this.Close();

        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (check_hebin.Checked)
            {

            }
            if (check_gj.Checked)//超过N mm
            {
                beam.overmm = over_box.Text;
                beam.Rebar_overmm = combox_Gjcy.Text;
            }
            beam.Concrete_type = combox_Hnt.Text;
            beam.Rebar_type = combox_Lzj.Text;
            beam.Strup_type = combox_Lgj.Text;
            beam.earth_type = combox_kzdj.Text;
            beam.Drawing_Manage_id = combox_peizhi.SelectedValue != null ? combox_peizhi.SelectedValue.ToString() : "";
            beam.beams = new List<Beam>();
            FillBeamStruct();//beam识别
            //transf("SaveData"); //保存           
            //this.Close(); 
        }

        private void select_range_SelectedIndexChanged(object sender, EventArgs e)
        {
            //识别范围
            dynamic Kven;
            if (select_range.Text == "窗口")
            {
                Kven = transf("select_range");
                if (Kven != null)
                {
                    //select_range.Text = "显示";
                    //select_range.Tag = Kven;
                    beam.pto = new List<Point3d> { new Point3d { X = Kven.Lx, Y = Kven.Ly }, new Point3d { X = Kven.Rx, Y = Kven.Ry } };
                    return;
                }
            }
            if (select_range.Text == "显示") transf("Range");
        }

        private void Line_Get_SelectedIndexChanged(object sender, EventArgs e)
        {
            //梁拾取
            if (Line_Get.Text == "拾取")
            {
                object Kven = transf("change_line");
                if (Kven != null)
                {
                    Line_Get.Text = "显示";
                }
            }
            if (Line_Get.Text == "显示")
            {
                wiff_Get.Text = "请选择";
                Msg_Get.Text = "请选择";
                transf("show_line");
            }
        }

        private void wiff_Get_SelectedIndexChanged(object sender, EventArgs e)
        {
            //标注拾取
            if (wiff_Get.Text == "拾取")
            {
                object Kven = transf("change_dim");
                if (Kven != null)
                {
                    wiff_Get.Text = "显示";
                }
            }
            if (wiff_Get.Text == "显示")
            {
                Line_Get.Text = "请选择";
                Msg_Get.Text = "请选择";
                transf("show_dims");
            }
        }

        private void Msg_Get_SelectedIndexChanged(object sender, EventArgs e)
        {
            //支座信息
            if (Msg_Get.Text == "拾取")
            {
                object Kven = transf("change_seat");
                if (Kven != null)
                {
                    Msg_Get.Text = "显示";
                }
            }
            if (Msg_Get.Text == "显示")
            {
                Line_Get.Text = "请选择";
                wiff_Get.Text = "请选择";
                transf("show_seat");
            }
        }
        private void FillBeamStruct()
        {
            #region beam
            HLTSmart smart = new HLTSmart();
            //第一步：遍历所有文字，找出满足如下正则表达式的文字，即为集中标注第一行
            List<Regex> regex1 = new List<Regex> {
                //new Regex(@"(KL|L|WKL|WL|KZL|LL|JL|DL)[-]?\d{1,4}")
                new Regex(@"((KL|L|WKL|WL|KZL|LL|JL|DL)(\d{1,4})\([A-Z0-9]{1,4}\)\s)?(\d{1,4})X(\d{1,4})")
                };
            List<Text> texts = new List<Text>();
            //获取dim_text值
            MxDrawText entity;
            foreach (var item in beam.dim_texts)
            {
                entity = Program.MainForm.axMxDrawX1.HandleToObject(item) as MxDrawText;
                if (entity != null)
                    texts.Add(new Text
                    {
                        Position = new Point3d { X = entity.Position.x, Y = entity.Position.y, Z = entity.Position.z },
                        Layer = entity.Layer,
                        TextString = entity.TextString,
                        Height = entity.Height,
                        Rotation = entity.Rotation
                    });
            }
            List<Text> Restexts = smart.GetWord(texts, regex1);
            //第二步：找到后还需验证这个字的左侧定位点 字体高度2倍范围内有无直线（即指示线）
            Point3d pt = new Point3d();
            MxDrawSelectionSet select = new MxDrawSelectionSet();
            foreach (var item in Restexts)
            {
                pt = item.Position;
                MxDrawPoint pt1 = new MxDrawPoint
                {
                    x = pt.X - 1 * item.Height,
                    y = pt.Y - 1 * item.Height,
                    z = pt.Z
                };
                MxDrawPoint pt2 = new MxDrawPoint
                {
                    x = pt.X + 1 * item.Height,
                    y = pt.Y + 1 * item.Height,
                    z = pt.Z
                };
                //Program.MainForm.axMxDrawX1.DrawLine(pt.X, pt.Y, pt.X+1, pt.Y+1);
                //Program.MainForm.axMxDrawX1.DrawLine(pt1.x, pt1.y, pt1.x, pt2.y);
                //Program.MainForm.axMxDrawX1.DrawLine(pt1.x, pt2.y, pt2.x, pt2.y);
                //Program.MainForm.axMxDrawX1.DrawLine(pt2.x, pt2.y, pt2.x, pt1.y);
                //Program.MainForm.axMxDrawX1.DrawLine(pt2.x, pt1.y, pt1.x, pt1.y);
                select.Select(MCAD_McSelect.mcSelectionSetCrossing, pt1, pt2, new MxDrawResbuf());
                MxDrawLine drawEntity; MxDrawPoint pst, ped;
                List<Text> text3 = new List<Text>();
                for (int i = 0; i < select.Count; i++)
                {
                    drawEntity = select.Item(i) as MxDrawLine;
                    //第三步：遍历第二步的结果，逐一按下面计算，例如第n个结果
                    if (drawEntity != null && drawEntity.ObjectName == "McDbLine")
                    {
                        Program.MainForm.axMxDrawX1.TwinkeEnt(drawEntity.ObjectID);
                        pst = drawEntity.GetStartPoint();
                        ped = drawEntity.GetEndPoint();
                        text3 = smart.SelectTextByBox(texts, new Point3d
                        {
                            X = pst.x,
                            Y = pst.y,
                            Z = pst.z
                        }, item.Position);
                        //第四步：从第三步结果中逐行用关键字匹配出参数信息
                        Beam beams = new Beam();
                        beams.owner = new List<string>();
                        beams.Sections = new List<Beam_Section>();
                        beams.Stirrup_info = new List<Stirrup_Dim>();
                        beams.Public_Bar = new List<Rebar_Dim>();
                        beams.Mid_Beam_Rebars = new List<List<Rebar_Dim>>();
                        beams.Waist_Bar = new List<Rebar_Dim>();
                        beams.Twist_Bar = new List<Rebar_Dim>();
                        string kval = "";

                        for (int k = 0; k < text3.Count; k++)
                        {
                            //KL14(1A) 300X400
                            kval = Regex.Match(text3[k].TextString, @"(KL|L|WKL|WL|KZL|LL|JL|DL)").Value;
                            if (kval != "")
                            {
                                beams.type = kval;//(Side_type)Enum.Parse(typeof(Side_type),kval);
                                beams.owner = GetItemAsync(text3[k].TextString);
                            }
                            //梁截面
                            kval = Regex.Match(text3[k].TextString, @"(\d{2,4}~)?(\d{2,4})(x|X)(\d{2,4})(~\d{2,4})?").Value;
                            string[] vals = kval.Split(new char[] { '~', 'x', 'X' });
                            if (kval != "")
                            {
                                if (kval.IndexOf('~') == -1)
                                {
                                    beams.Sections.Add(new Beam_Section
                                    {
                                        a = 1,
                                        b = Convert.ToDouble(vals[0]),
                                        h = Convert.ToDouble(vals[1])
                                    });
                                }
                                else
                                {
                                    if (kval.IndexOf("x", StringComparison.OrdinalIgnoreCase) > kval.IndexOf('~'))
                                    {
                                        //变截面宽  350~450x700
                                        beams.Sections.Add(new Beam_Section
                                        {
                                            a = 1,
                                            b = Convert.ToDouble(vals[0]),
                                            h = Convert.ToDouble(vals[2]),
                                        });
                                        beams.Sections.Add(new Beam_Section
                                        {
                                            a = 1,
                                            b = Convert.ToDouble(vals[1]),
                                            h = Convert.ToDouble(vals[2]),
                                        });
                                    }
                                    else
                                    {
                                        //变截面高   350x650~800
                                        beams.Sections.Add(new Beam_Section
                                        {
                                            a = 1,
                                            b = Convert.ToDouble(vals[0]),
                                            h = Convert.ToDouble(vals[1]),
                                        });
                                        beams.Sections.Add(new Beam_Section
                                        {
                                            a = 1,
                                            b = Convert.ToDouble(vals[0]),
                                            h = Convert.ToDouble(vals[2]),
                                        });
                                    }
                                }
                                continue;
                            }
                            //箍筋   Φ12@100/200(4)                           
                            kval = Regex.Match(text3[k].TextString, @"((%%130)|(%%131)|(%%132))(\d{1,3})@(\d{2,3})(/\d{2,3})?\(\d{1,2}\)").Value;
                            if (kval != "")
                            {
                                beams.Stirrup_info.Add(new Stirrup_Dim
                                {
                                    D = Convert.ToDouble(kval.Substring(5, kval.IndexOf('@') - 5)),
                                    Se = Convert.ToDouble(SplitStr(kval, kval.IndexOf('@'), "/(")),
                                    Sa = Convert.ToDouble(SplitStr(kval, kval.IndexOf('/'), "((")),
                                    n = Convert.ToInt32(SplitStr(kval, kval.IndexOf('('), "))"))
                                });
                                continue;
                            }
                            //通长钢筋   4Φ20;7Φ20 3/4
                            kval = Regex.Match(text3[k].TextString, @"^[A-Z]{0}(\(?\d{1,3}%%132\d{1,3}\)?)(\+\(?\d{1,3}%%132\d{1,3}\))?;?(\(?\d{1,3}%%132\d{1,3}\)?)?\s?(/?(\d{1,3})?)*").Value;
                            if (kval != "")
                            {
                                string[] sp1 = kval.Split(';');
                                for (int v = 0; v < sp1.Count(); v++)
                                {
                                    string[] sp2 = sp1[v].Split('+');
                                    for (int f = 0; f < sp2.Count(); f++)
                                    {
                                        sp2[f] = sp2[f].Replace("(", "");
                                        sp2[f] = sp2[f].Replace(")", "");
                                        sp2[f] = sp2[f].Replace("%%132", "|");
                                        string[] sp3 = sp2[f].Split('|');
                                        List<Rebar_Dim> Bdim = new List<Rebar_Dim>();
                                        if (sp2[f].IndexOf('/') == -1)
                                        {
                                            int c = 0;
                                            if (sp2.Count() > 1) c = f + 1;
                                            if (v == 1)
                                                Bdim.Add(new Rebar_Dim
                                                {
                                                    C = c,
                                                    n = Convert.ToInt32(sp3[0]),
                                                    D = Convert.ToInt32(sp3[1])
                                                });
                                            else
                                                beams.Public_Bar.Add(new Rebar_Dim
                                                {
                                                    C = c,
                                                    n = Convert.ToInt32(sp3[0]),
                                                    D = Convert.ToInt32(sp3[1])
                                                });
                                        }
                                        else
                                        {
                                            sp3[1] = sp3[1].Split(' ')[0];
                                            string xi = sp2[f].Substring(sp2[f].IndexOf(' '));
                                            string[] sp4 = xi.Trim().Split('/');
                                            for (int s = 0; s < sp4.Count(); s++)
                                            {
                                                if (v == 1)
                                                    Bdim.Add(new Rebar_Dim
                                                    {
                                                        n = Convert.ToInt32(sp4[s]),
                                                        D = Convert.ToInt32(sp3[1])
                                                    });
                                                else
                                                    beams.Public_Bar.Add(new Rebar_Dim
                                                    {
                                                        n = Convert.ToInt32(sp4[s]),
                                                        D = Convert.ToInt32(sp3[1])
                                                    });
                                            }
                                        }
                                        beams.Mid_Beam_Rebars.Add(Bdim);
                                    }
                                }
                                continue;
                            }
                            //腰筋   G4Φ12+N4Φ12
                            kval = Regex.Match(text3[k].TextString, @"(\+?(G|N)\d{1,3}%%132\d{1,3})*").Value;
                            if (kval != null)
                            {
                                string[] sp1 = kval.Split('+');
                                for (int c = 0; c < sp1.Length; c++)
                                {
                                    sp1[c] = sp1[c].Replace("%%132", "|");
                                    string[] sp2 = sp1[c].Split('|');
                                    if (sp2.Length == 2)
                                    {
                                        char G = sp2[0][0];
                                        int N = Convert.ToInt32(sp2[0].Substring(1));
                                        int C = 0; if (sp1.Length == 2) C = c + 1;
                                        if (G == 'G')
                                            beams.Waist_Bar.Add(new Rebar_Dim
                                            {
                                                n = N,
                                                C = C,
                                                D = Convert.ToDouble(sp2[1])
                                            });
                                        if (G == 'N')
                                            beams.Twist_Bar.Add(new Rebar_Dim
                                            {
                                                n = N,
                                                C = C,
                                                D = Convert.ToDouble(sp2[1])
                                            });
                                    }
                                }
                                continue;
                            }
                            //标高  (-0.150)
                            kval = Regex.Match(text3[k].TextString, @"(\(?-?\d{1,3}.\d{3}\)?)").Value;
                            if (kval != null)
                            {
                                kval = kval.Replace("(", "");
                                kval = kval.Replace(")", "");
                                beams.Sections = beams.Sections.Select(x => { x.H = Convert.ToDouble(kval); return x; }).ToList();
                            }


                        }
                        beam.beams.Add(beams);
                        beams.side_lines = new List<string>();
                        //识别梁
                        //1.集中标注指示线两个端点分别找到垂直距离最近的梁线，两者最近的就是第一根梁线L1

                        MxDrawLine LT1;
                        MxDrawLine L1 = GetLineForRange(drawEntity);
                        beams.side_lines.Add(L1.handle);
                        //L1 = Program.MainForm.axMxDrawX1.HandleToObject("61FBA") as MxDrawLine;
                        LT1 = L1;
                        for (int c1 = 0; c1 < 2; c1++)
                        {
                            //2.L1有两个点P1,P2，先从P1开始如下步骤，完了之后再P2
                            //3.找距离P1点2000内的支座线，并且L1与支座线交点距离P1小于300,若有结果则按3.2若无结果按3.1                        
                            L1 = LT1;
                            Program.MainForm.axMxDrawX1.TwinkeEnt(L1.ObjectID);
                            int cs = 0;
                            do
                            {
                                if (c1 == 1)
                                {
                                    MxDrawPoint temp = L1.StartPoint;
                                    L1.StartPoint = L1.EndPoint;
                                    L1.EndPoint = temp;
                                }
                                MxDrawPolyline seat = GetSeatForRange(L1.EndPoint, L1.StartPoint);
                                MxDrawLine L2 = new MxDrawLine();
                                MxDrawLine L2s = new MxDrawLine();
                                MxDrawLine Rline = new MxDrawLine();
                                if (seat.handle != "0")
                                {
                                    //有结果3.2
                                    L2 = GetparallelLine(L1);
                                    Rline = GetXpoint(L1.EndPoint, L1.StartPoint, seat);
                                    L1 = Rline;
                                    beams.side_lines.Add(L2.handle);
                                    beams.side_lines.Add(Rline.handle);
                                }
                                else
                                {
                                    //无结果3.1
                                    L2s = GetMoreLine(L1);
                                    L2 = GetparallelLine(L1);
                                    L1 = L2s;
                                    beams.side_lines.Add(L2s.handle);
                                }
                                //3.1 找到另一根梁线L2，L2的两个端点之一与P1最近且距离小于600且两线角度差别不超过2度，找到后凭L2另一个端点重复3       
                                /*3.2 说明一段梁结束，去寻找这段梁已识别梁线的平行线，即水平距离小于梁宽的1.5倍且两线相关，
                                再寻找与这些梁线共点且角度差别小于40度的梁线，找完写入beam；
                                然后用3的支座线与L1求到最远相关交点P3，求与P3最近的梁线，且角度差别小于40度，用这根新梁线远端点作为P1重复3
                                */
                                cs++;
                                //Program.MainForm.axMxDrawX1.StopAllTwinkeEnt();
                                Program.MainForm.axMxDrawX1.TwinkeEnt(L1.ObjectID);
                                Program.MainForm.axMxDrawX1.TwinkeEnt(L2s.ObjectID);
                                Program.MainForm.axMxDrawX1.TwinkeEnt(seat.ObjectID);
                                Program.MainForm.axMxDrawX1.TwinkeEnt(L2.ObjectID);
                                Program.MainForm.axMxDrawX1.TwinkeEnt(Rline.ObjectID);
                                if (cs > 10) break;
                            } while (L1.handle != "0");
                        }
                        //break;
                    }

                }
            }
            #endregion over
        }
        //字符串截取
        public string SplitStr(string str, int start, string regex)
        {
            string temp = "0";
            if (start == -1) return temp;
            start += 1;
            if (str.IndexOf(regex[0]) != -1)
                temp = str.Substring(start, str.IndexOf(regex[0]) - start);
            else if (str.IndexOf(regex[1]) != -1)
                temp = str.Substring(start, str.IndexOf(regex[1]) - start);
            return temp;

        }
        //找到复用梁
        public List<string> GetItemAsync(string kval)
        {
            List<string> vs = new List<string>();
            MxDrawSelectionSet select = new MxDrawSelectionSet();
            MxDrawResbuf filter = new MxDrawResbuf();
            filter.AddStringEx("TEXT,MTEXT", 5020);
            select.Select2(MCAD_McSelect.mcSelectionSetAll, null, null, null, filter);
            for (int i = 0; i < select.Count; i++)
            {
                MxDrawEntity entity = select.Item(i);
                if (entity == null) continue;
                if (entity.ObjectName == "McDbText")
                {
                    MxDrawText tx = entity as MxDrawText;
                    if (tx.TextString.Trim() != "" && kval.Contains(tx.TextString))
                    {
                        vs.Add(tx.handle);
                    }
                }
            }
            return vs;
        }
        //获取梁线
        public MxDrawLine GetLineForRange(MxDrawLine et)
        {
            MxDrawPoint pt = et.GetStartPoint();
            MxDrawPoint ept = et.GetEndPoint();
            MxDrawLayerTable layer = (Program.MainForm.axMxDrawX1.GetDatabase() as MxDrawDatabase).GetLayerTable();
            MxDrawSelectionSet collect = new MxDrawSelectionSet();
            MxDrawResbuf filter = new MxDrawResbuf();
            MxDrawLine entity;
            MxDrawLine result = new MxDrawLine();
            double distance = 100000000;
            collect.Select(MCAD_McSelect.mcSelectionSetAll, null, null, filter);
            for (int i = 0; i < collect.Count; i++)
            {
                entity = collect.Item(i) as MxDrawLine;
                if (entity == null) continue;
                MxDrawLayerTableRecord dd = layer.GetAt(entity.Layer);
                if (entity.ObjectName == "McDbLine" && entity.handle != et.handle && dd.Color.colorIndex != 1)
                {
                    //double rs1 = MathSience.pointToLineDistance(entity.GetStartPoint(), entity.GetEndPoint(), pt.x, pt.y);
                    //double rs2 = MathSience.pointToLineDistance(entity.GetStartPoint(), entity.GetEndPoint(), ept.x, ept.y);
                    double rs1 = MathSience.DistanceForPointToABLine(pt.x, pt.y, entity.GetStartPoint(), entity.GetEndPoint());
                    double rs2 = MathSience.DistanceForPointToABLine(ept.x, ept.y, entity.GetStartPoint(), entity.GetEndPoint());
                    if (rs1 <= distance)
                    {
                        distance = rs1;
                        result = entity;
                    }
                    if (rs2 <= distance)
                    {
                        distance = rs2;
                        result = entity;
                    }
                }
            }
            return result;
        }
        //获取支座
        public MxDrawPolyline GetSeatForRange(MxDrawPoint pt, MxDrawPoint spt)
        {
            double range1 = 2000, range2 = 300;
            MxDrawSelectionSet collect = new MxDrawSelectionSet();
            MxDrawResbuf filter = new MxDrawResbuf();
            MxDrawLayerTable layer = (Program.MainForm.axMxDrawX1.GetDatabase() as MxDrawDatabase).GetLayerTable();
            MxDrawPoint start = new MxDrawPoint
            {
                x = pt.x + range1,
                y = pt.y + range1,
                z = pt.z
            };
            MxDrawPoint end = new MxDrawPoint
            {
                x = pt.x - range1,
                y = pt.y - range1,
                z = pt.z
            };
            //Program.MainForm.axMxDrawX1.DrawLine(start.x, start.y, start.x, start.y);
            //Program.MainForm.axMxDrawX1.DrawLine(start.x, start.y, start.x, end.y);
            //Program.MainForm.axMxDrawX1.DrawLine(start.x, end.y, end.x, end.y);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x, end.y, end.x, start.y);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x, start.y, start.x, start.y);
            collect.Select(MCAD_McSelect.mcSelectionSetCrossing, start, end, filter);
            MxDrawEntity entity;
            //MxDrawLayerTable layer = (Program.MainForm.axMxDrawX1.GetDatabase() as MxDrawDatabase).GetLayerTable();
            //MxDrawLayerTableRecord dd;
            for (int i = 0; i < collect.Count; i++)
            {
                entity = collect.Item(i);
                if (entity == null) continue;
                if (entity.ObjectName == "McDbPolyline")
                {
                    MxDrawPolyline polyline = entity as MxDrawPolyline;
                    MxDrawLayerTableRecord dd = layer.GetAt(entity.Layer);
                    if (dd.Color.colorIndex != 9)
                    {
                        MxDrawPoint st = polyline.GetStartPoint();
                        MxDrawPoint et = polyline.GetEndPoint();
                        if (st.x == et.x && st.y == et.y)
                            return polyline;
                    }


                }
            }
            return new MxDrawPolyline();
        }
        //获取平行线
        public MxDrawLine GetparallelLine(MxDrawLine line)
        {
            double range = 600, range2 = 600;
            MxDrawPoint start = line.GetStartPoint();
            MxDrawPoint end = line.GetEndPoint();
            MxDrawSelectionSet select = new MxDrawSelectionSet();
            MxDrawResbuf filter = new MxDrawResbuf();
            MxDrawLayerTable layer = (Program.MainForm.axMxDrawX1.GetDatabase() as MxDrawDatabase).GetLayerTable();
            double angle = MathSience.GetAngle2(start, end);
            if (angle < 3 && angle > -2)
                range = 0;
            else
                range2 = 0;
            select.Select(MCAD_McSelect.mcSelectionSetCrossing, new MxDrawPoint
            {
                x = start.x - range,
                y = start.y - range2,
                z = start.z
            }, new MxDrawPoint
            {
                x = end.x + range,
                y = end.y + range2,
                z = end.z
            }, filter);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x - range, end.y - range2, end.x - range, end.y+range2);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x - range, end.y + range2, end.x + range, end.y + range2);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x + range, end.y + range2, end.x + range, end.y - range2);
            //Program.MainForm.axMxDrawX1.DrawLine(end.x + range, end.y - range2, end.x - range, end.y - range2);
            for (int i = 0; i < select.Count; i++)
            {
                MxDrawLine entity = select.Item(i) as MxDrawLine;
                if (entity == null) continue;
                if (select.Item(i).ObjectName == "McDbLine")
                {
                    MxDrawLayerTableRecord dd = layer.GetAt(entity.Layer);
                    if (line.handle != entity.handle && dd.Color.colorIndex != 1)
                    {
                        if (MathSience.parallel(entity.GetStartPoint(), entity.GetEndPoint(), line.GetStartPoint(), line.GetEndPoint()))
                        {
                            PointF point = MathSience.point_intersection(line.GetStartPoint(), line.GetEndPoint(), entity.GetStartPoint(), entity.GetEndPoint());
                            //if (point.X==0&&point.Y==0)
                            {
                                var c1 = entity.GetStartPoint();
                                var c2 = entity.GetEndPoint();
                                //if (MathSience.GetPointIsInLine(point, c1, c2, 2))
                                {
                                    return entity;
                                }

                            }
                        }
                    }
                }
            }
            return new MxDrawLine();
        }
        //获取相邻的梁线
        public MxDrawLine GetMoreLine(MxDrawLine line)
        {
            int range = 600;
            MxDrawSelectionSet collect = new MxDrawSelectionSet();
            MxDrawResbuf filter = new MxDrawResbuf();
            MxDrawPoint start = line.GetStartPoint();
            MxDrawPoint end = line.GetEndPoint();
            MxDrawLayerTable layer = (Program.MainForm.axMxDrawX1.GetDatabase() as MxDrawDatabase).GetLayerTable();
            collect.Select(MCAD_McSelect.mcSelectionSetCrossing, new MxDrawPoint
            {
                x = end.x + range,
                y = end.y + range,
                z = end.z
            }, new MxDrawPoint
            {
                x = end.x - range,
                y = end.y - range,
                z = end.z
            }, filter);
            //获取线判断角度<2
            for (int i = 0; i < collect.Count; i++)
            {
                MxDrawLine entity = collect.Item(i) as MxDrawLine;
                if (entity == null) continue;
                if (entity.handle == line.handle) continue;
                if (entity.ObjectName == "McDbLine")
                {
                    MxDrawLayerTableRecord cd = layer.GetAt(entity.Layer);
                    if (cd.Color.colorIndex != 1)
                    {
                        double angleLine = MathSience.GetAngle(line.EndPoint, entity.StartPoint, entity.EndPoint);
                        if (angleLine < 2)
                        {
                            double angle = MathSience.GetAngle2(line.GetStartPoint(), line.GetEndPoint());
                            double angle2 = MathSience.GetAngle2(entity.GetStartPoint(), entity.GetEndPoint());
                            if ((angle < 3 && angle > -2) || Math.Abs(Math.Round(angle)) == 180)//左右
                            {
                                if (angle2 < 3 && angle2 > -2)
                                    if (MathSience.GetDistance(entity.StartPoint.x, entity.StartPoint.y, entity.EndPoint.x, entity.EndPoint.y) > 100)
                                        return entity;
                            }
                            if ((angle > 80 && angle < 95) || (angle < -80 && angle > -95))//上下
                            {
                                if ((angle2 > 80 && angle2 < 95) || (angle2 < -80 && angle2 > -95))
                                {
                                    if (MathSience.GetDistance(entity.StartPoint.x, entity.StartPoint.y, entity.EndPoint.x, entity.EndPoint.y) > 100)
                                        return entity;
                                }
                            }
                        }
                    }
                }
            }
            return new MxDrawLine();
        }
        //支座与L1最远交点共点梁线
        public MxDrawLine GetXpoint(MxDrawPoint pts, MxDrawPoint pte, MxDrawPolyline seat)
        {
            MxDrawPoints pty = new MxDrawPoints();
            MxDrawSelectionSet collect = new MxDrawSelectionSet();
            for (int i = 0; i < seat.NumVerts; i++)
            {
                MxDrawPoint temp1 = seat.GetPointAt(i);
                temp1.x = Math.Round(temp1.x, 6);
                temp1.y = Math.Round(temp1.y, 6);
                double angle = MathSience.GetAngle2(pts, pte);
                if ((angle < 3 && angle > -2) || Math.Round(Math.Abs(angle)) == 180)//左右
                    if (Math.Round(pts.x, 6) != temp1.x && Math.Round(pte.x, 6) != temp1.x)
                    {
                        pty.Add2(temp1);
                    }
                if ((angle > 80 && angle < 95) || (angle < -80 && angle > -95))//上下
                    if (Math.Round(pts.y, 6) != temp1.y && Math.Round(pte.y, 6) != temp1.y)
                    {
                        pty.Add2(temp1);
                    }
            }
            if (pty.Count > 1)
            {
                PointF ptx = MathSience.point_intersection(pts, pte, pty.Item(0), pty.Item(1));
                collect.AllSelect();
                for (int i = 0; i < collect.Count; i++)
                {
                    MxDrawLine line = collect.Item(i) as MxDrawLine;
                    if (line == null) continue;
                    if (line.ObjectName == "McDbLine")
                    {
                        MxDrawPoint t1 = line.StartPoint;
                        MxDrawPoint t2 = line.EndPoint;
                        MxDrawPoint tp = new MxDrawPoint
                        {
                            x = ptx.X,
                            y = ptx.Y
                        };
                        if (Math.Abs(Math.Round(t1.x) - ptx.X) < 1.5 && Math.Abs(Math.Round(t1.y) - ptx.Y) < 1.5)
                        {
                            return line;
                        }
                        else if (Math.Abs(Math.Round(t2.x) - ptx.X) < 1.5 && Math.Abs(Math.Round(t2.y) - ptx.Y) < 1.5)
                        {
                            return line;
                        }
                    }
                }
            }
            return new MxDrawLine();
        }
    }
}
