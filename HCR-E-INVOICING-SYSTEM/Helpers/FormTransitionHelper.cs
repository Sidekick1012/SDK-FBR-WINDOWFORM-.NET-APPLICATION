using System;
using System.Drawing;
using System.Windows.Forms;

public static class FormTransitionHelper
{
    private static Timer activeFadeInTimer = null;

    public static void AnimateFadeIn(Form form, int durationMs = 250)
    {
        form.Opacity = 0;
        Timer timer = new Timer();
        timer.Interval = 10; // High frame rate
        double step = 10.0 / durationMs;

        timer.Tick += (s, e) =>
        {
            if (form.IsDisposed)
            {
                timer.Stop();
                timer.Dispose();
                return;
            }

            form.Opacity += step;
            if (form.Opacity >= 1)
            {
                form.Opacity = 1;
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }

    public static void AnimateFadeOut(Form form, Action onComplete, int durationMs = 150)
    {
        Timer timer = new Timer();
        timer.Interval = 10;
        double step = 10.0 / durationMs;

        timer.Tick += (s, e) =>
        {
            if (form.IsDisposed)
            {
                timer.Stop();
                timer.Dispose();
                onComplete?.Invoke();
                return;
            }

            form.Opacity -= step;
            if (form.Opacity <= 0)
            {
                form.Opacity = 0;
                timer.Stop();
                timer.Dispose();
                onComplete?.Invoke();
            }
        };
        timer.Start();
    }

    public static void NavigateTo(Form currentForm, Form targetForm, bool closeCurrent = false)
    {
        // Copy window state (Maximized, Normal, etc)
        targetForm.WindowState = currentForm.WindowState;
        targetForm.StartPosition = FormStartPosition.CenterScreen;

        AnimateFadeOut(currentForm, () =>
        {
            if (closeCurrent)
            {
                currentForm.Close();
            }
            else
            {
                currentForm.Hide();
            }

            targetForm.Show();
            AnimateFadeIn(targetForm);
        });
    }

    public static void ReturnToParent(Form currentForm, Form parentForm)
    {
        AnimateFadeOut(currentForm, () =>
        {
            currentForm.Hide(); // Hide first to prevent flickering
            parentForm.Show();
            AnimateFadeIn(parentForm);
            currentForm.Close();
        });
    }
}
