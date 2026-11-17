using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.Controls;

public sealed class FractalBackdropControl : Control
{
    public static readonly StyledProperty<BuilderThemeConfig?> ThemeConfigProperty =
        AvaloniaProperty.Register<FractalBackdropControl, BuilderThemeConfig?>(nameof(ThemeConfig));

    private readonly DispatcherTimer _timer;
    private double _phase;

    public BuilderThemeConfig? ThemeConfig
    {
        get => GetValue(ThemeConfigProperty);
        set => SetValue(ThemeConfigProperty, value);
    }

    static FractalBackdropControl()
    {
        AffectsRender<FractalBackdropControl>(ThemeConfigProperty);
    }

    public FractalBackdropControl()
    {
        IsHitTestVisible = false;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += (_, _) =>
        {
            var cfg = ThemeConfig;
            if (cfg is null || !cfg.Animate) return;
            _phase += 0.018 * (cfg.AnimationSpeed / 100.0);
            InvalidateVisual();
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var cfg = ThemeConfig ?? BuilderThemeConfig.CreateDefault();
        bool isDark = cfg.Mode != "light";
        var bgColor = ThemeService.ParseHex(isDark ? "#0B0A0F" : "#F2F2EE", Colors.Black);
        context.FillRectangle(new SolidColorBrush(bgColor), new Rect(0, 0, bounds.Width, bounds.Height));

        double satRatio = Math.Clamp(cfg.Saturation / 100.0, 0, 1);
        double peakOpacity = satRatio * (isDark ? 0.55 : 0.30);

        if (peakOpacity > 0.01)
        {
            double angleRad = (cfg.Angle + 90) * Math.PI / 180.0;
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);
            double maxAxis = Math.Max(Math.Abs(cosA), Math.Max(Math.Abs(sinA), 0.001));
            double ux = cosA / maxAxis;
            double uy = sinA / maxAxis;

            double x1 = (0.5 + 0.5 * ux) * bounds.Width;
            double y1 = (0.5 + 0.5 * uy) * bounds.Height;
            double x2 = (0.5 - 0.5 * ux) * bounds.Width;
            double y2 = (0.5 - 0.5 * uy) * bounds.Height;
            double radius = Math.Max(bounds.Width, bounds.Height) * 0.58;

            var c0 = ThemeService.ParseHex(cfg.Colors[0], Colors.LimeGreen);
            var c1 = ThemeService.ParseHex(cfg.Colors[1], Colors.HotPink);
            var c3 = ThemeService.ParseHex(cfg.Colors[3], Colors.DeepSkyBlue);

            byte a0 = (byte)(peakOpacity * 255);
            var g1 = new RadialGradientBrush
            {
                Center = new RelativePoint(x1 / bounds.Width, y1 / bounds.Height, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(x1 / bounds.Width, y1 / bounds.Height, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.55, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.55, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(a0, c0.R, c0.G, c0.B), 0),
                    new GradientStop(Color.FromArgb(0, c0.R, c0.G, c0.B), 1)
                }
            };
            context.FillRectangle(g1, new Rect(0, 0, bounds.Width, bounds.Height));

            var g2 = new RadialGradientBrush
            {
                Center = new RelativePoint(x2 / bounds.Width, y2 / bounds.Height, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(x2 / bounds.Width, y2 / bounds.Height, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.58, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.58, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(a0, c1.R, c1.G, c1.B), 0),
                    new GradientStop(Color.FromArgb((byte)(a0 / 3), c3.R, c3.G, c3.B), 0.45),
                    new GradientStop(Color.FromArgb(0, c3.R, c3.G, c3.B), 1)
                }
            };
            context.FillRectangle(g2, new Rect(0, 0, bounds.Width, bounds.Height));
        }

        double fracAlpha = Math.Clamp(cfg.FractalOpacity / 100.0, 0, 1);
        if (cfg.Fractal == FractalType.None || fracAlpha <= 0.01) return;

        DrawFractalLayer(context, bounds, cfg.Fractal, cfg, fracAlpha, 0);
        if (cfg.MultiFractal)
        {
            if (cfg.FractalSecondary != FractalType.None)
                DrawFractalLayer(context, bounds, cfg.FractalSecondary, cfg, fracAlpha * 0.55, 1.7);
            if (cfg.FractalTertiary != FractalType.None)
                DrawFractalLayer(context, bounds, cfg.FractalTertiary, cfg, fracAlpha * 0.38, 3.1);
        }
    }

    private void DrawFractalLayer(
        DrawingContext context,
        Rect bounds,
        FractalType type,
        BuilderThemeConfig cfg,
        double opacity,
        double seedOffset)
    {
        var baseColor = ThemeService.ParseHex(
            cfg.Colors[(int)(Math.Abs(seedOffset * 2) % cfg.Colors.Length)],
            Colors.DodgerBlue);
        byte alpha = (byte)Math.Clamp(opacity * 200, 8, 220);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B)), 1.2);
        double scale = Math.Clamp(cfg.FractalScale / 100.0, 0.15, 1.0);
        double cx = bounds.Width * 0.5;
        double cy = bounds.Height * 0.5;
        double size = Math.Min(bounds.Width, bounds.Height) * 0.42 * scale;
        double t = _phase + seedOffset;

        switch (type)
        {
            case FractalType.Lorenz:
            {
                double x = 0.1, y = 0, z = 0;
                double dt = 0.008;
                var prev = new Point(cx, cy);
                for (int i = 0; i < 650; i++)
                {
                    double dx = 10.0 * (y - x);
                    double dy = x * (28.0 - z) - y;
                    double dz = x * y - (8.0 / 3.0) * z;
                    x += dx * dt;
                    y += dy * dt;
                    z += dz * dt;
                    var next = new Point(cx + (x * Math.Cos(t * 0.25) - y * Math.Sin(t * 0.25)) * (size / 22.0),
                                         cy + (z - 24) * (size / 22.0));
                    if (i > 12) context.DrawLine(pen, prev, next);
                    prev = next;
                }
                break;
            }
            case FractalType.Fern:
            {
                double x = 0, y = 0;
                var rng = new Random(42 + (int)(seedOffset * 10));
                var brush = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
                for (int i = 0; i < 1200; i++)
                {
                    double r = rng.NextDouble();
                    double nx, ny;
                    if (r < 0.01) { nx = 0; ny = 0.16 * y; }
                    else if (r < 0.86) { nx = 0.85 * x + 0.04 * y; ny = -0.04 * x + 0.85 * y + 1.6; }
                    else if (r < 0.93) { nx = 0.2 * x - 0.26 * y; ny = 0.23 * x + 0.22 * y + 1.6; }
                    else { nx = -0.15 * x + 0.28 * y; ny = 0.26 * x + 0.24 * y + 0.44; }
                    x = nx; y = ny;
                    double px = cx + x * (size / 5.0) + Math.Sin(t * 0.4) * 18;
                    double py = cy + size - y * (size / 5.2);
                    context.DrawRectangle(brush, null, new Rect(px, py, 1.5, 1.5));
                }
                break;
            }
            case FractalType.Tree:
            {
                DrawTreeBranch(context, pen, cx, cy + size * 0.85, -Math.PI / 2 + Math.Sin(t * 0.4) * 0.06, size * 0.42, 7);
                break;
            }
            case FractalType.Sierpinski:
            {
                var p1 = new Point(cx, cy - size);
                var p2 = new Point(cx - size * 0.866, cy + size * 0.65);
                var p3 = new Point(cx + size * 0.866, cy + size * 0.65);
                DrawSierpinski(context, pen, p1, p2, p3, 5);
                break;
            }
            case FractalType.Dragon:
            case FractalType.Koch:
            {
                var pts = new List<Point> { new(cx - size * 0.8, cy), new(cx + size * 0.8, cy) };
                int iters = type == FractalType.Dragon ? 9 : 4;
                for (int k = 0; k < iters; k++)
                {
                    var next = new List<Point>(pts.Count * 2);
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        var a = pts[i];
                        var b = pts[i + 1];
                        next.Add(a);
                        if (type == FractalType.Dragon)
                        {
                            int sign = i % 2 == 0 ? 1 : -1;
                            double mx = (a.X + b.X) * 0.5 - sign * (b.Y - a.Y) * 0.5;
                            double my = (a.Y + b.Y) * 0.5 + sign * (b.X - a.X) * 0.5;
                            next.Add(new Point(mx, my));
                        }
                        else
                        {
                            var p1 = new Point(a.X + (b.X - a.X) / 3.0, a.Y + (b.Y - a.Y) / 3.0);
                            var p3 = new Point(a.X + 2.0 * (b.X - a.X) / 3.0, a.Y + 2.0 * (b.Y - a.Y) / 3.0);
                            double ang = -Math.PI / 3.0;
                            double dx = p3.X - p1.X, dy = p3.Y - p1.Y;
                            var p2 = new Point(p1.X + dx * Math.Cos(ang) - dy * Math.Sin(ang),
                                               p1.Y + dx * Math.Sin(ang) + dy * Math.Cos(ang));
                            next.Add(p1); next.Add(p2); next.Add(p3);
                        }
                    }
                    next.Add(pts[^1]);
                    pts = next;
                }
                for (int i = 0; i < pts.Count - 1; i++)
                    context.DrawLine(pen, pts[i], pts[i + 1]);
                break;
            }
            default:
            {
                // Julia / Mandelbrot / FBM geometric contour orbit approximation
                int rings = 6;
                for (int r = 1; r <= rings; r++)
                {
                    double rad = size * (r / (double)rings);
                    var prev = new Point(cx + rad, cy);
                    const int steps = 72;
                    for (int s = 1; s <= steps; s++)
                    {
                        double a = (s / (double)steps) * Math.PI * 2;
                        double wobble = Math.Sin(a * (r + 2) + t) * (size * 0.08 * (cfg.FractalUncertainty / 100.0));
                        var pt = new Point(cx + (rad + wobble) * Math.Cos(a), cy + (rad + wobble) * Math.Sin(a));
                        context.DrawLine(pen, prev, pt);
                        prev = pt;
                    }
                }
                break;
            }
        }
    }

    private static void DrawTreeBranch(DrawingContext ctx, Pen pen, double x1, double y1, double angle, double len, int depth)
    {
        if (depth <= 0 || len < 3) return;
        double x2 = x1 + Math.Cos(angle) * len;
        double y2 = y1 + Math.Sin(angle) * len;
        ctx.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        DrawTreeBranch(ctx, pen, x2, y2, angle - 0.36, len * 0.72, depth - 1);
        DrawTreeBranch(ctx, pen, x2, y2, angle + 0.36, len * 0.72, depth - 1);
    }

    private static void DrawSierpinski(DrawingContext ctx, Pen pen, Point p1, Point p2, Point p3, int depth)
    {
        if (depth == 0)
        {
            ctx.DrawLine(pen, p1, p2);
            ctx.DrawLine(pen, p2, p3);
            ctx.DrawLine(pen, p3, p1);
            return;
        }
        var m12 = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
        var m23 = new Point((p2.X + p3.X) * 0.5, (p2.Y + p3.Y) * 0.5);
        var m31 = new Point((p3.X + p1.X) * 0.5, (p3.Y + p1.Y) * 0.5);
        DrawSierpinski(ctx, pen, p1, m12, m31, depth - 1);
        DrawSierpinski(ctx, pen, m12, p2, m23, depth - 1);
        DrawSierpinski(ctx, pen, m31, m23, p3, depth - 1);
    }
}
