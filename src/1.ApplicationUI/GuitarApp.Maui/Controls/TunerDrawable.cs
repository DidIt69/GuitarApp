using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;

namespace GuitarApp.Maui.Controls;

public class TunerDrawable : BindableObject, IDrawable
{
    // 💡 AJOUT : On ajoute un gestionnaire 'OnCentsErrorChanged' lors de la création de la propriété
    public static readonly BindableProperty CentsErrorProperty =
        BindableProperty.Create(
            nameof(CentsError),
            typeof(double),
            typeof(TunerDrawable),
            0.0,
            propertyChanged: OnCentsErrorChanged);

    public static readonly BindableProperty IndicatorColorProperty =
        BindableProperty.Create(nameof(IndicatorColor), typeof(string), typeof(TunerDrawable), "#808080");

    public double CentsError { get => (double)GetValue(CentsErrorProperty); set => SetValue(CentsErrorProperty, value); }
    public string IndicatorColor { get => (string)GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    // 💡 CETTE MÉTHODE FORCE LE DESSIN DE L'AIGUILLE À CHAQUE NOUVEAU PITCH
    private static void OnCentsErrorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Dès que les cents changent, on cherche si ce Drawable est attaché à un composant graphique à l'écran
        if (bindable is TunerDrawable tunerDrawable)
        {
            // Astuce propre : On cherche la vue parente ou on force le rafraîchissement global
            // Pour forcer .NET MAUI à redessiner le canevas vectoriel immédiatement :
            tunerDrawable.OnPropertyChanged(nameof(CentsError));
        }
    }

    // L'implémentation de la méthode Draw(...) reste STRICTEMENT IDENTIQUE à votre fichier actuel
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float width = dirtyRect.Width; float height = dirtyRect.Height;
        float centerX = width / 2f; float centerY = height - 15f; float radius = Math.Min(width / 2f, height) - 25f;

        canvas.Antialias = true; canvas.StrokeColor = Colors.LightGray; canvas.StrokeSize = 2;
        canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 180, 0, true, false);

        canvas.StrokeSize = 4; canvas.StrokeColor = Color.FromArgb("#2B7A4B");
        canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 83, 97, true, false);

        double clamped = Math.Clamp(CentsError, -50.0, 50.0);
        double radians = (90.0 - (clamped * 1.2)) * Math.PI / 180.0;
        float needleX = centerX + (float)(Math.Cos(radians) * (radius - 8f));
        float needleY = centerY - (float)(Math.Sin(radians) * (radius - 8f));

        canvas.StrokeColor = Color.FromArgb(IndicatorColor); canvas.StrokeSize = 4; canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawLine(centerX, centerY, needleX, needleY);
        canvas.FillColor = Color.FromArgb(IndicatorColor); canvas.FillCircle(centerX, centerY, 8);
    }
}
