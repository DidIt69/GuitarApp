using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace GuitarApp.Maui.Controls;

public class WaveformDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty SamplesProperty = BindableProperty.Create(nameof(Samples), typeof(float[]), typeof(WaveformDrawable), null, propertyChanged: OnSamplesChanged);
    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(nameof(StrokeColor), typeof(string), typeof(WaveformDrawable), "#808080");

    public float[] Samples { get => (float[])GetValue(SamplesProperty); set => SetValue(SamplesProperty, value); }
    public string StrokeColor { get => (string)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }

    private static void OnSamplesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is WaveformDrawable w) w.OnPropertyChanged(nameof(Samples));
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float width = dirtyRect.Width; float height = dirtyRect.Height; float centerY = height / 2f;
        canvas.Antialias = true; canvas.StrokeColor = Color.FromArgb(StrokeColor); canvas.StrokeSize = 2;

        canvas.StrokeDashPattern = new float[] { 3, 3 }; canvas.DrawLine(0, centerY, width, centerY); canvas.StrokeDashPattern = null;
        if (Samples == null || Samples.Length == 0) return;

        PathF path = new(); float xStep = width / (Samples.Length - 1);
        path.MoveTo(0, centerY + (Samples[0] * (centerY - 3f)));
        for (int i = 1; i < Samples.Length; i++) path.LineTo(i * xStep, centerY + (Samples[i] * (centerY - 3f)));
        canvas.DrawPath(path);
    }
}
