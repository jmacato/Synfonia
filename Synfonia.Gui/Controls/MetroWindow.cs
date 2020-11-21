using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Styling;
using ReactiveUI;

namespace Synfonia.Controls
{
    public class MetroWindow : Window, IStyleable
    {
        public enum ClassLongIndex
        {
            GCLP_MENUNAME = -8,
            GCLP_HBRBACKGROUND = -10,
            GCLP_HCURSOR = -12,
            GCLP_HICON = -14,
            GCLP_HMODULE = -16,
            GCL_CBWNDEXTRA = -18,
            GCL_CBCLSEXTRA = -20,
            GCLP_WNDPROC = -24,
            GCL_STYLE = -26,
            GCLP_HICONSM = -34,
            GCW_ATOM = -32
        }

        public static readonly StyledProperty<Control> TitleBarContentProperty =
            AvaloniaProperty.Register<MetroWindow, Control>(nameof(TitleBarContent));

        public static readonly StyledProperty<Control> SideBarContentProperty =
            AvaloniaProperty.Register<MetroWindow, Control>(nameof(SideBarContent));

        public static readonly StyledProperty<bool> ClientDecorationsProperty =
            AvaloniaProperty.Register<MetroWindow, bool>(nameof(ClientDecorations));

        public static readonly StyledProperty<bool> SideBarEnabledProperty =
            AvaloniaProperty.Register<MetroWindow, bool>(nameof(SideBarEnabled));

        private Panel _bottomHorizontalGrip,
            _bottomLeftGrip,
            _bottomRightGrip,
            _leftVerticalGrip,
            _rightVerticalGrip,
            _topHorizontalGrip,
            _topLeftGrip,
            _topRightGrip;

        private Button _closeButton;

        private Button _minimiseButton;


        private bool _mouseDown;
        private Point _mouseDownPosition;
        private Button _restoreButton;
        private Path _restoreButtonPanelPath;
        private Button _sidebar_button;
        private DockPanel _titleBar;

        static MetroWindow()
        {
        }

        public MetroWindow()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // do this in code or we get a delay in osx.
                HasSystemDecorations = false;
                ClientDecorations = true;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var classes = (int) GetClassLongPtr(PlatformImpl.Handle.Handle, (int) ClassLongIndex.GCL_STYLE);

                    classes |= 0x00020000;

                    SetClassLong(PlatformImpl.Handle.Handle, ClassLongIndex.GCL_STYLE, new IntPtr(classes));
                }
            }
            else
            {
                ClientDecorations = false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                HasSystemDecorations = true;

            // This will need implementing properly once this is supported by avalonia itself.
            // var color = (ColorTheme.CurrentTheme.Background as SolidColorBrush).Color;
            // (PlatformImpl as Avalonia.Native.WindowImpl).SetTitleBarColor(color);

            this.WhenAnyValue(x => x.WindowState)
                .Select(x => x == WindowState.Maximized)
                .Subscribe(x => { PseudoClasses.Set(":maximised", x); });

            this.WhenAnyValue(x => x.SideBarEnabled)
                .DistinctUntilChanged()
                .Subscribe(x => { PseudoClasses.Set(":sidebar", x); });
        }

        public bool SideBarEnabled
        {
            get => GetValue(SideBarEnabledProperty);
            set => SetValue(SideBarEnabledProperty, value);
        }

        public bool ClientDecorations
        {
            get => GetValue(ClientDecorationsProperty);
            set => SetValue(ClientDecorationsProperty, value);
        }

        public Control TitleBarContent
        {
            get => GetValue(TitleBarContentProperty);
            set => SetValue(TitleBarContentProperty, value);
        }

        public Control SideBarContent
        {
            get => GetValue(SideBarContentProperty);
            set => SetValue(SideBarContentProperty, value);
        }

        Type IStyleable.StyleKey => typeof(MetroWindow);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
        private static extern IntPtr SetClassLong64(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetClassLong")]
        private static extern IntPtr SetClassLong32(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong);

        public static IntPtr SetClassLong(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4) return SetClassLong32(hWnd, nIndex, dwNewLong);

            return SetClassLong64(hWnd, nIndex, dwNewLong);
        }

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size > 4)
                return GetClassLongPtr64(hWnd, nIndex);
            return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_topHorizontalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.North, e);
            }
            else if (_bottomHorizontalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.South, e);
            }
            else if (_leftVerticalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.West, e);
            }
            else if (_rightVerticalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.East, e);
            }
            else if (_topLeftGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.NorthWest, e);
            }
            else if (_bottomLeftGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.SouthWest, e);
            }
            else if (_topRightGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.NorthEast, e);
            }
            else if (_bottomRightGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.SouthEast, e);
            }
            else if (_titleBar.IsPointerOver)
            {
                _mouseDown = true;
                _mouseDownPosition = e.GetPosition(this);

                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                    _mouseDown = false;
                }
            }
            else
            {
                _mouseDown = false;
            }

            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _mouseDown = false;
            base.OnPointerReleased(e);
        }

        private void ToggleWindowState()
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;

                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
            }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            _titleBar = e.NameScope.Find<DockPanel>("titlebar");
            _minimiseButton = e.NameScope.Find<Button>("minimiseButton");
            _restoreButton = e.NameScope.Find<Button>("restoreButton");
            _restoreButtonPanelPath = e.NameScope.Find<Path>("restoreButtonPanelPath");
            _closeButton = e.NameScope.Find<Button>("closeButton");
            _sidebar_button = e.NameScope.Find<Button>("sidebar_button");
            // _icon = e.NameScope.Find<Image>("icon");

            _topHorizontalGrip = e.NameScope.Find<Panel>("topHorizontalGrip");
            _bottomHorizontalGrip = e.NameScope.Find<Panel>("bottomHorizontalGrip");
            _leftVerticalGrip = e.NameScope.Find<Panel>("leftVerticalGrip");
            _rightVerticalGrip = e.NameScope.Find<Panel>("rightVerticalGrip");

            _topLeftGrip = e.NameScope.Find<Panel>("topLeftGrip");
            _bottomLeftGrip = e.NameScope.Find<Panel>("bottomLeftGrip");
            _topRightGrip = e.NameScope.Find<Panel>("topRightGrip");
            _bottomRightGrip = e.NameScope.Find<Panel>("bottomRightGrip");

            _minimiseButton.Click += delegate { WindowState = WindowState.Minimized; };
            _restoreButton.Click += delegate { ToggleWindowState(); };
            _titleBar.DoubleTapped += delegate { ToggleWindowState(); };
            _closeButton.Click += delegate { Close(); };
            _sidebar_button.Click += delegate { SideBarEnabled = !SideBarEnabled; };


            // _icon.DoubleTapped += (sender, ee) => { Close(); };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _topHorizontalGrip.IsVisible = false;
                _bottomHorizontalGrip.IsHitTestVisible = false;
                _leftVerticalGrip.IsHitTestVisible = false;
                _rightVerticalGrip.IsHitTestVisible = false;
                _topLeftGrip.IsHitTestVisible = false;
                _bottomLeftGrip.IsHitTestVisible = false;
                _topRightGrip.IsHitTestVisible = false;
                _bottomRightGrip.IsHitTestVisible = false;

                BorderThickness = new Thickness();
            }
        }
    }
}