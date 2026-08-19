# Turbo Switcher
Бесплатный открытый аналог Punto Switcher для Windows с обычным окном настроек и иконкой в трее.

Собрано на **.NET 8**, не на .NET Framework. `publish.bat` делает два exe.

`ghbdtn` → `привет`, `руддщ` → `hello`. После замены программа переключает раскладку окна.

## Сборка

Нужен [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) только у разработчика.

```bat
publish.bat
```

Готовые файлы (номер берётся из `<Version>` в проекте, сейчас `1.0.0`):

- `publish\TurboSwitcher 1.0.0.exe` — со встроенным runtime, копируется на другой ПК как есть
- `publish\TurboSwitcher 1.0.0-net8.exe` — маленький, на компьютере должен быть [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

Запуск из исходников (нужен SDK):

```bat
dotnet run --project src\TypoSwitch\TypoSwitch.csproj
```

Тесты:

```bat
dotnet test
```

## Возможности

- Автоисправление после пробела / Enter
- **Pause** — сменить последнее слово
- **Shift+Pause** — сменить выделенный текст
- Окно настроек, иконка в трее
- Автозагрузка с Windows
- Исключения (`lol`, `github`, `qwe`…) и игнор процессов
- Caps Lock отключает автозамену

Настройки: `%APPDATA%\Turbo Switcher\config.json`.
