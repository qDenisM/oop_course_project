using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tesseract;

namespace Web_Accounting_Exams.Services
{
    public class OCRService
    {
        private readonly string _tesseractDataPath;

        public OCRService()
        {
            
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            System.Diagnostics.Debug.WriteLine($"BaseDirectory: {baseDir}");
            
            var possiblePaths = new List<string>
            {
                Path.Combine(baseDir, "tessdata"),
            };

            var projectPaths = new[]
            {
                Path.Combine(baseDir, "..", "..", "..", "..", "tessdata"),
                Path.Combine(baseDir, "..", "..", "..", "tessdata"),
            };

            foreach (var path in projectPaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(path);
                    if (Directory.Exists(normalizedPath))
                    {
                        possiblePaths.Add(normalizedPath);
                    }
                }
                catch
                {
                }
            }

            _tesseractDataPath = null;
            foreach (var path in possiblePaths)
            {
                try
                {
                    var normalizedPath = Path.GetFullPath(path);
                    var rusFile = Path.Combine(normalizedPath, "rus.traineddata");
                    var engFile = Path.Combine(normalizedPath, "eng.traineddata");
                    
                    bool rusExists = File.Exists(rusFile);
                    bool engExists = File.Exists(engFile);
                    
                    System.Diagnostics.Debug.WriteLine($"Проверка пути: {normalizedPath}");
                    System.Diagnostics.Debug.WriteLine($"  rus.traineddata: {rusExists}");
                    System.Diagnostics.Debug.WriteLine($"  eng.traineddata: {engExists}");
                    
                    if (rusExists || engExists)
                    {
                        _tesseractDataPath = normalizedPath;
                        System.Diagnostics.Debug.WriteLine($"✓ Найдена папка tessdata: {_tesseractDataPath}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при проверке пути {path}: {ex.Message}");
                }
            }

            if (_tesseractDataPath == null)
            {
                _tesseractDataPath = Path.Combine(baseDir, "tessdata");
                System.Diagnostics.Debug.WriteLine($"Используется путь по умолчанию: {_tesseractDataPath}");
            
            if (!Directory.Exists(_tesseractDataPath))
            {
                Directory.CreateDirectory(_tesseractDataPath);
                    System.Diagnostics.Debug.WriteLine("Создана папка tessdata в bin");
                }
            }
        }

        public async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Начало OCR для файла: {imagePath}");
                    System.Diagnostics.Debug.WriteLine($"Путь к tessdata: {_tesseractDataPath}");

                    var rusPath = Path.Combine(_tesseractDataPath, "rus.traineddata");
                    var engPath = Path.Combine(_tesseractDataPath, "eng.traineddata");
                    
                    bool rusExists = File.Exists(rusPath);
                    bool engExists = File.Exists(engPath);
                    
                    System.Diagnostics.Debug.WriteLine($"rus.traineddata существует: {rusExists}");
                    System.Diagnostics.Debug.WriteLine($"eng.traineddata существует: {engExists}");

                    if (!rusExists && !engExists)
                    {
                        System.Diagnostics.Debug.WriteLine("Языковые данные Tesseract не найдены. Скачайте их с https://github.com/tesseract-ocr/tessdata");
                        return string.Empty;
                    }

                    using (var engine = new TesseractEngine(_tesseractDataPath, "rus+eng", EngineMode.Default))
                    {
                        System.Diagnostics.Debug.WriteLine("Tesseract engine создан успешно");
                        using (var img = Pix.LoadFromFile(imagePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"Изображение загружено: {img.Width}x{img.Height}");
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                System.Diagnostics.Debug.WriteLine($"OCR завершен. Распознано символов: {text?.Length ?? 0}");
                                return text;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка OCR: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                    }
                    
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var leptonicaDll = Path.Combine(baseDir, "leptonica-1.82.0.dll");
                    var tesseractDll = Path.Combine(baseDir, "tesseract41.dll");
                    
                    if (!File.Exists(leptonicaDll) || !File.Exists(tesseractDll))
                    {
                        System.Diagnostics.Debug.WriteLine($"ВНИМАНИЕ: Нативные библиотеки не найдены в {baseDir}");
                        System.Diagnostics.Debug.WriteLine($"  leptonica-1.82.0.dll: {File.Exists(leptonicaDll)}");
                        System.Diagnostics.Debug.WriteLine($"  tesseract41.dll: {File.Exists(tesseractDll)}");
                        System.Diagnostics.Debug.WriteLine("Пересоберите проект или скопируйте DLL вручную из NuGet пакета.");
                    }
                    
                    return string.Empty;
                }
            });
        }

        public async Task<Dictionary<string, string>> ExtractCertificateDataAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"Файл не найден: {imagePath}");
                return new Dictionary<string, string>();
            }

            var text = await ExtractTextFromImageAsync(imagePath);
            var data = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                System.Diagnostics.Debug.WriteLine("OCR не распознал текст из изображения");
                return data;
            }

            var previewText = text.Length > 500 ? text.Substring(0, 500) + "..." : text;
            System.Diagnostics.Debug.WriteLine($"Распознанный текст (первые 500 символов):\n{previewText}");
            System.Diagnostics.Debug.WriteLine($"Общая длина распознанного текста: {text.Length} символов");

            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            var fullText = text.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            
            var allNumbersInText = System.Text.RegularExpressions.Regex.Matches(fullText, @"\d+");
            System.Diagnostics.Debug.WriteLine($"Всего чисел в тексте: {allNumbersInText.Count}");
            if (allNumbersInText.Count > 0 && allNumbersInText.Count <= 50)
            {
                var numbersList = string.Join(", ", allNumbersInText.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value));
                System.Diagnostics.Debug.WriteLine($"Найденные числа: {numbersList}");
            }
            
            if (fullText.Contains("9") || fullText.Contains("9,") || fullText.Contains("9."))
            {
                System.Diagnostics.Debug.WriteLine("В тексте найдена цифра 9 (возможно, это средний балл)");
            }
            
            var nineMatches = System.Text.RegularExpressions.Regex.Matches(fullText, @"9");
            if (nineMatches.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Найдено вхождений цифры '9' в тексте: {nineMatches.Count}");
            }
            
            System.Diagnostics.Debug.WriteLine($"Всего строк в тексте: {lines.Length}");
            if (lines.Length > 0)
            {
                var lastLines = lines.Skip(Math.Max(0, lines.Length - 10)).Take(10).ToList();
                System.Diagnostics.Debug.WriteLine("Последние 10 строк документа:");
                foreach (var line in lastLines)
                {
                    System.Diagnostics.Debug.WriteLine($"  '{line}'");
                }
            }

            bool isCT = fullText.Contains("централизованное тестирование", StringComparison.OrdinalIgnoreCase) ||
                       (fullText.Contains("тестирование", StringComparison.OrdinalIgnoreCase) && 
                        fullText.Contains("набрал", StringComparison.OrdinalIgnoreCase) &&
                        (fullText.Contains("баллов из 100", StringComparison.OrdinalIgnoreCase) || 
                         System.Text.RegularExpressions.Regex.IsMatch(fullText, @"\d+\s+баллов?\s+из\s+100", System.Text.RegularExpressions.RegexOptions.IgnoreCase)));
            
            bool isDiploma = !isCT && (
                fullText.Contains("аттестат", StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(fullText, @"[Aa][Tt]{1,3}[EeCc][Tt]{1,3}[Aa][Tt]", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                fullText.Contains("об общем среднем образовании", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("средний балл аттестата", StringComparison.OrdinalIgnoreCase) ||
                (fullText.Contains("средний", StringComparison.OrdinalIgnoreCase) && 
                 (fullText.Contains("балл", StringComparison.OrdinalIgnoreCase) || fullText.Contains("бал", StringComparison.OrdinalIgnoreCase))) ||
                fullText.Contains("регистрационный", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("поведение", StringComparison.OrdinalIgnoreCase) ||
                (System.Text.RegularExpressions.Regex.IsMatch(fullText, @"(белорусский|русский|математика|физика|химия|биология|география|история)", System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
                 System.Text.RegularExpressions.Regex.Matches(fullText, @"(белорусский|русский|математика|физика|химия|биология|география|история)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count >= 3)
            );

            if (isDiploma)
            {
                System.Diagnostics.Debug.WriteLine("Определен тип документа: Аттестат");
                data["Type"] = "Diploma";
                
                data["Subject"] = CapitalizeFirstLetter("аттестат");

                var averageScorePatterns = new[]
                {
                    @"средний\s+балл\s+аттестата\s+(\d+[,.]?\d*)",
                    @"средний\s+балл\s+(\d+[,.]?\d*)",
                    @"балл\s+аттестата\s+(\d+[,.]?\d*)",
                    @"аттестата\s+(\d+[,.]?\d*)",
                    @"Средний\s+балл\s+аттестата\s+(\d+[,.]?\d*)",
                    @"Средний\s+балл\s+(\d+[,.]?\d*)",
                    @"[Сс]редний\s+[Бб]алл?\s+[Аа]ттестата?\s+(\d+[,.]?\d*)",
                    @"[Бб]алл?\s+[Аа]ттестата?\s+(\d+[,.]?\d*)",
                    @"[Сс]редний\s+[Бб]алл?\s+(\d+[,.]?\d*)",
                    @"[Бб]алл?\s+(\d+[,.]?\d*)",
                    @"(\d[,.]?\d)\s*(?=.*(?:балл|аттестат|поведение|регистрационный))",
                    @"([3-9][,.]0|10[,.]0)",
                    @"([3-9][,.]\d|10[,.]0)",
                    @"(?:средний|балл|аттестата|бал).{0,50}?([3-9][,.]?\d*|10[,.]?\d*)",
                    @"([3-9][,.]?\d*|10[,.]?\d*).{0,50}?(?:средний|балл|аттестата|бал)"
                };

                double averageScore = 0;
                bool scoreFound = false;
                
                foreach (var pattern in averageScorePatterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        fullText,
                        pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var scoreStr = match.Groups[1].Value.Replace(",", ".");
                        if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out averageScore))
                        {
                            if (averageScore >= 3.0 && averageScore <= 10.0)
                            {
                                int calculatedScore = (int)Math.Round(averageScore * 10);
                                data["Score"] = calculatedScore.ToString();
                                System.Diagnostics.Debug.WriteLine($"Найден средний балл аттестата: {averageScore}, вычисленный балл: {calculatedScore}");
                                scoreFound = true;
                                break;
                            }
                        }
                    }
                    if (scoreFound) break;
                }
                
                if (!scoreFound)
                {
                    System.Diagnostics.Debug.WriteLine("Поиск среднего балла альтернативными методами...");
                    
                    var numberMatches = System.Text.RegularExpressions.Regex.Matches(
                        fullText,
                        @"(\d+[,.]\d+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    System.Diagnostics.Debug.WriteLine($"Найдено чисел с десятичной частью: {numberMatches.Count}");
                    
                    var candidateScores = new List<double>();
                    
                    foreach (System.Text.RegularExpressions.Match match in numberMatches)
                    {
                        var scoreStr = match.Groups[1].Value.Replace(",", ".");
                        System.Diagnostics.Debug.WriteLine($"  Проверка числа: {scoreStr}");
                        if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out averageScore))
                        {
                            System.Diagnostics.Debug.WriteLine($"    Распарсено как: {averageScore}");
                            if (averageScore >= 3.0 && averageScore <= 10.0 && averageScore < 20.0)
                            {
                                candidateScores.Add(averageScore);
                                System.Diagnostics.Debug.WriteLine($"    ✓ Подходит как средний балл: {averageScore}");
                            }
                        }
                    }
                    
                    if (candidateScores.Count > 0)
                    {
                        averageScore = candidateScores[0];
                        int calculatedScore = (int)Math.Round(averageScore * 10);
                        data["Score"] = calculatedScore.ToString();
                        System.Diagnostics.Debug.WriteLine($"Найден средний балл аттестата (альтернативный поиск): {averageScore}, вычисленный балл: {calculatedScore}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Числа с десятичной частью не найдены, ищем целые числа...");
                        
                        var wholeNumberMatches = System.Text.RegularExpressions.Regex.Matches(
                            fullText,
                            @"\b([3-9]|10)\b",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        System.Diagnostics.Debug.WriteLine($"Найдено целых чисел от 3 до 10: {wholeNumberMatches.Count}");
                        
                        if (wholeNumberMatches.Count > 0)
                        {
                            foreach (System.Text.RegularExpressions.Match match in wholeNumberMatches)
                            {
                                if (int.TryParse(match.Groups[1].Value, out int wholeNumber) && wholeNumber >= 3 && wholeNumber <= 10)
                                {
                                    int calculatedScore = wholeNumber * 10;
                                    data["Score"] = calculatedScore.ToString();
                                    System.Diagnostics.Debug.WriteLine($"Найден средний балл аттестата (целое число): {wholeNumber}, вычисленный балл: {calculatedScore}");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Поиск числа в контексте ключевых слов...");
                            
                            bool foundInContext = false;
                            
                            var contextPatterns = new[]
                            {
                                @"(?:средний|балл|аттестата|бал|поведение|регистрационный).{0,30}?([3-9][,.]?\d*|10[,.]?\d*)",
                                @"([3-9][,.]?\d*|10[,.]?\d*).{0,30}?(?:средний|балл|аттестата|бал|поведение|регистрационный)"
                            };
                            
                            if (lines.Length > 0)
                            {
                                var lastLinesText = string.Join(" ", lines.Skip(Math.Max(0, lines.Length - 5)));
                                System.Diagnostics.Debug.WriteLine($"Поиск в последних строках: '{lastLinesText}'");
                                
                                var lastLinesNumbers = System.Text.RegularExpressions.Regex.Matches(
                                    lastLinesText,
                                    @"([3-9][,.]?\d*|10[,.]?\d*)",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                
                                System.Diagnostics.Debug.WriteLine($"Найдено чисел в последних строках: {lastLinesNumbers.Count}");
                                
                                foreach (System.Text.RegularExpressions.Match match in lastLinesNumbers)
                                {
                                    var scoreStr = match.Groups[1].Value.Replace(",", ".");
                                    System.Diagnostics.Debug.WriteLine($"  Проверка числа из последних строк: {scoreStr}");
                                    
                                    if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out averageScore))
                                    {
                                        if (averageScore >= 3.0 && averageScore <= 10.0)
                                        {
                                            int calculatedScore = (int)Math.Round(averageScore * 10);
                                            data["Score"] = calculatedScore.ToString();
                                            System.Diagnostics.Debug.WriteLine($"    ✓ Найден средний балл в последних строках: {averageScore}, вычисленный балл: {calculatedScore}");
                                            foundInContext = true;
                                            break;
                                        }
                                    }
                                    else if (int.TryParse(scoreStr, out int wholeNumber) && wholeNumber >= 3 && wholeNumber <= 10)
                                    {
                                        int calculatedScore = wholeNumber * 10;
                                        data["Score"] = calculatedScore.ToString();
                                        System.Diagnostics.Debug.WriteLine($"    ✓ Найден средний балл в последних строках (целое): {wholeNumber}, вычисленный балл: {calculatedScore}");
                                        foundInContext = true;
                                        break;
                                    }
                                }
                            }
                            
                            foreach (var pattern in contextPatterns)
                            {
                                var contextMatches = System.Text.RegularExpressions.Regex.Matches(
                                    fullText,
                                    pattern,
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                
                                System.Diagnostics.Debug.WriteLine($"  Паттерн '{pattern}': найдено {contextMatches.Count} совпадений");
                                
                                foreach (System.Text.RegularExpressions.Match match in contextMatches)
                                {
                                    var scoreStr = match.Groups[1].Value.Replace(",", ".");
                                    System.Diagnostics.Debug.WriteLine($"    Проверка: {scoreStr}");
                                    
                                    if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out averageScore))
                                    {
                                        if (averageScore >= 3.0 && averageScore <= 10.0)
                                        {
                                            int calculatedScore = (int)Math.Round(averageScore * 10);
                                            data["Score"] = calculatedScore.ToString();
                                            System.Diagnostics.Debug.WriteLine($"    ✓ Найден средний балл в контексте: {averageScore}, вычисленный балл: {calculatedScore}");
                                            foundInContext = true;
                                            break;
                                        }
                                    }
                                    else if (int.TryParse(scoreStr, out int wholeNumber) && wholeNumber >= 3 && wholeNumber <= 10)
                                    {
                                        int calculatedScore = wholeNumber * 10;
                                        data["Score"] = calculatedScore.ToString();
                                        System.Diagnostics.Debug.WriteLine($"    ✓ Найден средний балл в контексте (целое): {wholeNumber}, вычисленный балл: {calculatedScore}");
                                        foundInContext = true;
                                        break;
                                    }
                                }
                                if (foundInContext) break;
                            }
                            
                            if (!foundInContext)
                            {
                                System.Diagnostics.Debug.WriteLine("Последняя попытка: поиск любых чисел от 3 до 10...");
                                
                                if (lines.Length > 0)
                                {
                                    var lastLinesText = string.Join(" ", lines.Skip(Math.Max(0, lines.Length - 3)));
                                    System.Diagnostics.Debug.WriteLine($"Поиск в последних 3 строках: '{lastLinesText}'");
                                    
                                    var lastLinesNumbers = System.Text.RegularExpressions.Regex.Matches(
                                        lastLinesText,
                                        @"\b([3-9]|10)\b",
                                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    
                                    System.Diagnostics.Debug.WriteLine($"Найдено чисел 3-10 в последних строках: {lastLinesNumbers.Count}");
                                    
                                    if (lastLinesNumbers.Count > 0)
                                    {
                                        int bestNumber = 0;
                                        foreach (System.Text.RegularExpressions.Match match in lastLinesNumbers)
                                        {
                                            if (int.TryParse(match.Groups[1].Value, out int num) && num >= 3 && num <= 10)
                                            {
                                                if (num >= 8 && num <= 10)
                                                {
                                                    bestNumber = num;
                                                    break;
                                                }
                                                else if (bestNumber == 0)
                                                {
                                                    bestNumber = num;
                                                }
                                            }
                                        }
                                        
                                        if (bestNumber > 0)
                                        {
                                            int calculatedScore = bestNumber * 10;
                                            data["Score"] = calculatedScore.ToString();
                                            System.Diagnostics.Debug.WriteLine($"Найден средний балл в последних строках (последняя попытка): {bestNumber}, вычисленный балл: {calculatedScore}");
                                            foundInContext = true;
                                        }
                                    }
                                }
                                
                                if (!foundInContext)
                                {
                                    var allNumbers = System.Text.RegularExpressions.Regex.Matches(
                                        fullText,
                                        @"\b([3-9]|10)\b",
                                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    
                                    System.Diagnostics.Debug.WriteLine($"Найдено всех чисел 3-10 во всем тексте: {allNumbers.Count}");
                                    
                                    if (allNumbers.Count > 0)
                                    {
                                        int bestNumber = 0;
                                        foreach (System.Text.RegularExpressions.Match match in allNumbers)
                                        {
                                            if (int.TryParse(match.Groups[1].Value, out int num) && num >= 3 && num <= 10)
                                            {
                                                if (num >= 8 && num <= 10)
                                                {
                                                    bestNumber = num;
                                                    break;
                                                }
                                                else if (bestNumber == 0)
                                                {
                                                    bestNumber = num;
                                                }
                                            }
                                        }
                                        
                                        if (bestNumber > 0)
                                        {
                                            int calculatedScore = bestNumber * 10;
                                            data["Score"] = calculatedScore.ToString();
                                            System.Diagnostics.Debug.WriteLine($"Найден средний балл аттестата (последняя попытка): {bestNumber}, вычисленный балл: {calculatedScore}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Поиск цифры 9 в любом контексте...");
                                        
                                        var ninePatterns = new[]
                                        {
                                            @"9[,.]0",
                                            @"9[,.]\d",
                                            @"\b9\b",
                                            @"9[^0-9]"
                                        };
                                        
                                        bool foundNine = false;
                                        foreach (var pattern in ninePatterns)
                                        {
                                            var patternMatches = System.Text.RegularExpressions.Regex.Matches(fullText, pattern);
                                            if (patternMatches.Count > 0)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"  Найдено совпадений по паттерну '{pattern}': {patternMatches.Count}");
                                                
                                                int calculatedScore = 90;
                                                data["Score"] = calculatedScore.ToString();
                                                System.Diagnostics.Debug.WriteLine($"  ✓ Предполагаемый средний балл: 9.0, вычисленный балл: {calculatedScore}");
                                                foundNine = true;
                                                break;
                                            }
                                        }
                                        
                                        if (!foundNine)
                                        {
                                            System.Diagnostics.Debug.WriteLine("ВНИМАНИЕ: Средний балл не найден в тексте. Возможно, он перекрыт штампом.");
                                            System.Diagnostics.Debug.WriteLine("Рекомендуется ввести средний балл вручную в диалоге.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var diplomaNumberPatterns = new[]
                {
                    @"[АA]\s*№\s*(\d{6,8})",
                    @"[АA]\s*[N№]\s*(\d{6,8})",
                    @"Регистрационный\s*№\s*(\d+)",
                    @"Регистрационный\s*[N№]\s*(\d+)",
                    @"№\s*(\d{6,8})",
                    @"[N№]\s*(\d{6,8})",
                    @"[АA]\s*(\d{7})",
                    @"\b(\d{7,8})\b"
                };

                foreach (var pattern in diplomaNumberPatterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        fullText,
                        pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var number = match.Groups[1].Value;
                        data["CertificateNumber"] = number;
                        System.Diagnostics.Debug.WriteLine($"Найден номер аттестата: {number}");
                        break;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Определен тип документа: ЦТ");
                data["Type"] = "CT";
            }

            if (!isDiploma || !data.ContainsKey("CertificateNumber"))
            {
            foreach (var line in lines)
            {
                    if (line.Contains("СЕРТИФИКАТ", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("CEPTUPHUKAT", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("сертификат", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"Поиск номера в строке с СЕРТИФИКАТ: '{line}'");
                        
                        var certNumberMatch = System.Text.RegularExpressions.Regex.Match(
                            line,
                            @"[N№]\s*[—\-]\s*(\d{6,8})",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (!certNumberMatch.Success)
                        {
                            certNumberMatch = System.Text.RegularExpressions.Regex.Match(
                                line,
                                @"[N№]\s*(\d{6,8})",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        }
                        
                        if (certNumberMatch.Success)
                        {
                            var number = certNumberMatch.Groups[1].Value;
                            if (number.Length >= 6 && number.Length <= 8)
                            {
                                data["CertificateNumber"] = number;
                                System.Diagnostics.Debug.WriteLine($"Найден номер сертификата в строке с СЕРТИФИКАТ: {number}");
                                break;
                            }
                        }
                    }
                }
                
                if (!data.ContainsKey("CertificateNumber"))
                {
                    var certificateNumberPatterns = new[]
                    {
                        @"[N№]\s*[—\-]\s*(\d{6,8})",
                        @"№\s*[—\-]?\s*(\d{6,8})",
                        @"№\s*(\d{6,8})",
                        @"No\s*[—\-]?\s*(\d{6,8})",
                        @"N\s*[—\-]?\s*(\d{6,8})",
                        @"(\d{7})\s*",
                    };

                    foreach (var pattern in certificateNumberPatterns)
                    {
                        var certificateNumberMatch = System.Text.RegularExpressions.Regex.Match(
                            fullText, 
                            pattern, 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (certificateNumberMatch.Success)
                        {
                            var number = certificateNumberMatch.Groups[1].Value;
                            if (number.Length >= 6 && number.Length <= 8)
                            {
                                data["CertificateNumber"] = number;
                                System.Diagnostics.Debug.WriteLine($"Найден номер сертификата: {number}");
                                break;
                            }
                        }
                    }
                }
            }

            if (!isDiploma || !data.ContainsKey("Score"))
            {
                var scorePatterns = new[]
                {
                    @"набрал[а]?\s*[—\-]?\s*(\d{1,3})\s*\([^)]+\)",
                    @"набрал[а]?\s+(\d{1,3})\s*\([^)]+\)\s*баллов",
                    @"набрал[а]?\s*[—\-]?\s*(\d{1,3})\s+баллов",
                    @"набрал[а]?\s+(\d{1,3})\s+баллов",
                    @"(\d{1,3})\s*\([^)]+\)\s*баллов\s+из\s+100",
                    @"(\d{1,3})\s*\([^)]+\)\s*баллов",
                    @"(\d{1,3})\s+баллов\s+из\s+100\s*\([^)]+\)",
                    @"(\d{1,3})\s+балл[а-я]*\s+из\s+100\s*\([^)]+\)"
                };

                foreach (var pattern in scorePatterns)
                {
                    var scoreMatches = System.Text.RegularExpressions.Regex.Matches(
                        fullText, 
                        pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    System.Diagnostics.Debug.WriteLine($"Паттерн '{pattern}': найдено {scoreMatches.Count} совпадений");
                    
                    foreach (System.Text.RegularExpressions.Match scoreMatch in scoreMatches)
                    {
                        var scoreValue = scoreMatch.Groups[1].Value;
                        System.Diagnostics.Debug.WriteLine($"  Проверка найденного балла: {scoreValue}");
                        if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100)
                        {
                            if (score == 100 && pattern.Contains(@"из\s+100"))
                            {
                                System.Diagnostics.Debug.WriteLine($"  Пропущено: это 100 из 'из 100', а не балл");
                                continue;
                            }
                            
                            data["Score"] = scoreValue;
                            System.Diagnostics.Debug.WriteLine($"  ✓ Найден балл: {scoreValue}");
                            break;
                        }
                    }
                    if (data.ContainsKey("Score")) break;
                }

                if (!data.ContainsKey("Score"))
                {
                    System.Diagnostics.Debug.WriteLine("Поиск балла альтернативными методами...");
                    
                    var scoreWithTextBeforeBallMatch = System.Text.RegularExpressions.Regex.Match(
                        fullText,
                        @"(\d{1,3})\s*\([^)]+\)\s*баллов?\s+из\s+100",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (scoreWithTextBeforeBallMatch.Success)
                    {
                        var scoreValue = scoreWithTextBeforeBallMatch.Groups[1].Value;
                        if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100)
                        {
                            data["Score"] = scoreValue;
                            System.Diagnostics.Debug.WriteLine($"✓ Найден балл (с текстовым написанием перед 'баллов из 100'): {scoreValue}");
                        }
                    }
                    
                    if (!data.ContainsKey("Score"))
                    {
                        System.Diagnostics.Debug.WriteLine("Поиск числа с текстовым написанием в скобках...");
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            
                            var scoreAfterNabralMatch = System.Text.RegularExpressions.Regex.Match(
                                line,
                                @"набрал[а]?\s*[—\-]?\s*(\d{1,3})\s*\([^)]+\)",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            if (scoreAfterNabralMatch.Success)
                            {
                                var scoreValue = scoreAfterNabralMatch.Groups[1].Value;
                                System.Diagnostics.Debug.WriteLine($"  Найдено число после 'набрал' в строке {i}: '{line}' -> {scoreValue}");
                                
                                if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100)
                                {
                                    data["Score"] = scoreValue;
                                    System.Diagnostics.Debug.WriteLine($"  ✓ Найден балл после 'набрал': {scoreValue}");
                                    break;
                                }
                            }
                            
                            var scoreWithTextMatch = System.Text.RegularExpressions.Regex.Match(
                                line,
                                @"(\d{1,3})\s*\([^)]+\)",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            if (scoreWithTextMatch.Success)
                            {
                                var scoreValue = scoreWithTextMatch.Groups[1].Value;
                                System.Diagnostics.Debug.WriteLine($"  Найдено число с текстом в строке {i}: '{line}' -> {scoreValue}");
                                
                                if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100 && score > 20 && score < 100)
                                {
                                    var contextLines = new List<string>();
                                    for (int j = Math.Max(0, i - 2); j <= Math.Min(lines.Length - 1, i + 3); j++)
                                    {
                                        contextLines.Add(lines[j]);
                                    }
                                    var contextText = string.Join(" ", contextLines);
                                    
                                    System.Diagnostics.Debug.WriteLine($"  Контекст (строки {Math.Max(0, i - 2)}-{Math.Min(lines.Length - 1, i + 3)}): '{contextText}'");
                                    
                                    if ((contextText.Contains("балл", StringComparison.OrdinalIgnoreCase) || 
                                         contextText.Contains("баллов", StringComparison.OrdinalIgnoreCase) ||
                                         contextText.Contains("из 100", StringComparison.OrdinalIgnoreCase) ||
                                         System.Text.RegularExpressions.Regex.IsMatch(contextText, @"из\s+100", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) &&
                                        score != 100)
                                    {
                                        data["Score"] = scoreValue;
                                        System.Diagnostics.Debug.WriteLine($"  ✓ Найден балл (с текстовым написанием в контексте строк): {scoreValue}");
                                        break;
                                    }
                                }
                            }
                        }
                        
                        if (!data.ContainsKey("Score"))
                        {
                            var scoreWithTextMatches = System.Text.RegularExpressions.Regex.Matches(
                                fullText,
                                @"(\d{1,3})\s*\([^)]+\)",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            System.Diagnostics.Debug.WriteLine($"Найдено чисел с текстовым написанием во всем тексте: {scoreWithTextMatches.Count}");
                            
                            foreach (System.Text.RegularExpressions.Match scoreWithTextMatch in scoreWithTextMatches)
                            {
                                var scoreValue = scoreWithTextMatch.Groups[1].Value;
                                System.Diagnostics.Debug.WriteLine($"  Проверка: {scoreValue}");
                                
                                if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100 && score > 20)
                                {
                                    var startIndex = Math.Max(0, scoreWithTextMatch.Index - 100);
                                    var length = Math.Min(200, fullText.Length - startIndex);
                                    var context = fullText.Substring(startIndex, length);
                                    
                                    System.Diagnostics.Debug.WriteLine($"  Контекст: '{context}'");
                                    
                                    if (context.Contains("балл", StringComparison.OrdinalIgnoreCase) || 
                                        context.Contains("баллов", StringComparison.OrdinalIgnoreCase) ||
                                        context.Contains("из 100", StringComparison.OrdinalIgnoreCase) ||
                                        System.Text.RegularExpressions.Regex.IsMatch(context, @"из\s+100", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                    {
                                        data["Score"] = scoreValue;
                                        System.Diagnostics.Debug.WriteLine($"  ✓ Найден балл (с текстовым написанием в контексте 'балл' или 'из 100'): {scoreValue}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (!data.ContainsKey("Score"))
                    {
                        var scoreNearBallMatch = System.Text.RegularExpressions.Regex.Match(
                            fullText,
                            @"(\d{1,3})\s*балл",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (scoreNearBallMatch.Success)
                        {
                            var scoreValue = scoreNearBallMatch.Groups[1].Value;
                            if (int.TryParse(scoreValue, out int score) && score >= 0 && score <= 100)
                            {
                                data["Score"] = scoreValue;
                                System.Diagnostics.Debug.WriteLine($"Найден балл (альтернативный паттерн): {scoreValue}");
                            }
                        }
                    }
                }
            }

            if (!isDiploma || !data.ContainsKey("Subject"))
            {
                System.Diagnostics.Debug.WriteLine("Поиск предмета для ЦТ...");
                
                bool subjectFound = false;
                for (int i = 0; i < lines.Length && !subjectFound; i++)
                {
                    var line = lines[i].Trim();
                    
                    var lineSubjectPatterns = new[]
                    {
                        @"тестирование\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"централизованное\s+тестирование\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"участие\s+в\s+[0-9]+\s+году\s+в\s+централизованном\s+экзамене\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"\bпо\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d|$)"
                    };
                    
                    foreach (var pattern in lineSubjectPatterns)
                    {
                        var subjectMatch = System.Text.RegularExpressions.Regex.Match(
                            line,
                            pattern,
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (subjectMatch.Success)
                        {
                            var subject = subjectMatch.Groups[1].Value.Trim();
                            System.Diagnostics.Debug.WriteLine($"  Найдено в строке {i} '{line}': '{subject}'");
                            
                            subject = System.Text.RegularExpressions.Regex.Replace(
                                subject, 
                                @"\s+(и|и\s+набрал|набрал|баллов?|\d+).*$", 
                                "", 
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            subject = System.Text.RegularExpressions.Regex.Replace(
                                subject, 
                                @"[,\\.\\-]+$", 
                                "");
                            subject = subject.Trim();
                            
                            if (!string.IsNullOrWhiteSpace(subject) && 
                                subject.Length < 50 && 
                                subject.Length > 2 &&
                                !subject.Contains("баллов", StringComparison.OrdinalIgnoreCase) &&
                                !subject.Contains("набрал", StringComparison.OrdinalIgnoreCase))
                            {
                                subject = NormalizeSubjectName(subject);
                                data["Subject"] = subject;
                                System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (из строки): {subject}");
                                subjectFound = true;
                                break;
                            }
                        }
                    }
                }
                
                if (!subjectFound)
                {
                    var subjectPatterns = new[]
                    {
                        @"тестирование\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"централизованное\s+тестирование\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"участие\s+в\s+[0-9]+\s+году\s+в\s+централизованном\s+экзамене\s+по\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d)",
                        @"\bпо\s+([а-яёА-ЯЁ]+(?:\s+[а-яёА-ЯЁ]+)?)(?:\s+и\s+набрал|\s+набрал|\s+баллов|$|\s+\d|$)"
                    };

                    foreach (var pattern in subjectPatterns)
                    {
                        var subjectMatch = System.Text.RegularExpressions.Regex.Match(
                            fullText,
                            pattern,
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (subjectMatch.Success)
                        {
                            var subject = subjectMatch.Groups[1].Value.Trim();
                            System.Diagnostics.Debug.WriteLine($"  Найдено во всем тексте: '{subject}'");
                            
                            subject = System.Text.RegularExpressions.Regex.Replace(
                                subject, 
                                @"\s+(и|и\s+набрал|набрал|баллов?|\d+).*$", 
                                "", 
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            subject = System.Text.RegularExpressions.Regex.Replace(
                                subject, 
                                @"[,\\.\\-]+$", 
                                "");
                            subject = subject.Trim();
                            
                            if (!string.IsNullOrWhiteSpace(subject) && 
                                subject.Length < 50 && 
                                subject.Length > 2 &&
                                !subject.Contains("баллов", StringComparison.OrdinalIgnoreCase) &&
                                !subject.Contains("набрал", StringComparison.OrdinalIgnoreCase))
                            {
                                subject = NormalizeSubjectName(subject);
                                data["Subject"] = subject;
                                System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (из всего текста): {subject}");
                                subjectFound = true;
                                break;
                            }
                        }
                    }
                }

                if (!subjectFound)
                {
                    System.Diagnostics.Debug.WriteLine("Поиск предмета по ключевым словам...");
                    
                    if (fullText.Contains("русск", StringComparison.OrdinalIgnoreCase) && 
                        (fullText.Contains("язык", StringComparison.OrdinalIgnoreCase) || 
                         fullText.Contains("языку", StringComparison.OrdinalIgnoreCase) ||
                         fullText.Contains("языка", StringComparison.OrdinalIgnoreCase)))
                    {
                        data["Subject"] = CapitalizeFirstLetter("русский язык");
                        System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (ключевые слова): {data["Subject"]}");
                        subjectFound = true;
                    }
                    else if (fullText.Contains("английск", StringComparison.OrdinalIgnoreCase) && 
                             (fullText.Contains("язык", StringComparison.OrdinalIgnoreCase) || 
                              fullText.Contains("языку", StringComparison.OrdinalIgnoreCase) ||
                              fullText.Contains("языка", StringComparison.OrdinalIgnoreCase)))
                    {
                        data["Subject"] = CapitalizeFirstLetter("английский язык");
                        System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (ключевые слова): {data["Subject"]}");
                        subjectFound = true;
                    }
                    else if (fullText.Contains("математик", StringComparison.OrdinalIgnoreCase) || 
                             fullText.Contains("математике", StringComparison.OrdinalIgnoreCase) ||
                             fullText.Contains("математику", StringComparison.OrdinalIgnoreCase))
                    {
                        data["Subject"] = CapitalizeFirstLetter("математика");
                        System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (ключевые слова): {data["Subject"]}");
                        subjectFound = true;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(fullText, @"истори.{0,30}беларус|беларус.{0,30}истори", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        data["Subject"] = CapitalizeFirstLetter("история Беларуси");
                        System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (ключевые слова): {data["Subject"]}");
                        subjectFound = true;
                    }
                    else
                    {
                        var knownSubjects = new Dictionary<string, string[]>
                        {
                            { "физика", new[] { "физик" } },
                            { "химия", new[] { "хими" } },
                            { "биология", new[] { "биолог" } },
                            { "обществоведение", new[] { "обществовед" } },
                            { "география", new[] { "географ" } }
                        };

                        foreach (var subject in knownSubjects)
                        {
                            foreach (var keyword in subject.Value)
                            {
                                if (fullText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                                {
                                    data["Subject"] = CapitalizeFirstLetter(subject.Key);
                                    System.Diagnostics.Debug.WriteLine($"✓ Найден предмет (ключевые слова): {data["Subject"]}");
                                    subjectFound = true;
                                    break;
                                }
                            }
                            if (subjectFound) break;
                        }
                    }
                }
            }

            return data;
        }

        private string NormalizeSubjectName(string subject)
        {
            subject = System.Text.RegularExpressions.Regex.Replace(subject, @"\s+", " ").Trim();
            
            subject = System.Text.RegularExpressions.Regex.Replace(subject, @"^[,\\.\\-\\s]+|[,\\.\\-\\s]+$", "");
            subject = subject.Trim();
            
            var subjectLower = subject.ToLower();
            
            if (subjectLower.Contains("английск") && (subjectLower.Contains("язык") || subjectLower.Contains("языку") || subjectLower.Contains("языка")))
                return CapitalizeFirstLetter("английский язык");
            if (subjectLower.Contains("русск") && (subjectLower.Contains("язык") || subjectLower.Contains("языку") || subjectLower.Contains("языка")))
                return CapitalizeFirstLetter("русский язык");
            if (subjectLower.Contains("белорусск") && (subjectLower.Contains("язык") || subjectLower.Contains("языку") || subjectLower.Contains("языка")))
                return CapitalizeFirstLetter("белорусский язык");
            
            if (subjectLower.Contains("истори") && subjectLower.Contains("беларус"))
                return CapitalizeFirstLetter("история Беларуси");
            
            if (subjectLower.Contains("математик") || subjectLower.Contains("математике") || subjectLower.Contains("математику"))
                return CapitalizeFirstLetter("математика");
            if (subjectLower.Contains("физик") || subjectLower.Contains("физике") || subjectLower.Contains("физику"))
                return CapitalizeFirstLetter("физика");
            if (subjectLower.Contains("хими") || subjectLower.Contains("химии") || subjectLower.Contains("химию"))
                return CapitalizeFirstLetter("химия");
            if (subjectLower.Contains("биолог") || subjectLower.Contains("биологии") || subjectLower.Contains("биологию"))
                return CapitalizeFirstLetter("биология");
            if (subjectLower.Contains("обществовед") || subjectLower.Contains("обществоведению"))
                return CapitalizeFirstLetter("обществоведение");
            if (subjectLower.Contains("географ") || subjectLower.Contains("географии") || subjectLower.Contains("географию"))
                return CapitalizeFirstLetter("география");

            return CapitalizeFirstLetter(subject);
        }

        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0)
                    result.Append(" ");
                
                var word = words[i];
                if (word.Length > 0)
                {
                    result.Append(char.ToUpper(word[0]));
                    if (word.Length > 1)
                        result.Append(word.Substring(1).ToLower());
                }
            }
            
            return result.ToString();
        }
    }
}

