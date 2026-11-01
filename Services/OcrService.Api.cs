using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
                    string lastErr = GetRecentServerErrors();
                    string msg = "Could not connect to OCR Server at " + ServerUrl + ". The engine could not be started or is still initializing.";
                    if (!string.IsNullOrEmpty(lastErr))
                    {
                        msg += "\n\nServer Output:\n" + lastErr;
                    }
                    throw new Exception(msg);
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
                    throw new Exception("Could not connect to OCR Server at " + ServerUrl + ". Please ensure the OCR background service is running.\n\nDetails: " + ex.Message, ex);
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
                        var jObj = JObject.Parse(json);

                        result.Value = jObj["value"]?.ToString() ?? string.Empty;
                        result.RawValue = jObj["raw_value"]?.ToString() ?? string.Empty;
                        result.Rating = jObj["rating"]?.ToString() ?? "Low";
                        result.Confidence = jObj["confidence"]?.Value<double>() ?? 0.0;
                        result.ExecutionTimeSeconds = jObj["execution_time_seconds"]?.Value<double>() ?? 0.0;

                        if (jObj["tokens"] is JArray tokensArr)
                        {
                            foreach (var tb in tokensArr)
                            {
                                var tokenBbox = tb["bbox"]?.ToObject<double[]>() ?? new double[4];
                                result.Tokens.Add(new OcrWordToken
                                {
                                    Text = tb["text"]?.ToString() ?? string.Empty,
                                    Confidence = tb["conf"]?.Value<double>() ?? 0.0,
                                    Bbox = tokenBbox,
                                    IsLabel = tb["is_label"]?.Value<bool>() ?? false
                                });
                            }
                        }

                        if (jObj["filtered_labels"] is JArray labelsArr)
                        {
                            foreach (var sm in labelsArr)
                            {
                                string lbl = sm.ToString();
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

            JObject jObj;
            try
            {
                jObj = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrService] JSON parse error: " + ex.Message);
                return;
            }

            resp.FormId = jObj["form_id"]?.ToString() ?? "CSWDO-066-2";
            resp.OverallRating = jObj["overall_rating"]?.ToString() ?? "Low";
            resp.OverallConfidence = jObj["overall_confidence"]?.Value<double>() ?? 0.0;
            resp.ExecutionTimeSeconds = jObj["execution_time_seconds"]?.Value<double>() ?? 0.0;
            resp.PreviewImageBase64 = jObj["preview_image"]?.ToString() ?? string.Empty;
            resp.DebugImagePath = jObj["debug_image_path"]?.ToString() ?? string.Empty;

            // 1. Circumstance
            if (jObj["circumstance"] is JObject circObj)
            {
                resp.Circumstance = new OcrCircumstanceResult
                {
                    Detected = circObj["detected"]?.Value<bool>() ?? false,
                    Code = circObj["code"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    Label = circObj["label"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    Confidence = circObj["confidence"]?.Value<double>() ?? 0.0,
                    SubfieldCause = circObj["subfield_cause"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    SubfieldDate = circObj["subfield_date"]?.ToString() ?? string.Empty,
                    SubfieldDisability = circObj["subfield_disability"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    SubfieldPeriod = circObj["subfield_period"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    SubfieldStayAbroad = circObj["subfield_stay_abroad"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                    SubfieldNullity = circObj["subfield_nullity"]?.Value<bool>() ?? false,
                    SubfieldAnnulment = circObj["subfield_annulment"]?.Value<bool>() ?? false
                };
            }

            // 2. Dynamic Form Fields
            if (jObj["fields"] is JObject fieldsObj)
            {
                foreach (var prop in fieldsObj.Properties())
                {
                    string key = prop.Name;
                    var fToken = prop.Value;
                    if (fToken == null) continue;

                    string val = fToken["value"]?.ToString() ?? string.Empty;
                    if (key.Equals("barangay", StringComparison.OrdinalIgnoreCase))
                    {
                        val = FuzzyMatchBarangay(val);
                    }
                    else if (!string.IsNullOrWhiteSpace(val) && !bool.TryParse(val, out _))
                    {
                        val = val.ToUpperInvariant();
                    }

                    double conf = fToken["confidence"]?.Value<double>() ?? 0.0;
                    string rating = fToken["rating"]?.ToString() ?? "Low";
                    double[] bbox = fToken["bbox"]?.ToObject<double[]>() ?? new double[4];
                    string category = fToken["category"]?.ToString() ?? "General";

                    resp.Fields[key] = new OcrFieldResult
                    {
                        Value = val,
                        Confidence = conf,
                        Rating = rating,
                        Bbox = bbox,
                        Category = category
                    };
                }

                // Ensure canonical aliases exist for Date of Application
                if (resp.Fields.TryGetValue("application_date", out var appDateFld) && !resp.Fields.ContainsKey("date_of_application"))
                {
                    resp.Fields["date_of_application"] = appDateFld;
                }
                else if (resp.Fields.TryGetValue("date_of_application", out var dateAppFld) && !resp.Fields.ContainsKey("application_date"))
                {
                    resp.Fields["application_date"] = dateAppFld;
                }
            }

            // 3. Family Members
            if (jObj["family_members"] is JArray memArray)
            {
                foreach (var mToken in memArray)
                {
                    string memberName = mToken["memberName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(memberName))
                    {
                        double nameConf = mToken["nameConf"]?.Value<double>() ?? 0.90;
                        double sexConf  = mToken["sexConf"]?.Value<double>() ?? 0.90;
                        double ageConf  = mToken["ageConf"]?.Value<double>() ?? 0.90;
                        double dobConf  = mToken["dobConf"]?.Value<double>() ?? 0.90;
                        double civConf  = mToken["civConf"]?.Value<double>() ?? 0.90;
                        double relConf  = mToken["relConf"]?.Value<double>() ?? 0.90;
                        double eduConf  = mToken["eduConf"]?.Value<double>() ?? 0.90;
                        double incConf  = mToken["incConf"]?.Value<double>() ?? 0.90;

                        resp.FamilyMembers.Add(new FamilyMember
                        {
                            MemberName          = memberName?.ToUpperInvariant(),
                            Sex                 = mToken["sex"]?.ToString() ?? string.Empty,
                            Age                 = mToken["age"]?.ToString() ?? string.Empty,
                            Birthdate           = mToken["birthdate"]?.ToString() ?? string.Empty,
                            CivilStatus         = mToken["civilStatus"]?.ToString() ?? string.Empty,
                            Relationship        = mToken["relationship"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                            EducationEmployment = mToken["educationEmployment"]?.ToString()?.ToUpperInvariant() ?? string.Empty,
                            Income              = mToken["income"]?.ToString() ?? string.Empty,
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

        #endregion
    }
}
