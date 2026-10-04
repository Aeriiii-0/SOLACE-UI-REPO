# SOLACE-UI Developer & AI Guidelines

You are an expert C# / WPF / Python developer. Your goal is to help maintain and build the SOLACE-UI. 

When generating code or suggesting refactors, you MUST adhere to the following architectural rules.

## 1. Architecture & Folder Layout
- **Core/**: Global application state & base classes (`ObservableObject.cs`, `RelayCommand.cs`).
  - **Core/Stores/**: Global state singletons (`AccountStore.cs`, `NavigationStore.cs`). Do not chain data through constructors.
- **Views/**: Pure XAML UI layouts. Divided into:
  - `Pages/` (e.g., `DashboardPage.xaml`, `SubsidyRecommendationPage.xaml`)
  - `Dialogs/` (e.g., `OcrScanDialog.xaml`, `RecordDialog.xaml`)
  - `Components/`: Reusable UI bits to keep Pages under 200 lines (e.g., `<components:SubsidyTriageChart />`).
- **ViewModels/**: Presentation logic, commands, and state. Must not contain direct hardware or HTTP calls.
- **Models/**: Client-side domain models used for UI binding (`SoloParentRecord.cs`, `HouseholdMember.cs`). Must implement `INotifyPropertyChanged`.
- **DTOs/**: Data Transfer Objects used purely for API payloads (formerly `Models/Api/`). Clean auto-properties with NO UI logic.
- **Services/**: Application logic, hardware (WIA Scanner), and REST API integration.
  - **Services/Interfaces/**: Contracts for Dependency Injection (e.g., `IOcrService`, `ISoloParentApiService`). 
  - **Services/Implementations/**: Concrete classes (`WiaScannerService.cs`, `OcrService.cs`, `ApiClient.cs`).
- **Converters/**: WPF XAML value converters (`BoolToVisibilityConverter.cs`).

## 2. WPF & MVVM Core Rules
- **Strict MVVM:** Do NOT write business, API, or data logic inside `.xaml.cs` code-behind files. Code-behind is strictly for UI-manipulation logic (focus, animations, hardware events).
- **Dependency Injection:** Never instantiate services using the `new` keyword inside ViewModels. Inject interfaces via constructor injection.
- **UI Threading:** Always execute async API callbacks or background threads updating observable collections on the WPF UI thread using `App.Current.Dispatcher`.

## 3. File Size & Componentization (Strict Rule)
- **Hard Limit:** No single C# (.cs) or XAML (.xaml) file may exceed 250 lines of code.
- **Decomposition First:** If a requested feature will push a file over this limit, your FIRST step must be to extract existing UI sections into standalone `UserControl` files inside the `Views/Components/` folder.

## 4. Refactoring Strategy (The Boy Scout Rule)
- **Do not perform "Big Bang" mass refactors.** The codebase is migrating incrementally to the new structured architecture.
- **New Features:** Must strictly use the new segmented architecture (`Views/Pages/`, `Views/Components/`, `Core/Stores/`, `DTOs/`).
- **Legacy Files:** If asked to modify an existing, working monolithic file, apply the "Boy Scout Rule"—extract one component or interface to leave the file cleaner than you found it. If a legacy file is not being modified for a feature or bug fix, leave it alone.