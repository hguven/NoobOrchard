﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Orchard.Logging;

namespace Orchard.Localization.Services {
    
    public class LocalizationStreamParser : ILocalizationStreamParser {

        private static readonly Dictionary<char, char> _escapeTranslations = new Dictionary<char, char> {
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' }
        };

        public LocalizationStreamParser() {
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; private set; }

        public void ParseLocalizationStream(string text, IDictionary<string, string> translations, bool merge) {
            var reader = new StringReader(text);
            string poLine;
            var scopes = new List<string>();
            var id = string.Empty;

            while ((poLine = reader.ReadLine()) != null) {
                if (poLine.StartsWith("#:")) {
                    scopes.Add(ParseScope(poLine));
                    continue;
                }

                if (poLine.StartsWith("msgctxt")) {
                    scopes.Add(ParseContext(poLine));
                    continue;
                }

                if (poLine.StartsWith("msgid")) {
                    id = ParseId(poLine);
                    continue;
                }

                if (poLine.StartsWith("msgstr")) {
                    var translation = ParseTranslation(poLine);
                    // ignore incomplete localizations (empty msgid or msgstr)
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(translation)) {
                        if(scopes.Count == 0) {
                            scopes.Add(string.Empty);
                        }
                        foreach (var scope in scopes) {
                            var scopedKey = (scope + "|" + id).ToLowerInvariant();
                            if (!translations.ContainsKey(scopedKey)) {
                                translations.Add(scopedKey, translation);
                            }
                            else {
                                if (merge) {
                                    translations[scopedKey] = translation;
                                }
                            }
                        }
                    }

                    id = string.Empty;
                    scopes = new List<string>();
                }

            }
        }

        private static string Unescape(string str) {
            StringBuilder sb = null;
            bool escaped = false;
            for (var i = 0; i < str.Length; i++) {
                var c = str[i];
                if (escaped) {
                    if (sb == null) {
                        sb = new StringBuilder(str.Length);
                        if (i > 1) {
                            sb.Append(str.Substring(0, i - 1));
                        }
                    }
                    char unescaped;
                    if (_escapeTranslations.TryGetValue(c, out unescaped)) {
                        sb.Append(unescaped);
                    }
                    else {
                        // General rule: \x ==> x
                        sb.Append(c);
                    }
                    escaped = false;
                }
                else {
                    if (c == '\\') {
                        escaped = true;
                    }
                    else if (sb != null) {
                        sb.Append(c);
                    }
                }
            }
            return sb == null ? str : sb.ToString();
        }

        private string TrimQuote(string str) {
            if (str.StartsWith("\"") && str.EndsWith("\"")) {
                if (str.Length == 1) {
                    // Handle corner case - string containing single quote
                    Logger.Warn("Invalid localization string detected: " + str);
                    return "";
                }

                return str.Substring(1, str.Length - 2);
            }

            return str;
        }

        private string ParseTranslation(string poLine) {
            return Unescape(TrimQuote(poLine.Substring(6).Trim()));
        }

        private string ParseId(string poLine) {
            return Unescape(TrimQuote(poLine.Substring(5).Trim()));
        }

        private string ParseScope(string poLine) {
            return Unescape(TrimQuote(poLine.Substring(2).Trim()));
        }

        private string ParseContext(string poLine) {
            return Unescape(TrimQuote(poLine.Substring(7).Trim()));
        }
    }
}