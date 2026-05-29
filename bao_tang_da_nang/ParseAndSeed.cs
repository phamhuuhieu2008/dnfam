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

            if (db.Questions.Any())
            {
                Console.WriteLine("Dữ liệu đã có sẵn. Bỏ qua bước nạp tự động...");
                return;
            }

            // Xóa câu hỏi cũ và các record liên quan nếu cần
            db.Database.ExecuteSqlRaw("DELETE FROM \"SessionDetails\"");
            db.Database.ExecuteSqlRaw("DELETE FROM \"QuizSessions\"");
            db.Questions.RemoveRange(db.Questions);
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

            var topicBlocks = Regex.Split(questionsText, @"(ĐỀ TÀI\s*\d+:.*)");
            int totalQuestions = 0;
            
            for (int i = 1; i < topicBlocks.Length; i += 2)
            {
                var topicTitle = topicBlocks[i].Trim();
                var topicContent = topicBlocks[i + 1];

                var topicKeyMatch = Regex.Match(topicTitle, @"ĐỀ TÀI\s*\d+");
                var topicKey = topicKeyMatch.Success ? topicKeyMatch.Value : "";
                var answers = answersDict.ContainsKey(topicKey) ? answersDict[topicKey] : new Dictionary<string, string>();

                var qBlocks = Regex.Split(topicContent, @"(Câu\s*\d+:.*)");
                for (int j = 1; j < qBlocks.Length; j += 2)
                {
                    var qHeader = qBlocks[j].Trim();
                    var qNumMatch = Regex.Match(qHeader, @"Câu\s*(\d+):");
                    if (!qNumMatch.Success) continue;
                    
                    var qNum = qNumMatch.Groups[1].Value;
                    var qContent = qHeader.Substring(qHeader.IndexOf(":") + 1).Trim();
                    var optsText = qBlocks[j + 1];

                    var correctAnsLetter = answers.ContainsKey(qNum) ? answers[qNum] : "A";

                    totalQuestions++;
                    string mappedTopicName = "";
                    if (totalQuestions <= 75) mappedTopicName = "Chủ đề 1: Địa lý & Lịch sử Bảo tàng";
                    else if (totalQuestions <= 150) mappedTopicName = "Chủ đề 2: Thiên nhiên & Con người";
                    else if (totalQuestions <= 225) mappedTopicName = "Chủ đề 3: Địa chất & Hệ sinh thái biển";
                    else mappedTopicName = "Chủ đề 4: Tiền - Sơ sử & Sa Huỳnh";

                    var question = new Question
                    {
                        LocationName = mappedTopicName,
                        Content = qContent,
                        Points = 10,
                        IsActive = true
                    };

                    var optMatches = Regex.Matches(optsText, @"([A-D])\.\s*(.*?)(?=(?:[A-D]\.|$))", RegexOptions.Singleline);
                    foreach (Match m in optMatches)
                    {
                        var letter = m.Groups[1].Value;
                        var content = m.Groups[2].Value.Trim();

                        if (letter == "A") question.OptionA = content;
                        else if (letter == "B") question.OptionB = content;
                        else if (letter == "C") question.OptionC = content;
                        else if (letter == "D") question.OptionD = content;

                        if (letter == correctAnsLetter)
                        {
                            question.CorrectOption = letter;
                        }
                    }

                    if (!string.IsNullOrEmpty(question.OptionA) && !string.IsNullOrEmpty(question.CorrectOption))
                    {
                        db.Questions.Add(question);
                    }
                }
            }

            db.SaveChanges();
            Console.WriteLine("Imported all questions successfully!");
        }
    }
}
