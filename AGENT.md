# WPF UI Architecture Blueprint & Guidelines

## Architecture & Layout
- **Views/**: XAML windows and user controls (`MainWindow.xaml`, etc.). Pure UI layouts only.
- **ViewModels/**: Presentation logic, commands (`RelayCommand`), and bindable properties (`ObservableProperty`).
- **Models/**: Client-side domain models (`SoloParentRecord.cs`, `HouseholdMember.cs`, `SpouseInfo.cs`, `OcrModels.cs`, `AuditLog.cs`, `EmergencyContact.cs`, `ScannerSettings.cs`) and API payload models under `Models/Api/`[cite: 2].
- **Services/**: Application services, hardware integration (`WiaScannerService.cs`, `OcrService.cs`, `SoloParentExportService.cs`), and HTTP API integration services under `Services/Api/` (`SoloParentApiService.cs`, `AuthApiService.cs`, `UserApiService.cs`, `AnalyticsApiService.cs`, `ApiClient.cs`)[cite: 2].
- **Converters/**: WPF XAML value converters (`BoolToVisibilityConverter.cs`)[cite: 2].
- **Assets/ & Templates/**: Static image assets (`LoginDesign.png`, `Logo.png`, `SidebarLogo.png`) and UI templates[cite: 2].

## Core Development Rules
1. **Strict MVVM:** Do NOT write business, API, or data logic inside `.xaml.cs` code-behind files (`MainWindow.xaml.cs`)[cite: 2].
2. **API & Hardware Delegation:** API calls must go through `Services/Api/`[cite: 2]. Hardware or export logic must go through specialized services in `Services/`[cite: 2].
3. **UI Threading:** Always execute async API callbacks or background threads updating observable collections on the WPF UI thread using `App.Current.Dispatcher`.
4. **Data Binding:** Use XAML bindings to ViewModels for all dynamic UI elements and converters in `Converters/` for visibility toggles[cite: 2].