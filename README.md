# Typo Switcher
Бесплатный открытый аналог Punto Switcher для Windows с обычным окном настроек и иконкой в трее.

Собрано на **.NET 8**, не на .NET Framework. `publish.bat` делает один `TypoSwitch.exe` со встроенным runtime: на другой компьютер его можно скопировать как есть, ничего ставить не нужно.

`ghbdtn` → `привет`, `руддщ` → `hello`. После замены программа переключает раскладку окна.

## Сборка

Нужен [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) только у разработчика. Пользователю SDK не нужен.

```bat
publish.bat
```

Готовый файл: `publish\TypoSwitch.exe`.

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

Настройки: `%APPDATA%\Typo Switcher\config.json`.
