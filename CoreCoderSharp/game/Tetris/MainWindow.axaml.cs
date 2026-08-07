using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Tetris;

public partial class MainWindow : Window
{
    private readonly GameEngine _engine = new();
    private DispatcherTimer? _timer;
    private bool _running;
    private bool _wasGameOver;

    public MainWindow()
    {
        InitializeComponent();

        GameBoard.Engine = _engine;
        BestText.Text = HighScoreManager.Load().ToString();

        SoundManager.Init();

        KeyDown += OnKeyDown;
        Closed += (_, _) => StopTimer();

        Restart();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                if (_engine.MoveLeft()) SoundManager.Play("move");
                break;
            case Key.Right:
                if (_engine.MoveRight()) SoundManager.Play("move");
                break;
            case Key.Down:
                _engine.SoftDrop();
                break;
            case Key.Up:
                if (_engine.Rotate()) SoundManager.Play("rotate");
                break;
            case Key.Space:
                if (!_engine.IsGameOver && !_engine.IsPaused)
                {
                    _engine.HardDrop();
                    AfterLock();
                }
                break;
            case Key.C:
            case Key.LeftShift:
            case Key.RightShift:
                if (_engine.Hold()) SoundManager.Play("hold");
                break;
            case Key.P:
                TogglePause();
                break;
            case Key.R:
                Restart();
                break;
            default:
                return;
        }
        Refresh();
    }

    /// <summary>开始（或继续）一局游戏。</summary>
    private void Start()
    {
        _running = true;
        _engine.IsPaused = false;
        PauseButton.Content = "暂停";
        StartTimer();
    }

    /// <summary>重新开始。</summary>
    private void Restart()
    {
        _engine.Reset();
        _wasGameOver = false;
        Start();
        Refresh();
    }

    private void TogglePause()
    {
        if (_engine.IsGameOver) return;

        _engine.IsPaused = !_engine.IsPaused;
        PauseButton.Content = _engine.IsPaused ? "继续" : "暂停";

        if (_engine.IsPaused) StopTimer();
        else StartTimer();
        Refresh();
    }

    private void OnPauseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => TogglePause();

    private void OnRestartClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Restart();

    private void OnSoundClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SoundManager.Enabled = !SoundManager.Enabled;
        SoundButton.Content = SoundManager.Enabled ? "音效:开" : "音效:关";
        if (SoundManager.Enabled) SoundManager.Play("rotate");
    }

    private void StartTimer()
    {
        StopTimer();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_engine.DropIntervalMs),
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_running || _engine.IsPaused || _engine.IsGameOver) return;

        if (!_engine.SoftDrop())
        {
            // 落到底部 → 锁定 → 消行
            _engine.HardDrop();
            AfterLock();
        }
        Refresh();
    }

    /// <summary>方块锁定后：播放音效、更新计分与速度。</summary>
    private void AfterLock()
    {
        if (_engine.LastClearCount > 0) SoundManager.Play("clear");
        else SoundManager.Play("drop");

        // 消行后速度可能变化 → 重建计时器
        StopTimer();
        if (!_engine.IsGameOver && !_engine.IsPaused)
            StartTimer();
    }

    /// <summary>刷新全部界面元素。</summary>
    private void Refresh()
    {
        ScoreText.Text = _engine.Score.ToString();
        LevelText.Text = _engine.Level.ToString();
        LinesText.Text = _engine.LinesCleared.ToString();

        HoldPreview.Piece = _engine.HeldType;
        Preview1.Piece = _engine.GetNextType(0);
        Preview2.Piece = _engine.GetNextType(1);
        Preview3.Piece = _engine.GetNextType(2);

        GameBoard.InvalidateVisual();
        HoldPreview.InvalidateVisual();
        Preview1.InvalidateVisual();
        Preview2.InvalidateVisual();
        Preview3.InvalidateVisual();

        // 游戏结束边沿：保存最高分 + 音效
        if (_engine.IsGameOver && !_wasGameOver)
        {
            _wasGameOver = true;
            StopTimer();
            _running = false;
            PauseButton.Content = "暂停";
            if (HighScoreManager.SaveIfHigher(_engine.Score))
                BestText.Text = _engine.Score.ToString();
            SoundManager.Play("over");
        }
    }
}
