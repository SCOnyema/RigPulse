using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RigPulse.App.ViewModels;

namespace RigPulse.App.Views;

/// <summary>
/// The floating widget. Code-behind handles only window behaviour: dragging and placement.
/// </summary>
public partial class WidgetWindow : Window
{
    /// <summary>Used to place the window before layout has measured its real height.</summary>
    private const double EstimatedHeight = 160;

    private static readonly TimeSpan SavePositionDelay = TimeSpan.FromMilliseconds(500);

    private readonly WidgetViewModel _viewModel;
    private readonly DispatcherTimer _savePositionTimer;

    public WidgetWindow(WidgetViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = _viewModel = viewModel;

        // Moving fires LocationChanged many times per second; save once the window settles.
        _savePositionTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = SavePositionDelay };
        _savePositionTimer.Tick += (_, _) => SavePosition();

        _viewModel.CloseRequested += (_, _) => Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // The window handle exists but nothing is visible yet, so placing it here avoids a jump.
        var saved = _viewModel.SavedPosition is { } p ? new Point(p.Left, p.Top) : (Point?)null;
        MoveTo(WindowPlacement.Resolve(this, saved, new Size(Width, EstimatedHeight)));
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Re-clamp with the real measured size, then start tracking moves.
        MoveTo(WindowPlacement.Resolve(this, new Point(Left, Top), new Size(ActualWidth, ActualHeight)));
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (!e.Cancel)
        {
            SavePosition();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        LocationChanged -= OnLocationChanged;
        SizeChanged -= OnSizeChanged;
        base.OnClosed(e);
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _savePositionTimer.Stop();
        _savePositionTimer.Start();
    }

    /// <summary>
    /// Opening the details makes the window taller; nudge it up if it would run off the screen.
    /// </summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        MoveTo(WindowPlacement.Resolve(this, new Point(Left, Top), e.NewSize));

    private void SavePosition()
    {
        _savePositionTimer.Stop();
        _viewModel.SavePosition(Left, Top);
    }

    private void MoveTo(Point position)
    {
        Left = position.X;
        Top = position.Y;
    }
}
