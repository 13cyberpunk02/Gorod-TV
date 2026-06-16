using GorodTV.Core.ViewModels;

namespace GorodTV.Pages;

public partial class LoginPage : ContentPage
{
    private CancellationTokenSource? _blobAnimCts;
    public LoginPage(LoginViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;	
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _blobAnimCts = new CancellationTokenSource();
        // три эллипса плывут с разной скоростью и амплитудой
        _ = FloatBlob(Blob1, 80, -90, 24, 18, 5200, _blobAnimCts.Token);
        _ = FloatBlob(Blob2, -70, 60, -20, -26, 6400, _blobAnimCts.Token);
        _ = FloatBlob(Blob3, -50, -60, 16, 22, 4300, _blobAnimCts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _blobAnimCts?.Cancel();
        _blobAnimCts = null;
    }

    /// <summary>
    /// Бесконечное мягкое "плавание" эллипса вокруг базовой точки.
    /// </summary>
    private static async Task FloatBlob(VisualElement blob,
        double baseX, double baseY, double dx, double dy,
        uint duration, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await blob.TranslateToAsync(baseX + dx, baseY + dy, duration, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;
                await blob.TranslateToAsync(baseX, baseY, duration, Easing.SinInOut);
            }
        }
        catch (Exception)
        {
            // страница могла быть выгружена — просто выходим
        }
    }
}