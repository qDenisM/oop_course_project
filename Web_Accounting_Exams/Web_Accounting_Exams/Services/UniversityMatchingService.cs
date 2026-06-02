using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Services
{
    public class MatchedSpeciality
    {
        public Speciality Speciality { get; set; } = null!;
        public University University { get; set; } = null!;
        public int TotalScore { get; set; }
        public int MatchedSubjectsCount { get; set; }
        public int RequiredSubjectsCount { get; set; }
        public List<string> MatchedSubjects { get; set; } = new();
        public List<string> MissingSubjects { get; set; } = new();
        public bool AllRequirementsMet { get; set; }
        public bool HasAnyMatch { get; set; }
    }

    public class UniversityMatchingService
    {
        private readonly ApplicationDbContext _context;

        public UniversityMatchingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MatchedSpeciality>> FindMatchingUniversitiesAsync(int userId, int? minTotalScore = null, bool showOnlyWithMatches = true)
        {
            var userCertificates = await _context.Certificates
                .Where(c => c.UserId == userId && c.Status == CertificateStatus.Approved)
                .ToListAsync();

            if (!userCertificates.Any())
            {
                return new List<MatchedSpeciality>();
            }

            var totalScore = userCertificates.Sum(c => c.Score);

            if (minTotalScore.HasValue && totalScore < minTotalScore.Value)
            {
                return new List<MatchedSpeciality>();
            }

            var userSubjects = userCertificates
                .Where(c => !string.IsNullOrEmpty(c.Subject))
                .ToDictionary(
                    c => NormalizeSubjectName(c.Subject),
                    c => c.Score,
                    StringComparer.OrdinalIgnoreCase
                );

            var specialities = await _context.Specialities
                .Include(s => s.University)
                .Include(s => s.Subjects)
                .Where(s => s.IsActive && s.University.IsActive)
                .ToListAsync();

            var matchedSpecialities = new List<MatchedSpeciality>();

            foreach (var speciality in specialities)
            {
                var requiredSubjects = speciality.Subjects
                    .Where(s => s.IsRequired)
                    .ToList();

                if (!requiredSubjects.Any())
                {
                    matchedSpecialities.Add(new MatchedSpeciality
                    {
                        Speciality = speciality,
                        University = speciality.University,
                        TotalScore = totalScore,
                        MatchedSubjectsCount = 0,
                        RequiredSubjectsCount = 0,
                        AllRequirementsMet = true
                    });
                    continue;
                }

                var matchedSubjects = new List<string>();
                var missingSubjects = new List<string>();
                int matchedCount = 0;

                foreach (var requiredSubject in requiredSubjects)
                {
                    var normalizedSubjectName = NormalizeSubjectName(requiredSubject.SubjectName);
                    
                    var userSubject = userSubjects.Keys.FirstOrDefault(
                        us => AreSubjectsMatching(us, normalizedSubjectName)
                    );

                    if (userSubject != null)
                    {
                        var userScore = userSubjects[userSubject];
                        
                        if (requiredSubject.MinScore.HasValue)
                        {
                            if (userScore >= requiredSubject.MinScore.Value)
                            {
                                matchedSubjects.Add($"{requiredSubject.SubjectName} ({userScore} баллов)");
                                matchedCount++;
                            }
                            else
                            {
                                missingSubjects.Add($"{requiredSubject.SubjectName} (требуется минимум {requiredSubject.MinScore}, у вас {userScore})");
                            }
                        }
                        else
                        {
                            matchedSubjects.Add($"{requiredSubject.SubjectName} ({userScore} баллов)");
                            matchedCount++;
                        }
                    }
                    else
                    {
                        missingSubjects.Add(requiredSubject.SubjectName);
                    }
                }

                if (matchedCount == requiredSubjects.Count)
                {
                    matchedSpecialities.Add(new MatchedSpeciality
                    {
                        Speciality = speciality,
                        University = speciality.University,
                        TotalScore = totalScore,
                        MatchedSubjectsCount = matchedCount,
                        RequiredSubjectsCount = requiredSubjects.Count,
                        MatchedSubjects = matchedSubjects,
                        MissingSubjects = missingSubjects,
                        AllRequirementsMet = true,
                        HasAnyMatch = true
                    });
                }
                else if (matchedCount > 0)
                {
                    matchedSpecialities.Add(new MatchedSpeciality
                    {
                        Speciality = speciality,
                        University = speciality.University,
                        TotalScore = totalScore,
                        MatchedSubjectsCount = matchedCount,
                        RequiredSubjectsCount = requiredSubjects.Count,
                        MatchedSubjects = matchedSubjects,
                        MissingSubjects = missingSubjects,
                        AllRequirementsMet = false,
                        HasAnyMatch = true
                    });
                }
            }

            return matchedSpecialities
                .OrderByDescending(m => m.AllRequirementsMet)
                .ThenByDescending(m => m.MatchedSubjectsCount)
                .ThenByDescending(m => m.TotalScore)
                .ToList();
        }

        private string NormalizeSubjectName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
                return string.Empty;

            return subjectName.Trim().ToLower();
        }

        private bool AreSubjectsMatching(string userSubject, string requiredSubject)
        {
            var userNormalized = NormalizeSubjectName(userSubject);
            var requiredNormalized = NormalizeSubjectName(requiredSubject);

            if (userNormalized == requiredNormalized)
                return true;

            if (userNormalized.Contains(requiredNormalized) || requiredNormalized.Contains(userNormalized))
            {
                var commonWords = new[] { "язык", "математик", "физик", "хими", "биолог", "истори", "географ" };
                return commonWords.Any(word => 
                    userNormalized.Contains(word) && requiredNormalized.Contains(word));
            }
            var synonyms = new Dictionary<string, string[]>
            {
                { "русский язык", new[] { "русск", "русскому языку", "русского языка" } },
                { "белорусский язык", new[] { "белорусск", "белорусскому языку", "белорусского языка" } },
                { "английский язык", new[] { "английск", "английскому языку", "английского языка" } },
                { "математика", new[] { "математик", "математике", "математику" } },
                { "физика", new[] { "физик", "физике", "физику" } },
                { "химия", new[] { "хими", "химии", "химию" } },
                { "биология", new[] { "биолог", "биологии", "биологию" } },
                { "история беларуси", new[] { "истори", "беларус" } },
                { "география", new[] { "географ", "географии", "географию" } }
            };

            foreach (var synonym in synonyms)
            {
                if ((userNormalized.Contains(synonym.Key) || synonym.Value.Any(v => userNormalized.Contains(v))) &&
                    (requiredNormalized.Contains(synonym.Key) || synonym.Value.Any(v => requiredNormalized.Contains(v))))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

