﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace BetcityAnnunciator
{
    public class BetcityEvent
    {
        public string Championship { get; set; }
        public string Title { get; set; }
        public string RawScore { get; set; } //["69:72"," (20:17, 30:23, 19:23, 0:9)"]
        public string MainScore => RawScore.Split(',').FirstOrDefault()?.Trim('\"', '[') ?? string.Empty;
        public string AdditionalScore => RawScore.Replace(MainScore, string.Empty);

        //["1:1"," (6:4, 4:6, 0:1)"]
        public List<string> SetScores => AdditionalScore
            .Trim(' ', '(', ')', '\"', ']', ',', '[')
            .Split(',')
            .Select(i => i.Trim())
            .ToList();

        public string FirstSetScore => SetScores.ElementAtOrDefault(0) ?? string.Empty;
        public string SecondSetScore => SetScores.ElementAtOrDefault(1) ?? string.Empty;
        public string ThirdSetScore => SetScores.ElementAtOrDefault(2) ?? string.Empty;

        public string LastSetScore => SetScores.LastOrDefault() ?? string.Empty;

        public bool ContainsScore(List<string> scores, Color color)
        {
            var found = false;

            foreach (var score in scores)
            {
                if (ContainsScore(score))
                {
                    found = true;
                    Color = color;
                }
            }

            return found;
        }

        public bool ContainsScore(string score)
        {
            if (string.IsNullOrWhiteSpace(score))
            {
                return false;
            }

            var setString = score.Split(',').FirstOrDefault() ?? string.Empty;
            int.TryParse(setString, out var set);

            if (set > 0)
            {
                var setScore = SetScores.ElementAtOrDefault(set - 1) ?? string.Empty;

                return ContainsScore(setScore, score.Split(',').LastOrDefault());
            }

            return ContainsScore(LastSetScore, score);
        }

        public bool ContainsScore(string set, string score)
        {
            return string.Equals(set, score, StringComparison.OrdinalIgnoreCase);
        }

        public Color Color { get; set; } = SystemColors.ControlColor;
    }
}