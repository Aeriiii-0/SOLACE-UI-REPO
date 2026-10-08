# Activity Log Change Tracking Guide

## Overview

The SOLACE-UI now features comprehensive activity logging that captures **detailed field-level changes** whenever a record is edited. This allows administrators and authorized users to:

- **Track what changed:** See exactly which fields were modified
- **View before/after values:** Understand the previous and current state of each changed field
- **Audit trail:** Maintain a complete history of all modifications with timestamps, user, and role
- **Compare changes:** Quickly identify what was edited in a multi-field update

## Architecture

### New Components

#### 1. **ChangeDetail Model** (`Models/ChangeDetail.cs`)
Represents a single field-level change:
- `FieldName`: Human-readable field name (e.g., "First Name", "Civil Status")
- `OldValue`: Previous value before the change
- `NewValue`: New value after the change
- `ChangedAt`: Timestamp of the change
- `Display`: Formatted string showing "Field: OldValue → NewValue"

#### 2. **ChangeTrackingService** (`Services/ChangeTrackingService.cs`)
Detects and captures all field-level changes between two `SoloParentRecord` instances:
- Compares all 50+ fields in a SoloParentRecord
- Generates `ChangeDetail` entries only for fields that changed
- Formats values appropriately (dates, booleans, etc.)
- Returns a list of changes ready for audit logging

**Method:**
```csharp
public List<ChangeDetail> DetectChanges(SoloParentRecord before, SoloParentRecord after)
```

#### 3. **Enhanced AuditLog Model** (`Models/AuditLog.cs`)
Extended to support multiple field changes:
- New property: `Changes` — collection of `ChangeDetail` entries
- Updated `HasChangeDetail` to check for changes or legacy single-field updates
- Backward compatible with existing single-field audit logs

#### 4. **Enhanced AuditLogService** (`Services/AuditLogService.cs`)
New method for logging updates with detailed changes:
```csharp
public void LogUpdateWithChanges(string recordId, string recordName, string by, string role = "",
    List<ChangeDetail> changes = null)
```

### Updated UI

#### ViewAllLogsOverlay.xaml - Detail Panel
The audit log detail view now displays:
- **"What Changed" section** with an itemized list of all changes
- Each change shows:
  - **Field name** (bold)
  - **Before value** (red background)
  - **→ arrow** (visual indicator)
  - **After value** (green background)
- Dividers between multiple changes for clarity
- Backward compatible with legacy single-field format

## Usage Flow

### When a Record is Edited

1. User opens a record in `RecordDialog`
2. User makes changes to one or more fields
3. User clicks "Save" or confirms the edit
4. `SoloParentRecordsViewModel.SaveEditedRecordAsync()` is called with:
   - `rawRecord` — the original (before) state
   - `editedRecord` — the new (after) state

5. **Change Detection:**
   ```csharp
   var changeService = new ChangeTrackingService();
   var changes = changeService.DetectChanges(rawRecord, editedRecord);
   ```

6. **Audit Logging:**
   ```csharp
   _auditLog.LogUpdateWithChanges(editedRecord.Id, editedRecord.Name, 
       GetCurrentUser(), CurrentUserRole, changes);
   ```

7. The audit log entry now contains:
   - All metadata (who, when, where)
   - Complete list of field changes with before/after values
   - Display-friendly formatted description

### When a Record is Renewed

The renewal flow also uses change tracking:
1. Backend processes renewal (updates expiration date)
2. User may edit fields like "Civil Status" or "Monthly Income"
3. Changes are captured and logged with same detail level

## Fields Tracked

The change tracking service monitors all these field categories:

### Identity & Metadata
- Last Name, First Name, Middle Name, Extension Name
- Status

### Personal Information
- Sex, Civil Status, Date of Birth, Place of Birth
- Citizenship, Blood Type, Height, Weight

### Background
- Educational Attainment, PhilSys Number
- Religion, Occupation, Monthly Income
- Employment Status (Employed, Self-Employed, Not Employed)

### Address & Contact
- Address, Barangay, Contact Number

### Emergency Contact
- Emergency Contact Name, Relationship, Address, Number

### Family
- Number of Children
- Family members (via separate tracking if needed)

### Circumstances (A1-F)
- All 17 circumstance flags and their related details
- Dates, causes, periods, disability info, etc.

### Special Status
- LGBT, Pantawid Beneficiary, Indigenous flags

### Needs & Income
- Needs & Problems, Other Income Source

## Display Format

### In Audit Log List
- Shows a "Has Changes" badge if multiple fields were modified
- Allows double-click to view detailed changes

### In Audit Log Detail View
```
WHAT CHANGED

Field: First Name
Before: [red box] John
→
After: [green box] Jonathan

─────────────────

Field: Civil Status
Before: [red box] Married
→
After: [green box] Separated

─────────────────

... (more changes)
```

## Backward Compatibility

- Existing single-field audit logs (from legacy code) continue to display correctly
- The detail view shows the "Before" / "After" section using either:
  - New format: Multiple itemized changes
  - Legacy format: Single FieldChanged / OldValue / NewValue triplet
- No migration needed; old and new formats coexist

## Example: Complete Audit Trail

**Original Record:**
- Name: "John Santos"
- Civil Status: "Married"
- Monthly Income: "15000"

**Edit 1 - User changed first name and civil status:**
```
Action: UPDATE_SOLO_PARENT
Time: Oct 8, 2026 2:30 PM
By: Maria Lopez (Administrator)
Description: Updated record: John Santos [uuid-123] — 2 field(s) changed

Changes:
✓ First Name: "John" → "Jonathan"
✓ Civil Status: "Married" → "Separated"
```

**Edit 2 - User updated income:**
```
Action: UPDATE_SOLO_PARENT
Time: Oct 8, 2026 3:45 PM
By: Admin User (Administrator)
Description: Updated record: Jonathan Santos [uuid-123] — 1 field(s) changed

Changes:
✓ Monthly Income: "15000" → "18500"
```

**View in Activity Logs:**
- Browse to Dashboard → View All Logs
- Search for record "Jonathan Santos"
- Double-click each entry to see detailed changes
- Track complete history of modifications over time

## Code Integration Points

### For Developers

When implementing new edit workflows:

1. **Use the ChangeTrackingService:**
   ```csharp
   var changeService = new ChangeTrackingService();
   var changes = changeService.DetectChanges(original, modified);
   ```

2. **Log with change details:**
   ```csharp
   auditLog.LogUpdateWithChanges(recordId, recordName, userName, role, changes);
   ```

3. **Do NOT use the legacy method:**
   ```csharp
   // ❌ Don't do this anymore for full record edits
   auditLog.LogUpdate(id, name, user, role, "Field", oldVal, newVal);
   
   // ✅ Do this instead
   auditLog.LogUpdateWithChanges(id, name, user, role, changes);
   ```

### For UI Designers

When extending the audit log detail view:

1. Changes are automatically populated in `SelectedLog.Changes` collection
2. Each `ChangeDetail` provides `FieldName`, `OldValue`, `NewValue`, `Display`
3. ItemsControl in XAML iterates through the collection
4. Styling via existing badges and color themes (red for old, green for new)

## Benefits

✅ **Complete Transparency:** Every field change is recorded with before/after values  
✅ **Compliance:** Satisfies audit requirements for record modifications  
✅ **Accountability:** Clear attribution to user, role, timestamp  
✅ **Debugging:** Easy to trace how records evolved over time  
✅ **Reconciliation:** Compare expected vs. actual state changes  
✅ **User-Friendly:** Visual before/after comparison in UI  
✅ **Scalable:** Automatically handles new fields added to SoloParentRecord  
✅ **Backward Compatible:** Coexists with legacy audit entries  

## Testing

### Manual Testing Checklist

- [ ] Edit a record with a single field change → verify log shows 1 change
- [ ] Edit a record with 5+ field changes → verify all are logged
- [ ] View the audit log detail → confirm changes display with before/after
- [ ] Check date formatting in changes (dates should show as YYYY-MM-DD)
- [ ] Test with booleans (should show "Yes" for true, omitted for false)
- [ ] Verify role-based access (basic users see only their edits)
- [ ] Test renewal workflow → check if changes are captured correctly
- [ ] Verify pagination in audit logs with many entries
- [ ] Export/print audit logs → confirm all change details included

## Future Enhancements

- **Diff highlighting:** Visual diff view comparing two versions side-by-side
- **Bulk comparison:** Select 2 dates and see all changes between them
- **Change statistics:** Dashboard showing most-changed fields, busiest days
- **Revert capability:** Quick restore to previous version (with new audit entry)
- **Change aggregation:** Group changes by field to show trend over time
- **Export to PDF:** Detailed audit report with all changes formatted
