using System;
using System.Drawing;
using System.Windows.Forms;

public static class FormTransitionHelper
{
    private static Timer activeFadeInTimer = null;

    public static void AnimateFadeIn(Form form, int durationMs = 250, Action onComplete = null)
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
                onComplete?.Invoke();
                return;
            }

            form.Opacity += step;
            if (form.Opacity >= 1)
            {
                form.Opacity = 1;
                timer.Stop();
                timer.Dispose();
                onComplete?.Invoke();
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

        // Set targetForm opacity to 0 first, then show it (so it overlays currentForm)
        targetForm.Opacity = 0;
        targetForm.Show();

        // Fade in targetForm. Once done, hide or close currentForm.
        AnimateFadeIn(targetForm, 200, () =>
        {
            if (currentForm != null && !currentForm.IsDisposed)
            {
                if (closeCurrent)
                {
                    currentForm.Close();
                }
                else
                {
                    currentForm.Hide();
                }
            }
        });
    }

    public static void ReturnToParent(Form currentForm, Form parentForm)
    {
        // Copy window state
        parentForm.WindowState = currentForm.WindowState;
        parentForm.StartPosition = FormStartPosition.CenterScreen;

        // Set parentForm opacity to 0 first, then show it (so it overlays currentForm)
        parentForm.Opacity = 0;
        parentForm.Show();

        // Fade in parentForm. Once done, hide or close currentForm.
        AnimateFadeIn(parentForm, 200, () =>
        {
            if (currentForm != null && !currentForm.IsDisposed)
            {
                currentForm.Hide();
                currentForm.Close();
            }
        });
    }
}

