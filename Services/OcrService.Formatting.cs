using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public static partial class OcrService
    {
        #region Official Biñan Barangays & Fuzzy Matching

        public static readonly string[] BinanBarangays = new[]
        {
            "Biñan (Poblacion)", "Bungahan", "Canlalay", "Casile", "De La Paz",
            "Ganado", "Langkiwa", "Loma", "Malaban", "Malamig", "Mambog",
            "Platero", "Poblacion", "San Antonio", "San Francisco (Halang)",
            "San Jose", "San Vicente", "Santo Domingo", "Santo Niño", "Santo Tomas (Calabuso)",
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
            { "sto domingo", "Santo Domingo" },
            { "sto. domingo", "Santo Domingo" },
            { "santo domingo", "Santo Domingo" },
            { "sto nino", "Santo Niño" },
            { "sto. nino", "Santo Niño" },
            { "sto niño", "Santo Niño" },
            { "sto. niño", "Santo Niño" },
            { "santo nino", "Santo Niño" },
            { "delapaz", "De La Paz" },
            { "dela paz", "De La Paz" },
            { "mambog", "Mambog" },
            { "mampalasan", "Mambog" },
            { "mamplasan", "Mambog" },
            { "sorosoro", "Soro-Soro" },
            { "soro soro", "Soro-Soro" },
            { "binan poblacion", "Biñan (Poblacion)" },
            { "binan (poblacion)", "Biñan (Poblacion)" },
            { "biñan (poblacion)", "Biñan (Poblacion)" },
            { "binan", "Biñan (Poblacion)" },
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
            norm = Regex.Replace(norm, @"^(sex|gender|kasarian)\s*[:\-_.]*", "");
            string clean = Regex.Replace(norm, @"[^a-z]", "");

            if (clean == "f" || clean == "female" || clean == "fem" || clean == "b" || clean == "babae" || clean == "w" || clean == "woman" || clean == "girl" || clean == "daughter" || clean == "sister")
                return "Female";
            if (clean == "m" || clean == "male" || clean == "mas" || clean == "masc" || clean == "l" || clean == "lalaki" || clean == "man" || clean == "boy" || clean == "son" || clean == "brother")
                return "Male";

            // Handwritten OCR single-character confusions in Sex column
            if (clean == "h" || clean == "n" || clean == "iv")
                return "Male";
            if (clean == "t" || clean == "p")
                return "Female";

            if (clean.StartsWith("fem") || norm.Contains("female") || norm.Contains("babae") || norm.Contains("daughter"))
                return "Female";
            if (clean.StartsWith("mas") || (norm.Contains("male") && !norm.Contains("female")) || norm.Contains("lalaki") || norm.Contains("son"))
                return "Male";

            if (clean.StartsWith("f"))
                return "Female";
            if (clean.StartsWith("m") && clean != "mother")
                return "Male";

            if (clean.Contains("other") || clean.Contains("lgbt") || clean == "x")
                return "Others";

            return string.Empty;
        }

        public static readonly string[] StandardReligions = new[]
        {
            "Catholic",
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

            // 1. Direct & keyword matching for Catholic
            string[] cathKeywords = new[] { "cath", "katol", "roman", "rc", "ctolc", "catol", "katlk", "cthlc", "cathl", "cato", "kato", "ctc" };
            if (cathKeywords.Any(k => clean.Contains(k)) || norm.Contains("catholic") || norm.Contains("katoliko") || norm.Contains("r.c."))
                return "Catholic";

            if (ComputeSimilarity(clean, "catholic") >= 0.45 || ComputeSimilarity(clean, "katoliko") >= 0.45)
                return "Catholic";

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
            norm = Regex.Replace(norm, @"^(civil\s*s?tatus|status|kasarian)\s*[:\-_.]*", "");
            string clean = Regex.Replace(norm, @"[^a-z0-9]", "");
            string alpha = Regex.Replace(norm, @"[^a-z]", "");

            // 1. Single letter or short abbreviation
            if (alpha == "s" || alpha == "sin" || alpha == "sngl" || alpha == "sing" || alpha == "simgle" || alpha == "simglt" || alpha == "singl" || clean == "5")
                return "Single";
            if (alpha == "m" || alpha == "mar" || alpha == "marr" || alpha == "mared" || alpha == "marrd" || alpha == "mrd" || clean == "rn")
                return "Married";
            if (alpha == "d" || alpha == "div" || alpha == "dv")
                return "Divorced";
            if (alpha == "a" || alpha == "ann" || alpha == "anl" || alpha == "anul" || alpha == "anulled" || alpha == "annul" || clean == "4")
                return "Annulled";
            if (alpha == "w" || alpha == "wid" || alpha == "wdw" || alpha == "balo" || alpha == "biyuda" || alpha == "biyudo")
                return "Widowed";
            if (alpha == "sep" || alpha == "sp" || norm.Contains("s/p") || alpha == "hiwalay" || alpha == "separated")
                return "Separated";
            if (alpha == "c" || alpha == "live" || alpha == "livein" || alpha == "kinakasama")
                return "Common-law / Live-in";

            // 2. Keyword containment
            if (alpha.Contains("single") || alpha.Contains("dalaga") || alpha.Contains("binata") || alpha.Contains("walang"))
                return "Single";
            if (alpha.Contains("married") || alpha.Contains("kasal"))
                return "Married";
            if (alpha.Contains("widow") || alpha.Contains("biyud") || alpha.Contains("balo"))
                return "Widowed";
            if (alpha.Contains("sep") || alpha.Contains("hiwalay") || alpha.Contains("defacto"))
                return "Separated";
            if (alpha.Contains("divorc") || alpha.Contains("diborsyo"))
                return "Divorced";
            if (alpha.Contains("annul") || alpha.Contains("pawalang"))
                return "Annulled";
            if (alpha.Contains("live") || alpha.Contains("common") || alpha.Contains("kinakasama"))
                return "Common-law / Live-in";

            foreach (var st in StandardCivilStatuses)
            {
                if (ComputeSimilarity(alpha, st.ToLowerInvariant()) >= 0.60)
                    return st;
            }

            return IsOcrNotApplicable(raw) ? string.Empty : clean_text_capitalized(raw);
        }

        public static readonly string[] StandardRelationships = new[]
        {
            "Child",
            "Spouse",
            "Parent",
            "Sibling",
            "Grandchild",
            "Grandparent",
            "Relative",
            "Ward",
            "Other"
        };

        public static string FuzzyMatchRelationship(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || IsOcrNotApplicable(raw)) return string.Empty;
            string norm = raw.Trim().ToLowerInvariant();
            string clean = Regex.Replace(norm, @"[^a-z]", "");

            if (clean.Contains("child") || clean.Contains("anak") || clean.Contains("son") || clean.Contains("daughter") || clean.Contains("dtr") || clean.Contains("bata")) return "Child";
            if (clean.Contains("spouse") || clean.Contains("asawa") || clean.Contains("husband") || clean.Contains("wife") || clean.Contains("partner")) return "Spouse";
            if (clean.Contains("mother") || clean.Contains("father") || clean.Contains("nanay") || clean.Contains("tatay") || clean.Contains("ina") || clean.Contains("ama") || clean.Contains("mom") || clean.Contains("dad") || clean.Contains("parent") || clean.Contains("mama") || clean.Contains("papa")) return "Parent";
            if (clean.Contains("sister") || clean.Contains("brother") || clean.Contains("ate") || clean.Contains("kuya") || clean.Contains("kapatid") || clean.Contains("sibling") || clean.Contains("bro") || clean.Contains("sis")) return "Sibling";
            if (clean.Contains("grandchild") || clean.Contains("apo") || clean.Contains("grandson") || clean.Contains("granddaughter")) return "Grandchild";
            if (clean.Contains("grandmother") || clean.Contains("grandfather") || clean.Contains("lolo") || clean.Contains("lola") || clean.Contains("grandparent")) return "Grandparent";
            if (clean.Contains("aunt") || clean.Contains("uncle") || clean.Contains("tita") || clean.Contains("tito") || clean.Contains("cousin") || clean.Contains("pinsan") || clean.Contains("inlaw") || clean.Contains("hipag") || clean.Contains("bayaw") || clean.Contains("relative") || clean.Contains("kamag")) return "Relative";
            if (clean.Contains("ward") || clean.Contains("alaga") || clean.Contains("guardian") || clean.Contains("tagapangalaga")) return "Ward";
            if (clean.Contains("friend") || clean.Contains("kaibigan") || clean.Contains("neighbor") || clean.Contains("kapitbahay") || clean.Contains("other")) return "Other";

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

            // College / Bachelor Degrees
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

            // Early Childhood / Kindergarten / Preschool
            if (clean.Contains("kinder") || clean.Contains("nursery") || clean.Contains("daycare") || clean.Contains("prep") || clean.Contains("preschool"))
            {
                return "Elementary";
            }

            // Grade levels (G1-G6 -> Elementary, G7-G12 -> High School)
            if (Regex.IsMatch(norm, @"(g|gr|grade)[\.\s\-]*(1[0-2]|[7-9])") || clean.Contains("jhs") || clean.Contains("shs"))
            {
                return "High School";
            }
            if (Regex.IsMatch(norm, @"(g|gr|grade)[\.\s\-]*[1-6]"))
            {
                return "Elementary";
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

            if (clean == "student" || clean == "estudyante" || clean == "pupil")
                return "Student";

            if (clean.Contains("none") || clean.Contains("wala") || clean == "na" || clean == "notattending")
                return "None";

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
            return raw.Trim().ToUpperInvariant();
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

        private static int NormalizeHandwrittenYear(int yr)
        {
            if (yr >= 1920 && yr <= 2030) return yr;
            int last2 = Math.Abs(yr) % 100;
            return last2 <= 24 ? 2000 + last2 : 1900 + last2;
        }

        public static bool TryParseOcrDate(string raw, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string s = raw.Trim();
            s = Regex.Replace(s, @"^(.*?)(date\s*of\s*(this\s*)?application|application\s*date|petsa(\s*ng\s*aplikasyon)?|date\s*of\s*birth|birthdate|dob|kaarawan|bday|date)\s*[:\-_.]*", "", RegexOptions.IgnoreCase).Trim();
            s = Regex.Replace(s, @"^\s*\(?\s*m+[/.]?d+[/.]?y+\s*\)?\s*", "", RegexOptions.IgnoreCase).Trim();

            // 1. Check written month names if text contains letters
            if (Regex.IsMatch(s, @"[a-zA-Z]{3,}"))
            {
                s = Regex.Replace(s, @"\b([a-zA-Z]{3,})[\s\-_,]+([QZOlISB])[\s\-_,]*(\d{4})\b", m => {
                    char c = m.Groups[2].Value[0];
                    char repl;
                    switch (c)
                    {
                        case 'Q': case 'q': case 'Z': case 'z':
                            repl = '2'; break;
                        case 'O': case 'o':
                            repl = '0'; break;
                        case 'l': case 'I': case 'i':
                            repl = '1'; break;
                        case 'S': case 's':
                            repl = '5'; break;
                        case 'B':
                            repl = '8'; break;
                        default:
                            repl = c; break;
                    }
                    return $"{m.Groups[1].Value} {repl}, {m.Groups[3].Value}";
                }, RegexOptions.IgnoreCase);

                // Day first: e.g. 19 June 2004, 2 May 2002, 2-May-2002, 19th June 2004
                var mDayFirst = Regex.Match(s, @"\b(\d{1,2})(?:st|nd|rd|th)?[\s\-_,]+([a-zA-Z]{3,})[\s\-_,]+(\d{4}|\d{2})\b");
                if (mDayFirst.Success)
                {
                    int day = int.Parse(mDayFirst.Groups[1].Value);
                    string mStr = mDayFirst.Groups[2].Value.ToLowerInvariant();
                    string yStr = mDayFirst.Groups[3].Value;
                    int yr = yStr.Length == 4 ? int.Parse(yStr) : (int.Parse(yStr) < 40 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));
                    yr = NormalizeHandwrittenYear(yr);
                    int mNum = ParseMonthWordToNum(mStr);
                    if (mNum >= 1 && mNum <= 12 && day >= 1 && day <= DateTime.DaysInMonth(yr, mNum))
                    {
                        try { result = new DateTime(yr, mNum, day); return true; } catch { }
                    }
                }

                // Month first: e.g. June 19, 2004, May 2, 2002, MAY 21973
                var mMonthFirst = Regex.Match(s, @"\b([a-zA-Z]{3,})[\s\-_,]*(\d{1,2})(?:st|nd|rd|th)?[\s\-_,]*(\d{4}|\d{2})\b");
                if (mMonthFirst.Success)
                {
                    string mStr = mMonthFirst.Groups[1].Value.ToLowerInvariant();
                    int day = int.Parse(mMonthFirst.Groups[2].Value);
                    string yStr = mMonthFirst.Groups[3].Value;
                    int yr = yStr.Length == 4 ? int.Parse(yStr) : (int.Parse(yStr) < 40 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));
                    yr = NormalizeHandwrittenYear(yr);
                    int mNum = ParseMonthWordToNum(mStr);
                    if (mNum >= 1 && mNum <= 12 && day >= 1 && day <= DateTime.DaysInMonth(yr, mNum))
                    {
                        try { result = new DateTime(yr, mNum, day); return true; } catch { }
                    }
                }

                // Concat: e.g. Jum192004, June192004
                var mConcat = Regex.Match(s, @"\b([a-zA-Z]{3,})(\d{1,2})(\d{4})\b");
                if (mConcat.Success)
                {
                    string mStr = mConcat.Groups[1].Value.ToLowerInvariant();
                    int day = int.Parse(mConcat.Groups[2].Value);
                    int yr = NormalizeHandwrittenYear(int.Parse(mConcat.Groups[3].Value));
                    int mNum = ParseMonthWordToNum(mStr);
                    if (mNum >= 1 && mNum <= 12 && day >= 1 && day <= DateTime.DaysInMonth(yr, mNum))
                    {
                        try { result = new DateTime(yr, mNum, day); return true; } catch { }
                    }
                }

                // Fallback standard DateTime.TryParse with invariant and US cultures
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    return true;
                if (DateTime.TryParse(s, new CultureInfo("en-US"), DateTimeStyles.None, out result))
                    return true;
            }

            // 2. Check ISO YYYY-MM-DD or YYYY/MM/DD
            var matchIso = Regex.Match(s, @"^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})");
            if (matchIso.Success)
            {
                int y = NormalizeHandwrittenYear(int.Parse(matchIso.Groups[1].Value));
                int m = int.Parse(matchIso.Groups[2].Value);
                int d = int.Parse(matchIso.Groups[3].Value);
                try
                {
                    if (y >= 1900 && y <= 2099 && m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(y, m))
                    {
                        result = new DateTime(y, m, d);
                        return true;
                    }
                }
                catch { }
            }

            // 3. Numeric with separators: DD/MM/YYYY or MM/DD/YYYY
            var mSlash = Regex.Match(s, @"^(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})$");
            if (mSlash.Success)
            {
                int p1 = int.Parse(mSlash.Groups[1].Value);
                int p2 = int.Parse(mSlash.Groups[2].Value);
                string yStr = mSlash.Groups[3].Value;
                int yr = yStr.Length == 4 ? int.Parse(yStr) : (int.Parse(yStr) < 40 ? 2000 + int.Parse(yStr) : 1900 + int.Parse(yStr));
                yr = NormalizeHandwrittenYear(yr);

                // If p1 > 12: p1 is definitely Day, p2 is Month (DD/MM/YYYY)
                if (p1 > 12 && p2 <= 12 && p1 <= DateTime.DaysInMonth(yr, p2))
                {
                    result = new DateTime(yr, p2, p1);
                    return true;
                }
                // If p2 > 12: p1 is definitely Month, p2 is Day (MM/DD/YYYY)
                if (p2 > 12 && p1 <= 12 && p2 <= DateTime.DaysInMonth(yr, p1))
                {
                    result = new DateTime(yr, p1, p2);
                    return true;
                }
                // Both <= 12: In Philippine context, default to DD/MM/YYYY
                if (p2 <= 12 && p1 <= DateTime.DaysInMonth(yr, p2))
                {
                    result = new DateTime(yr, p2, p1);
                    return true;
                }
                if (p1 <= 12 && p2 <= DateTime.DaysInMonth(yr, p1))
                {
                    result = new DateTime(yr, p1, p2);
                    return true;
                }
            }

            // 4. Missing one separator: e.g. 1/252020 or 01/252020
            var mOneSlash1 = Regex.Match(s, @"^(\d{1,2})[/\-.](\d{1,2})(\d{4})$");
            if (mOneSlash1.Success)
            {
                int p1 = int.Parse(mOneSlash1.Groups[1].Value);
                int p2 = int.Parse(mOneSlash1.Groups[2].Value);
                int yr = NormalizeHandwrittenYear(int.Parse(mOneSlash1.Groups[3].Value));
                int m = p1, d = p2;
                if (m > 12 && d <= 12) { int tmp = m; m = d; d = tmp; }
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    result = new DateTime(yr, m, d);
                    return true;
                }
            }

            // Missing first separator: e.g. 0125/2020
            var mOneSlash2 = Regex.Match(s, @"^(\d{1,2})(\d{2})[/\-.](\d{4})$");
            if (mOneSlash2.Success)
            {
                int p1 = int.Parse(mOneSlash2.Groups[1].Value);
                int p2 = int.Parse(mOneSlash2.Groups[2].Value);
                int yr = NormalizeHandwrittenYear(int.Parse(mOneSlash2.Groups[3].Value));
                int m = p1, d = p2;
                if (m > 12 && d <= 12) { int tmp = m; m = d; d = tmp; }
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    result = new DateTime(yr, m, d);
                    return true;
                }
            }

            // 5. Check pure 8 digits: 06192004 or 20040619
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

                // Try DDMMYYYY
                int d2 = int.Parse(digitsOnly.Substring(0, 2));
                int m2 = int.Parse(digitsOnly.Substring(2, 2));
                int y2 = int.Parse(digitsOnly.Substring(4, 4));
                if (y2 >= 1920 && y2 <= 2030 && m2 >= 1 && m2 <= 12 && d2 >= 1 && d2 <= DateTime.DaysInMonth(y2, m2))
                {
                    try { result = new DateTime(y2, m2, d2); return true; } catch { }
                }
            }

            if (digitsOnly.Length == 7)
            {
                int d = int.Parse(digitsOnly.Substring(0, 1));
                int m = int.Parse(digitsOnly.Substring(1, 2));
                int yr = int.Parse(digitsOnly.Substring(3, 4));
                if (yr >= 1920 && yr <= 2030 && m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    try { result = new DateTime(yr, m, d); return true; } catch { }
                }
            }

            if (digitsOnly.Length == 6)
            {
                int d = int.Parse(digitsOnly.Substring(0, 2));
                int m = int.Parse(digitsOnly.Substring(2, 2));
                int y2 = int.Parse(digitsOnly.Substring(4, 2));
                int yr = y2 < 40 ? 2000 + y2 : 1900 + y2;
                if (m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(yr, m))
                {
                    try { result = new DateTime(yr, m, d); return true; } catch { }
                }
            }

            if (DateTime.TryParse(s, out result))
                return true;

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
            return t.ToUpperInvariant();
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
            s = Regex.Replace(s, @"^(date\s*of\s*(this\s*)?application|application\s*date|petsa(\s*ng\s*aplikasyon)?|date\s*of\s*birth|birthdate|dob|kaarawan|bday|date)\s*[:\-_.]*", "", RegexOptions.IgnoreCase).Trim();
            s = Regex.Replace(s, @"^\s*\(?\s*m+[/.]?d+[/.]?y+\s*\)?\s*", "", RegexOptions.IgnoreCase).Trim();

            if (TryParseOcrDate(s, out DateTime dt))
            {
                parsedDate = dt;
                return dt.ToString("dd/MM/yyyy");
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
                Surname               = GetFieldValue(ocr, "last_name")?.ToUpperInvariant(),
                FirstName             = GetFieldValue(ocr, "first_name")?.ToUpperInvariant(),
                MiddleName            = GetFieldValue(ocr, "middle_name")?.ToUpperInvariant(),
                ExtensionName         = GetFieldValue(ocr, "ext_name")?.ToUpperInvariant(),
                CivilStatus           = GetFieldValue(ocr, "civil_status"),
                Sex                   = GetFieldValue(ocr, "sex"),
                PlaceOfBirth          = GetFieldValue(ocr, "birthplace")?.ToUpperInvariant(),
                EducationalAttainment = GetFieldValue(ocr, "educational_attainment"),
                PhilSysNumber         = GetFieldValue(ocr, "philsys_number"),
                Religion              = GetFieldValue(ocr, "religion")?.ToUpperInvariant(),
                Occupation            = GetFieldValue(ocr, "occupation")?.ToUpperInvariant(),
                MonthlyIncome         = GetFieldValue(ocr, "monthly_income"),
                Address               = GetFieldValue(ocr, "address")?.ToUpperInvariant(),
                Barangay              = FuzzyMatchBarangay(GetFieldValue(ocr, "barangay")),
                ContactNumber         = FormatPhoneNumber(GetFieldValue(ocr, "contact_number")),
                EmergencyContactName  = GetFieldValue(ocr, "emergency_name")?.ToUpperInvariant(),
                EmergencyRelationship = GetFieldValue(ocr, "emergency_relationship")?.ToUpperInvariant(),
                EmergencyAddress      = GetFieldValue(ocr, "emergency_address")?.ToUpperInvariant(),
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
    }
}
