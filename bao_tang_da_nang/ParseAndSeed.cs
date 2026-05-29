using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Bảo_Tàng_Đà_Nẵng.Models;
using Bảo_Tàng_Đà_Nẵng.Data;

namespace Bảo_Tàng_Đà_Nẵng
{
    public class ParseAndSeed
    {
        public static void Run()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));

            using var db = new AppDbContext(optionsBuilder.Options);

            // Đảm bảo database và bảng được tạo trên Postgres
            db.Database.EnsureCreated();

            // Always force seed when this is called to ensure English data is loaded
            var existingCount = db.Questions.Count();

            Console.WriteLine($"[SEED] Đang xóa {existingCount} câu cũ và seed lại toàn bộ...");
            // Xóa câu hỏi cũ và các record liên quan
            db.Database.ExecuteSqlRaw("DELETE FROM \"SessionDetails\"");
            db.Database.ExecuteSqlRaw("DELETE FROM \"QuizSessions\"");
            db.Database.ExecuteSqlRaw("DELETE FROM \"Questions\"");
            db.SaveChanges();

            var lines = File.ReadAllLines("questions_clean.txt");
            var text = string.Join("\n", lines);

            // Bảng đáp án
            var answerIndex = text.IndexOf("BẢNG ĐÁP ÁN TRA CỨU");
            var questionsText = answerIndex > 0 ? text.Substring(0, answerIndex) : text;
            var answerText = answerIndex > 0 ? text.Substring(answerIndex) : "";

            var answersDict = new Dictionary<string, Dictionary<string, string>>();
            string currentAnsTopic = "";

            foreach (var line in answerText.Split('\n'))
            {
                var topicMatch = Regex.Match(line, @"►\s*(Đề tài\s*\d+)");
                if (topicMatch.Success)
                {
                    currentAnsTopic = topicMatch.Groups[1].Value.ToUpper();
                    answersDict[currentAnsTopic] = new Dictionary<string, string>();
                }
                else if (!string.IsNullOrEmpty(currentAnsTopic))
                {
                    var ansMatches = Regex.Matches(line, @"(\d+)\s*-\s*([A-D])");
                    foreach (Match m in ansMatches)
                    {
                        answersDict[currentAnsTopic][m.Groups[1].Value] = m.Groups[2].Value;
                    }
                }
            }

            var linesEn = File.ReadAllLines("questions_clean_en.txt");
            
            string currentTopicTitle = "";
            string currentTopicKey = "";
            Question currentQ = null;
            int totalQuestions = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var lineEn = i < linesEn.Length ? linesEn[i].Trim() : line;
                
                if (line == "BẢNG ĐÁP ÁN TRA CỨU") break; // Stop reading questions

                if (line.StartsWith("ĐỀ TÀI"))
                {
                    currentTopicTitle = line;
                    var matchKey = Regex.Match(line, @"ĐỀ TÀI\s*\d+");
                    currentTopicKey = matchKey.Success ? matchKey.Value : "";
                }
                else if (line.StartsWith("Câu ") && line.Contains(":"))
                {
                    if (currentQ != null && !string.IsNullOrEmpty(currentQ.OptionA))
                    {
                        db.Questions.Add(currentQ);
                    }
                    
                    var qNumMatch = Regex.Match(line, @"Câu\s*(\d+):");
                    var qNum = qNumMatch.Success ? qNumMatch.Groups[1].Value : "";
                    
                    var correctAnsLetter = "A";
                    if (answersDict.ContainsKey(currentTopicKey) && answersDict[currentTopicKey].ContainsKey(qNum))
                    {
                        correctAnsLetter = answersDict[currentTopicKey][qNum];
                    }

                    var qContent = line.Substring(line.IndexOf(":") + 1).Trim();
                    var qContentEn = lineEn.Contains(":") ? lineEn.Substring(lineEn.IndexOf(":") + 1).Trim() : lineEn;
                    // Fix Google Translate sometimes removing or keeping the number
                    if (qContentEn.StartsWith("Question ", StringComparison.OrdinalIgnoreCase)) 
                    {
                        int colonIdx = qContentEn.IndexOf(":");
                        if (colonIdx > 0) qContentEn = qContentEn.Substring(colonIdx + 1).Trim();
                    }

                    totalQuestions++;
                    currentQ = new Question
                    {
                        LocationName = currentTopicTitle,
                        Content = qContent,
                        ContentEn = qContentEn,
                        Points = 10,
                        IsActive = true,
                        CorrectOption = correctAnsLetter
                    };
                }
                else if (currentQ != null)
                {
                    if (line.StartsWith("A. ")) { currentQ.OptionA = line.Substring(3).Trim(); currentQ.OptionAEn = lineEn.Length >= 3 ? lineEn.Substring(3).Trim() : lineEn; }
                    else if (line.StartsWith("B. ")) { currentQ.OptionB = line.Substring(3).Trim(); currentQ.OptionBEn = lineEn.Length >= 3 ? lineEn.Substring(3).Trim() : lineEn; }
                    else if (line.StartsWith("C. ")) { currentQ.OptionC = line.Substring(3).Trim(); currentQ.OptionCEn = lineEn.Length >= 3 ? lineEn.Substring(3).Trim() : lineEn; }
                    else if (line.StartsWith("D. ")) { currentQ.OptionD = line.Substring(3).Trim(); currentQ.OptionDEn = lineEn.Length >= 3 ? lineEn.Substring(3).Trim() : lineEn; }
                }
            }
            if (currentQ != null && !string.IsNullOrEmpty(currentQ.OptionA))
            {
                db.Questions.Add(currentQ);
            }

            db.SaveChanges();
            Console.WriteLine("Imported all questions successfully!");
        }
    }
}
