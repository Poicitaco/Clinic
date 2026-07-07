# Copilot Instructions

## Project Guidelines
- ClinicManagement Architecture: 5 projects (.NET Framework 4.7.2). Core = Entities (no deps), DataAccess = EF6 DBContext (deps: Core), Business = Services (deps: Core, DataAccess), UI = WPF/MVVM (deps: Core, Business), Tests = unit tests. Strict Rule: Never put business logic or data access logic in UI code-behind.