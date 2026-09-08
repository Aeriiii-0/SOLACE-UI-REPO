using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public static class OcrService
    {
        public static string ServerUrl { get; set; } = "http://127.0.0.1:8000";
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        private static Process _serverProcess;

        public static async Task<bool> IsServerOnlineAsync()
        {
            try
            {
                using (var response = await _http.GetAsync(ServerUrl + "/health"))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EnsureServerRunningAsync(int timeoutSeconds = 25)
        {
            if (await IsServerOnlineAsync()) return true;

            try
            {
                StartLocalServerProcess();

                // Poll for up to timeoutSeconds for the server to spin up
                int iterations = Math.Max(10, timeoutSeconds * 2);
                for (int i = 0; i < iterations; i++)
                {
                    await Task.Delay(500);
                    if (await IsServerOnlineAsync())
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return await IsServerOnlineAsync();
        }

        public static void StartLocalServerProcess()
        {
            if (_serverProcess != null && !_serverProcess.HasExited) return;

            string ocrDir = FindOcrDirectory();

            // 1. Check for standalone compiled binary (e.g. PyInstaller distribution)
            string exePath = Path.Combine(ocrDir, "ocr_server.exe");
            if (File.Exists(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = ocrDir,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };
                _serverProcess = Process.Start(psi);
                return;
            }

            // 2. Check for local Python virtual environment or system Python
            string serverPy = Path.Combine(ocrDir, "server.py");
            if (!File.Exists(serverPy)) return;

            string pythonExe = FindPythonExecutable(ocrDir);

            var pyPsi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{serverPy}\"",
                WorkingDirectory = ocrDir,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            try
            {
                _serverProcess = Process.Start(pyPsi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrService] Failed to start Python server process: " + ex.Message);
            }
        }

        public static string FindOcrDirectory()
        {
            // 1. Check AppDomain BaseDirectory and search upwards up to 5 levels
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                if (string.IsNullOrEmpty(current)) break;
                string candidate = Path.Combine(current, "OcrService");
                if (Directory.Exists(candidate) && (File.Exists(Path.Combine(candidate, "server.py")) || File.Exists(Path.Combine(candidate, "ocr_server.exe"))))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            // 2. Check current working directory and search upwards
            string cwd = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                if (string.IsNullOrEmpty(cwd)) break;
                string candidate = Path.Combine(cwd, "OcrService");
                if (Directory.Exists(candidate) && (File.Exists(Path.Combine(candidate, "server.py")) || File.Exists(Path.Combine(candidate, "ocr_server.exe"))))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(cwd);
                if (parent == null) break;
                cwd = parent.FullName;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OcrService");
        }

        private static string FindPythonExecutable(string ocrDir)
        {
            // 1. Check venv inside OcrService directory
            string venvPython = Path.Combine(ocrDir, "venv", "Scripts", "python.exe");
            if (File.Exists(venvPython)) return venvPython;

            // 2. Check common Python installation paths
            string[] knownPaths = new[]
            {
                @"C:\Python311\python.exe",
                @"C:\Program Files\Python311\python.exe",
                @"C:\Python310\python.exe",
                @"C:\Program Files\Python310\python.exe",
                @"C:\Python312\python.exe",
                @"C:\Program Files\Python312\python.exe"
            };

            foreach (var p in knownPaths)
            {
                if (File.Exists(p)) return p;
            }

            return "python.exe";
        }

        public static void StopLocalServerProcess()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
            }
            catch { }
        }

        public static async Task<OcrFormResponse> ExtractFormAsync(string imagePath, string mode = "global")
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
                    response = await _http.PostAsync($"{ServerUrl}/extract-form?mode={Uri.EscapeDataString(mode)}", form);
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

        public static async Task<OcrRoiResult> ExtractRoiAsync(string imagePath, double x, double y, double w, double h)
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

                    var response = await _http.PostAsync(ServerUrl + "/extract-roi", form);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        result.Value = ExtractJsonString(json, "value");
                        result.Rating = ExtractJsonString(json, "rating");
                        result.Confidence = ExtractJsonDouble(json, "confidence");
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

        public static async Task<string> GetTemplateJsonAsync()
        {
            try
            {
                if (await IsServerOnlineAsync())
                {
                    var resp = await _http.GetAsync(ServerUrl + "/template");
                    if (resp.IsSuccessStatusCode)
                    {
                        return await resp.Content.ReadAsStringAsync();
                    }
                }
            }
            catch { }

            string localPath = GetLocalTemplatePath();
            if (File.Exists(localPath))
            {
                return File.ReadAllText(localPath);
            }
            return string.Empty;
        }

        public static async Task<bool> SaveTemplateJsonAsync(string json)
        {
            bool savedServer = false;
            try
            {
                if (await IsServerOnlineAsync())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var resp = await _http.PostAsync(ServerUrl + "/template", content);
                    savedServer = resp.IsSuccessStatusCode;
                }
            }
            catch { }

            try
            {
                string localPath = GetLocalTemplatePath();
                string dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(localPath, json, new System.Text.UTF8Encoding(false));
                return true;
            }
            catch
            {
                return savedServer;
            }
        }

        public static string GetLocalTemplatePath()
        {
            string ocrDir = FindOcrDirectory();
            return Path.Combine(ocrDir, "cswdo_template.json");
        }

        public static Dictionary<string, double[]> GetTemplateBoundingBoxes()
        {
            var dict = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = GetLocalTemplatePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var root = serializer.Deserialize<Dictionary<string, object>>(json);
                    if (root != null && root.TryGetValue("fields", out var fieldsObj) && fieldsObj is Dictionary<string, object> fields)
                    {
                        foreach (var kvp in fields)
                        {
                            if (kvp.Value is Dictionary<string, object> fieldDict &&
                                fieldDict.TryGetValue("bbox", out var bboxObj) &&
                                bboxObj is System.Collections.IEnumerable list)
                            {
                                var nums = list.Cast<object>().Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray();
                                if (nums.Length >= 4)
                                {
                                    dict[kvp.Key] = new[] { nums[0], nums[1], nums[2], nums[3] };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrService] Error loading template bboxes: " + ex.Message);
            }
            return dict;
        }

        #region Official Biñan Barangays & Fuzzy Matching

        public static readonly string[] BinanBarangays = new[]
        {
            "Biñan Poblacion", "Bungahan", "Canlalay", "Casile", "De La Paz",
            "Ganado", "Langkiwa", "Loma", "Malaban", "Malamig", "Mamplasan",
            "Platero", "Poblacion", "San Antonio", "San Francisco (Halang)",
            "San Jose", "San Vicente", "Santo Niño", "Santo Tomas (Calabuso)",
            "Soro-Soro", "Timbao", "Tubigan", "Zapote"
        };

        private static readonly Dictionary<string, string> BarangayAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "halang", "San Francisco (Halang)" },
            { "san francisco", "San Francisco (Halang)" },
            { "san fran", "San Francisco (Halang)" },
            { "calabuso", "Santo Tomas (Calabuso)" },
            { "sto tomas", "Santo Tomas (Calabuso)" },
            { "sto. tomas", "Santo Tomas (Calabuso)" },
            { "sto tomas calabuso", "Santo Tomas (Calabuso)" },
            { "sto. tomas calabuso", "Santo Tomas (Calabuso)" },
            { "santo tomas", "Santo Tomas (Calabuso)" },
            { "sto nino", "Santo Niño" },
            { "sto. nino", "Santo Niño" },
            { "sto niño", "Santo Niño" },
            { "sto. niño", "Santo Niño" },
            { "santo nino", "Santo Niño" },
            { "delapaz", "De La Paz" },
            { "dela paz", "De La Paz" },
            { "mampalasan", "Mamplasan" },
            { "mamplasan", "Mamplasan" },
            { "sorosoro", "Soro-Soro" },
            { "soro soro", "Soro-Soro" },
            { "binan poblacion", "Biñan Poblacion" },
            { "binan", "Biñan Poblacion" },
            { "poblacion", "Poblacion" },
            { "pob", "Poblacion" }
        };

        /// <summary>
        /// Evaluates OCR text against the 24 official Biñan barangays using aliases and fuzzy string matching.
        /// </summary>
        public static string FuzzyMatchBarangay(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText) || IsOcrNotApplicable(rawText)) return string.Empty;

            string cleaned = rawText.Trim();
            if (IsOcrNotApplicable(cleaned)) return string.Empty;

            string norm = NormalizeBarangayText(cleaned);
            if (string.IsNullOrEmpty(norm)) return string.Empty;

            // 1. Direct alias match
            if (BarangayAliases.TryGetValue(norm, out string directAlias))
            {
                return directAlias;
            }

            // 2. Substring check against aliases
            foreach (var kvp in BarangayAliases)
            {
                if (kvp.Key.Length >= 4 && (norm == kvp.Key || norm.Contains(kvp.Key)))
                {
                    return kvp.Value;
                }
            }

            // 3. Direct / core name match against canonical barangays
            foreach (var brgy in BinanBarangays)
            {
                string normBrgy = NormalizeBarangayText(brgy);
                if (normBrgy == norm) return brgy;

                string core = NormalizeBarangayText(brgy.Split('(')[0].Trim());
                if (!string.IsNullOrEmpty(core) && (core == norm || (core.Length >= 5 && norm.Contains(core))))
                {
                    return brgy;
                }
            }

            // 4. Fuzzy Levenshtein / similarity ratio across all candidates
            string bestMatch = null;
            double bestScore = 0.0;

            var candidates = new List<KeyValuePair<string, string>>();
            foreach (var b in BinanBarangays)
            {
                candidates.Add(new KeyValuePair<string, string>(NormalizeBarangayText(b), b));
                string core = NormalizeBarangayText(b.Split('(')[0].Trim());
                if (!string.IsNullOrEmpty(core) && core != NormalizeBarangayText(b))
                {
                    candidates.Add(new KeyValuePair<string, string>(core, b));
                }
            }
            foreach (var kvp in BarangayAliases)
            {
                candidates.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
            }

            foreach (var candidate in candidates)
            {
                double sim = ComputeSimilarity(norm, candidate.Key);
                if (norm.StartsWith(candidate.Key) || candidate.Key.StartsWith(norm))
                {
                    sim = Math.Max(sim, 0.82);
                }

                if (sim > bestScore)
                {
                    bestScore = sim;
                    bestMatch = candidate.Value;
                }
            }

            if (bestMatch != null && bestScore >= 0.60)
            {
                return bestMatch;
            }

            return cleaned;
        }

        public static readonly string[] StandardExtensions = new[] { "Jr.", "Sr.", "II", "III", "IV", "V" };

        public static string FuzzyMatchExtension(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant().Replace(".", "").Replace(" ", "");
            if (norm == "jr" || norm == "junior" || norm == "jnr" || norm == "jr.") return "Jr.";
            if (norm == "sr" || norm == "senior" || norm == "snr" || norm == "sr.") return "Sr.";
            if (norm == "ii" || norm == "2nd" || norm == "two") return "II";
            if (norm == "iii" || norm == "3rd" || norm == "three") return "III";
            if (norm == "iv" || norm == "4th" || norm == "four") return "IV";
            if (norm == "v" || norm == "5th" || norm == "five") return "V";

            foreach (var ext in StandardExtensions)
            {
                string extNorm = ext.ToLowerInvariant().Replace(".", "");
                if (ComputeSimilarity(norm, extNorm) >= 0.70)
                    return ext;
            }
            return string.Empty;
        }

        public static string FuzzyMatchSex(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            if (norm.StartsWith("f") || norm.Contains("fem") || norm.Contains("babae") || norm == "w") return "Female";
            if (norm.StartsWith("m") || norm.Contains("masc") || norm.Contains("lalaki") || norm == "man") return "Male";
            if (norm.Contains("other") || norm.Contains("lgbt") || norm.Contains("x")) return "Others";
            return string.Empty;
        }

        public static readonly string[] StandardReligions = new[]
        {
            "Roman Catholic",
            "Islam",
            "Iglesia ni Cristo",
            "Seventh-day Adventist",
            "Evangelical / Born Again",
            "Jehovah's Witnesses",
            "Baptist",
            "Methodist",
            "Others"
        };

        public static string FuzzyMatchReligion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            string clean = Regex.Replace(norm, @"[^a-z0-9]", "");

            // 1. Direct & keyword matching for Roman Catholic
            string[] cathKeywords = new[] { "cath", "katol", "roman", "rc", "ctolc", "catol", "katlk", "cthlc", "cathl", "cato", "kato", "ctc" };
            if (cathKeywords.Any(k => clean.Contains(k)) || norm.Contains("catholic") || norm.Contains("katoliko") || norm.Contains("r.c."))
                return "Roman Catholic";

            if (ComputeSimilarity(clean, "catholic") >= 0.45 || ComputeSimilarity(clean, "katoliko") >= 0.45)
                return "Roman Catholic";

            // 2. Other denominations
            if (clean.Contains("islam") || clean.Contains("muslim") || clean.Contains("moslem") || clean.Contains("mslm")) return "Islam";
            if (clean.Contains("inc") || clean.Contains("iglesia") || clean.Contains("cristo") || clean.Contains("kristo")) return "Iglesia ni Cristo";
            if (clean.Contains("advent") || clean.Contains("sda") || clean.Contains("seventh") || clean.Contains("7th")) return "Seventh-day Adventist";
            if (clean.Contains("born") || clean.Contains("evangel") || clean.Contains("again") || clean.Contains("christian")) return "Evangelical / Born Again";
            if (clean.Contains("jehovah") || clean.Contains("witness") || clean.Contains("jw") || clean.Contains("saksi")) return "Jehovah's Witnesses";
            if (clean.Contains("baptist") || clean.Contains("bautis") || clean.Contains("bap")) return "Baptist";
            if (clean.Contains("methodist") || clean.Contains("metod")) return "Methodist";

            foreach (var rel in StandardReligions)
            {
                if (ComputeSimilarity(norm, rel.ToLowerInvariant()) >= 0.55)
                    return rel;
            }

            return IsOcrNotApplicable(raw) ? string.Empty : clean_text_capitalized(raw);
        }

        public static bool IsOcrNotApplicable(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return true;
            string norm = raw.Trim().ToUpperInvariant();
            string clean = Regex.Replace(norm, @"[^A-Z0-9]", "");
            string[] naList = new[] {
                "N/A", "NA", "NLA", "N\\A", "N|A", "N1A", "NIA", "N-A", "N_A", "N A",
                "N.A.", "N.A", "NONE", "NONE.", "WALA", "-", "--", "—", "0", "0.00",
                "NULL", "NOT APPLICABLE", "NOT APPL", "NO", "N. A."
            };
            if (naList.Contains(norm)) return true;
            if (clean == "NA" || clean == "NLA" || clean == "NIA" || clean == "N1A" || clean == "NONE" || clean == "WALA" || clean == "NOTAPPLICABLE" || clean == "NOTAPPL" || clean == "NULL") return true;
            return false;
        }

        public static string FuzzyMatchIncome(string raw)
        {
            if (IsOcrNotApplicable(raw)) return "0";
            string digits = Regex.Replace(raw.Trim(), @"[^\d.]", "");
            if (string.IsNullOrWhiteSpace(digits)) return "0";
            if (double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                return (val == Math.Floor(val)) ? ((long)val).ToString() : val.ToString("F2", CultureInfo.InvariantCulture);
            }
            return digits;
        }

        public static readonly string[] StandardCivilStatuses = new[]
        {
            "Single",
            "Married",
            "Widowed",
            "Separated",
            "Divorced",
            "Annulled",
            "Common-law / Live-in",
            "Others"
        };

        public static string FuzzyMatchCivilStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            string clean = Regex.Replace(norm, @"[^a-z]", "");

            // 1. Single letter or short abbreviation
            if (clean == "s" || clean == "sin" || clean == "sngl" || clean == "simgle" || clean == "simglt" || clean == "singl")
                return "Single";
            if (clean == "m" || clean == "mar" || clean == "marr" || clean == "mared" || clean == "marrd")
                return "Married";
            if (clean == "w" || clean == "wid" || clean == "wdw" || clean == "balo" || clean == "biyuda" || clean == "biyudo")
                return "Widowed";
            if (clean == "d" || clean == "div")
                return "Divorced";
            if (clean == "a" || clean == "anul" || clean == "anulled" || clean == "annul")
                return "Annulled";
            if (clean == "sep" || clean == "hiwalay")
                return "Separated";
            if (clean == "c" || clean == "live" || clean == "livein" || clean == "kinakasama")
                return "Common-law / Live-in";

            // 2. Keyword containment
            if (clean.Contains("single") || clean.Contains("dalaga") || clean.Contains("binata") || clean.Contains("walang"))
                return "Single";
            if (clean.Contains("married") || clean.Contains("kasal"))
                return "Married";
            if (clean.Contains("widow") || clean.Contains("biyud") || clean.Contains("balo"))
                return "Widowed";
            if (clean.Contains("sep") || clean.Contains("hiwalay") || clean.Contains("defacto"))
                return "Separated";
            if (clean.Contains("divorc") || clean.Contains("diborsyo"))
                return "Divorced";
            if (clean.Contains("annul") || clean.Contains("pawalang"))
                return "Annulled";
            if (clean.Contains("live") || clean.Contains("common") || clean.Contains("kinakasama"))
                return "Common-law / Live-in";

            foreach (var st in StandardCivilStatuses)
            {
                if (ComputeSimilarity(clean, st.ToLowerInvariant()) >= 0.60)
                    return st;
            }

            return IsOcrNotApplicable(raw) ? string.Empty : clean_text_capitalized(raw);
        }

        public static readonly string[] StandardRelationships = new[]
        {
            "Mother",
            "Father",
            "Sister",
            "Brother",
            "Son",
            "Daughter",
            "Child",
            "Spouse",
            "Aunt",
            "Uncle",
            "Cousin",
            "Grandmother",
            "Grandfather",
            "In-law",
            "Friend",
            "Neighbor",
            "Guardian",
            "Others"
        };

        public static string FuzzyMatchRelationship(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            string clean = Regex.Replace(norm, @"[^a-z]", "");

            if (clean.Contains("sister") || clean.Contains("sis") || clean.Contains("ate") || clean.Contains("sstr")) return "Sister";
            if (clean.Contains("brother") || clean.Contains("bro") || clean.Contains("kuya") || clean.Contains("brthr")) return "Brother";
            if (clean.Contains("mother") || clean.Contains("ina") || clean.Contains("nanay") || clean.Contains("mom") || clean.Contains("mama") || clean.Contains("nay")) return "Mother";
            if (clean.Contains("father") || clean.Contains("ama") || clean.Contains("tatay") || clean.Contains("dad") || clean.Contains("papa") || clean.Contains("tay")) return "Father";
            if (clean.Contains("daughter") || clean.Contains("dauglster") || clean.Contains("dtr") || clean.Contains("daugh")) return "Daughter";
            if (clean.Contains("son") || clean.Contains("anaknalalaki")) return "Son";
            if (clean.Contains("child") || clean.Contains("anak") || clean.Contains("bata")) return "Child";
            if (clean.Contains("spouse") || clean.Contains("asawa") || clean.Contains("husband") || clean.Contains("wife") || clean.Contains("partner")) return "Spouse";
            if (clean.Contains("aunt") || clean.Contains("tita") || clean.Contains("tiya")) return "Aunt";
            if (clean.Contains("uncle") || clean.Contains("tito") || clean.Contains("tiyo")) return "Uncle";
            if (clean.Contains("cousin") || clean.Contains("pinsan")) return "Cousin";
            if (clean.Contains("grandmother") || clean.Contains("lola") || clean.Contains("grandma")) return "Grandmother";
            if (clean.Contains("grandfather") || clean.Contains("lolo") || clean.Contains("grandpa")) return "Grandfather";
            if (clean.Contains("inlaw") || clean.Contains("biyanan") || clean.Contains("hipag") || clean.Contains("bayaw")) return "In-law";
            if (clean.Contains("friend") || clean.Contains("kaibigan")) return "Friend";
            if (clean.Contains("neighbor") || clean.Contains("kapitbahay")) return "Neighbor";
            if (clean.Contains("guardian") || clean.Contains("tagapangalaga")) return "Guardian";

            foreach (var r in StandardRelationships)
            {
                if (ComputeSimilarity(clean, r.ToLowerInvariant()) >= 0.60)
                    return r;
            }

            return IsOcrNotApplicable(raw) ? string.Empty : clean_text_capitalized(raw);
        }

        public static readonly string[] StandardEducations = new[]
        {
            "Elementary Level",
            "Elementary Graduate",
            "High School Level",
            "High School Graduate",
            "Vocational / TVET",
            "College Level",
            "College Graduate",
            "Post-Graduate",
            "None",
            "Others"
        };

        public static string FuzzyMatchEducation(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            string clean = Regex.Replace(norm, @"[^a-z0-9]", "");

            // Post Graduate
            if (clean.Contains("post") || clean.Contains("master") || clean.Contains("doctor") || 
                clean.Contains("phd") || clean.Contains("ms") || clean.Contains("ma") || 
                clean.Contains("mba") || clean.Contains("juris") || clean.Contains("law"))
            {
                return "Post Graduate";
            }

            // College / Bachelor Degrees (e.g. BS Nursing, BSN, BS, BA, AB, College, University, Engineering, Education)
            if (clean.Contains("col") || clean.Contains("univ") || clean.Contains("tertiary") ||
                clean.Contains("bs") || clean.Contains("ba") || clean.Contains("bachelor") ||
                clean.Contains("nurs") || clean.Contains("eng") || clean.Contains("educ") ||
                clean.Contains("degree") || clean.Contains("undergrad") || clean.Contains("grad") ||
                clean.Contains("crim") || clean.Contains("psych") || clean.Contains("account") ||
                clean.Contains("tapos"))
            {
                return "College";
            }

            // Vocational / TVET / TESDA / Caregiver
            if (clean.Contains("voc") || clean.Contains("tech") || clean.Contains("tvet") || 
                clean.Contains("tesda") || clean.Contains("caregiver") || clean.Contains("nc2") || clean.Contains("diploma"))
            {
                return "Vocational";
            }

            // High School
            if (clean.Contains("high") || clean.Contains("hs") || clean.Contains("secondary") || 
                clean.Contains("mataas") || clean.Contains("senior") || clean.Contains("junior") || clean.Contains("shs") || clean.Contains("jhs"))
            {
                return "High School";
            }

            // Elementary
            if (clean.Contains("elem") || clean.Contains("primary") || clean.Contains("mababa") || clean.Contains("grade"))
            {
                return "Elementary";
            }

            if (clean.Contains("none") || clean.Contains("wala") || clean.Contains("na"))
                return string.Empty;

            // Fallback similarity check against standard 5
            string[] standardOptions = { "College", "High School", "Elementary", "Vocational", "Post Graduate" };
            foreach (var opt in standardOptions)
            {
                if (ComputeSimilarity(clean, opt.ToLowerInvariant()) >= 0.50)
                    return opt;
            }

            return IsOcrNotApplicable(raw) ? string.Empty : clean_text_capitalized(raw);
        }

        private static string clean_text_capitalized(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw.Trim().ToLowerInvariant());
        }

        private static string NormalizeBarangayText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text.ToLowerInvariant().Replace("ñ", "n");
            t = Regex.Replace(t, @"[^a-z0-9\s]", " ");
            t = Regex.Replace(t, @"\s+", " ").Trim();
            return t;
        }

        private static double ComputeSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;
            if (s1 == s2) return 1.0;

            int dist = LevenshteinDistance(s1, s2);
            int maxLen = Math.Max(s1.Length, s2.Length);
            if (maxLen == 0) return 1.0;

            return 1.0 - ((double)dist / maxLen);
        }

        private static int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s1.Length, s2.Length];
        }

        #endregion

        public static bool TryParseOcrDate(string raw, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string s = raw.Trim();
            s = Regex.Replace(s, @"^(date\s*of\s*birth|birthdate|dob|kaarawan|bday)\s*[:\-_.]*", "", RegexOptions.IgnoreCase).Trim();
            s = Regex.Replace(s, @"^\s*\(?\s*m+[/.]?d+[/.]?y+\s*\)?\s*", "", RegexOptions.IgnoreCase).Trim();

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;
            if (DateTime.TryParse(s, out result))
                return true;

            // Check ISO YYYY-MM-DD or YYYY/MM/DD
            var matchIso = Regex.Match(s, @"^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})");
            if (matchIso.Success)
            {
                int y = int.Parse(matchIso.Groups[1].Value);
                int m = int.Parse(matchIso.Groups[2].Value);
                int d = int.Parse(matchIso.Groups[3].Value);
                try
                {
                    result = new DateTime(y, m, d);
                    return true;
                }
                catch { }
            }

            // Check missing one slash: e.g. 1/252020 or 01/252020
            var mOneSlash1 = Regex.Match(s, @"^(\d{1,2})[/\-.](\d{1,2})(\d{4})$");
            if (mOneSlash1.Success)
            {
                int p1 = int.Parse(mOneSlash1.Groups[1].Value);
                int p2 = int.Parse(mOneSlash1.Groups[2].Value);
                int yr = int.Parse(mOneSlash1.Groups[3].Value);
                int m = p1, d = p2;
                if (m > 12 && d <= 12) { int tmp = m; m = d; d = tmp; }
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    result = new DateTime(yr, m, d);
                    return true;
                }
            }

            // Check missing first slash: e.g. 0125/2020
            var mOneSlash2 = Regex.Match(s, @"^(\d{1,2})(\d{2})[/\-.](\d{4})$");
            if (mOneSlash2.Success)
            {
                int p1 = int.Parse(mOneSlash2.Groups[1].Value);
                int p2 = int.Parse(mOneSlash2.Groups[2].Value);
                int yr = int.Parse(mOneSlash2.Groups[3].Value);
                int m = p1, d = p2;
                if (m > 12 && d <= 12) { int tmp = m; m = d; d = tmp; }
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    result = new DateTime(yr, m, d);
                    return true;
                }
            }

            // Check written month: e.g. June 19 2004 or Jum192004 or 19 June 2004
            var mWordMatch = Regex.Match(s, @"([a-zA-Z]{3,})\s*(\d{1,2})\s*(\d{4}|\d{2})");
            if (mWordMatch.Success)
            {
                string mStr = mWordMatch.Groups[1].Value.ToLowerInvariant();
                int day = int.Parse(mWordMatch.Groups[2].Value);
                string yStr = mWordMatch.Groups[3].Value;
                int yr = yStr.Length == 4 ? int.Parse(yStr) : (int.Parse(yStr) < 40 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));
                int mNum = ParseMonthWordToNum(mStr);
                if (mNum > 0 && day >= 1 && day <= 31)
                {
                    try
                    {
                        result = new DateTime(yr, mNum, day);
                        return true;
                    }
                    catch { }
                }
            }

            var mDayFirst = Regex.Match(s, @"(\d{1,2})\s*([a-zA-Z]{3,})\s*(\d{4}|\d{2})");
            if (mDayFirst.Success)
            {
                int day = int.Parse(mDayFirst.Groups[1].Value);
                string mStr = mDayFirst.Groups[2].Value.ToLowerInvariant();
                string yStr = mDayFirst.Groups[3].Value;
                int yr = yStr.Length == 4 ? int.Parse(yStr) : (int.Parse(yStr) < 40 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));
                int mNum = ParseMonthWordToNum(mStr);
                if (mNum > 0 && day >= 1 && day <= 31)
                {
                    try
                    {
                        result = new DateTime(yr, mNum, day);
                        return true;
                    }
                    catch { }
                }
            }

            // Check pure 8 digits: 06192004 or 20040619
            var digitsOnly = Regex.Replace(s, @"\D", "");
            if (digitsOnly.Length == 8)
            {
                int y1 = int.Parse(digitsOnly.Substring(0, 4));
                int m1 = int.Parse(digitsOnly.Substring(4, 2));
                int d1 = int.Parse(digitsOnly.Substring(6, 2));
                if (y1 >= 1920 && y1 <= 2030 && m1 >= 1 && m1 <= 12 && d1 >= 1 && d1 <= 31)
                {
                    try { result = new DateTime(y1, m1, d1); return true; } catch { }
                }

                int m2 = int.Parse(digitsOnly.Substring(0, 2));
                int d2 = int.Parse(digitsOnly.Substring(2, 2));
                int y2 = int.Parse(digitsOnly.Substring(4, 4));
                if (y2 >= 1920 && y2 <= 2030 && m2 >= 1 && m2 <= 12 && d2 >= 1 && d2 <= 31)
                {
                    try { result = new DateTime(y2, m2, d2); return true; } catch { }
                }
            }

            if (digitsOnly.Length == 7)
            {
                int m = int.Parse(digitsOnly.Substring(0, 1));
                int d = int.Parse(digitsOnly.Substring(1, 2));
                int yr = int.Parse(digitsOnly.Substring(3, 4));
                if (yr >= 1920 && yr <= 2030 && m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    try { result = new DateTime(yr, m, d); return true; } catch { }
                }
            }

            if (digitsOnly.Length == 6)
            {
                int m = int.Parse(digitsOnly.Substring(0, 2));
                int d = int.Parse(digitsOnly.Substring(2, 2));
                int y2 = int.Parse(digitsOnly.Substring(4, 2));
                int yr = y2 < 40 ? 2000 + y2 : 1900 + y2;
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    try { result = new DateTime(yr, m, d); return true; } catch { }
                }
            }

            return false;
        }

        private static int ParseMonthWordToNum(string w)
        {
            if (string.IsNullOrEmpty(w)) return 0;
            if (w.StartsWith("jan") || w.StartsWith("ene")) return 1;
            if (w.StartsWith("feb") || w.StartsWith("peb")) return 2;
            if (w.StartsWith("mar")) return 3;
            if (w.StartsWith("apr") || w.StartsWith("abr")) return 4;
            if (w.StartsWith("may")) return 5;
            if (w.StartsWith("jun") || w.StartsWith("hun") || w.StartsWith("jum") || w.StartsWith("jue")) return 6;
            if (w.StartsWith("jul") || w.StartsWith("hul")) return 7;
            if (w.StartsWith("aug") || w.StartsWith("ago")) return 8;
            if (w.StartsWith("sep") || w.StartsWith("set")) return 9;
            if (w.StartsWith("oct") || w.StartsWith("okt")) return 10;
            if (w.StartsWith("nov") || w.StartsWith("nob")) return 11;
            if (w.StartsWith("dec") || w.StartsWith("dek") || w.StartsWith("dis")) return 12;
            return 0;
        }

        public static string CleanPersonName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string t = raw.Trim();
            // Strip any residual printed label prefixes like 'Middle ame', 'Middle name', 'First ame', 'Last ame', etc.
            t = Regex.Replace(t, @"^\s*\(?\s*(m[i1lI]dd?[li1I]?e?\s+(a?m[eo]|name)|middle\b|last\s+(a?m[eo]|name)|last\b|first\s+(a?m[eo]|name)|first\b|gitnang\s+pangalan)\s*\)?\s*[:\-_.]*\s*", "", RegexOptions.IgnoreCase);
            // Convert common OCR digit confusions in names (4 -> A, 0 -> O)
            t = Regex.Replace(t, "4", "A");
            t = Regex.Replace(t, "0", "O");
            // Only allow letters, ñ, Ñ, spaces, hyphens, periods, and apostrophes
            t = Regex.Replace(t, @"[^a-zA-ZñÑ\s\.\-']", "");
            t = Regex.Replace(t, @"\s+", " ").Trim();
            return t;
        }

        public static string CleanAgeString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string t = raw.Trim();
            t = Regex.Replace(t, @"^(age|edad)\s*[:\-_.]*", "", RegexOptions.IgnoreCase).Trim();
            var pureMatch = Regex.Match(t, @"\b\d{1,3}\b");
            if (pureMatch.Success && int.TryParse(pureMatch.Value, out int pureAge) && pureAge >= 0 && pureAge <= 120)
            {
                return pureAge.ToString();
            }

            if (t.Length <= 3)
            {
                t = Regex.Replace(t, "[Gg]", "6");
                t = Regex.Replace(t, "[Oo]", "0");
                t = Regex.Replace(t, @"[Il|/]", "1");
                t = Regex.Replace(t, "[Ss]", "5");
                t = Regex.Replace(t, "[Bb]", "8");
                var match = Regex.Match(t, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int age) && age >= 0 && age <= 120)
                {
                    return age.ToString();
                }
            }
            return string.Empty;
        }

        public static string NormalizeDateString(string raw, out DateTime? parsedDate)
        {
            parsedDate = null;
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;

            string s = raw.Trim();
            s = Regex.Replace(s, @"^(date\s*of\s*birth|birthdate|dob|kaarawan|bday)\s*[:\-_.]*", "", RegexOptions.IgnoreCase).Trim();
            s = Regex.Replace(s, @"^\s*\(?\s*m+[/.]?d+[/.]?y+\s*\)?\s*", "", RegexOptions.IgnoreCase).Trim();

            // 1. Try generic TryParseOcrDate
            if (TryParseOcrDate(s, out DateTime dt))
            {
                parsedDate = dt;
                return dt.ToString("MM/dd/yyyy");
            }

            // 2. Explicit MM/DD/YY or MM/DD/YYYY as specified on the CSWDO form
            var m = Regex.Match(s, @"^(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})$");
            if (m.Success)
            {
                int p1 = int.Parse(m.Groups[1].Value);
                int p2 = int.Parse(m.Groups[2].Value);
                string yStr = m.Groups[3].Value;
                int yr = yStr.Length == 4 
                    ? int.Parse(yStr) 
                    : (int.Parse(yStr) <= DateTime.Today.Year % 100 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));

                int month = p1;
                int day = p2;
                if (month > 12 && day <= 12)
                {
                    int tmp = month; month = day; day = tmp;
                }

                try
                {
                    if (month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(yr, month))
                    {
                        var d = new DateTime(yr, month, day);
                        parsedDate = d;
                        return d.ToString("MM/dd/yyyy");
                    }
                }
                catch { }
            }

            return s;
        }

        public static int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;
            return age;
        }

        public static string FormatPhoneNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.StartsWith("63") && digits.Length == 12)
                digits = "0" + digits.Substring(2);
            else if (digits.StartsWith("9") && digits.Length == 10)
                digits = "0" + digits;

            if (digits.Length == 11)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}";
            if (digits.Length > 7)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7)}";
            if (digits.Length > 4)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4)}";
            return digits;
        }

        public static SoloParentRecord ConvertToSoloParentRecord(OcrFormResponse ocr)
        {
            var r = new SoloParentRecord
            {
                Surname               = GetFieldValue(ocr, "last_name"),
                FirstName             = GetFieldValue(ocr, "first_name"),
                MiddleName            = GetFieldValue(ocr, "middle_name"),
                ExtensionName         = GetFieldValue(ocr, "ext_name"),
                CivilStatus           = GetFieldValue(ocr, "civil_status"),
                Sex                   = GetFieldValue(ocr, "sex"),
                PlaceOfBirth          = GetFieldValue(ocr, "birthplace"),
                EducationalAttainment = GetFieldValue(ocr, "educational_attainment"),
                PhilSysNumber         = GetFieldValue(ocr, "philsys_number"),
                Religion              = GetFieldValue(ocr, "religion"),
                Occupation            = GetFieldValue(ocr, "occupation"),
                MonthlyIncome         = GetFieldValue(ocr, "monthly_income"),
                Address               = GetFieldValue(ocr, "address"),
                Barangay              = FuzzyMatchBarangay(GetFieldValue(ocr, "barangay")),
                ContactNumber         = FormatPhoneNumber(GetFieldValue(ocr, "contact_number")),
                EmergencyContactName  = GetFieldValue(ocr, "emergency_name"),
                EmergencyRelationship = GetFieldValue(ocr, "emergency_relationship"),
                EmergencyAddress      = GetFieldValue(ocr, "emergency_address"),
                EmergencyContactNumber= FormatPhoneNumber(GetFieldValue(ocr, "emergency_number")),
                Status                = "Valid",
                LastUpdated           = DateTime.Today
            };

            string dobStr = GetFieldValue(ocr, "birthdate");
            if (TryParseOcrDate(dobStr, out DateTime dob))
            {
                r.DateOfBirth = dob;
            }

            string appDateStr = GetFieldValue(ocr, "date_of_application");
            if (TryParseOcrDate(appDateStr, out DateTime appDate))
            {
                r.DateOfApplication = appDate;
            }
            else
            {
                r.DateOfApplication = DateTime.Today;
            }

            string emp = GetFieldValue(ocr, "employment_status").ToLower();
            r.IsEmployed     = emp.Contains("employed") && !emp.Contains("self") && !emp.Contains("not");
            r.IsSelfEmployed = emp.Contains("self");
            r.IsNotEmployed  = emp.Contains("not") || emp.Contains("unemployed");

            if (ocr.Circumstance != null && ocr.Circumstance.Detected)
            {
                string code = ocr.Circumstance.Code?.ToUpperInvariant() ?? "";
                r.CircumstanceA1 = (code == "A1" || code == "1");
                r.CircumstanceA2 = (code == "A2" || code == "2");
                r.CircumstanceA3 = (code == "A3" || code == "3");
                r.CircumstanceA4 = (code == "A4" || code == "4");
                r.CircumstanceA5 = (code == "A5" || code == "5");
                r.CircumstanceA6 = (code == "A6" || code == "6");
                r.CircumstanceA7 = (code == "A7" || code == "7");
                r.CircumstanceB  = (code == "B"  || (code == "8" && ocr.Circumstance.Label != null && ocr.Circumstance.Label.Contains("OFW")));
                r.CircumstanceC  = (code == "C"  || code == "8" || (code == "9" && ocr.Circumstance.Label != null && ocr.Circumstance.Label.Contains("Unmarried")));
                r.CircumstanceD  = (code == "D"  || code == "10");
                r.CircumstanceE  = (code == "E"  || code == "11" || code == "9");
                r.CircumstanceF  = (code == "F"  || code == "12");
            }

            if (ocr.FamilyMembers != null && ocr.FamilyMembers.Count > 0)
            {
                r.FamilyMembers = new List<FamilyMember>(ocr.FamilyMembers);
                r.Children = ocr.FamilyMembers.Count;
            }

            r.Name = r.FullName;
            return r;
        }

        private static string GetFieldValue(OcrFormResponse ocr, string key)
        {
            if (ocr.Fields != null && ocr.Fields.TryGetValue(key, out var res))
            {
                return res.Value ?? string.Empty;
            }
            return string.Empty;
        }

        #region JSON Parsing Helpers

        private static void ParseFormJson(string json, OcrFormResponse resp)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            resp.FormId = ExtractJsonString(json, "form_id");
            resp.OverallRating = ExtractJsonString(json, "overall_rating");
            resp.OverallConfidence = ExtractJsonDouble(json, "overall_confidence");
            resp.PreviewImageBase64 = ExtractJsonString(json, "preview_image");

            // Parse Section II Encircled Circumstance
            var circMatch = Regex.Match(json, "\"circumstance\"\\s*:\\s*\\{([^}]+)\\}", RegexOptions.Singleline);
            if (circMatch.Success)
            {
                string cBlock = circMatch.Groups[1].Value;
                resp.Circumstance = new OcrCircumstanceResult
                {
                    Detected = ExtractJsonBool(cBlock, "detected"),
                    Code = ExtractJsonString(cBlock, "code"),
                    Label = ExtractJsonString(cBlock, "label"),
                    Confidence = ExtractJsonDouble(cBlock, "confidence"),
                    SubfieldCause = ExtractJsonString(cBlock, "subfield_cause"),
                    SubfieldDate = ExtractJsonString(cBlock, "subfield_date"),
                    SubfieldDisability = ExtractJsonString(cBlock, "subfield_disability"),
                    SubfieldPeriod = ExtractJsonString(cBlock, "subfield_period"),
                    SubfieldStayAbroad = ExtractJsonString(cBlock, "subfield_stay_abroad"),
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
                            MemberName          = memberName,
                            Sex                 = ExtractJsonString(block, "sex"),
                            Age                 = ExtractJsonString(block, "age"),
                            Birthdate           = ExtractJsonString(block, "birthdate"),
                            CivilStatus         = ExtractJsonString(block, "civilStatus"),
                            Relationship        = ExtractJsonString(block, "relationship"),
                            EducationEmployment = ExtractJsonString(block, "educationEmployment"),
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

        private static double[] ExtractBbox(string block)
        {
            var match = Regex.Match(block, "\"bbox\"\\s*:\\s*\\[([0-9.,\\s]+)\\]");
            if (match.Success)
            {
                var parts = match.Groups[1].Value.Split(',');
                if (parts.Length == 4)
                {
                    return new[]
                    {
                        double.Parse(parts[0], CultureInfo.InvariantCulture),
                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture)
                    };
                }
            }
            return new double[4];
        }

        #endregion
    }
}
