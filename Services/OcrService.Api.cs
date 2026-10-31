using System;

using System.Collections.Generic;

using System.Globalization;

using System.IO;

using System.Net.Http;

using System.Text.RegularExpressions;

using System.Threading.Tasks;

using SOLUM_UI.Models;

namespace SOLUM_UI.Services

{

    public static partial class OcrService

    {

        public static async Task<OcrFormResponse> ExtractFormAsync(string imagePath, string mode = "global", System.Threading.CancellationToken cancellationToken = default)

        {

            var result = new OcrFormResponse();

            if (!File.Exists(imagePath))

            {

                throw new FileNotFoundException("Image file not found at path: " + imagePath);

            }

            // Ensure server is running and warmed up

            if (!await IsServerOnlineAsync())

            {

                bool ready = await EnsureServerRunningAsync(25);

                if (!ready)

                {

                    throw new Exception("Could not connect to OCR Server at " + ServerUrl + ". The engine could not be started or is still initializing.");

                }

            }

            using (var form = new MultipartFormDataContent())

            using (var fileStream = File.OpenRead(imagePath))

            using (var streamContent = new StreamContent(fileStream))

            {

                form.Add(streamContent, "file", Path.GetFileName(imagePath));

                HttpResponseMessage response;

                try

                {

                    response = await _http.PostAsync($"{ServerUrl}/extract-form?mode={Uri.EscapeDataString(mode)}&include_preview=true", form, cancellationToken);

                }

                catch (Exception ex)

                {

                    throw new Exception("Could not connect to OCR Server at " + ServerUrl + ". Please ensure 'server.py' is running.\n\nDetails: " + ex.Message, ex);

                }

                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)

                {

                    throw new Exception($"OCR Server Error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}):\n\n{json}");

                }

                ParseFormJson(json, result);

                result.Status = "success";

            }

            return result;

        }

        public static async Task<OcrRoiResult> ExtractRoiAsync(string imagePath, double x, double y, double w, double h, string fieldKey = "")

        {

            var result = new OcrRoiResult { Bbox = new[] { x, y, w, h } };

            if (!File.Exists(imagePath)) return result;

            // Ensure server is running

            if (!await IsServerOnlineAsync())

            {

                bool ready = await EnsureServerRunningAsync(25);

                if (!ready) return result;

            }

            try

            {

                using (var form = new MultipartFormDataContent())

                using (var fileStream = File.OpenRead(imagePath))

                using (var streamContent = new StreamContent(fileStream))

                {

                    form.Add(streamContent, "file", Path.GetFileName(imagePath));

                    form.Add(new StringContent(x.ToString(CultureInfo.InvariantCulture)), "x");

                    form.Add(new StringContent(y.ToString(CultureInfo.InvariantCulture)), "y");

                    form.Add(new StringContent(w.ToString(CultureInfo.InvariantCulture)), "w");

                    form.Add(new StringContent(h.ToString(CultureInfo.InvariantCulture)), "h");

                    form.Add(new StringContent(fieldKey ?? ""), "field_key");

                    var response = await _http.PostAsync(ServerUrl + "/extract-roi", form);

                    if (response.IsSuccessStatusCode)

                    {

                        string json = await response.Content.ReadAsStringAsync();

                        result.Value = ExtractJsonString(json, "value");

                        result.RawValue = ExtractJsonString(json, "raw_value");

                        result.Rating = ExtractJsonString(json, "rating");

                        result.Confidence = ExtractJsonDouble(json, "confidence");

                        result.ExecutionTimeSeconds = ExtractJsonDouble(json, "execution_time_seconds");

                        // Parse tokens
                        var tokensMatch = Regex.Match(json, "\"tokens\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                        if (tokensMatch.Success)
                        {
                            var tokenBlocks = Regex.Matches(tokensMatch.Groups[1].Value, @"\{([^}]+)\}");
                            foreach (Match tb in tokenBlocks)
                            {
                                string b = tb.Groups[1].Value;
                                result.Tokens.Add(new OcrWordToken
                                {
                                    Text = ExtractJsonString(b, "text"),
                                    Confidence = ExtractJsonDouble(b, "conf"),
                                    Bbox = ExtractBboxFromBlock(b),
                                    IsLabel = ExtractJsonBool(b, "is_label")
                                });
                            }
                        }

                        // Parse filtered labels
                        var labelsMatch = Regex.Match(json, @"""filtered_labels""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
                        if (labelsMatch.Success)
                        {
                            var strMatches = Regex.Matches(labelsMatch.Groups[1].Value, @"""([^""]*)""");
                            foreach (Match sm in strMatches)
                            {
                                string lbl = sm.Groups[1].Value;
                                if (!string.IsNullOrWhiteSpace(lbl))
                                {
                                    result.FilteredLabels.Add(lbl);
                                }
                            }
                        }
                        result.Status = "success";

                    }

                }

            }

            catch (Exception ex)

            {

                result.Status = "error";

                System.Diagnostics.Debug.WriteLine("OCR ROI Error: " + ex.Message);

            }

            return result;

        }

        #region JSON Parsing Helpers

        private static void ParseFormJson(string json, OcrFormResponse resp)

        {

            if (string.IsNullOrWhiteSpace(json)) return;

            resp.FormId = ExtractJsonString(json, "form_id");

            resp.OverallRating = ExtractJsonString(json, "overall_rating");

            resp.OverallConfidence = ExtractJsonDouble(json, "overall_confidence");

            resp.PreviewImageBase64 = ExtractJsonString(json, "preview_image");
            resp.DebugImagePath = ExtractJsonString(json, "debug_image_path");

            // Parse Section II Encircled Circumstance

            var circMatch = Regex.Match(json, "\"circumstance\"\\s*:\\s*\\{([^}]+)\\}", RegexOptions.Singleline);

            if (circMatch.Success)

            {

                string cBlock = circMatch.Groups[1].Value;

                resp.Circumstance = new OcrCircumstanceResult

                {

                    Detected = ExtractJsonBool(cBlock, "detected"),

                    Code = ExtractJsonString(cBlock, "code")?.ToUpperInvariant(),

                    Label = ExtractJsonString(cBlock, "label")?.ToUpperInvariant(),

                    Confidence = ExtractJsonDouble(cBlock, "confidence"),

                    SubfieldCause = ExtractJsonString(cBlock, "subfield_cause")?.ToUpperInvariant(),

                    SubfieldDate = ExtractJsonString(cBlock, "subfield_date"),

                    SubfieldDisability = ExtractJsonString(cBlock, "subfield_disability")?.ToUpperInvariant(),

                    SubfieldPeriod = ExtractJsonString(cBlock, "subfield_period")?.ToUpperInvariant(),

                    SubfieldStayAbroad = ExtractJsonString(cBlock, "subfield_stay_abroad")?.ToUpperInvariant(),

                    SubfieldNullity = ExtractJsonBool(cBlock, "subfield_nullity"),

                    SubfieldAnnulment = ExtractJsonBool(cBlock, "subfield_annulment")

                };

            }

            var knownFields = new[]

            {

                "is_new_applicant", "is_renewal", "date_of_application",

                "last_name", "first_name", "middle_name", "ext_name",

                "civil_status", "sex", "birthdate", "birthplace", "age",

                "educational_attainment", "philsys_number", "religion",

                "occupation", "monthly_income", "employment_status",

                "address", "barangay", "contact_number",

                "emergency_name", "emergency_relationship", "emergency_address", "emergency_number"

            };

            foreach (var key in knownFields)

            {

                // Find the field block: "field_key": { ... }

                var fieldMatch = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\{([^}]+)\\}", RegexOptions.Singleline);

                if (fieldMatch.Success)

                {

                    string block = fieldMatch.Groups[1].Value;

                    string val = ExtractJsonString(block, "value");

                    if (key == "barangay")

                    {

                        val = FuzzyMatchBarangay(val);

                    }

                    else if (!string.IsNullOrWhiteSpace(val) && !bool.TryParse(val, out _))

                    {

                        val = val.ToUpperInvariant();

                    }

                    double conf = ExtractJsonDouble(block, "confidence");

                    string rating = ExtractJsonString(block, "rating");

                    double[] bbox = ExtractBboxFromBlock(block);

                    resp.Fields[key] = new OcrFieldResult

                    {

                        Value = val,

                        Confidence = conf,

                        Rating = rating,

                        Bbox = bbox

                    };

                }

            }

            // Parse Family Members: "family_members": [ { ... }, { ... } ]

            var membersMatch = Regex.Match(json, "\"family_members\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);

            if (membersMatch.Success && !string.IsNullOrWhiteSpace(membersMatch.Groups[1].Value))

            {

                var memberBlocks = Regex.Matches(membersMatch.Groups[1].Value, "\\{([^}]+)\\}", RegexOptions.Singleline);

                foreach (Match m in memberBlocks)

                {

                    string block = m.Groups[1].Value;

                    string memberName = ExtractJsonString(block, "memberName");

                    if (!string.IsNullOrWhiteSpace(memberName))

                    {

                        double nameConf = ExtractJsonDouble(block, "nameConf");

                        double sexConf  = ExtractJsonDouble(block, "sexConf");

                        double ageConf  = ExtractJsonDouble(block, "ageConf");

                        double dobConf  = ExtractJsonDouble(block, "dobConf");

                        double civConf  = ExtractJsonDouble(block, "civConf");

                        double relConf  = ExtractJsonDouble(block, "relConf");

                        double eduConf  = ExtractJsonDouble(block, "eduConf");

                        double incConf  = ExtractJsonDouble(block, "incConf");

                        resp.FamilyMembers.Add(new FamilyMember

                        {

                            MemberName          = memberName?.ToUpperInvariant(),

                            Sex                 = ExtractJsonString(block, "sex"),

                            Age                 = ExtractJsonString(block, "age"),

                            Birthdate           = ExtractJsonString(block, "birthdate"),

                            CivilStatus         = ExtractJsonString(block, "civilStatus"),

                            Relationship        = ExtractJsonString(block, "relationship")?.ToUpperInvariant(),

                            EducationEmployment = ExtractJsonString(block, "educationEmployment")?.ToUpperInvariant(),

                            Income              = ExtractJsonString(block, "income"),

                            NameConf            = nameConf > 0 ? nameConf : 0.90,

                            SexConf             = sexConf  > 0 ? sexConf  : 0.90,

                            AgeConf             = ageConf  > 0 ? ageConf  : 0.90,

                            DobConf             = dobConf  > 0 ? dobConf  : 0.90,

                            CivConf             = civConf  > 0 ? civConf  : 0.90,

                            RelConf             = relConf  > 0 ? relConf  : 0.90,

                            EduConf             = eduConf  > 0 ? eduConf  : 0.90,

                            IncConf             = incConf  > 0 ? incConf  : 0.90

                        });

                    }

                }

            }

        }

        private static double[] ExtractBboxFromBlock(string block)

        {

            var bboxMatch = Regex.Match(block, "\"bbox\"\\s*:\\s*\\[([^\\]]+)\\]");

            if (bboxMatch.Success)

            {

                var parts = bboxMatch.Groups[1].Value.Split(',');

                if (parts.Length == 4)

                {

                    if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&

                        double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&

                        double.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) &&

                        double.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h))

                    {

                        return new[] { x, y, w, h };

                    }

                }

            }

            return new double[4];

        }

        private static string ExtractJsonString(string json, string key)

        {

            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"(.*?)\"");

            return match.Success ? Regex.Unescape(match.Groups[1].Value) : string.Empty;

        }

        private static bool ExtractJsonBool(string json, string key)

        {

            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);

            return match.Success && string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);

        }

        private static double ExtractJsonDouble(string json, string key)

        {

            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*([0-9.]+)");

            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))

            {

                return val;

            }

            return 0.0;

        }

        #endregion

    }

}