using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OneThing90.Core;
using OneThing90.Services;

namespace OneThing90.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly StateStore _store;
    private readonly StartupService _startupService;
    private readonly DispatcherTimer _sessionTimer;
    private readonly DispatcherTimer _reminderTimer;
    private readonly AppState _state;

    private DateTime _sessionStartedAtLocal;
    private DateTime? _currentRunStartedAtLocal;
    private TimeSpan _elapsedBeforePause = TimeSpan.Zero;
    private TimeSpan _sessionGoal = TimeSpan.FromMinutes(90);
    private bool _isPaused;
    private bool _isTimerRunning;
    private string _thingName = string.Empty;
    private string _why = string.Empty;
    private string _startDateText = string.Empty;
    private string _targetMinutesText = string.Empty;
    private string _reminderStartText = string.Empty;
    private string _reminderEndText = string.Empty;
    private string _reminderIntervalText = string.Empty;
    private bool _remindersEnabled;
    private bool _minimizeToTray;
    private bool _launchAtLogin;
    private string _themeMode = "System";
    private string _accentColor = "Teal";
    private string _densityMode = "Comfortable";
    private string _customAccentHex = "#0D9488";
    private bool _useCustomWindowColor;
    private string _customWindowHex = "#202020";
    private string _timerDisplay = "90:00";
    private string _timerSubtitle = "Готов к старту.";
    private string _pauseButtonText = "Пауза";
    private double _timerProgress;
    private double _planProgress;
    private string _planDayText = string.Empty;
    private string _todayStatusText = string.Empty;
    private string _todayMinutesText = string.Empty;
    private string _daysCompletedText = string.Empty;
    private string _streakText = string.Empty;
    private string _daysRemainingText = string.Empty;
    private string _reminderStatusText = string.Empty;
    private string _bannerTitle = string.Empty;
    private string _bannerMessage = string.Empty;
    private Visibility _bannerVisibility = Visibility.Collapsed;

    public MainViewModel(StateStore store, StartupService startupService)
    {
        _store = store;
        _startupService = startupService;
        _state = _store.Load();
        NormalizeLegacyDefaults();
        _state.Settings.LaunchAtLogin = _startupService.IsEnabled();

        Days = [];

        SavePlanCommand = new RelayCommand(SavePlan);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        SetThemeCommand = new RelayCommand(parameter => SetTheme(parameter as string));
        SetAccentCommand = new RelayCommand(parameter => SetAccent(parameter as string));
        SetDensityCommand = new RelayCommand(parameter => SetDensity(parameter as string));
        ApplyCustomColorsCommand = new RelayCommand(ApplyCustomColors);
        ResetCustomColorsCommand = new RelayCommand(ResetCustomColors);
        StartSessionCommand = new RelayCommand(StartSessionFromPreset);
        StartFocusCommand = new RelayCommand(() => StartSession(_state.Plan.TargetMinutes));
        StartRescueCommand = new RelayCommand(() => StartSession(15));
        PauseResumeCommand = new RelayCommand(PauseOrResume);
        FinishCommand = new RelayCommand(FinishCurrentSession);
        Snooze15Command = new RelayCommand(() => Snooze(TimeSpan.FromMinutes(15)));
        Snooze60Command = new RelayCommand(() => Snooze(TimeSpan.FromHours(1)));
        SnoozeTodayCommand = new RelayCommand(SnoozeUntilTomorrow);
        DismissBannerCommand = new RelayCommand(() => BannerVisibility = Visibility.Collapsed);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);

        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sessionTimer.Tick += (_, _) => TickSession();

        _reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _reminderTimer.Tick += (_, _) => CheckReminder(DateTime.Now);
        _reminderTimer.Start();

        LoadEditableFields();
        RefreshAll();
        CheckReminder(DateTime.Now);
    }

    public event EventHandler<NotificationRequest>? NotificationRequested;
    public event EventHandler? AppearanceChanged;

    public ObservableCollection<DayCellViewModel> Days { get; }

    public ICommand SavePlanCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand SetThemeCommand { get; }
    public ICommand SetAccentCommand { get; }
    public ICommand SetDensityCommand { get; }
    public ICommand ApplyCustomColorsCommand { get; }
    public ICommand ResetCustomColorsCommand { get; }
    public ICommand StartSessionCommand { get; }
    public ICommand StartFocusCommand { get; }
    public ICommand StartRescueCommand { get; }
    public ICommand PauseResumeCommand { get; }
    public ICommand FinishCommand { get; }
    public ICommand Snooze15Command { get; }
    public ICommand Snooze60Command { get; }
    public ICommand SnoozeTodayCommand { get; }
    public ICommand DismissBannerCommand { get; }
    public ICommand OpenDataFolderCommand { get; }

    public string ThingName
    {
        get => _thingName;
        set => SetProperty(ref _thingName, value);
    }

    public string Why
    {
        get => _why;
        set => SetProperty(ref _why, value);
    }

    public string StartDateText
    {
        get => _startDateText;
        set => SetProperty(ref _startDateText, value);
    }

    public string TargetMinutesText
    {
        get => _targetMinutesText;
        set => SetProperty(ref _targetMinutesText, value);
    }

    public string ReminderStartText
    {
        get => _reminderStartText;
        set => SetProperty(ref _reminderStartText, value);
    }

    public string ReminderEndText
    {
        get => _reminderEndText;
        set => SetProperty(ref _reminderEndText, value);
    }

    public string ReminderIntervalText
    {
        get => _reminderIntervalText;
        set => SetProperty(ref _reminderIntervalText, value);
    }

    public bool RemindersEnabled
    {
        get => _remindersEnabled;
        set => SetProperty(ref _remindersEnabled, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool LaunchAtLogin
    {
        get => _launchAtLogin;
        set => SetProperty(ref _launchAtLogin, value);
    }

    public string ThemeMode
    {
        get => _themeMode;
        set => SetProperty(ref _themeMode, value);
    }

    public string AccentColor
    {
        get => _accentColor;
        set => SetProperty(ref _accentColor, value);
    }

    public string DensityMode
    {
        get => _densityMode;
        set => SetProperty(ref _densityMode, value);
    }

    public string CustomAccentHex
    {
        get => _customAccentHex;
        set => SetProperty(ref _customAccentHex, value);
    }

    public bool UseCustomWindowColor
    {
        get => _useCustomWindowColor;
        set => SetProperty(ref _useCustomWindowColor, value);
    }

    public string CustomWindowHex
    {
        get => _customWindowHex;
        set => SetProperty(ref _customWindowHex, value);
    }

    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        private set => SetProperty(ref _isTimerRunning, value);
    }

    public string TimerDisplay
    {
        get => _timerDisplay;
        private set => SetProperty(ref _timerDisplay, value);
    }

    public string TimerSubtitle
    {
        get => _timerSubtitle;
        private set => SetProperty(ref _timerSubtitle, value);
    }

    public string PauseButtonText
    {
        get => _pauseButtonText;
        private set => SetProperty(ref _pauseButtonText, value);
    }

    public double TimerProgress
    {
        get => _timerProgress;
        private set => SetProperty(ref _timerProgress, value);
    }

    public double PlanProgress
    {
        get => _planProgress;
        private set => SetProperty(ref _planProgress, value);
    }

    public string PlanDayText
    {
        get => _planDayText;
        private set => SetProperty(ref _planDayText, value);
    }

    public string TodayStatusText
    {
        get => _todayStatusText;
        private set => SetProperty(ref _todayStatusText, value);
    }

    public string TodayMinutesText
    {
        get => _todayMinutesText;
        private set => SetProperty(ref _todayMinutesText, value);
    }

    public string DaysCompletedText
    {
        get => _daysCompletedText;
        private set => SetProperty(ref _daysCompletedText, value);
    }

    public string StreakText
    {
        get => _streakText;
        private set => SetProperty(ref _streakText, value);
    }

    public string DaysRemainingText
    {
        get => _daysRemainingText;
        private set => SetProperty(ref _daysRemainingText, value);
    }

    public string ReminderStatusText
    {
        get => _reminderStatusText;
        private set => SetProperty(ref _reminderStatusText, value);
    }

    public string BannerTitle
    {
        get => _bannerTitle;
        private set => SetProperty(ref _bannerTitle, value);
    }

    public string BannerMessage
    {
        get => _bannerMessage;
        private set => SetProperty(ref _bannerMessage, value);
    }

    public Visibility BannerVisibility
    {
        get => _bannerVisibility;
        private set => SetProperty(ref _bannerVisibility, value);
    }

    private void LoadEditableFields()
    {
        ThingName = _state.Plan.ThingName;
        Why = _state.Plan.Why;
        StartDateText = _state.Plan.StartedOnLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        TargetMinutesText = _state.Plan.TargetMinutes.ToString(CultureInfo.InvariantCulture);

        var settings = _state.Settings;
        RemindersEnabled = settings.RemindersEnabled;
        MinimizeToTray = settings.MinimizeToTray;
        LaunchAtLogin = settings.LaunchAtLogin;
        ThemeMode = NormalizeOption(settings.ThemeMode, ["System", "Dark", "Light", "Paper"], "System");
        AccentColor = NormalizeOption(settings.AccentColor, ["Teal", "Blue", "Green", "Violet", "Amber", "Rose", "Custom"], "Teal");
        DensityMode = NormalizeOption(settings.DensityMode, ["Compact", "Comfortable", "Spacious"], "Comfortable");
        CustomAccentHex = TryNormalizeHex(settings.CustomAccentHex, out var customAccentHex) ? customAccentHex : "#0D9488";
        UseCustomWindowColor = settings.UseCustomWindowColor;
        CustomWindowHex = TryNormalizeHex(settings.CustomWindowHex, out var customWindowHex) ? customWindowHex : "#202020";
        ReminderStartText = FormatClock(settings.ReminderStartHour, settings.ReminderStartMinute);
        ReminderEndText = FormatClock(settings.ReminderEndHour, settings.ReminderEndMinute);
        ReminderIntervalText = settings.ReminderIntervalMinutes.ToString(CultureInfo.InvariantCulture);
    }

    private void SavePlan()
    {
        var name = string.IsNullOrWhiteSpace(ThingName) ? "Главное дело" : ThingName.Trim();
        var reason = string.IsNullOrWhiteSpace(Why) ? "Делать то, что важно, даже после тяжелого дня." : Why.Trim();

        if (!DateTime.TryParseExact(StartDateText.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var startedOn))
        {
            ShowBanner("Проверь дату старта", "Используй формат yyyy-mm-dd, чтобы 90-дневный путь считался точно.");
            return;
        }

        if (!int.TryParse(TargetMinutesText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetMinutes)
            || targetMinutes is < 5 or > 240)
        {
            ShowBanner("Проверь цель", "Количество минут должно быть от 5 до 240.");
            return;
        }

        _state.Plan.ThingName = name;
        _state.Plan.Why = reason;
        _state.Plan.StartedOnLocal = startedOn.Date;
        _state.Plan.TargetMinutes = targetMinutes;

        _store.Save(_state);
        LoadEditableFields();
        RefreshAll();
        ShowBanner("План сохранен", "Главное дело зафиксировано.");
    }

    private void SaveSettings()
    {
        if (!TryParseClock(ReminderStartText, out var start))
        {
            ShowBanner("Проверь начало напоминаний", "Используй 24-часовой формат, например 19:00.");
            return;
        }

        if (!TryParseClock(ReminderEndText, out var end))
        {
            ShowBanner("Проверь конец напоминаний", "Используй 24-часовой формат, например 23:00.");
            return;
        }

        if (!int.TryParse(ReminderIntervalText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval)
            || interval is < 5 or > 240)
        {
            ShowBanner("Проверь интервал", "Укажи число от 5 до 240 минут.");
            return;
        }

        _state.Settings.RemindersEnabled = RemindersEnabled;
        _state.Settings.MinimizeToTray = MinimizeToTray;
        _state.Settings.LaunchAtLogin = LaunchAtLogin;
        _state.Settings.ThemeMode = NormalizeOption(ThemeMode, ["System", "Dark", "Light", "Paper"], "System");
        _state.Settings.AccentColor = NormalizeOption(AccentColor, ["Teal", "Blue", "Green", "Violet", "Amber", "Rose", "Custom"], "Teal");
        _state.Settings.DensityMode = NormalizeOption(DensityMode, ["Compact", "Comfortable", "Spacious"], "Comfortable");
        _state.Settings.CustomAccentHex = TryNormalizeHex(CustomAccentHex, out var customAccentHex) ? customAccentHex : "#0D9488";
        _state.Settings.UseCustomWindowColor = UseCustomWindowColor;
        _state.Settings.CustomWindowHex = TryNormalizeHex(CustomWindowHex, out var customWindowHex) ? customWindowHex : "#202020";
        _state.Settings.ReminderStartHour = start.Hours;
        _state.Settings.ReminderStartMinute = start.Minutes;
        _state.Settings.ReminderEndHour = end.Hours;
        _state.Settings.ReminderEndMinute = end.Minutes;
        _state.Settings.ReminderIntervalMinutes = interval;

        try
        {
            _startupService.SetEnabled(LaunchAtLogin);
        }
        catch
        {
            _state.Settings.LaunchAtLogin = _startupService.IsEnabled();
            LaunchAtLogin = _state.Settings.LaunchAtLogin;
            ShowBanner("Автозапуск не изменен", "Windows не разрешила обновить запись автозапуска.");
        }

        _store.Save(_state);
        LoadEditableFields();
        RefreshAll();
        AppearanceChanged?.Invoke(this, EventArgs.Empty);
        ShowBanner("Настройки сохранены", "Правила напоминаний обновлены.");
    }

    private void SetTheme(string? theme)
    {
        var normalizedTheme = NormalizeOption(theme, ["System", "Dark", "Light", "Paper"], "System");
        ThemeMode = normalizedTheme;
        _state.Settings.ThemeMode = normalizedTheme;
        SaveAppearanceOnly();
    }

    private void SetAccent(string? accent)
    {
        var normalizedAccent = NormalizeOption(accent, ["Teal", "Blue", "Green", "Violet", "Amber", "Rose", "Custom"], "Teal");
        AccentColor = normalizedAccent;
        _state.Settings.AccentColor = normalizedAccent;
        SaveAppearanceOnly();
    }

    private void SetDensity(string? density)
    {
        var normalizedDensity = NormalizeOption(density, ["Compact", "Comfortable", "Spacious"], "Comfortable");
        DensityMode = normalizedDensity;
        _state.Settings.DensityMode = normalizedDensity;
        SaveAppearanceOnly();
    }

    private void SaveAppearanceOnly()
    {
        _store.Save(_state);
        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyCustomColors()
    {
        if (!TryNormalizeHex(CustomAccentHex, out var accentHex))
        {
            ShowBanner("Проверь цвет акцента", "Укажи HEX в формате #0D9488 или 0D9488.");
            return;
        }

        if (UseCustomWindowColor && !TryNormalizeHex(CustomWindowHex, out _))
        {
            ShowBanner("Проверь цвет фона", "Укажи HEX в формате #202020 или 202020.");
            return;
        }

        CustomAccentHex = accentHex;
        AccentColor = "Custom";
        _state.Settings.CustomAccentHex = accentHex;
        _state.Settings.AccentColor = "Custom";
        _state.Settings.UseCustomWindowColor = UseCustomWindowColor;
        _state.Settings.CustomWindowHex = TryNormalizeHex(CustomWindowHex, out var windowHex) ? windowHex : "#202020";
        CustomWindowHex = _state.Settings.CustomWindowHex;

        SaveAppearanceOnly();
        ShowBanner("Цвета применены", "Пользовательская палитра сохранена.");
    }

    private void ResetCustomColors()
    {
        AccentColor = "Teal";
        CustomAccentHex = "#0D9488";
        UseCustomWindowColor = false;
        CustomWindowHex = "#202020";

        _state.Settings.AccentColor = AccentColor;
        _state.Settings.CustomAccentHex = CustomAccentHex;
        _state.Settings.UseCustomWindowColor = UseCustomWindowColor;
        _state.Settings.CustomWindowHex = CustomWindowHex;

        SaveAppearanceOnly();
        ShowBanner("Цвета сброшены", "Вернулась стандартная палитра OneThing90.");
    }

    public void StartDefaultSessionFromTray()
    {
        StartSession(_state.Plan.TargetMinutes);
    }

    public void SnoozeFromTray(TimeSpan duration)
    {
        Snooze(duration);
    }

    private void StartSessionFromPreset(object? parameter)
    {
        var text = parameter?.ToString();
        var minutes = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMinutes)
            ? parsedMinutes
            : _state.Plan.TargetMinutes;

        StartSession(Math.Clamp(minutes, 5, 240));
    }

    private void StartSession(int minutes)
    {
        if (IsTimerRunning)
        {
            ShowBanner("Сессия уже идет", "Заверши или поставь текущий блок на паузу перед новым стартом.");
            return;
        }

        _sessionGoal = TimeSpan.FromMinutes(minutes);
        _sessionStartedAtLocal = DateTime.Now;
        _currentRunStartedAtLocal = _sessionStartedAtLocal;
        _elapsedBeforePause = TimeSpan.Zero;
        _isPaused = false;
        IsTimerRunning = true;
        PauseButtonText = "Пауза";
        BannerVisibility = Visibility.Collapsed;
        _state.SnoozedUntilLocal = null;
        _store.Save(_state);

        TickSession();
        _sessionTimer.Start();
    }

    private void PauseOrResume()
    {
        if (!IsTimerRunning)
        {
            return;
        }

        if (_isPaused)
        {
            _currentRunStartedAtLocal = DateTime.Now;
            _isPaused = false;
            PauseButtonText = "Пауза";
            TimerSubtitle = "Фокус-сессия идет.";
            _sessionTimer.Start();
            return;
        }

        _elapsedBeforePause = GetElapsed();
        _currentRunStartedAtLocal = null;
        _isPaused = true;
        PauseButtonText = "Продолжить";
        TimerSubtitle = "Пауза. Сессию можно продолжить.";
        _sessionTimer.Stop();
        TickSession();
    }

    private void FinishCurrentSession()
    {
        if (!IsTimerRunning)
        {
            return;
        }

        CompleteSession(wasAutomatic: false);
    }

    private void TickSession()
    {
        if (!IsTimerRunning)
        {
            TimerDisplay = $"{_state.Plan.TargetMinutes:00}:00";
            TimerSubtitle = "Готов к старту.";
            TimerProgress = 0;
            return;
        }

        var elapsed = GetElapsed();
        if (elapsed >= _sessionGoal)
        {
            CompleteSession(wasAutomatic: true);
            return;
        }

        var remaining = _sessionGoal - elapsed;
        TimerDisplay = FormatDuration(remaining);
        TimerProgress = Math.Clamp(elapsed.TotalSeconds / _sessionGoal.TotalSeconds * 100, 0, 100);
        TimerSubtitle = _isPaused
            ? "Пауза. Продолжай, когда будешь готов."
            : $"В работе: {_state.Plan.ThingName}.";
    }

    private void CompleteSession(bool wasAutomatic)
    {
        var elapsed = wasAutomatic ? _sessionGoal : GetElapsed();
        var minutes = Math.Max(1, (int)Math.Round(elapsed.TotalMinutes, MidpointRounding.AwayFromZero));

        _sessionTimer.Stop();
        IsTimerRunning = false;
        _currentRunStartedAtLocal = null;
        _elapsedBeforePause = TimeSpan.Zero;
        _isPaused = false;
        PauseButtonText = "Пауза";

        _state.Sessions.Add(new FocusSession
        {
            StartedAtLocal = _sessionStartedAtLocal,
            EndedAtLocal = DateTime.Now,
            Minutes = minutes,
            CompletedTarget = minutes >= _state.Plan.TargetMinutes,
            Note = string.Empty
        });

        _store.Save(_state);
        RefreshAll();

        if (minutes >= _state.Plan.TargetMinutes)
        {
            ShowBanner("Сегодня закрыто", "90-минутная сессия записана.");
            NotificationRequested?.Invoke(this, new NotificationRequest("OneThing90", "Сегодня закрыто. Главное дело сдвинулось вперед."));
        }
        else
        {
            ShowBanner("Частичная сессия сохранена", $"Записано минут: {minutes}. Сегодня еще можно добить цель.");
        }

        TickSession();
    }

    private TimeSpan GetElapsed()
    {
        if (!IsTimerRunning)
        {
            return TimeSpan.Zero;
        }

        if (_isPaused || _currentRunStartedAtLocal is null)
        {
            return _elapsedBeforePause;
        }

        return _elapsedBeforePause + (DateTime.Now - _currentRunStartedAtLocal.Value);
    }

    private void Snooze(TimeSpan duration)
    {
        _state.SnoozedUntilLocal = DateTime.Now.Add(duration);
        _store.Save(_state);
        RefreshAll();
        ShowBanner("Отложено", $"Следующее напоминание после {_state.SnoozedUntilLocal:HH:mm}.");
    }

    private void SnoozeUntilTomorrow()
    {
        var tomorrow = DateTime.Today.AddDays(1).Add(GetReminderStart());
        _state.SnoozedUntilLocal = tomorrow;
        _store.Save(_state);
        RefreshAll();
        ShowBanner("Сегодня пропущено", $"Следующее напоминание завтра в {tomorrow:HH:mm}.");
    }

    private void CheckReminder(DateTime now)
    {
        RefreshReminderStatus();

        if (!ShouldSendReminder(now))
        {
            return;
        }

        _state.LastReminderLocal = now;
        _store.Save(_state);

        var title = "Главное дело простаивает";
        var message = $"Сегодня еще нет полной сессии по делу: {_state.Plan.ThingName}. Запустить {_state.Plan.TargetMinutes} минут?";
        ShowBanner(title, message);
        NotificationRequested?.Invoke(this, new NotificationRequest("OneThing90", message));
    }

    private bool ShouldSendReminder(DateTime now)
    {
        if (!_state.Settings.RemindersEnabled || IsTimerRunning || IsDayComplete(now.Date))
        {
            return false;
        }

        if (!IsInsideReminderWindow(now.TimeOfDay))
        {
            return false;
        }

        if (_state.SnoozedUntilLocal is { } snoozedUntil && snoozedUntil > now)
        {
            return false;
        }

        if (_state.LastReminderLocal is { } last
            && last.Date == now.Date
            && now - last < TimeSpan.FromMinutes(_state.Settings.ReminderIntervalMinutes))
        {
            return false;
        }

        return true;
    }

    private void RefreshAll()
    {
        RefreshMetrics();
        RefreshDays();
        RefreshReminderStatus();
    }

    private void RefreshMetrics()
    {
        var today = DateTime.Today;
        var start = _state.Plan.StartedOnLocal.Date;
        var dayNumber = (today - start).Days + 1;
        var completedDays = CountCompletedDays();
        var todayMinutes = GetMinutesForDay(today);
        var streak = CalculateStreak();

        PlanProgress = Math.Clamp(completedDays / (double)_state.Plan.DurationDays * 100, 0, 100);
        PlanDayText = dayNumber < 1
            ? $"Старт через {Math.Abs(dayNumber) + 1} дн."
            : dayNumber > _state.Plan.DurationDays
                ? "90-дневный путь завершен"
                : $"День {dayNumber} из {_state.Plan.DurationDays}";

        TodayMinutesText = $"{todayMinutes} / {_state.Plan.TargetMinutes} мин";
        DaysCompletedText = $"{completedDays} / {_state.Plan.DurationDays}";
        StreakText = $"{streak} дн. подряд";
        DaysRemainingText = $"{Math.Max(0, _state.Plan.DurationDays - completedDays)} осталось";

        TodayStatusText = todayMinutes >= _state.Plan.TargetMinutes
            ? "Полная сессия сегодня записана."
            : todayMinutes > 0
                ? "Есть частичный прогресс. Добей блок."
                : "Сегодня сессии еще не было.";
    }

    private void RefreshDays()
    {
        Days.Clear();

        var start = _state.Plan.StartedOnLocal.Date;
        for (var i = 0; i < _state.Plan.DurationDays; i++)
        {
            var date = start.AddDays(i);
            var minutes = GetMinutesForDay(date);
            var status = GetDayStatus(date, minutes);

            Days.Add(new DayCellViewModel
            {
                Number = i + 1,
                DateText = date.ToString("dd.MM", CultureInfo.InvariantCulture),
                Status = status,
                ToolTip = $"{date:yyyy-MM-dd}: {minutes} мин"
            });
        }
    }

    private void RefreshReminderStatus()
    {
        if (!_state.Settings.RemindersEnabled)
        {
            ReminderStatusText = "Напоминания выключены.";
            return;
        }

        if (IsDayComplete(DateTime.Today))
        {
            ReminderStatusText = "Напоминать не нужно. Сегодняшняя цель закрыта.";
            return;
        }

        if (_state.SnoozedUntilLocal is { } snoozedUntil && snoozedUntil > DateTime.Now)
        {
            ReminderStatusText = $"Отложено до {snoozedUntil:HH:mm}.";
            return;
        }

        var window = $"{FormatClock(_state.Settings.ReminderStartHour, _state.Settings.ReminderStartMinute)}-{FormatClock(_state.Settings.ReminderEndHour, _state.Settings.ReminderEndMinute)}";
        ReminderStatusText = IsInsideReminderWindow(DateTime.Now.TimeOfDay)
            ? $"Сейчас слежу. Окно напоминаний: {window}."
            : $"Следующее окно напоминаний: {window}.";
    }

    private void NormalizeLegacyDefaults()
    {
        var changed = false;

        if (_state.Plan.ThingName == "One important thing")
        {
            _state.Plan.ThingName = "Главное дело";
            changed = true;
        }

        if (_state.Plan.Why is "Show up for the work that matters." or "Build it for 90 focused days.")
        {
            _state.Plan.Why = "Делать то, что важно, даже после тяжелого дня.";
            changed = true;
        }

        if (changed)
        {
            _store.Save(_state);
        }
    }

    private int CountCompletedDays()
    {
        var start = _state.Plan.StartedOnLocal.Date;
        var end = start.AddDays(_state.Plan.DurationDays - 1);
        var count = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (IsDayComplete(date))
            {
                count++;
            }
        }

        return count;
    }

    private int CalculateStreak()
    {
        var start = _state.Plan.StartedOnLocal.Date;
        var date = DateTime.Today;

        if (!IsDayComplete(date))
        {
            date = date.AddDays(-1);
        }

        var streak = 0;
        while (date >= start && IsDayComplete(date))
        {
            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }

    private bool IsDayComplete(DateTime date)
    {
        return GetMinutesForDay(date.Date) >= _state.Plan.TargetMinutes;
    }

    private int GetMinutesForDay(DateTime date)
    {
        return _state.Sessions
            .Where(session => session.EndedAtLocal.Date == date.Date)
            .Sum(session => Math.Max(0, session.Minutes));
    }

    private string GetDayStatus(DateTime date, int minutes)
    {
        if (minutes >= _state.Plan.TargetMinutes)
        {
            return "Complete";
        }

        if (date.Date == DateTime.Today)
        {
            return minutes > 0 ? "PartialToday" : "Today";
        }

        if (date.Date < DateTime.Today)
        {
            return minutes > 0 ? "Partial" : "Missed";
        }

        return "Future";
    }

    private bool IsInsideReminderWindow(TimeSpan time)
    {
        var start = GetReminderStart();
        var end = GetReminderEnd();

        return start <= end
            ? time >= start && time <= end
            : time >= start || time <= end;
    }

    private TimeSpan GetReminderStart()
    {
        return new TimeSpan(_state.Settings.ReminderStartHour, _state.Settings.ReminderStartMinute, 0);
    }

    private TimeSpan GetReminderEnd()
    {
        return new TimeSpan(_state.Settings.ReminderEndHour, _state.Settings.ReminderEndMinute, 0);
    }

    private static bool TryParseClock(string value, out TimeSpan time)
    {
        return TimeSpan.TryParseExact(value.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time)
            || TimeSpan.TryParseExact(value.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time);
    }

    private static string FormatClock(int hour, int minute)
    {
        return $"{hour:00}:{minute:00}";
    }

    private static string FormatDuration(TimeSpan value)
    {
        var totalMinutes = Math.Max(0, (int)value.TotalMinutes);
        return $"{totalMinutes:00}:{value.Seconds:00}";
    }

    private static string NormalizeOption(string? value, string[] allowedValues, string fallback)
    {
        return allowedValues.FirstOrDefault(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
            ?? fallback;
    }

    private static bool TryNormalizeHex(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length != 6 || !hex.All(Uri.IsHexDigit))
        {
            return false;
        }

        normalized = $"#{hex.ToUpperInvariant()}";
        return true;
    }

    private void ShowBanner(string title, string message)
    {
        BannerTitle = title;
        BannerMessage = message;
        BannerVisibility = Visibility.Visible;
    }

    private void OpenDataFolder()
    {
        Directory.CreateDirectory(_store.AppDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _store.AppDirectory,
            UseShellExecute = true
        });
    }
}
