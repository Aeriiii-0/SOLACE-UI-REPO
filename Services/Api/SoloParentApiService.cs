using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Api
{
    public class SoloParentApiService
    {
        private static readonly Lazy<SoloParentApiService> _instance =
            new Lazy<SoloParentApiService>(() => new SoloParentApiService());
        public static SoloParentApiService Instance => _instance.Value;

        private SoloParentApiService() { }

        public async Task<BaseResponse<PagedResult<SoloParentSummaryDto>>> GetSoloParentsAsync(GetSoloParentRequest request)
        {
            var queryParams = BuildQueryParams(request);
            string queryString = string.Join("&", queryParams);
            string url = "api/soloparents?" + queryString;
            var response = await ApiClient.Instance.GetAsync<PagedResult<SoloParentSummaryDto>>(url);
            if (!response.Succeeded && response.Errors != null && response.Errors.Any(e => e.Contains("404")))
            {
                url = "api/soloparent?" + queryString;
                response = await ApiClient.Instance.GetAsync<PagedResult<SoloParentSummaryDto>>(url);
            }
            return response;
        }

        /// <summary>
        /// Same endpoint as GetSoloParentsAsync but deserialises into the richer
        /// SoloParentListItemDto so callers get full PersonalInfo without N+1 detail calls.
        /// </summary>
        public async Task<BaseResponse<PagedResult<SoloParentListItemDto>>> GetSoloParentsFullAsync(GetSoloParentRequest request)
        {
            var queryParams = BuildQueryParams(request);
            string queryString = string.Join("&", queryParams);
            string url = "api/soloparents?" + queryString;
            var response = await ApiClient.Instance.GetAsync<PagedResult<SoloParentListItemDto>>(url);
            if (!response.Succeeded && response.Errors != null && response.Errors.Any(e => e.Contains("404")))
            {
                url = "api/soloparent?" + queryString;
                response = await ApiClient.Instance.GetAsync<PagedResult<SoloParentListItemDto>>(url);
            }
            return response;
        }

        private List<string> BuildQueryParams(GetSoloParentRequest request)
        {
            var queryParams = new List<string>();

            if (request.Id.HasValue)
                queryParams.Add($"id={request.Id.Value}");

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                queryParams.Add($"firstName={Uri.EscapeDataString(request.FirstName.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.LastName))
                queryParams.Add($"lastName={Uri.EscapeDataString(request.LastName.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.Fullname))
                queryParams.Add($"fullname={Uri.EscapeDataString(request.Fullname.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.PhilsysId))
                queryParams.Add($"philsysId={Uri.EscapeDataString(request.PhilsysId.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.Sex) && !request.Sex.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"sex={Uri.EscapeDataString(request.Sex.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.Barangay) && !request.Barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"barangay={Uri.EscapeDataString(request.Barangay.Trim())}");

            if (request.IsActive.HasValue)
                queryParams.Add($"isActive={request.IsActive.Value.ToString().ToLowerInvariant()}");

            if (request.IsDeleted.HasValue)
                queryParams.Add($"isDeleted={request.IsDeleted.Value.ToString().ToLowerInvariant()}");

            if (request.IsLGBT.HasValue)
                queryParams.Add($"isLGBT={request.IsLGBT.Value.ToString().ToLowerInvariant()}");

            if (request.IsPantawidBeneficiary.HasValue)
                queryParams.Add($"isPantawidBeneficiary={request.IsPantawidBeneficiary.Value.ToString().ToLowerInvariant()}");

            if (request.IsIndigenous.HasValue)
                queryParams.Add($"isIndigenous={request.IsIndigenous.Value.ToString().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(request.Status) && !request.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"status={Uri.EscapeDataString(request.Status.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

            if (!string.IsNullOrWhiteSpace(request.SortOrder))
                queryParams.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder)}");

            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");

            return queryParams;
        }

        public async Task<BaseResponse<SoloParentDto>> GetSoloParentByIdAsync(Guid id)
        {
            return await ApiClient.Instance.GetAsync<SoloParentDto>($"api/soloparent/{id}");
        }

        public async Task<BaseResponse<Guid>> CreateSoloParentAsync(CreateSoloParentRequest request)
        {
            return await ApiClient.Instance.PostAsync<CreateSoloParentRequest, Guid>("api/soloparent", request);
        }

        public async Task<BaseResponse<bool>> UpdateSoloParentAsync(Guid id, UpdateSoloParentRequest request)
        {
            request.Id = id;
            return await ApiClient.Instance.PutAsync<UpdateSoloParentRequest, bool>($"api/soloparent/{id}", request);
        }

        public async Task<BaseResponse<bool>> DeleteSoloParentAsync(Guid id)
        {
            return await ApiClient.Instance.DeleteAsync<bool>($"api/soloparent/{id}");
        }

        public async Task<BaseResponse<RenewSoloParentResponse>> RenewSoloParentRecordAsync(Guid id, RenewSoloParentRequest request = null)
        {
            return await ApiClient.Instance.PostAsync<object, RenewSoloParentResponse>($"api/soloparent/{id}/renew", request ?? (object)new { });
        }

        // =========================================================================
        // Mapping Helpers: SoloParentRecord <-> Backend DTOs
        // =========================================================================

        public CreateSoloParentRequest MapToCreateRequest(SoloParentRecord r)
        {
            var req = new CreateSoloParentRequest
            {
                PersonalInfo = MapPersonalInfo(r),
                Employment = MapEmploymentInfo(r),
                ContactDetails = MapContactInfo(r),
                AddressDetails = MapAddressInfo(r),
                EmergencyContact = MapEmergencyContact(r),
                ProblemPresented = MapProblemPresented(r),
                IsLGBT = r.IsLGBT,
                IsPantawidBeneficiary = r.IsPantawidBeneficiary,
                IsIndigenous = r.IsIndigenous,
                NeedsAndProblems = r.NeedsAndProblems,
                OtherSourcesOfIncome = r.OtherIncomeSource
            };

            if (r.FamilyMembers != null)
            {
                foreach (var fm in r.FamilyMembers)
                {
                    if (!string.IsNullOrWhiteSpace(fm.MemberName))
                    {
                        req.FamilyMembers.Add(MapCreateFamilyMember(fm));
                    }
                }
            }

            return req;
        }

        public UpdateSoloParentRequest MapToUpdateRequest(Guid id, SoloParentRecord r, SoloParentDto existingEntity = null)
        {
            var problemPresented = (existingEntity?.ProblemPresented != null && !HasCircumstanceChanged(r, existingEntity.ProblemPresented))
                ? existingEntity.ProblemPresented
                : MapProblemPresented(r);

            var req = new UpdateSoloParentRequest
            {
                Id = id,
                PersonalInfo = MapPersonalInfo(r),
                Employment = MapEmploymentInfo(r),
                ContactDetails = MapContactInfo(r),
                AddressDetails = MapAddressInfo(r),
                EmergencyContact = MapEmergencyContact(r),
                ProblemPresented = problemPresented,

                IsIndigenous = r.IsIndigenous,
                IsLGBT = r.IsLGBT,
                IsPantawidBeneficiary = r.IsPantawidBeneficiary,
                NeedsAndProblems = r.NeedsAndProblems,
                OtherSourcesOfIncome = r.OtherIncomeSource
            };

            var existingIds = existingEntity?.FamilyMembers != null
                ? new HashSet<Guid>(existingEntity.FamilyMembers.Select(f => f.Id))
                : new HashSet<Guid>();

            var processedExistingIds = new HashSet<Guid>();

            if (r.FamilyMembers != null)
            {
                foreach (var fm in r.FamilyMembers)
                {
                    string mName = fm.MemberName?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(mName)) continue;

                    // 1. Existing family member (has database GUID)
                    if (fm.Id.HasValue && !fm.IsNew && (existingIds.Count == 0 || existingIds.Contains(fm.Id.Value)))
                    {
                        processedExistingIds.Add(fm.Id.Value);
                        req.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                        {
                            Id = fm.Id.Value,
                            Name = fm.MemberName,
                            Sex = NormalizeSex(fm.Sex),
                            Age = ParseAge(fm.Age, fm.Birthdate),
                            Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                            CivilStatus = string.IsNullOrWhiteSpace(fm.CivilStatus) ? "Single" : fm.CivilStatus,
                            Relationship = string.IsNullOrWhiteSpace(fm.Relationship) ? "Child" : fm.Relationship,
                            EducationalLevel = string.IsNullOrWhiteSpace(fm.EducationEmployment) ? "Student" : fm.EducationEmployment,
                            Income = ParseDecimal(fm.Income),
                            HasSpecialNeeds = fm.HasSpecialNeeds,
                            SpecialNeedsCategory = fm.SpecialNeedsCategory,
                            SpecialNeedsDescription = fm.SpecialNeedsDescription
                        });
                    }
                    // 2. Fallback: If Id was missing but matches an unclaimed existing member by Name
                    else if (!fm.Id.HasValue && !fm.IsNew && existingEntity?.FamilyMembers != null &&
                             existingEntity.FamilyMembers.FirstOrDefault(f => !processedExistingIds.Contains(f.Id) && string.Equals(f.Name?.Trim(), mName, StringComparison.OrdinalIgnoreCase)) is FamilyMemberDto matched)
                    {
                        processedExistingIds.Add(matched.Id);
                        req.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                        {
                            Id = matched.Id,
                            Name = fm.MemberName,
                            Sex = NormalizeSex(fm.Sex),
                            Age = ParseAge(fm.Age, fm.Birthdate),
                            Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                            CivilStatus = string.IsNullOrWhiteSpace(fm.CivilStatus) ? "Single" : fm.CivilStatus,
                            Relationship = string.IsNullOrWhiteSpace(fm.Relationship) ? "Child" : fm.Relationship,
                            EducationalLevel = string.IsNullOrWhiteSpace(fm.EducationEmployment) ? "Student" : fm.EducationEmployment,
                            Income = ParseDecimal(fm.Income),
                            HasSpecialNeeds = fm.HasSpecialNeeds,
                            SpecialNeedsCategory = fm.SpecialNeedsCategory,
                            SpecialNeedsDescription = fm.SpecialNeedsDescription
                        });
                    }
                    // 3. New family member (omits ID per API contract)
                    else
                    {
                        req.NewFamilyMembers.Add(MapCreateFamilyMember(fm));
                    }
                }
            }

            // 4. Collect removed IDs:
            // First, from explicit removals tracked during UI deletion
            if (r.RemovedFamilyMemberIds != null)
            {
                foreach (var removedId in r.RemovedFamilyMemberIds)
                {
                    if ((existingIds.Count == 0 || existingIds.Contains(removedId)) && !req.RemovedFamilyMemberIds.Contains(removedId))
                    {
                        req.RemovedFamilyMemberIds.Add(removedId);
                    }
                }
            }

            // Second, reconcile: any existing member not present in processedExistingIds
            foreach (var existingId in existingIds)
            {
                if (!processedExistingIds.Contains(existingId) && !req.RemovedFamilyMemberIds.Contains(existingId))
                {
                    req.RemovedFamilyMemberIds.Add(existingId);
                }
            }

            return req;
        }

        public RenewSoloParentRequest MapToRenewRequest(Guid id, SoloParentRecord r, SoloParentDto existingEntity = null)
        {
            var problemPresented = (existingEntity?.ProblemPresented != null && !HasCircumstanceChanged(r, existingEntity.ProblemPresented))
                ? existingEntity.ProblemPresented
                : MapProblemPresented(r);

            var req = new RenewSoloParentRequest
            {
                CivilStatus = !string.IsNullOrWhiteSpace(r.CivilStatus) ? r.CivilStatus.Trim() : "Single",
                Employment = MapEmploymentInfo(r),
                ProblemPresented = problemPresented,
                NeedsAndProblems = r.NeedsAndProblems?.Trim(),
                OtherSourcesOfIncome = r.OtherIncomeSource?.Trim(),
                FamilyMembers = new RenewFamilyMembersRequest()
            };

            var existingIds = existingEntity?.FamilyMembers != null
                ? new HashSet<Guid>(existingEntity.FamilyMembers.Select(f => f.Id))
                : new HashSet<Guid>();

            var processedExistingIds = new HashSet<Guid>();

            if (r.FamilyMembers != null)
            {
                foreach (var fm in r.FamilyMembers)
                {
                    string mName = fm.MemberName?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(mName)) continue;

                    // 1. Existing family member (has database GUID)
                    if (fm.Id.HasValue && !fm.IsNew && (existingIds.Count == 0 || existingIds.Contains(fm.Id.Value)))
                    {
                        processedExistingIds.Add(fm.Id.Value);
                        req.FamilyMembers.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                        {
                            Id = fm.Id.Value,
                            Name = fm.MemberName,
                            Sex = NormalizeSex(fm.Sex),
                            Age = ParseAge(fm.Age, fm.Birthdate),
                            Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                            CivilStatus = string.IsNullOrWhiteSpace(fm.CivilStatus) ? "Single" : fm.CivilStatus,
                            Relationship = string.IsNullOrWhiteSpace(fm.Relationship) ? "Child" : fm.Relationship,
                            EducationalLevel = string.IsNullOrWhiteSpace(fm.EducationEmployment) ? "Student" : fm.EducationEmployment,
                            Income = ParseDecimal(fm.Income),
                            HasSpecialNeeds = fm.HasSpecialNeeds,
                            SpecialNeedsCategory = fm.SpecialNeedsCategory,
                            SpecialNeedsDescription = fm.SpecialNeedsDescription
                        });
                    }
                    // 2. Fallback: If Id was missing but matches an unclaimed existing member by Name
                    else if (!fm.Id.HasValue && !fm.IsNew && existingEntity?.FamilyMembers != null &&
                             existingEntity.FamilyMembers.FirstOrDefault(f => !processedExistingIds.Contains(f.Id) && string.Equals(f.Name?.Trim(), mName, StringComparison.OrdinalIgnoreCase)) is FamilyMemberDto matched)
                    {
                        processedExistingIds.Add(matched.Id);
                        req.FamilyMembers.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                        {
                            Id = matched.Id,
                            Name = fm.MemberName,
                            Sex = NormalizeSex(fm.Sex),
                            Age = ParseAge(fm.Age, fm.Birthdate),
                            Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                            CivilStatus = string.IsNullOrWhiteSpace(fm.CivilStatus) ? "Single" : fm.CivilStatus,
                            Relationship = string.IsNullOrWhiteSpace(fm.Relationship) ? "Child" : fm.Relationship,
                            EducationalLevel = string.IsNullOrWhiteSpace(fm.EducationEmployment) ? "Student" : fm.EducationEmployment,
                            Income = ParseDecimal(fm.Income),
                            HasSpecialNeeds = fm.HasSpecialNeeds,
                            SpecialNeedsCategory = fm.SpecialNeedsCategory,
                            SpecialNeedsDescription = fm.SpecialNeedsDescription
                        });
                    }
                    // 3. New family member
                    else
                    {
                        req.FamilyMembers.NewFamilyMembers.Add(MapCreateFamilyMember(fm));
                    }
                }
            }

            if (r.RemovedFamilyMemberIds != null)
            {
                foreach (var removedId in r.RemovedFamilyMemberIds)
                {
                    if ((existingIds.Count == 0 || existingIds.Contains(removedId)) && !req.FamilyMembers.RemovedFamilyMemberIds.Contains(removedId))
                    {
                        req.FamilyMembers.RemovedFamilyMemberIds.Add(removedId);
                    }
                }
            }

            foreach (var existingId in existingIds)
            {
                if (!processedExistingIds.Contains(existingId) && !req.FamilyMembers.RemovedFamilyMemberIds.Contains(existingId))
                {
                    req.FamilyMembers.RemovedFamilyMemberIds.Add(existingId);
                }
            }

            return req;
        }

        public SoloParentRecord MapToRecord(SoloParentDto dto)
        {
            if (dto == null) return null;

            bool isActive = dto.IsActive;
            DateTime validUntil = dto.CurrentTermExpiresAt ?? DateTime.MinValue;

            // Derive application-type flags and latest-activity date from RecordTerms.
            // The API never returns IsNewApplicant / IsRenewal directly; RecordTermDto.Type
            // ("New" or "Renewal") is the only authoritative source for these values.
            bool   isNewApplicant  = false;
            bool   isRenewal       = false;
            DateTime lastTermStart = DateTime.MinValue;   // best proxy for "last updated"

            if (dto.RecordTerms != null && dto.RecordTerms.Count > 0)
            {
                foreach (var rt in dto.RecordTerms)
                {
                    // Validity window
                    if (DateTime.TryParse(rt.ExpiresAt, out var exp))
                    {
                        if (exp > validUntil) validUntil = exp;
                        if (exp >= DateTime.Today && !dto.IsDeleted) isActive = true;
                    }

                    // Track start date so we can pick the most-recent term
                    DateTime termStart = rt.DateCreated ?? DateTime.MinValue;
                    if (!DateTime.TryParse(rt.StartsAt, out var startParsed))
                        startParsed = DateTime.MinValue;
                    if (startParsed > termStart) termStart = startParsed;

                    if (termStart >= lastTermStart)
                    {
                        // Most-recent (or equal) term — use its Type to classify the record
                        lastTermStart = termStart;
                        string termType = rt.Type ?? string.Empty;
                        isRenewal      = termType.Equals("Renewal", StringComparison.OrdinalIgnoreCase);
                        isNewApplicant = termType.Equals("New",     StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            // Fall back: if no terms came back, treat every record as a new applicant so
            // ApplicationType shows "New Record" instead of the empty "—" sentinel.
            if (!isNewApplicant && !isRenewal)
                isNewApplicant = true;

            // LastUpdated: prefer the most-recent term's start date over DateCreated because
            // DateCreated never changes after the initial insert.
            DateTime lastUpdated = lastTermStart != DateTime.MinValue ? lastTermStart : dto.DateCreated;

            string computedStatus = "Inactive";
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                computedStatus = dto.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Valid" : dto.Status;
            }
            else
            {
                computedStatus = isActive ? "Valid" : "Inactive";
            }

            var record = new SoloParentRecord
            {
                Id = dto.Id.ToString(),
                CreatedBy = "Admin",
                LastUpdated = lastUpdated,
                Status = computedStatus,
                DateOfApplication = dto.DateCreated,
                IsNewApplicant = isNewApplicant,
                IsRenewal      = isRenewal,
                CurrentTermExpiresAt = dto.CurrentTermExpiresAt ?? (validUntil != DateTime.MinValue ? (DateTime?)validUntil : null),
                RecordTerms = dto.RecordTerms ?? new List<RecordTermDto>(),

                Surname = dto.PersonalInfo?.LastName ?? string.Empty,
                FirstName = dto.PersonalInfo?.FirstName ?? string.Empty,
                MiddleName = dto.PersonalInfo?.MiddleName ?? string.Empty,
                ExtensionName = dto.PersonalInfo?.ExtensionName ?? string.Empty,
                DateOfBirth = dto.PersonalInfo?.Birthdate ?? DateTime.MinValue,
                PlaceOfBirth = dto.PersonalInfo?.Birthplace ?? string.Empty,
                Sex = dto.PersonalInfo?.Sex ?? string.Empty,
                CivilStatus = dto.PersonalInfo?.CivilStatus ?? string.Empty,
                EducationalAttainment = dto.PersonalInfo?.EducationalAttainment ?? string.Empty,
                PhilSysNumber = dto.PersonalInfo?.PhilsysCardNumber ?? string.Empty,
                Religion = dto.PersonalInfo?.Religion ?? string.Empty,

                Occupation = dto.Employment?.Occupation ?? string.Empty,
                MonthlyIncome = dto.Employment?.MonthlyIncome > 0 ? dto.Employment.MonthlyIncome.ToString("N2") : string.Empty,
                IsEmployed = string.Equals(dto.Employment?.EmploymentStatus, "employed", StringComparison.OrdinalIgnoreCase),
                IsSelfEmployed = string.Equals(dto.Employment?.EmploymentStatus, "self_employed", StringComparison.OrdinalIgnoreCase),
                IsNotEmployed = string.Equals(dto.Employment?.EmploymentStatus, "not_employed", StringComparison.OrdinalIgnoreCase),

                Address = dto.AddressDetails?.Address ?? string.Empty,
                Barangay = dto.AddressDetails?.Barangay ?? string.Empty,
                ContactNumber = dto.ContactDetails?.ApplicantContactNumber ?? string.Empty,

                EmergencyContactName = dto.EmergencyContact?.EmergencyPersonName ?? string.Empty,
                EmergencyRelationship = dto.EmergencyContact?.RelationshipToEmergencyPerson ?? string.Empty,
                EmergencyAddress = dto.EmergencyContact?.EmergencyPersonContactAddress ?? string.Empty,
                EmergencyContactNumber = dto.EmergencyContact?.EmergencyPersonContactNumber ?? string.Empty,

                ProblemPresented = dto.ProblemPresented?.Name ?? string.Empty,
                NeedsAndProblems = !string.IsNullOrWhiteSpace(dto.NeedsAndProblems)
                    ? dto.NeedsAndProblems
                    : (!IsKnownClassification(dto.ProblemPresented?.Name) ? (dto.ProblemPresented?.Name ?? string.Empty) : string.Empty),
                OtherIncomeSource = dto.OtherSourcesOfIncome ?? string.Empty,
                CircumstanceA2Cause = dto.ProblemPresented?.CauseOfDeath ?? string.Empty,
                CircumstanceA2Date = DateTime.TryParse(dto.ProblemPresented?.date, out var a2d) ? a2d : DateTime.MinValue,
                CircumstanceA4Disability = dto.ProblemPresented?.TypeOfDisability ?? string.Empty,
                CircumstanceA5Period = dto.ProblemPresented?.PeriodOfSeparation ?? string.Empty,
                CircumstanceA6Nullity = dto.ProblemPresented?.isNullity == true,
                CircumstanceA6Annulment = dto.ProblemPresented?.isAnnulmentOfMarraige == true,
                CircumstanceBStayAbroad = dto.ProblemPresented?.LengthOfAbroad.HasValue == true ? dto.ProblemPresented.LengthOfAbroad.Value.ToString() : string.Empty,
                IsPantawidBeneficiary = dto.IsPantawidBeneficiary,
                IsIndigenous = dto.IsIndigenous,
                IsLGBT = dto.IsLGBT,
            };

            record.Name = $"{record.Surname}, {record.FirstName} {record.MiddleName}".Trim();

            // Match circumstance checkboxes from ProblemPresentedNumber and Name
            string probNum = dto.ProblemPresented?.ProblemPresentedNumber ?? string.Empty;
            string probName = dto.ProblemPresented?.Name ?? string.Empty;

            var numTokens = new HashSet<string>(
                Regex.Matches(probNum, @"\b([A-Fa-f][1-7]?|[1-7])\b")
                     .Cast<Match>()
                     .Select(m => m.Value.ToUpperInvariant()),
                StringComparer.OrdinalIgnoreCase);
            string trimmedNum = probNum.Trim().ToUpperInvariant();
            bool HasCode(string code) => numTokens.Contains(code) || trimmedNum == code;

            record.CircumstanceA1 = HasCode("A1") || probName.IndexOf("rape", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceA2 = HasCode("A2") || probName.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("deceased", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("widow", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(record.CircumstanceA2Cause);
            record.CircumstanceA3 = HasCode("A3") || probName.IndexOf("detention", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("prison", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("detained", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceA4 = HasCode("A4") || probName.IndexOf("incapacity", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("disability", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(record.CircumstanceA4Disability);
            record.CircumstanceA5 = HasCode("A5") || probName.IndexOf("separation", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("separated", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(record.CircumstanceA5Period);
            record.CircumstanceA6 = HasCode("A6") || probName.IndexOf("nullity", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("annulment", StringComparison.OrdinalIgnoreCase) >= 0 || record.CircumstanceA6Nullity || record.CircumstanceA6Annulment;
            record.CircumstanceA7 = HasCode("A7") || probName.IndexOf("abandonment", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("abandoned", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceB  = HasCode("B")  || probName.IndexOf("ofw", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("abroad", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(record.CircumstanceBStayAbroad);
            record.CircumstanceC  = HasCode("C")  || probName.IndexOf("unmarried", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("single mother", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("single father", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceD  = HasCode("D")  || probName.IndexOf("guardian", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("adoptive", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("foster", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceE  = HasCode("E")  || probName.IndexOf("relative", StringComparison.OrdinalIgnoreCase) >= 0 || probName.IndexOf("4th civil", StringComparison.OrdinalIgnoreCase) >= 0;
            record.CircumstanceF  = HasCode("F")  || probName.IndexOf("pregnant", StringComparison.OrdinalIgnoreCase) >= 0;

            if (dto.FamilyMembers != null)
            {
                record.FamilyMembers = dto.FamilyMembers.Select(fm => new FamilyMember
                {
                    Id = fm.Id,
                    IsNew = false,
                    MemberName = fm.Name,
                    Sex = fm.Sex,
                    Age = fm.Age.ToString(),
                    Birthdate = fm.Birthdate != DateTime.MinValue ? fm.Birthdate.ToString("yyyy-MM-dd") : string.Empty,
                    CivilStatus = fm.CivilStatus,
                    Relationship = fm.Relationship,
                    EducationEmployment = fm.EducationalLevel,
                    Income = fm.Income > 0 ? fm.Income.ToString("N2") : "0",
                    HasSpecialNeeds = fm.HasSpecialNeeds,
                    SpecialNeedsCategory = fm.SpecialNeedsCategory,
                    SpecialNeedsDescription = fm.SpecialNeedsDescription
                }).ToList();

                record.Children = record.FamilyMembers.Count;
            }

            return record;
        }

        /// <summary>
        /// Maps a list-page item (SoloParentListItemDto) to a SoloParentRecord.
        /// Resolves name from root-level fields OR nested PersonalInfo,
        /// whichever is populated.
        /// </summary>
        public SoloParentRecord MapToRecord(SoloParentListItemDto dto)
        {
            if (dto == null) return null;

            // Resolve first/last name: prefer root-level flat fields, fall back to PersonalInfo
            string firstName = !string.IsNullOrWhiteSpace(dto.PersonalInfo?.FirstName)
                ? dto.PersonalInfo.FirstName
                : dto.FirstName ?? string.Empty;

            string lastName = !string.IsNullOrWhiteSpace(dto.PersonalInfo?.LastName)
                ? dto.PersonalInfo.LastName
                : dto.LastName ?? string.Empty;

            string middleName = !string.IsNullOrWhiteSpace(dto.PersonalInfo?.MiddleName)
                ? dto.PersonalInfo.MiddleName
                : dto.MiddleName ?? string.Empty;

            // Adapt to SoloParentDto and delegate to the full mapper
            var adapted = new SoloParentDto
            {
                Id                    = dto.Id,
                PersonalInfo          = new PersonalInfo
                {
                    FirstName              = firstName,
                    LastName               = lastName,
                    MiddleName             = middleName,
                    ExtensionName          = dto.PersonalInfo?.ExtensionName,
                    CivilStatus            = dto.PersonalInfo?.CivilStatus ?? string.Empty,
                    Sex                    = dto.PersonalInfo?.Sex ?? string.Empty,
                    Birthdate              = dto.PersonalInfo?.Birthdate ?? DateTime.MinValue,
                    Birthplace             = dto.PersonalInfo?.Birthplace ?? string.Empty,
                    EducationalAttainment  = dto.PersonalInfo?.EducationalAttainment ?? string.Empty,
                    PhilsysCardNumber      = dto.PersonalInfo?.PhilsysCardNumber ?? string.Empty,
                    Religion               = dto.PersonalInfo?.Religion ?? string.Empty,
                },
                Employment            = dto.Employment,
                ContactDetails        = dto.ContactDetails,
                AddressDetails        = dto.AddressDetails,
                EmergencyContact      = dto.EmergencyContact,
                ProblemPresented      = dto.ProblemPresented,
                NeedsAndProblems      = dto.NeedsAndProblems,
                OtherSourcesOfIncome  = dto.OtherSourcesOfIncome,
                IsLGBT                = dto.IsLGBT,
                IsPantawidBeneficiary = dto.IsPantawidBeneficiary,
                IsIndigenous          = dto.IsIndigenous,
                DateCreated           = dto.DateCreated,
                IsActive              = dto.IsActive,
                Status                = dto.Status,
            };

            var record = MapToRecord(adapted);

            // If the name is still blank (API returns a flat fullName), use it directly
            if (string.IsNullOrWhiteSpace(record.Surname) && !string.IsNullOrWhiteSpace(dto.FullName))
                record.Name = dto.FullName;

            return record;
        }

        // =========================================================================
        // Internal field mappers & sanitizers (enforcing backend FluentValidation)
        // =========================================================================

        private PersonalInfo MapPersonalInfo(SoloParentRecord r)
        {
            DateTime dob = r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth : new DateTime(1990, 1, 1);
            int age = DateTime.Today.Year - dob.Year;
            if (dob.Date > DateTime.Today.AddYears(-age)) age--;
            if (age < 15) age = 15;
            if (age > 120) age = 120;

            return new PersonalInfo
            {
                FirstName = !string.IsNullOrWhiteSpace(r.FirstName) ? r.FirstName.Trim() : "Applicant",
                LastName = !string.IsNullOrWhiteSpace(r.Surname) ? r.Surname.Trim() : "Applicant",
                MiddleName = r.MiddleName?.Trim(),
                ExtensionName = r.ExtensionName?.Trim(),
                CivilStatus = !string.IsNullOrWhiteSpace(r.CivilStatus) ? r.CivilStatus.Trim() : "Single",
                Sex = NormalizeSex(r.Sex),
                Birthdate = dob,
                Birthplace = !string.IsNullOrWhiteSpace(r.PlaceOfBirth) ? r.PlaceOfBirth.Trim() : "Biñan",
                Age = age,
                EducationalAttainment = !string.IsNullOrWhiteSpace(r.EducationalAttainment) ? r.EducationalAttainment.Trim() : "High School Graduate",
                PhilsysCardNumber = r.PhilSysNumber?.Trim(),
                Religion = !string.IsNullOrWhiteSpace(r.Religion) ? r.Religion.Trim() : "Roman Catholic"
            };
        }

        private EmploymentInfo MapEmploymentInfo(SoloParentRecord r)
        {
            string status = "not_employed";
            if (r.IsSelfEmployed) status = "self_employed";
            else if (r.IsEmployed) status = "employed";

            string occupation = !string.IsNullOrWhiteSpace(r.Occupation) ? r.Occupation.Trim() : (status == "not_employed" ? "Unemployed" : "Employed");
            decimal income = ParseDecimal(r.MonthlyIncome);

            return new EmploymentInfo
            {
                EmploymentStatus = status,
                Occupation = occupation,
                MonthlyIncome = income
            };
        }

        private ContactInfo MapContactInfo(SoloParentRecord r)
        {
            string cleanNumber = CleanPhoneNumber(r.ContactNumber);
            if (string.IsNullOrWhiteSpace(cleanNumber) || cleanNumber.Length < 10)
            {
                cleanNumber = "09170000000";
            }

            return new ContactInfo
            {
                ApplicantContactNumber = cleanNumber
            };
        }

        private AddressInfo MapAddressInfo(SoloParentRecord r)
        {
            return new AddressInfo
            {
                Address = !string.IsNullOrWhiteSpace(r.Address) ? r.Address.Trim() : "Biñan",
                Barangay = !string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay.Trim() : "Biñan Poblacion"
            };
        }

        private EmergencyContactInfo MapEmergencyContact(SoloParentRecord r)
        {
            string name = !string.IsNullOrWhiteSpace(r.EmergencyContactName) ? r.EmergencyContactName.Trim() : "Emergency Contact";
            string rel = !string.IsNullOrWhiteSpace(r.EmergencyRelationship) ? r.EmergencyRelationship.Trim() : "Relative";
            string addr = !string.IsNullOrWhiteSpace(r.EmergencyAddress) ? r.EmergencyAddress.Trim() : (!string.IsNullOrWhiteSpace(r.Address) ? r.Address.Trim() : "Biñan");
            string num = CleanPhoneNumber(r.EmergencyContactNumber);

            return new EmergencyContactInfo
            {
                EmergencyPersonName = name,
                RelationshipToEmergencyPerson = rel,
                EmergencyPersonContactAddress = addr,
                EmergencyPersonContactNumber = num
            };
        }

        private static readonly HashSet<string> KnownClassifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Birth from rape", "Death of Spouse", "Detention of Spouse", "Physical & Mental Incapacity of Spouse",
            "Legal/de facto Separation", "Declaration of Nullity/Annulment", "Abandonment of Spouse",
            "OFW-related Solo Parent", "Unmarried Mother/Father", "Legal Guardian / Adoptive Parent",
            "Relative within 4th civil degree", "Pregnant Woman", "Solo Parent", "Adolescent Parent",
            "Abandonment (A2)", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "B", "C", "D", "E", "F"
        };

        private static bool IsKnownClassification(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return KnownClassifications.Contains(name.Trim());
        }

        private ProblemPresentedDetails MapProblemPresented(SoloParentRecord r)
        {
            var codes = new List<string>();
            string title = "Solo Parent";

            if (r.CircumstanceA1) { codes.Add("A1"); title = "Birth from rape"; }
            if (r.CircumstanceA2) { codes.Add("A2"); title = "Death of Spouse"; }
            if (r.CircumstanceA3) { codes.Add("A3"); title = "Detention of Spouse"; }
            if (r.CircumstanceA4) { codes.Add("A4"); title = "Physical & Mental Incapacity of Spouse"; }
            if (r.CircumstanceA5) { codes.Add("A5"); title = "Legal/de facto Separation"; }
            if (r.CircumstanceA6) { codes.Add("A6"); title = "Declaration of Nullity/Annulment"; }
            if (r.CircumstanceA7) { codes.Add("A7"); title = "Abandonment of Spouse"; }
            if (r.CircumstanceB)  { codes.Add("B");  title = "OFW-related Solo Parent"; }
            if (r.CircumstanceC)  { codes.Add("C");  title = "Unmarried Mother/Father"; }
            if (r.CircumstanceD)  { codes.Add("D");  title = "Legal Guardian / Adoptive Parent"; }
            if (r.CircumstanceE)  { codes.Add("E");  title = "Relative within 4th civil degree"; }
            if (r.CircumstanceF)  { codes.Add("F");  title = "Pregnant Woman"; }

            // If no circumstance checkbox is checked, retain existing classification title if available
            if (codes.Count == 0 && !string.IsNullOrWhiteSpace(r.ProblemPresented))
            {
                title = r.ProblemPresented.Trim();
            }

            string code = codes.Count > 0 ? string.Join(",", codes) : string.Empty;

            decimal? lengthAbroad = null;
            if (decimal.TryParse(r.CircumstanceBStayAbroad, out var lab)) lengthAbroad = lab;

            return new ProblemPresentedDetails
            {
                ProblemPresentedNumber = code,
                Name = title,
                CauseOfDeath = !string.IsNullOrWhiteSpace(r.CircumstanceA2Cause) ? r.CircumstanceA2Cause.Trim() : null,
                date = r.CircumstanceA2Date != DateTime.MinValue ? r.CircumstanceA2Date.ToString("yyyy-MM-dd") : null,
                TypeOfDisability = !string.IsNullOrWhiteSpace(r.CircumstanceA4Disability) ? r.CircumstanceA4Disability.Trim() : null,
                PeriodOfSeparation = !string.IsNullOrWhiteSpace(r.CircumstanceA5Period) ? r.CircumstanceA5Period.Trim() : null,
                LengthOfAbroad = lengthAbroad,
                isNullity = r.CircumstanceA6 ? r.CircumstanceA6Nullity : (bool?)null,
                isAnnulmentOfMarraige = r.CircumstanceA6 ? r.CircumstanceA6Annulment : (bool?)null
            };
        }

        public static string GetCircumstanceCode(SoloParentRecord r)
        {
            if (r == null) return string.Empty;
            if (r.CircumstanceA1) return "A1";
            if (r.CircumstanceA2) return "A2";
            if (r.CircumstanceA3) return "A3";
            if (r.CircumstanceA4) return "A4";
            if (r.CircumstanceA5) return "A5";
            if (r.CircumstanceA6) return "A6";
            if (r.CircumstanceA7) return "A7";
            if (r.CircumstanceB) return "B";
            if (r.CircumstanceC) return "C";
            if (r.CircumstanceD) return "D";
            if (r.CircumstanceE) return "E";
            if (r.CircumstanceF) return "F";
            return ExtractCircumstanceCode(r.ProblemPresented);
        }

        public static string ExtractCircumstanceCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var match = Regex.Match(text, @"\b([A-Fa-f][1-7]?)\b");
            if (match.Success) return match.Value.ToUpperInvariant();

            string lower = text.ToLowerInvariant();
            if (lower.Contains("rape")) return "A1";
            if (lower.Contains("death") || lower.Contains("deceased") || lower.Contains("widow")) return "A2";
            if (lower.Contains("detention") || lower.Contains("prison") || lower.Contains("detained")) return "A3";
            if (lower.Contains("incapacity") || lower.Contains("disability")) return "A4";
            if (lower.Contains("separation") || lower.Contains("separated")) return "A5";
            if (lower.Contains("nullity") || lower.Contains("annulment")) return "A6";
            if (lower.Contains("abandonment") || lower.Contains("abandoned")) return "A7";
            if (lower.Contains("ofw") || lower.Contains("abroad") || lower.Contains("overseas")) return "B";
            if (lower.Contains("unmarried") || lower.Contains("single mother") || lower.Contains("single father")) return "C";
            if (lower.Contains("guardian") || lower.Contains("adoptive") || lower.Contains("foster")) return "D";
            if (lower.Contains("relative") || lower.Contains("4th civil")) return "E";
            if (lower.Contains("pregnant")) return "F";

            return string.Empty;
        }

        public static bool HasCircumstanceChanged(SoloParentRecord r, ProblemPresentedDetails existing)
        {
            if (existing == null) return true;

            string rCode = GetCircumstanceCode(r);
            string existingCode = !string.IsNullOrWhiteSpace(existing.ProblemPresentedNumber)
                ? ExtractCircumstanceCode(existing.ProblemPresentedNumber)
                : ExtractCircumstanceCode(existing.Name);

            // If neither has a detected code, check if raw name is equal
            if (string.IsNullOrWhiteSpace(rCode) && string.IsNullOrWhiteSpace(existingCode))
            {
                return !string.Equals((r.ProblemPresented ?? "").Trim(), (existing.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }

            if (!string.Equals(rCode, existingCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Same code — check subfields
            if (rCode == "A2")
            {
                bool causeChanged = !string.Equals((r.CircumstanceA2Cause ?? "").Trim(), (existing.CauseOfDeath ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                string existingDateStr = !string.IsNullOrWhiteSpace(existing.date) && DateTime.TryParse(existing.date, out var ed) ? ed.ToString("yyyy-MM-dd") : "";
                string newDateStr = r.CircumstanceA2Date != DateTime.MinValue ? r.CircumstanceA2Date.ToString("yyyy-MM-dd") : "";
                bool dateChanged = !string.Equals(existingDateStr, newDateStr, StringComparison.OrdinalIgnoreCase);
                return causeChanged || dateChanged;
            }
            if (rCode == "A4")
            {
                return !string.Equals((r.CircumstanceA4Disability ?? "").Trim(), (existing.TypeOfDisability ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }
            if (rCode == "A5")
            {
                return !string.Equals((r.CircumstanceA5Period ?? "").Trim(), (existing.PeriodOfSeparation ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }
            if (rCode == "A6")
            {
                bool oldNullity = existing.isNullity == true;
                bool oldAnnulment = existing.isAnnulmentOfMarraige == true;
                return r.CircumstanceA6Nullity != oldNullity || r.CircumstanceA6Annulment != oldAnnulment;
            }
            if (rCode == "B")
            {
                decimal.TryParse(r.CircumstanceBStayAbroad, out var newAbroad);
                decimal oldAbroad = existing.LengthOfAbroad ?? 0;
                return newAbroad != oldAbroad;
            }

            return false;
        }

        private CreateFamilyMemberRequest MapCreateFamilyMember(FamilyMember fm)
        {
            return new CreateFamilyMemberRequest
            {
                Name = !string.IsNullOrWhiteSpace(fm.MemberName) ? fm.MemberName.Trim() : "Dependent",
                Sex = NormalizeSex(fm.Sex),
                Age = ParseAge(fm.Age, fm.Birthdate),
                Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                CivilStatus = !string.IsNullOrWhiteSpace(fm.CivilStatus) ? fm.CivilStatus.Trim() : "Single",
                Relationship = !string.IsNullOrWhiteSpace(fm.Relationship) ? fm.Relationship.Trim() : "Child",
                EducationalLevel = !string.IsNullOrWhiteSpace(fm.EducationEmployment) ? fm.EducationEmployment.Trim() : "Student",
                Income = ParseDecimal(fm.Income),
                HasSpecialNeeds = fm.HasSpecialNeeds,
                SpecialNeedsCategory = fm.SpecialNeedsCategory,
                SpecialNeedsDescription = fm.SpecialNeedsDescription
            };
        }

        private static string NormalizeSex(string sex)
        {
            if (string.IsNullOrWhiteSpace(sex)) return "Female";
            string s = sex.Trim().ToUpperInvariant();
            if (s.StartsWith("M")) return "Male";
            return "Female";
        }

        private static string CleanPhoneNumber(string num)
        {
            if (string.IsNullOrWhiteSpace(num)) return string.Empty;
            return Regex.Replace(num, @"[^\d+]", "");
        }

        private static decimal ParseDecimal(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0m;
            string clean = Regex.Replace(val, @"[^\d\.]", "");
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return Math.Max(0m, result);
            }
            return 0m;
        }

        private static int ParseAge(string ageStr, string birthdateStr)
        {
            if (int.TryParse(ageStr, out var age) && age >= 0 && age <= 120)
                return age;

            if (DateTime.TryParse(birthdateStr, out var dt) && dt != DateTime.MinValue)
            {
                int calc = DateTime.Today.Year - dt.Year;
                if (dt.Date > DateTime.Today.AddYears(-calc)) calc--;
                if (calc >= 0 && calc <= 120) return calc;
            }

            return 5;
        }

        private static DateTime ParseDate(string dateStr, DateTime fallback)
        {
            if (DateTime.TryParse(dateStr, out var dt) && dt != DateTime.MinValue)
                return dt;
            return fallback;
        }
    }
}
