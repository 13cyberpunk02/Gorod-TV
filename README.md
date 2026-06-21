# Город ТВ

IPTV-приложение «Город ТВ» — сервис цифрового телевидения от провайдера **Инфо-Лан**.
Кроссплатформенное приложение на **.NET MAUI**: версия для смартфонов и порт под
**Android TV**.

> «Город ТВ» — название приложения. «Инфо-Лан» — компания-провайдер, которая
> предоставляет сервис.

---

## Содержание

- [Возможности](#возможности)
- [Стек](#стек)
- [Структура решения](#структура-решения)
- [Сборка и запуск](#сборка-и-запуск)
- [Сборка APK для Android TV](#сборка-apk-для-android-tv)
- [Конфигурация](#конфигурация)
- [Важные технические нюансы](#важные-технические-нюансы)
- [English summary](#english-summary)

---

## Возможности

- Авторизация по номеру договора и паролю (сессия сохраняется, поддержан автологин).
- Категории каналов, список каналов, избранное.
- Плеер прямого эфира (HLS) с программой передач (EPG) и архивом передач.
- Текущая передача под карточкой канала.
- Отдельный интерфейс под Android TV: навигация пультом (D-pad), экранная
  клавиатура, крупные карточки, тёмная тема.

---

## Стек

- **.NET MAUI** (`net10.0-android` для TV-версии).
- **CommunityToolkit.Maui** и **CommunityToolkit.Maui.MediaElement** — UI-утилиты
  и медиаплеер.
- **CommunityToolkit.Mvvm** — MVVM (ObservableObject, RelayCommand).
- **BouncyCastle.Cryptography** — подпись запросов (HMAC-RIPEMD160).
- **System.Text.Json** с source-generation для сериализации.
- Шрифты: **Onest** (текст), **Material Symbols Rounded** (иконки).

---

## Структура решения

Решение состоит из трёх проектов:

| Проект | Назначение |
|--------|------------|
| `GorodTV.Core` | Общая логика: модели, ViewModels, сервисы (API-клиент, сессия, избранное, диалоги), мапперы. Не зависит от платформы. |
| `GorodTV` | Версия для смартфонов (UI). Ссылается на Core. |
| `GorodTv.Tv` | Версия для Android TV (UI, навигация пультом). Ссылается на Core. |

Обе UI-версии переиспользуют один и тот же `GorodTV.Core` — вся бизнес-логика и
работа с API живут там, а каждый «head» добавляет только свой интерфейс.

### Ключевые сервисы (Core)

- `IGorodTvService` — фасад: авторизация, категории, каналы, EPG, избранное.
- `IApiClient` — низкоуровневый HTTP-клиент: строит запрос, шлёт, десериализует.
- `BaseApiRequests` — формирование URL и подпись `sign` (HMAC-RIPEMD160).
- `ISessionStore`, `IFavoritesStore`, `IAppSettings` — хранилища (Preferences).
- `ChannelMapper` — преобразование DTO ответа API в доменные модели.

---

## Сборка и запуск

### Требования

- .NET SDK с установленным MAUI workload.
- Для Android: Android SDK (через Visual Studio или вручную).
- Visual Studio 2022 (Windows) или CLI.

### Запуск на эмуляторе / в разработке

Открой решение в Visual Studio, выбери нужный проект (`GorodTV` или `GorodTv.Tv`)
стартовым, выбери целевой эмулятор и запусти (**F5**).

> **Важно:** для разработки на эмуляторе запускай именно из Visual Studio (F5).
> Так среда сама доставляет сборки (Fast Deployment). Debug-APK, скопированный
> на устройство вручную, **не запустится** — его сборки не упакованы внутрь.

### CLI

```powershell
dotnet build GorodTv.Tv\GorodTv.Tv.csproj -c Debug -f net10.0-android
```

---

## Сборка APK для Android TV

Для установки на реальное устройство нужен **Release-APK** (самодостаточный).

```powershell
dotnet build GorodTv.Tv\GorodTv.Tv.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk
```

Готовый файл:

```
GorodTv.Tv\bin\Release\net10.0-android\ru.info_lan.gorod.tv-Signed.apk
```

Устанавливай именно файл с суффиксом `-Signed`.

### Поддержка архитектур

В проекте указаны все ABI, чтобы APK ставился и на 32-битные, и на 64-битные
устройства:

```xml
<RuntimeIdentifiers>android-arm64;android-arm;android-x64;android-x86</RuntimeIdentifiers>
<AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
<AndroidPackageFormat>apk</AndroidPackageFormat>
```

> Многие ТВ-приставки (например, на Amlogic) — **32-битные (armeabi-v7a)**.
> `android-arm` в списке обязателен для их поддержки.

### Установка на ТВ

1. Скопируй `...-Signed.apk` на флешку (FAT32/exFAT).
2. На ТВ установи файловый менеджер (X-plore / File Commander).
3. Разреши установку из неизвестных источников для этого менеджера.
4. Открой APK через менеджер и установи.

Альтернатива — установка по ADB через сеть:

```
adb connect <IP_ТВ>:5555
adb install -r ru.info_lan.gorod.tv-Signed.apk
```

---

## Конфигурация

- **Базовый адрес API** задаётся при регистрации `HttpClient` в `MauiProgram`.
- **Подпись запросов** (`sign`) считается в `BaseApiRequests` через HMAC-RIPEMD160.
  Алгоритм должен совпадать с серверным — менять не следует.
- **Иконки приложения** (adaptive): `Resources/AppIcon/`.
- **Внутренние логотипы**: `logo_app.svg` (заставка/логин), `logo_g.svg` (сайдбар TV).

---

## Важные технические нюансы

Эти моменты не очевидны и уже стоили отладки — учитывай их при доработке.

### Release-сборка для Android TV (arm32)

Для Release **отключены marshal methods** — иначе приложение падает на старте на
32-битных ARM-устройствах (assertion `marshal-ilgen-stub.c`, нативный SIGABRT):

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <AndroidEnableMarshalMethods>false</AndroidEnableMarshalMethods>
</PropertyGroup>
```

Симптом без этого фикса: в Debug и на эмуляторе (x86_64) всё работает, а
Release-APK на реальном ТВ падает на системной заставке. Причина — marshal
methods включены в Release по умолчанию и ломаются на arm32.

### Сериализация JSON под линкер

Используется **source-generated** `JsonSerializerContext` (`GorodTvJsonContext`).
В нём должны быть перечислены **все** сетевые DTO. Для надёжности в
`JsonSerializerOptions` добавлен комбинированный резолвер (source-gen + рефлексия),
чтобы незарегистрированный тип не ронял приложение:

```csharp
TypeInfoResolver = JsonTypeInfoResolver.Combine(
    GorodTvJsonContext.Default,
    new DefaultJsonTypeInfoResolver());
```

> Если включишь линкер (`PublishTrimmed=true`), рефлексия-фолбэк может обрезаться —
> тогда **все** DTO обязаны быть перечислены в контексте.

### Регистрация страниц в DI

Каждая страница, которую создаёт навигация Shell и у которой конструктор с
зависимостями, **обязана** быть зарегистрирована в `MauiProgram`
(`AddTransient<...>`). Пропуск регистрации стартовой страницы приводит к крашу
на старте (до отрисовки UI).

### Навигация на Android TV (D-pad)

- Фокус карточки задаётся **явно** при появлении страницы (первая карточка
  списка фокусируется через нативный `RecyclerView`).
- Списки используют виртуализированный `CollectionView` — это быстрее и
  предсказуемо по высоте.
- Подсветка фокуса — рамкой и цветом фона; **без zoom/scale** (на ТВ обрезается
  по краям).

### Загрузка данных

Категории и каналы загружаются одним запросом и кэшируются в памяти. Текущая
программа (EPG) для списка догружается лениво батчами с отменой при уходе со
страницы — чтобы не блокировать UI на слабых устройствах. Полная программа канала
загружается в плеере при открытии канала.

---

## English summary

**Город ТВ (GorodTV)** is an IPTV app for the provider **Инфо-Лан (Info-Lan)**,
built with **.NET MAUI**. It ships as a phone app and an **Android TV** port.

**Solution layout (3 projects):**

- `GorodTV.Core` — shared logic: models, view-models, services (API client,
  session, favorites, dialogs), mappers. Platform-independent.
- `GorodTV` — phone UI. References Core.
- `GorodTv.Tv` — Android TV UI (D-pad navigation). References Core.

**Tech:** .NET MAUI, CommunityToolkit (MAUI + MediaElement + MVVM),
BouncyCastle (HMAC-RIPEMD160 request signing), System.Text.Json with source
generation, Onest + Material Symbols fonts.

**Build a TV APK:**

```powershell
dotnet build GorodTv.Tv\GorodTv.Tv.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk
```

Install the `...-Signed.apk` from `bin\Release\net10.0-android\`. A **Release**
APK is required for manual install on a device; a Debug APK won't run when copied
manually (its assemblies aren't bundled).

**Gotchas:**

- Release builds disable marshal methods
  (`<AndroidEnableMarshalMethods>false</AndroidEnableMarshalMethods>`) — otherwise
  the app crashes at startup on 32-bit ARM TV devices (`marshal-ilgen-stub.c`
  assertion). Debug and the x86_64 emulator are unaffected, which makes this easy
  to miss.
- JSON uses a source-generated `JsonSerializerContext`; all network DTOs must be
  listed. A combined resolver (source-gen + reflection) is used as a safety net.
- Every DI-constructed Shell page must be registered in `MauiProgram`.
- Android TV `RuntimeIdentifiers` include `android-arm` (armeabi-v7a) because many
  TV boxes are 32-bit.

---

## Лицензия

Этот проект распространяется под лицензией [MIT](LICENSE.txt). Подробнее см. в файле [LICENSE](LICENSE.txt).
