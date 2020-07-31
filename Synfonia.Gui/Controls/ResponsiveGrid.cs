using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace Synfonia.Controls
{
    public class ResponsiveGrid : Grid
    {
        static ResponsiveGrid()
        {
            AffectsMeasure<ResponsiveGrid>(MaxDivisionProperty,
                           BreakPointsProperty,
                           XSProperty,
                           SMProperty,
                           MDProperty,
                           LGProperty,
                           XSProperty,
                           SMProperty,
                           XS_OffsetProperty,
                           XS_PullProperty,
                           XS_PushProperty,
                           LG_OffsetProperty,
                           LG_PullProperty,
                           LG_PushProperty,
                           MD_OffsetProperty,
                           MD_PushProperty,
                           MD_PullProperty
                           );

        }

        public static readonly StyledProperty<int> MaxDivisionProperty =
            AvaloniaProperty.Register<ResponsiveGrid, int>(nameof(MaxDivision), 12);

        public int MaxDivision
        {
            get { return (int)GetValue(MaxDivisionProperty); }
            set { SetValue(MaxDivisionProperty, value); }
        }

        public static readonly StyledProperty<SizeThresholds> BreakPointsProperty =
            AvaloniaProperty.Register<ResponsiveGrid, SizeThresholds>(nameof(Thresholds), null);

        public SizeThresholds Thresholds
        {
            get { return GetValue(BreakPointsProperty); }
            set { SetValue(BreakPointsProperty, value); }
        }

        public static readonly AttachedProperty<int> XSProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS", 0);

        public static int GetXS(AvaloniaObject obj)
        {
            return obj.GetValue(XSProperty);
        }

        public static void SetXS(AvaloniaObject obj, int value)
        {
            obj.SetValue(XSProperty, value);
        }

        public static readonly AttachedProperty<int> SMProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM", 0);

        public static int GetSM(AvaloniaObject obj)
        {
            return obj.GetValue(SMProperty);
        }

        public static void SetSM(AvaloniaObject obj, int value)
        {
            obj.SetValue(SMProperty, value);
        }

        public static readonly AttachedProperty<int> MDProperty =
               AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD", 0);

        public static int GetMD(AvaloniaObject obj)
        {
            return obj.GetValue(MDProperty);
        }

        public static void SetMD(AvaloniaObject obj, int value)
        {
            obj.SetValue(MDProperty, value);
        }

        public static readonly AttachedProperty<int> LGProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG", 0);

        public static int GetLG(AvaloniaObject obj)
        {
            return obj.GetValue(LGProperty);
        }

        public static void SetLG(AvaloniaObject obj, int value)
        {
            obj.SetValue(LGProperty, value);
        }

        public static int GetXS_Offset(AvaloniaObject obj)
        {
            return obj.GetValue(XS_OffsetProperty);
        }
        public static void SetXS_Offset(AvaloniaObject obj, int value)
        {
            obj.SetValue(XS_OffsetProperty, value);
        }

        public static readonly AttachedProperty<int> XS_OffsetProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Offset", 0);
        public static int GetSM_Offset(AvaloniaObject obj)
        {
            return obj.GetValue(SM_OffsetProperty);
        }
        public static void SetSM_Offset(AvaloniaObject obj, int value)
        {
            obj.SetValue(SM_OffsetProperty, value);
        }

        public static readonly AttachedProperty<int> SM_OffsetProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Offset", 0);


        public static int GetMD_Offset(AvaloniaObject obj)
        {
            return obj.GetValue(MD_OffsetProperty);
        }
        public static void SetMD_Offset(AvaloniaObject obj, int value)
        {
            obj.SetValue(MD_OffsetProperty, value);
        }

        public static readonly AttachedProperty<int> MD_OffsetProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Offset", 0);



        public static int GetLG_Offset(AvaloniaObject obj)
        {
            return obj.GetValue(LG_OffsetProperty);
        }
        public static void SetLG_Offset(AvaloniaObject obj, int value)
        {
            obj.SetValue(LG_OffsetProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for LG_Offset.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> LG_OffsetProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Offset", 0);






        public static int GetXS_Push(AvaloniaObject obj)
        {
            return obj.GetValue(XS_PushProperty);
        }
        public static void SetXS_Push(AvaloniaObject obj, int value)
        {
            obj.SetValue(XS_PushProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for XS_Push.  This enables animation, styling, binding, etc...

        public static readonly AttachedProperty<int> XS_PushProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Push", 0);




        public static int GetSM_Push(AvaloniaObject obj)
        {
            return obj.GetValue(SM_PushProperty);
        }
        public static void SetSM_Push(AvaloniaObject obj, int value)
        {
            obj.SetValue(SM_PushProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for SM_Push.  This enables animation, styling, binding, etc... 
        public static readonly AttachedProperty<int> SM_PushProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Push", 0);



        public static int GetMD_Push(AvaloniaObject obj)
        {
            return obj.GetValue(MD_PushProperty);
        }
        public static void SetMD_Push(AvaloniaObject obj, int value)
        {
            obj.SetValue(MD_PushProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for MD_Push.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> MD_PushProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Push", 0);


        public static int GetLG_Push(AvaloniaObject obj)
        {
            return obj.GetValue(LG_PushProperty);
        }
        public static void SetLG_Push(AvaloniaObject obj, int value)
        {
            obj.SetValue(LG_PushProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for LG_Push.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> LG_PushProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Push", 0);







        public static int GetXS_Pull(AvaloniaObject obj)
        {
            return obj.GetValue(XS_PullProperty);
        }
        public static void SetXS_Pull(AvaloniaObject obj, int value)
        {
            obj.SetValue(XS_PullProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for XS_Pull.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> XS_PullProperty =
               AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Pull", 0);


        public static int GetSM_Pull(AvaloniaObject obj)
        {
            return obj.GetValue(SM_PullProperty);
        }
        public static void SetSM_Pull(AvaloniaObject obj, int value)
        {
            obj.SetValue(SM_PullProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for SM_Pull.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> SM_PullProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Pull", 0);


        public static int GetMD_Pull(AvaloniaObject obj)
        {
            return obj.GetValue(MD_PullProperty);
        }
        public static void SetMD_Pull(AvaloniaObject obj, int value)
        {
            obj.SetValue(MD_PullProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for MD_Pull.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> MD_PullProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Pull", 0);

        public static int GetLG_Pull(AvaloniaObject obj)
        {
            return obj.GetValue(LG_PullProperty);
        }
        public static void SetLG_Pull(AvaloniaObject obj, int value)
        {
            obj.SetValue(LG_PullProperty, value);
        }

        // Using a AvaloniaProperty as the backing store for LG_Pull.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> LG_PullProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Pull", 0);

        public static int GetActualColumn(AvaloniaObject obj)
        {
            return obj.GetValue(ActualColumnProperty);
        }
        protected static void SetActualColumn(AvaloniaObject obj, int value)
        {
            obj.SetValue(ActualColumnProperty, value);
        }
        // Using a AvaloniaProperty as the backing store for ActualColumn.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> ActualColumnProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualColumn", 0);



        public static int GetActualRow(AvaloniaObject obj)
        {
            return obj.GetValue(ActualRowProperty);
        }
        protected static void SetActualRow(AvaloniaObject obj, int value)
        {
            obj.SetValue(ActualRowProperty, value);
        }
        // Using a AvaloniaProperty as the backing store for ActualRow.  This enables animation, styling, binding, etc...
        public static readonly AttachedProperty<int> ActualRowProperty =
                AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualRow", 0);


        public ResponsiveGrid()
        {
            this.MaxDivision = 12;
            this.Thresholds = new SizeThresholds();
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            var count = 0;
            var currentRow = 0;

            var availableWidth = double.IsPositiveInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / this.MaxDivision;
            var children = this.Children.OfType<Control>();


            foreach (Control child in this.Children)
            {
                if (child != null)
                {
                    // Collapsedの時はレイアウトしない
                    if (!child.IsVisible) { continue; }

                    var span = this.GetSpan(child, availableSize.Width);
                    var offset = this.GetOffset(child, availableSize.Width);
                    var push = this.GetPush(child, availableSize.Width);
                    var pull = this.GetPull(child, availableSize.Width);

                    if (count + span + offset > this.MaxDivision)
                    {
                        // リセット
                        currentRow++;
                        count = 0;
                    }

                    SetActualColumn(child, count + offset + push - pull);
                    SetActualRow(child, currentRow);

                    count += (span + offset);

                    var size = new Size(availableWidth * span, double.PositiveInfinity);
                    child.Measure(size);
                }
            }

            // 行ごとにグルーピングする
            var group = this.Children.OfType<Control>()
                                     .GroupBy(x => GetActualRow(x));

            Size totalSize = new Size();
            if (group.Count() != 0)
            {
                totalSize = new Size(
                    group.Max(rows => rows.Sum(o => o.DesiredSize.Width)),
                    group.Sum(rows => rows.Max(o => o.DesiredSize.Height))
                    );
            }

            return totalSize;
        }

        protected int GetSpan(Control element, double width)
        {
            var span = 0;

            var getXS = new Func<Control, int>((o) => { var x = GetXS(o); return x != 0 ? x : this.MaxDivision; });
            var getSM = new Func<Control, int>((o) => { var x = GetSM(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<Control, int>((o) => { var x = GetMD(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<Control, int>((o) => { var x = GetLG(o); return x != 0 ? x : getMD(o); });

            if (width < this.Thresholds.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.Thresholds.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.Thresholds.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected int GetOffset(Control element, double width)
        {
            var span = 0;

            var getXS = new Func<Control, int>((o) => { var x = GetXS_Offset(o); return x != 0 ? x : 0; });
            var getSM = new Func<Control, int>((o) => { var x = GetSM_Offset(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<Control, int>((o) => { var x = GetMD_Offset(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<Control, int>((o) => { var x = GetLG_Offset(o); return x != 0 ? x : getMD(o); });

            if (width < this.Thresholds.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.Thresholds.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.Thresholds.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected int GetPush(Control element, double width)
        {
            var span = 0;

            var getXS = new Func<Control, int>((o) => { var x = GetXS_Push(o); return x != 0 ? x : 0; });
            var getSM = new Func<Control, int>((o) => { var x = GetSM_Push(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<Control, int>((o) => { var x = GetMD_Push(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<Control, int>((o) => { var x = GetLG_Push(o); return x != 0 ? x : getMD(o); });

            if (width < this.Thresholds.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.Thresholds.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.Thresholds.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }
        protected int GetPull(Control element, double width)
        {
            var span = 0;

            var getXS = new Func<Control, int>((o) => { var x = GetXS_Pull(o); return x != 0 ? x : 0; });
            var getSM = new Func<Control, int>((o) => { var x = GetSM_Pull(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<Control, int>((o) => { var x = GetMD_Pull(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<Control, int>((o) => { var x = GetLG_Pull(o); return x != 0 ? x : getMD(o); });

            if (width < this.Thresholds.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.Thresholds.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.Thresholds.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnWidth = finalSize.Width / this.MaxDivision;

            // 行ごとにグルーピングする
            var group = this.Children.OfType<Control>()
                                     .GroupBy(x => GetActualRow(x));

            double temp = 0;
            foreach (var rows in group)
            {
                double max = 0;

                var columnHeight = rows.Max(o => o.DesiredSize.Height);
                foreach (var element in rows)
                {
                    var column = GetActualColumn(element);
                    var row = GetActualRow(element);
                    var columnSpan = this.GetSpan(element, finalSize.Width);

                    var rect = new Rect(columnWidth * column, temp, columnWidth * columnSpan, columnHeight);

                    element.Arrange(rect);

                    max = Math.Max(element.DesiredSize.Height, max);
                }

                temp += max;
            }

            return finalSize;

        }


        // // ShowGridLinesで表示する際に利用するペンの定義
        // private static readonly Pen _guidePen1
        //     = new Pen(Brushes.Yellow, 1);
        // private static readonly Pen _guidePen2
        //     = new Pen(Brushes.Blue, 1) { DashStyle = new DashStyle(new double[] { 4, 4 }, 0) };

        // protected override void On

        // protected override void OnRender(DrawingContext dc)
        // {
        //     base.OnRender(dc);
        //     // ShowGridLinesが有効な場合、各種エレメントを描画する前に、ガイド用のグリッドを描画する。
        //     if (this.ShowGridLines)
        //     {
        //         var gridNum = this.MaxDivision;
        //         var unit = this.ActualWidth / gridNum;
        //         for (var i = 0; i <= gridNum; i++)
        //         {
        //             var x = (int)(unit * i);
        //             dc.DrawLine(_guidePen1, new Point(x, 0), new Point(x, this.ActualHeight));
        //             dc.DrawLine(_guidePen2, new Point(x, 0), new Point(x, this.ActualHeight));
        //         }
        //     }
        // }
    }
}