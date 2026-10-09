# SOLACE-UI Developer & AI Guidelines

You are an expert C# / WPF / Python developer. Your goal is to help maintain and build the SOLACE-UI.

When generating code or suggesting refactors, you MUST adhere to the following architectural rules.

## 1. Architecture & Folder Layout
- **Core/**: Global application state & base classes (`ObservableObject.cs`, `RelayCommand.cs`).
  - **Core/Stores/**: Global state singletons (`AccountStore.cs`, `NavigationStore.cs`). Do not chain data through constructors.
- **Views/**: Pure XAML UI layouts. Divided into:
  - `Pages/` (e.g., `DashboardPage.xaml`, `SoloParentRecordsPage.xaml`, `SubsidyRecommendationPage.xaml`)
  - `Dialogs/` (e.g., `OcrScanDialog.xaml`, `RecordDialog.xaml`, `RenewDialog.xaml`, `ConfirmDialog.xaml`)
  - `Components/`: Reusable UI bits to keep Pages under 250 lines (e.g., `<components:SubsidyTriageChart />`, `<components:ActivityPanel />`)
- **ViewModels/**: Presentation logic, commands, and state. Must not contain direct hardware or HTTP calls.
- **Models/**: Client-side domain models used for UI binding (`SoloParentRecord.cs`, `HouseholdMember.cs`, `SpouseInfo.cs`, `OcrModels.cs`, `AuditLog.cs`, `EmergencyContact.cs`, `ScannerSettings.cs`). Must implement `INotifyPropertyChanged`.
- **DTOs/**: Data Transfer Objects used purely for API payloads (`Models/Api/AuthDtos.cs`, `SoloParentDtos.cs`, `UserDtos.cs`, `BaseResponse.cs`). Clean auto-properties with NO UI logic.
- **Services/**: Application logic, hardware (WIA Scanner), and REST API integration.
  - **Services/Interfaces/**: Contracts for Dependency Injection (e.g., `IOcrService`, `ISoloParentApiService`).
  - **Services/Implementations/**: Concrete classes (`WiaScannerService.cs`, `OcrService.cs`, `ApiClient.cs`, `SoloParentApiService.cs`, `AuthApiService.cs`).
- **Converters/**: WPF XAML value converters (`BoolToVisibilityConverter.cs`, `InverseBoolToVisibilityConverter.cs`, `InverseBoolConverter.cs`).
- **Assets/**: Static image assets (`LoginDesign.png`, `Logo.png`, `SidebarLogo.png`, and other UI resources).

## 2. WPF & MVVM Core Rules
- **Strict MVVM:** Do NOT write business, API, or data logic inside `.xaml.cs` code-behind files. Code-behind is strictly for UI-manipulation logic (focus, animations, hardware events, layout adjustments).
- **Dependency Injection:** Never instantiate services using the `new` keyword inside ViewModels. Inject interfaces via constructor injection.
- **UI Threading:** Always execute async API callbacks or background threads updating observable collections on the WPF UI thread using `App.Current.Dispatcher`.
- **Data Binding:** Use XAML bindings to ViewModels for all dynamic UI elements and converters in `Converters/` for visibility toggles and format conversions.

## 3. File Size & Componentization (Strict Rule)
- **Hard Limit:** No single C# (.cs) or XAML (.xaml) file may exceed 250 lines of code.
- **Decomposition First:** If a requested feature will push a file over this limit, your FIRST step must be to extract existing UI sections into standalone `UserControl` files inside the `Views/Components/` folder.
- **When to Split:**
  - Complex dialogs with multiple sections → extract tabs, panels, or detail views as separate components
  - Repeated UI patterns across pages → extract into `Views/Components/`
  - ViewModel files exceeding 250 lines → split by domain concern (e.g., `SoloParentRecordViewModel.cs` + `SoloParentRecordDetailViewModel.cs`)

## 4. Refactoring Strategy (The Boy Scout Rule)
- **Do not perform "Big Bang" mass refactors.** The codebase is incrementally adopting the new structured architecture.
- **New Features:** Must strictly use the new segmented architecture (`Views/Pages/`, `Views/Components/`, `Core/Stores/`, `DTOs/`).
- **Legacy Code:** If asked to modify an existing, working legacy file or monolithic section, apply the "Boy Scout Rule"—extract one component or interface to leave the file cleaner than you found it.
- **Untouched Legacy:** If a legacy file is not being modified for a feature or bug fix, leave it alone. Do not preemptively refactor.

## 5. Observable Models & Bindings
- All domain models (`Models/`) must inherit from a base class that implements `INotifyPropertyChanged`.
- Use `ObservableProperty<T>` or explicit property backing with `RaisePropertyChanged()` for any property that updates UI.
- Computed properties (like `DotColor`, `DotIcon`, `DotLabel`, `ActionLabel` in `AuditLog.cs`) may be simple calculated getters without change notification.

## 6. Services & API Integration
- **API Services** go in `Services/Implementations/` and must implement an interface in `Services/Interfaces/`.
- **DTOs** are used for API request/response payloads. Convert DTOs to domain `Models/` inside the service layer, never expose DTOs directly to ViewModels.
- **Error Handling:** Services should log errors and throw meaningful exceptions. ViewModels catch and display errors in the UI.
- **Async/Await:** Always use `async/await` for I/O operations. Avoid `.Result` or `.Wait()`.

## 7. UI Styling & Layout
- **Responsive Design:** Use `Star` sizing (`Width="*"`, `Width="2*"`) for flexible layouts. Use `Auto` only for content-sized elements. Use pixel widths for fixed-size elements (buttons, icons, margins).
- **Column Resizing:** For `ListView`/`GridView` tables with dynamic columns, compute widths in code-behind resize handlers (e.g., `RecordsList.SizeChanged`) distributing available space proportionally.
- **Icons & Visuals:**
  - Use Material Design SVG paths for consistency (stored as const strings in models, e.g., `DotIcon` property).
  - Render icons inside `Viewbox` containers for crisp scaling.
  - Prefer colored action dots with icon glyphs over plain text for status indication.
- **Disabled Elements:** Always set `IsEnabled` bindings to commands and add visual feedback (opacity, greyed-out borders) in control templates.

## 8. Code Quality & Consistency
- **Naming:** Use clear, descriptive names. Prefer full words over abbreviations (e.g., `SoloParentRecord` not `SPR`).
- **Comments:** Document public APIs, complex algorithms, and non-obvious business logic. Skip obvious comments (e.g., `// Set the name` is noise).
- **Constants:** Extract magic numbers/strings to named constants or configuration properties.
- **Testing:** For critical business logic, unit tests should accompany the feature. Use mocking for services in unit tests.
