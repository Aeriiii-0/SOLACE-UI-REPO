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
            var queryParams = new List<string>();

            if (request.Id.HasValue)
                queryParams.Add($"id={request.Id.Value}");

            if (!string.IsNullOrWhiteSpace(request.Fullname))
                queryParams.Add($"fullname={Uri.EscapeDataString(request.Fullname.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.Sex) && !request.Sex.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"sex={Uri.EscapeDataString(request.Sex.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.Barangay) && !request.Barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"barangay={Uri.EscapeDataString(request.Barangay.Trim())}");

            if (request.IsActive.HasValue)
                queryParams.Add($"isActive={request.IsActive.Value.ToString().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(request.SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

            if (!string.IsNullOrWhiteSpace(request.SortOrder))
                queryParams.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder)}");

            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");

            string url = "api/soloparent?" + string.Join("&", queryParams);
            return await ApiClient.Instance.GetAsync<PagedResult<SoloParentSummaryDto>>(url);
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

        public async Task<BaseResponse<bool>> RenewSoloParentRecordAsync(Guid id)
        {
            return await ApiClient.Instance.PostAsync<object, bool>($"api/soloparent/{id}/renew", new { });
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
                ProblemPresented = MapProblemPresented(r)
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
            var req = new UpdateSoloParentRequest
            {
                Id = id,
                PersonalInfo = MapPersonalInfo(r),
                Employment = MapEmploymentInfo(r),
                ContactDetails = MapContactInfo(r),
                AddressDetails = MapAddressInfo(r),
                EmergencyContact = MapEmergencyContact(r),
                ProblemPresented = MapProblemPresented(r)
            };

            var existingMap = existingEntity?.FamilyMembers?.ToDictionary(f => f.Name?.Trim() ?? string.Empty, f => f)
                              ?? new Dictionary<string, FamilyMemberDto>(StringComparer.OrdinalIgnoreCase);

            var currentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (r.FamilyMembers != null)
            {
                foreach (var fm in r.FamilyMembers)
                {
                    string mName = fm.MemberName?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(mName)) continue;
                    currentNames.Add(mName);

                    if (existingMap.TryGetValue(mName, out var existingFm))
                    {
                        req.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                        {
                            Id = existingFm.Id,
                            Name = fm.MemberName,
                            Sex = NormalizeSex(fm.Sex),
                            Age = ParseAge(fm.Age, fm.Birthdate),
                            Birthdate = ParseDate(fm.Birthdate, DateTime.UtcNow.AddYears(-10)),
                            CivilStatus = string.IsNullOrWhiteSpace(fm.CivilStatus) ? "Single" : fm.CivilStatus,
                            Relationship = string.IsNullOrWhiteSpace(fm.Relationship) ? "Child" : fm.Relationship,
                            EducationalLevel = string.IsNullOrWhiteSpace(fm.EducationEmployment) ? "Student" : fm.EducationEmployment,
                            Income = ParseDecimal(fm.Income)
                        });
                    }
                    else
                    {
                        req.NewFamilyMembers.Add(MapCreateFamilyMember(fm));
                    }
                }
            }

            if (existingEntity?.FamilyMembers != null)
            {
                foreach (var fm in existingEntity.FamilyMembers)
                {
                    if (!currentNames.Contains(fm.Name?.Trim() ?? string.Empty))
                    {
                        req.RemovedFamilyMemberIds.Add(fm.Id);
                    }
                }
            }

            return req;
        }

        public SoloParentRecord MapToRecord(SoloParentDto dto)
        {
            if (dto == null) return null;

            bool isActive = false;
            DateTime validUntil = DateTime.MinValue;

            if (dto.RecordTerms != null && dto.RecordTerms.Count > 0)
            {
                foreach (var rt in dto.RecordTerms)
                {
                    if (DateTime.TryParse(rt.ExpiresAt, out var exp))
                    {
                        if (exp > validUntil) validUntil = exp;
                        if (exp >= DateTime.Today) isActive = true;
                    }
                }
            }

            var record = new SoloParentRecord
            {
                Id = dto.Id.ToString(),
                CreatedBy = "Admin",
                LastUpdated = dto.DateCreated,
                Status = isActive ? "Valid" : "Inactive",
                DateOfApplication = dto.DateCreated,

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
                IsPantawidBeneficiary = dto.PersonalInfo?.IsPantawidBeneficiary ?? false,
                IsIndigenousPerson = dto.PersonalInfo?.IsIndigenousPerson ?? false,
                IsLGBTQ = dto.PersonalInfo?.IsLGBTQ ?? false,

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

                NeedsAndProblems = dto.ProblemPresented?.Name ?? string.Empty,
                CircumstanceA2Cause = dto.ProblemPresented?.CauseOfDeath ?? string.Empty,
                CircumstanceA4Disability = dto.ProblemPresented?.TypeOfDisability ?? string.Empty,
                CircumstanceA5Period = dto.ProblemPresented?.PeriodOfSeparation ?? string.Empty,
                CircumstanceA6Nullity = dto.ProblemPresented?.isNullity == true,
                CircumstanceA6Annulment = dto.ProblemPresented?.isAnnulmentOfMarraige == true,
                CircumstanceBStayAbroad = dto.ProblemPresented?.LengthOfAbroad.HasValue == true ? dto.ProblemPresented.LengthOfAbroad.Value.ToString() : string.Empty,
            };

            record.Name = $"{record.Surname}, {record.FirstName} {record.MiddleName}".Trim();

            // Match circumstance checkboxes from ProblemPresentedNumber
            string probNum = dto.ProblemPresented?.ProblemPresentedNumber ?? string.Empty;
            record.CircumstanceA1 = probNum.Contains("A1");
            record.CircumstanceA2 = probNum.Contains("A2") || !string.IsNullOrWhiteSpace(record.CircumstanceA2Cause);
            record.CircumstanceA3 = probNum.Contains("A3");
            record.CircumstanceA4 = probNum.Contains("A4") || !string.IsNullOrWhiteSpace(record.CircumstanceA4Disability);
            record.CircumstanceA5 = probNum.Contains("A5") || !string.IsNullOrWhiteSpace(record.CircumstanceA5Period);
            record.CircumstanceA6 = probNum.Contains("A6") || record.CircumstanceA6Nullity || record.CircumstanceA6Annulment;
            record.CircumstanceA7 = probNum.Contains("A7");
            record.CircumstanceB = probNum.Contains("B") || !string.IsNullOrWhiteSpace(record.CircumstanceBStayAbroad);
            record.CircumstanceC = probNum.Contains("C");
            record.CircumstanceD = probNum.Contains("D");
            record.CircumstanceE = probNum.Contains("E");
            record.CircumstanceF = probNum.Contains("F");

            if (dto.FamilyMembers != null)
            {
                record.FamilyMembers = dto.FamilyMembers.Select(fm => new FamilyMember
                {
                    MemberName = fm.Name,
                    Sex = fm.Sex,
                    Age = fm.Age.ToString(),
                    Birthdate = fm.Birthdate != DateTime.MinValue ? fm.Birthdate.ToString("yyyy-MM-dd") : string.Empty,
                    CivilStatus = fm.CivilStatus,
                    Relationship = fm.Relationship,
                    EducationEmployment = fm.EducationalLevel,
                    Income = fm.Income > 0 ? fm.Income.ToString("N2") : "0"
                }).ToList();

                record.Children = record.FamilyMembers.Count;
            }

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
                Religion = !string.IsNullOrWhiteSpace(r.Religion) ? r.Religion.Trim() : "Roman Catholic",
                IsPantawidBeneficiary = r.IsPantawidBeneficiary,
                IsIndigenousPerson = r.IsIndigenousPerson,
                IsLGBTQ = r.IsLGBTQ
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

            string code = codes.Count > 0 ? string.Join(",", codes) : "A1";
            if (!string.IsNullOrWhiteSpace(r.NeedsAndProblems))
            {
                title = r.NeedsAndProblems;
            }

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
                isNullity = r.CircumstanceA6Nullity ? true : (bool?)null,
                isAnnulmentOfMarraige = r.CircumstanceA6Annulment ? true : (bool?)null
            };
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
                Income = ParseDecimal(fm.Income)
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
